namespace Payment.Application.Abstractions;

public interface IReconciliationProvider
{
    Task<ProviderReconciliationOutcome> ReconcileAsync(
        string providerReference,
        string? providerPaymentReference,
        CancellationToken cancellationToken
    );
}
