using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using Stripe;
using DomainPaymentMethod = Payment.Domain.Enums.PaymentMethod;

namespace Payment.Infrastructure.Providers.Stripe;

public sealed class StripeProvider(IStripeGateway gateway, ILogger<StripeProvider>? logger = null, PaymentMetrics? metrics = null)
    : IPaymentProvider,
        IRefundProvider,
        IReconciliationProvider
{
    public string Name => "Stripe";

    public bool Supports(DomainPaymentMethod method) => method == DomainPaymentMethod.Card;

    public bool SupportsRefunds(DomainPaymentMethod method) => method == DomainPaymentMethod.Card;

    public async Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var session = await gateway.CreateEmbeddedCheckoutAsync(request, cancellationToken);
            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                return new ProviderPaymentOutcome(
                    PaymentStatus.RequiresReconciliation,
                    session.Id,
                    null,
                    session.PaymentIntentId
                );
            return new ProviderPaymentOutcome(
                PaymentStatus.RequiresAction,
                session.Id,
                new ProviderNextAction(
                    "embedded_checkout",
                    new Dictionary<string, string> { ["clientSecret"] = session.ClientSecret }
                ),
                session.PaymentIntentId
            );
        }
        catch (StripeException exception)
            when (exception.StripeError?.Type is "card_error" or "invalid_request_error")
        {
            LogStripeFailure("create Checkout Session", exception);
            return new ProviderPaymentOutcome(PaymentStatus.Failed, null);
        }
    }

    public Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerReference,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Stripe embedded checkout does not use the capture flow.");

    public async Task<ProviderRefundOutcome> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return MapRefund(await gateway.CreateRefundAsync(request, cancellationToken));
        }
        catch (StripeException exception)
            when (exception.StripeError?.Type is "card_error" or "invalid_request_error")
        {
            LogStripeFailure("create refund", exception);
            return new ProviderRefundOutcome(RefundStatus.Failed, null);
        }
    }

    public async Task<ProviderReconciliationOutcome> ReconcileAsync(
        string providerReference,
        string? providerPaymentReference,
        CancellationToken cancellationToken
    )
    {
        var intentId = providerPaymentReference;
        if (string.IsNullOrWhiteSpace(intentId))
        {
            var session = await gateway.GetCheckoutSessionAsync(
                providerReference,
                cancellationToken
            );
            intentId = session.PaymentIntentId;
            if (session.PaymentStatus == "paid")
                return new ProviderReconciliationOutcome(PaymentStatus.Succeeded, intentId);
        }
        if (string.IsNullOrWhiteSpace(intentId))
            return new ProviderReconciliationOutcome(PaymentStatus.RequiresReconciliation);
        return MapIntent(await gateway.GetPaymentIntentAsync(intentId, cancellationToken));
    }

    internal static ProviderRefundOutcome MapRefund(StripeRefund refund) =>
        new(
            refund.Status switch
            {
                "succeeded" => RefundStatus.Succeeded,
                "failed" or "canceled" => RefundStatus.Failed,
                _ => RefundStatus.Pending,
            },
            refund.Id
        );

    internal static ProviderReconciliationOutcome MapIntent(StripePaymentIntent intent) =>
        new(
            intent.Status switch
            {
                "succeeded" => PaymentStatus.Succeeded,
                "canceled" => PaymentStatus.Cancelled,
                "requires_action" => PaymentStatus.RequiresAction,
                "requires_payment_method" => PaymentStatus.Failed,
                _ => PaymentStatus.Processing,
            },
            intent.Id
        );

    private void LogStripeFailure(string operation, StripeException exception) =>
        LogStripeFailureCore(operation, exception);

    private void LogStripeFailureCore(string operation, StripeException exception)
    {
        var reason = exception.StripeError?.Type ?? exception.GetType().Name;
        metrics?.ProviderFailure(Name, operation, reason);
        logger?.LogWarning(
            "Stripe failed to {Operation}. Type: {StripeErrorType}; code: {StripeErrorCode}; parameter: {StripeErrorParameter}",
            operation,
            exception.StripeError?.Type,
            exception.StripeError?.Code,
            exception.StripeError?.Param
        );
    }
}
