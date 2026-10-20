using Payment.Application.Abstractions;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Providers;

public sealed class PaymentProviderResolver(
    IEnumerable<IPaymentProvider> providers,
    PaymentRoutingOptions routing
) : IPaymentProviderResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> providerByKey = providers
        .ToDictionary(provider => provider.ProviderKey, StringComparer.OrdinalIgnoreCase);

    public IPaymentProvider ResolveForNewPayment(PaymentMethod method)
    {
        if (!routing.Methods.TryGetValue(method.ToString(), out var providerKey))
            throw new InvalidOperationException($"No provider is configured for {method}.");
        return ResolveByKey(providerKey);
    }

    public IPaymentProvider ResolveByKey(string providerKey) =>
        providerByKey.TryGetValue(providerKey, out var provider)
            ? provider
            : throw new InvalidOperationException($"Provider '{providerKey}' is not registered.");
}
