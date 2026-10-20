using Payment.Application.Abstractions;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Providers;

public sealed class PaymentProviderResolver(
    IEnumerable<IPaymentProvider> providers,
    PaymentProviderOptions options
) : IPaymentProviderResolver
{
    public IPaymentProvider Resolve(PaymentMethod method) =>
        providers.FirstOrDefault(x =>
            x.Supports(method)
            && string.Equals(x.Name, ProviderNameFor(method), StringComparison.OrdinalIgnoreCase)
        )
        ?? throw new InvalidOperationException(
            $"Provider '{ProviderNameFor(method)}' does not support {method}."
        );

    private string ProviderNameFor(PaymentMethod method) =>
        method switch
        {
            PaymentMethod.Card => options.Card,
            PaymentMethod.PayPal => "PayPal",
            _ => throw new InvalidOperationException($"No provider is configured for {method}."),
        };
}
