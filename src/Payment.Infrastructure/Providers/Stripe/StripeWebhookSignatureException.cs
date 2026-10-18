namespace Payment.Infrastructure.Providers.Stripe;

/// <summary>
/// Indicates that Stripe could not authenticate an inbound webhook payload.
/// </summary>
/// <remarks>
/// Controllers map this exception to a client rejection before any event or payment state is persisted.
/// See <see href="https://docs.stripe.com/webhooks?lang=dotnet">Stripe webhook signature verification</see>.
/// </remarks>
public sealed class StripeWebhookSignatureException : Exception
{
    public StripeWebhookSignatureException(Exception innerException) : base("Stripe webhook signature verification failed.", innerException) { }
}
