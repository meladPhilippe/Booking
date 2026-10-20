namespace Payment.Application.Abstractions;

public interface IRefundProvider
{
    Task<ProviderRefundOutcome> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken
    );
}
