using Payment.Application.Abstractions;

namespace Payment.Infrastructure;

public sealed class DevelopmentMerchantContext(DevelopmentMerchantOptions options)
    : IMerchantContext
{
    public string MerchantId { get; } =
        string.IsNullOrWhiteSpace(options.Id)
            ? throw new InvalidOperationException("DevelopmentMerchant:Id must be configured.")
            : options.Id;
}
