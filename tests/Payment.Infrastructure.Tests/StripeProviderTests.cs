using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Providers.Stripe;

namespace Payment.Infrastructure.Tests;

public sealed class StripeProviderTests
{
    [Fact]
    public async Task Create_returns_embedded_checkout_action_and_references()
    {
        var provider = new StripeProvider(new FakeGateway { Session = new StripeCheckoutSession("cs_test_123", "cs_secret_123", "pi_test_123", "unpaid", null, null) });
        var outcome = await provider.CreateAsync(Request(), CancellationToken.None);
        Assert.Equal(PaymentStatus.RequiresAction, outcome.Status);
        Assert.Equal("cs_test_123", outcome.ProviderReference);
        Assert.Equal("pi_test_123", outcome.ProviderPaymentReference);
        Assert.Equal("embedded_checkout", outcome.NextAction!.Type);
    }

    [Theory]
    [InlineData("succeeded", RefundStatus.Succeeded)]
    [InlineData("failed", RefundStatus.Failed)]
    [InlineData("pending", RefundStatus.Pending)]
    public async Task Refund_maps_stripe_status(string stripeStatus, RefundStatus expected)
    {
        var provider = new StripeProvider(new FakeGateway { Refund = new StripeRefund("re_test_123", stripeStatus, null) });
        var outcome = await provider.RefundAsync(new ProviderRefundRequest(Guid.NewGuid(), Guid.NewGuid(), "pi_test_123", Money.Create(10m, "EGP", 2), 1000), CancellationToken.None);
        Assert.Equal(expected, outcome.Status);
    }

    [Fact]
    public async Task Reconcile_uses_checkout_session_then_payment_intent()
    {
        var provider = new StripeProvider(new FakeGateway
        {
            Session = new StripeCheckoutSession("cs_test_123", null, "pi_test_123", "unpaid", null, null),
            Intent = new StripePaymentIntent("pi_test_123", "succeeded", null)
        });
        var outcome = await provider.ReconcileAsync("cs_test_123", null, CancellationToken.None);
        Assert.Equal(PaymentStatus.Succeeded, outcome.Status);
        Assert.Equal("pi_test_123", outcome.ProviderPaymentReference);
    }

    private static ProviderPaymentRequest Request() => new(Guid.NewGuid(), "booking-1", Money.Create(10m, "EGP", 2), 1000, PaymentMethod.Card, "https://merchant.example/return", "https://merchant.example/cancel", null);

    private sealed class FakeGateway : IStripeGateway
    {
        public StripeCheckoutSession Session { get; init; } = new("cs_default", "cs_secret_default", "pi_default", "unpaid", null, null);
        public StripePaymentIntent Intent { get; init; } = new("pi_default", "processing", null);
        public StripeRefund Refund { get; init; } = new("re_default", "pending", null);
        public void VerifyWebhook(string payload, string signature) { }
        public Task<StripeCheckoutSession> CreateEmbeddedCheckoutAsync(ProviderPaymentRequest request, CancellationToken cancellationToken) => Task.FromResult(Session);
        public Task<StripeCheckoutSession> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult(Session);
        public Task<StripePaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken) => Task.FromResult(Intent);
        public Task<StripeRefund> CreateRefundAsync(ProviderRefundRequest request, CancellationToken cancellationToken) => Task.FromResult(Refund);
        public Task<StripeRefund> GetRefundAsync(string refundId, CancellationToken cancellationToken) => Task.FromResult(Refund);
    }
}
