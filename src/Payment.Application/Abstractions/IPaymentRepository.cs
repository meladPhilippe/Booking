using Payment.Domain.Entities;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Application.Abstractions;

public interface IPaymentRepository
{
    Task<IdempotentCreateResult> CreateOrGetAsync(DomainPayment payment, IdempotencyOperation operation, CancellationToken cancellationToken);
    Task<IdempotentCreateResult> ClaimOperationAsync(IdempotencyOperation operation, CancellationToken cancellationToken);
    Task UpdateAsync(DomainPayment payment, CancellationToken cancellationToken);
    Task<DomainPayment?> GetAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken);
    Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<bool> TryRecordWebhookEventAsync(ProviderWebhookEvent webhookEvent, CancellationToken cancellationToken);
}

public enum IdempotentCreateKind { Created, Replay, FingerprintConflict }

public sealed record IdempotentCreateResult(IdempotentCreateKind Kind, DomainPayment? Payment = null);
