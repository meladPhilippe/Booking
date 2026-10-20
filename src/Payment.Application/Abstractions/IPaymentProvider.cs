using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public interface IPaymentProvider
{
    string Name { get; }
    bool Supports(PaymentMethod method);
    Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    );
    Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerReference,
        CancellationToken cancellationToken
    );
}
