using Payment.Application.Abstractions;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Providers;

public sealed class PaymentProviderResolver(IEnumerable<IPaymentProvider> providers)
    : IPaymentProviderResolver
{
    public IPaymentProvider Resolve(PaymentMethod method) =>
        providers.FirstOrDefault(x => x.Supports(method))
        ?? throw new InvalidOperationException($"No provider supports {method}.");
}
