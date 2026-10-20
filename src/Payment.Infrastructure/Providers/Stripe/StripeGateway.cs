using Payment.Application.Abstractions;
using Stripe;
using Stripe.Checkout;

namespace Payment.Infrastructure.Providers.Stripe;

public sealed class StripeGateway(SelectedStripeProfile profile) : IStripeGateway
{
    private readonly StripeClient _client = new(profile.Options.SecretKey);

    public void VerifyWebhook(string payload, string signature) =>
        _client.ParseEventNotification(payload, signature, profile.Options.WebhookSigningSecret);

    public async Task<StripeCheckoutSession> CreateEmbeddedCheckoutAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var session = await new SessionService(_client).CreateAsync(
            new SessionCreateOptions
            {
                Mode = "payment",
                UiMode = "embedded",
                ReturnUrl = request.ReturnUrl,
                ClientReferenceId = request.PaymentId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = request.PaymentId.ToString(),
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["payment_id"] = request.PaymentId.ToString(),
                    },
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Amount.Currency.ToLowerInvariant(),
                            UnitAmount = request.AmountInMinorUnits,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.MerchantReference,
                            },
                        },
                    },
                ],
            },
            new RequestOptions { IdempotencyKey = request.PaymentId.ToString("N") },
            cancellationToken
        );
        return ToCheckoutSession(session);
    }

    public async Task<StripeCheckoutSession> GetCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken
    ) =>
        ToCheckoutSession(
            await new SessionService(_client).GetAsync(
                sessionId,
                cancellationToken: cancellationToken
            )
        );

    public async Task<StripePaymentIntent> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken
    )
    {
        var intent = await new PaymentIntentService(_client).GetAsync(
            paymentIntentId,
            cancellationToken: cancellationToken
        );
        return new StripePaymentIntent(intent.Id, intent.Status, intent.Metadata);
    }

    public async Task<StripeRefund> CreateRefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken
    )
    {
        var refund = await new RefundService(_client).CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = request.ProviderPaymentReference,
                Amount = request.AmountInMinorUnits,
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = request.PaymentId.ToString(),
                    ["refund_id"] = request.RefundId.ToString(),
                },
            },
            new RequestOptions { IdempotencyKey = request.RefundId.ToString("N") },
            cancellationToken
        );
        return new StripeRefund(refund.Id, refund.Status, refund.Metadata);
    }

    public async Task<StripeRefund> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken
    )
    {
        var refund = await new RefundService(_client).GetAsync(
            refundId,
            cancellationToken: cancellationToken
        );
        return new StripeRefund(refund.Id, refund.Status, refund.Metadata);
    }

    private static StripeCheckoutSession ToCheckoutSession(Session session) =>
        new(
            session.Id,
            session.ClientSecret,
            session.PaymentIntentId,
            session.PaymentStatus,
            session.Metadata,
            session.ClientReferenceId
        );
}
