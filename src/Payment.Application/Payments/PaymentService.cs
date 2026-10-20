using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Application.Payments;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentProviderResolver providerResolver,
    IMerchantContext merchantContext,
    ICurrencyRules currencyRules,
    IReturnUrlPolicy returnUrlPolicy,
    TimeProvider clock
)
{
    /// <summary>
    /// Validates, idempotently creates, and submits a payment to its configured provider.
    /// </summary>
    public async Task<CreatePaymentResult> CreateAsync(
        CreatePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        // Step 1: Validate the requested currency and construct a correctly scaled money value.
        if (!currencyRules.TryGetDecimalPlaces(command.Currency, out var decimalPlaces))
            throw new PaymentValidationException(
                "unsupported_currency",
                "The requested currency is not supported."
            );

        Money amount;
        try
        {
            amount = Money.Create(command.Amount, command.Currency, decimalPlaces);
        }
        catch (ArgumentException exception)
        {
            throw new PaymentValidationException("invalid_amount", exception.Message);
        }

        // Step 2: Validate idempotency and ensure both browser return URLs are safe and allowed.
        var idempotencyKey = Idempotency.ValidateKey(command.IdempotencyKey);
        if (
            !Uri.TryCreate(command.ReturnUrl, UriKind.Absolute, out var returnUrl)
            || returnUrl.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(returnUrl.UserInfo)
            || !string.IsNullOrEmpty(returnUrl.Fragment)
        )
            throw new PaymentValidationException(
                "invalid_return_url",
                "The return URL must be an absolute HTTPS URL without user information or a fragment."
            );
        if (!returnUrlPolicy.IsAllowed(returnUrl))
            throw new PaymentValidationException(
                "invalid_return_url",
                "The return URL origin is not allowed."
            );
        if (
            !Uri.TryCreate(command.CancelUrl, UriKind.Absolute, out var cancelUrl)
            || cancelUrl.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(cancelUrl.UserInfo)
            || !string.IsNullOrEmpty(cancelUrl.Fragment)
            || !returnUrlPolicy.IsAllowed(cancelUrl)
        )
            throw new PaymentValidationException(
                "invalid_cancel_url",
                "The cancel URL must be an allowed absolute HTTPS URL without user information or a fragment."
            );
        // Step 3: Resolve the configured provider for the requested payment method.
        IPaymentProvider provider;
        try
        {
            provider = providerResolver.ResolveForNewPayment(command.Method);
        }
        catch (InvalidOperationException)
        {
            throw new PaymentValidationException(
                "unsupported_payment_method",
                "The requested payment method is not configured."
            );
        }
        // Step 4: Create the local payment in Processing before contacting the provider.
        var now = clock.GetUtcNow();
        DomainPayment payment;
        try
        {
            payment = DomainPayment.Create(
                merchantContext.MerchantId,
                command.MerchantReference,
                amount,
                command.Method,
                provider.ProviderKey,
                now
            );
        }
        catch (ArgumentException exception)
        {
            throw new PaymentValidationException("invalid_request", exception.Message);
        }

        payment.ApplyOutcome(PaymentStatus.Processing, null, now);
        // Step 5: Persist or replay the idempotent create-payment operation.
        var operation = IdempotencyOperation.Create(
            merchantContext.MerchantId,
            Idempotency.CreatePaymentOperation,
            idempotencyKey,
            Idempotency.FingerprintVersion,
            Idempotency.CreateFingerprint(
                merchantContext.MerchantId,
                command.MerchantReference,
                command.Amount,
                command.Currency,
                command.Method,
                returnUrl.AbsoluteUri,
                cancelUrl.AbsoluteUri
            ),
            payment.Id,
            now
        );
        var claim = await repository.CreateOrGetAsync(payment, operation, cancellationToken);
        if (claim.Kind == IdempotentCreateKind.FingerprintConflict)
            throw new PaymentConflictException(
                "idempotency_key_reused",
                "The Idempotency-Key has already been used with a different payment request."
            );
            
        if (claim.Kind == IdempotentCreateKind.Replay)
        {
            var existing = claim.Payment!;
            return new CreatePaymentResult(
                ToResponse(existing, null),
                true,
                existing.Status is PaymentStatus.Pending or PaymentStatus.Processing
            );
        }

        // Step 6: Create the provider payment using minor currency units.
        ProviderPaymentOutcome outcome;
        try
        {
            var scale =
                decimalPlaces == 0
                    ? 1m
                    : decimal.Parse(
                        "1" + new string('0', decimalPlaces),
                        System.Globalization.CultureInfo.InvariantCulture
                    );
            outcome = await provider.CreateAsync(
                new ProviderPaymentRequest(
                    payment.Id,
                    command.MerchantReference,
                    amount,
                    checked((long)(amount.Value * scale)),
                    command.Method,
                    returnUrl.AbsoluteUri,
                    cancelUrl.AbsoluteUri,
                    command.SimulatedOutcome
                ),
                cancellationToken
            );
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            outcome = new ProviderPaymentOutcome(PaymentStatus.RequiresReconciliation, null);
        }
        // Step 7: Persist the provider outcome and return any required client next action.
        payment.ApplyOutcome(
            outcome.Status,
            outcome.ProviderReference,
            clock.GetUtcNow(),
            outcome.ProviderPaymentReference
        );
        await repository.UpdateAsync(payment, cancellationToken);
        return new CreatePaymentResult(ToResponse(payment, outcome.NextAction), false, false);
    }

    /// <summary>
    /// Retrieves one payment belonging to the current merchant.
    /// </summary>
    public async Task<PaymentResponse> GetAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        // Step 1: Load the payment within the current merchant boundary.
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        // Step 2: Map the domain entity to the provider-neutral API response.
        return ToResponse(payment, null);
    }

    /// <summary>
    /// Idempotently captures an approved payment when its persisted provider supports capture.
    /// </summary>
    public async Task<PaymentResponse> CaptureAsync(
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        // Step 1: Load the payment and ensure it is an eligible provider capture.
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        if (
            string.IsNullOrWhiteSpace(payment.ProviderReference)
            || payment.Status is not (PaymentStatus.RequiresAction or PaymentStatus.Processing)
        )
            throw new PaymentValidationException(
                "capture_not_supported",
                "This payment cannot be captured."
            );
        // Step 2: Claim an idempotent capture operation or return its previous result.
        var key = Idempotency.ValidateKey(idempotencyKey);
        var fingerprint = Idempotency.CreateFingerprint(
            merchantContext.MerchantId,
            payment.Id.ToString(),
            0m,
            "",
            PaymentMethod.PayPal,
            "capture",
            "capture"
        );
        var claim = await repository.ClaimOperationAsync(
            IdempotencyOperation.Create(
                merchantContext.MerchantId,
                Idempotency.CapturePaymentOperation,
                key,
                Idempotency.FingerprintVersion,
                fingerprint,
                payment.Id,
                clock.GetUtcNow()
            ),
            cancellationToken
        );
        if (claim.Kind == IdempotentCreateKind.FingerprintConflict)
            throw new PaymentConflictException(
                "idempotency_key_reused",
                "The Idempotency-Key has already been used with a different capture request."
            );
        if (claim.Kind == IdempotentCreateKind.Replay)
            return ToResponse(payment, null);
        // Step 3: Capture through the persisted provider and use reconciliation when uncertain.
        var provider = providerResolver.ResolveByKey(payment.Provider);
        if (provider is not ICaptureProvider captureProvider)
            throw new PaymentValidationException(
                "capture_not_supported",
                "This payment provider does not support capture."
            );
        ProviderPaymentOutcome outcome;
        try
        {
            outcome = await captureProvider.CaptureAsync(
                payment.Id,
                payment.ProviderReference,
                cancellationToken
            );
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            outcome = new ProviderPaymentOutcome(
                PaymentStatus.RequiresReconciliation,
                payment.ProviderReference
            );
        }
        // Step 4: Apply only legal status transitions and return the latest local state.
        if (
            payment.Status != outcome.Status
            && DomainPayment.CanTransition(payment.Status, outcome.Status)
        )
        {
            payment.ApplyOutcome(
                outcome.Status,
                outcome.ProviderReference,
                clock.GetUtcNow(),
                outcome.ProviderPaymentReference
            );
            await repository.UpdateAsync(payment, cancellationToken);
        }
        return ToResponse(payment, null);
    }

    /// <summary>
    /// Idempotently creates a full or partial refund for an eligible successful payment.
    /// </summary>
    public async Task<RefundResponse> RefundAsync(
        Guid paymentId,
        RefundPaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        // Step 1: Load the payment and verify that it can be refunded.
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        if (
            payment.Status != PaymentStatus.Succeeded
            || string.IsNullOrWhiteSpace(payment.ProviderPaymentReference)
        )
            throw new PaymentValidationException(
                "refund_not_supported",
                "This payment is not eligible for a refund."
            );
        // Step 2: Validate the requested refund amount in the payment currency.
        if (!currencyRules.TryGetDecimalPlaces(payment.Amount.Currency, out var decimalPlaces))
            throw new PaymentValidationException(
                "unsupported_currency",
                "The payment currency is not supported."
            );
        Money amount;
        try
        {
            amount = Money.Create(command.Amount, payment.Amount.Currency, decimalPlaces);
        }
        catch (ArgumentException exception)
        {
            throw new PaymentValidationException("invalid_amount", exception.Message);
        }
        // Step 3: Resolve a provider that supports refunds for this payment method.
        var provider = providerResolver.ResolveByKey(payment.Provider) as IRefundProvider;
        if (provider is null)
            throw new PaymentValidationException(
                "refund_not_supported",
                "Refunds are not supported for this payment method."
            );
        // Step 4: Persist or replay the idempotent refund operation and enforce the remaining refundable balance.
        var key = Idempotency.ValidateKey(command.IdempotencyKey);
        var now = clock.GetUtcNow();
        var refund = Refund.Create(payment.Id, payment.MerchantId, amount, payment.Provider, now);
        var claim = await repository.CreateRefundOrGetAsync(
            refund,
            IdempotencyOperation.Create(
                payment.MerchantId,
                Idempotency.RefundPaymentOperation,
                key,
                Idempotency.FingerprintVersion,
                Idempotency.CreateFingerprint(
                    payment.MerchantId,
                    payment.Id.ToString(),
                    amount.Value,
                    amount.Currency,
                    payment.Method,
                    "refund",
                    "refund"
                ),
                payment.Id,
                now,
                refund.Id
            ),
            amount.Value,
            cancellationToken
        );
        if (claim.Kind == IdempotentCreateKind.FingerprintConflict)
            throw new PaymentConflictException(
                "refund_not_available",
                "The refund exceeds the remaining refundable amount or the idempotency key was reused."
            );
        if (claim.Kind == IdempotentCreateKind.Replay)
            return ToRefundResponse(claim.Refund!);
        // Step 5: Submit the refund to the provider using minor currency units.
        ProviderRefundOutcome outcome;
        try
        {
            var scale =
                decimalPlaces == 0
                    ? 1m
                    : decimal.Parse(
                        "1" + new string('0', decimalPlaces),
                        System.Globalization.CultureInfo.InvariantCulture
                    );
            outcome = await provider.RefundAsync(
                new ProviderRefundRequest(
                    payment.Id,
                    refund.Id,
                    payment.ProviderPaymentReference,
                    amount,
                    checked((long)(amount.Value * scale))
                ),
                cancellationToken
            );
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            outcome = new ProviderRefundOutcome(RefundStatus.RequiresReconciliation, null);
        }
        // Step 6: Persist the provider refund outcome and return the local refund state.
        if (refund.Status != outcome.Status)
        {
            refund.ApplyOutcome(outcome.Status, outcome.ProviderReference, clock.GetUtcNow());
            await repository.UpdateRefundAsync(refund, cancellationToken);
        }
        return ToRefundResponse(refund);
    }

    /// <summary>
    /// Reconciles a payment whose provider outcome was uncertain by retrieving its authoritative provider status.
    /// </summary>
    public async Task<PaymentResponse> ReconcileAsync(
        Guid paymentId,
        CancellationToken cancellationToken
    )
    {
        // Step 1: Load a payment whose previous provider result was uncertain.
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        if (
            payment.Status != PaymentStatus.RequiresReconciliation
            || string.IsNullOrWhiteSpace(payment.ProviderReference)
        )
            throw new PaymentValidationException(
                "reconciliation_not_required",
                "This payment does not require reconciliation."
            );
        // Step 2: Resolve a provider capable of retrieving the authoritative external state.
        var provider =
            providerResolver.ResolveByKey(payment.Provider) as IPaymentReconciliationProvider
            ?? throw new PaymentValidationException(
                "reconciliation_not_supported",
                "This payment provider cannot reconcile this payment."
            );
        // Step 3: Retrieve the provider state and apply a legal local transition.
        var outcome = await provider.ReconcileAsync(
            payment.ProviderReference,
            payment.ProviderPaymentReference,
            cancellationToken
        );
        if (
            payment.Status != outcome.Status
            && DomainPayment.CanTransition(payment.Status, outcome.Status)
        )
        {
            payment.ApplyOutcome(
                outcome.Status,
                null,
                clock.GetUtcNow(),
                outcome.ProviderPaymentReference
            );
            await repository.UpdateAsync(payment, cancellationToken);
        }
        return ToResponse(payment, null);
    }

    /// <summary>
    /// Retrieves all refunds for a payment after confirming that the payment belongs to the current merchant.
    /// </summary>
    public async Task<IReadOnlyList<RefundResponse>> GetRefundsAsync(
        Guid paymentId,
        CancellationToken cancellationToken
    )
    {
        // Step 1: Confirm that the payment belongs to the current merchant.
        _ =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        // Step 2: Load and map all refunds associated with the payment.
        return (
            await repository.GetRefundsAsync(
                merchantContext.MerchantId,
                paymentId,
                cancellationToken
            )
        )
            .Select(ToRefundResponse)
            .ToList();
    }

    /// <summary>
    /// Maps the domain payment and an optional provider action into the API payment response.
    /// </summary>
    private static PaymentResponse ToResponse(
        DomainPayment payment,
        ProviderNextAction? nextAction
    ) =>
        // Step 1: Project the internal payment state into the public response contract.
        new(
            payment.Id,
            payment.MerchantReference,
            payment.Amount.Value,
            payment.Amount.Currency,
            payment.Method,
            payment.Provider,
            payment.ProviderReference,
            payment.Status,
            nextAction,
            payment.CreatedAt,
            payment.UpdatedAt
        );

    /// <summary>
    /// Maps a domain refund into the API refund response.
    /// </summary>
    private static RefundResponse ToRefundResponse(Refund refund) =>
        // Step 1: Project the internal refund state into the public response contract.
        new(
            refund.Id,
            refund.PaymentId,
            refund.Amount.Value,
            refund.Amount.Currency,
            refund.Status,
            refund.CreatedAt,
            refund.UpdatedAt
        );
}
