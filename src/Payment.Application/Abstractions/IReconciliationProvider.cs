namespace Payment.Application.Abstractions;

public interface IPaymentReconciliationProvider
{
    Task<ProviderReconciliationOutcome> ReconcileAsync(
        string providerReference,
        string? providerPaymentReference,
        CancellationToken cancellationToken
    );
}
