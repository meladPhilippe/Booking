namespace Payment.Application.Abstractions;

public interface IPaymentProvider
{
    string ProviderKey { get; }
    Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    );
}
