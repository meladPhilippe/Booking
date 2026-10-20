using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Providers.Stripe;

public interface IStripeGateway
{
    void VerifyWebhook(string payload, string signature);
    Task<StripeCheckoutSession> CreateEmbeddedCheckoutAsync(ProviderPaymentRequest request, CancellationToken cancellationToken);
    Task<StripeCheckoutSession> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);
    Task<StripePaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken);
    Task<StripeRefund> CreateRefundAsync(ProviderRefundRequest request, CancellationToken cancellationToken);
    Task<StripeRefund> GetRefundAsync(string refundId, CancellationToken cancellationToken);
}

public sealed record StripeCheckoutSession(string Id, string? ClientSecret, string? PaymentIntentId, string? PaymentStatus, IReadOnlyDictionary<string, string>? Metadata, string? ClientReferenceId);
public sealed record StripePaymentIntent(string Id, string? Status, IReadOnlyDictionary<string, string>? Metadata);
public sealed record StripeRefund(string Id, string? Status, IReadOnlyDictionary<string, string>? Metadata);
