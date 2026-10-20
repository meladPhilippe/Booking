using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public interface IRefundProvider
{
    bool SupportsRefunds(PaymentMethod method);
    Task<ProviderRefundOutcome> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken
    );
}
