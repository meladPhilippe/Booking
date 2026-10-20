using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using Payment.Infrastructure;
using Payment.Infrastructure.Providers;

namespace Payment.Infrastructure.Tests;

public sealed class PaymentProviderResolverTests
{
    [Fact]
    public void New_payment_uses_the_configured_route_but_existing_payment_uses_its_persisted_key()
    {
        var stripe = new StubProvider("stripe");
        var adyen = new StubProvider("adyen");
        var resolver = new PaymentProviderResolver(
            [stripe, adyen],
            new PaymentRoutingOptions
            {
                Methods = new Dictionary<string, string> { ["Card"] = "adyen" },
            }
        );

        Assert.Same(adyen, resolver.ResolveForNewPayment(PaymentMethod.Card));
        Assert.Same(stripe, resolver.ResolveByKey("stripe"));
    }

    private sealed class StubProvider(string providerKey) : IPaymentProvider
    {
        public string ProviderKey => providerKey;

        public Task<ProviderPaymentOutcome> CreateAsync(
            ProviderPaymentRequest request,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
