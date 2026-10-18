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
    public async Task<CreatePaymentResult> CreateAsync(
        CreatePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
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
        IPaymentProvider provider;
        try
        {
            provider = providerResolver.Resolve(command.Method);
        }
        catch (InvalidOperationException)
        {
            throw new PaymentValidationException(
                "unsupported_payment_method",
                "The requested payment method is not configured."
            );
        }
        var now = clock.GetUtcNow();
        DomainPayment payment;
        try
        {
            payment = DomainPayment.Create(
                merchantContext.MerchantId,
                command.MerchantReference,
                amount,
                command.Method,
                provider.Name,
                now
            );
        }
        catch (ArgumentException exception)
        {
            throw new PaymentValidationException("invalid_request", exception.Message);
        }

        payment.ApplyOutcome(PaymentStatus.Processing, null, now);
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
        payment.ApplyOutcome(outcome.Status, outcome.ProviderReference, clock.GetUtcNow());
        await repository.UpdateAsync(payment, cancellationToken);
        return new CreatePaymentResult(ToResponse(payment, outcome.NextAction), false, false);
    }

    public async Task<PaymentResponse> GetAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        return ToResponse(payment, null);
    }

    public async Task<PaymentResponse> CaptureAsync(
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        var payment =
            await repository.GetAsync(merchantContext.MerchantId, paymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentId);
        if (
            payment.Method != PaymentMethod.PayPal
            || string.IsNullOrWhiteSpace(payment.ProviderReference)
            || payment.Status is not (PaymentStatus.RequiresAction or PaymentStatus.Processing)
        )
            throw new PaymentValidationException(
                "capture_not_supported",
                "This payment cannot be captured."
            );
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
        var provider = providerResolver.Resolve(PaymentMethod.PayPal);
        ProviderPaymentOutcome outcome;
        try
        {
            outcome = await provider.CaptureAsync(
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
        if (
            payment.Status != outcome.Status
            && DomainPayment.CanTransition(payment.Status, outcome.Status)
        )
        {
            payment.ApplyOutcome(outcome.Status, outcome.ProviderReference, clock.GetUtcNow());
            await repository.UpdateAsync(payment, cancellationToken);
        }
        return ToResponse(payment, null);
    }

    private static PaymentResponse ToResponse(
        DomainPayment payment,
        ProviderNextAction? nextAction
    ) =>
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
}
