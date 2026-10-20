namespace Payment.Application.Abstractions;

public interface ICaptureProvider
{
    Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerOperationId,
        CancellationToken cancellationToken
    );
}
