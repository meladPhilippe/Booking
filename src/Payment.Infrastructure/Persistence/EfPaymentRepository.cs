using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Persistence;

public sealed class EfPaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    public async Task<IdempotentCreateResult> ClaimOperationAsync(IdempotencyOperation operation, CancellationToken cancellationToken)
    {
        dbContext.IdempotencyOperations.Add(operation);
        try { await dbContext.SaveChangesAsync(cancellationToken); return new IdempotentCreateResult(IdempotentCreateKind.Created); }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.IdempotencyOperations.AsNoTracking().SingleOrDefaultAsync(x => x.MerchantId == operation.MerchantId && x.Operation == operation.Operation && x.Key == operation.Key, cancellationToken);
            if (existing is null) throw;
            return existing.Fingerprint == operation.Fingerprint ? new IdempotentCreateResult(IdempotentCreateKind.Replay) : new IdempotentCreateResult(IdempotentCreateKind.FingerprintConflict);
        }
    }
    public async Task<IdempotentCreateResult> CreateOrGetAsync(DomainPayment payment, IdempotencyOperation operation, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Payments.Add(payment);
        dbContext.IdempotencyOperations.Add(operation);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdempotentCreateResult(IdempotentCreateKind.Created, payment);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.IdempotencyOperations.AsNoTracking().SingleOrDefaultAsync(x =>
                x.MerchantId == operation.MerchantId && x.Operation == operation.Operation && x.Key == operation.Key,
                cancellationToken);
            if (existing is null) throw;
            if (existing.FingerprintVersion != operation.FingerprintVersion || existing.Fingerprint != operation.Fingerprint)
                return new IdempotentCreateResult(IdempotentCreateKind.FingerprintConflict);
            var existingPayment = await GetAsync(operation.MerchantId, existing.PaymentId, cancellationToken);
            return new IdempotentCreateResult(IdempotentCreateKind.Replay, existingPayment);
        }
    }

    public Task<DomainPayment?> GetAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken) => dbContext.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId && x.MerchantId == merchantId, cancellationToken);
    public Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken) => dbContext.Payments.SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

    public async Task<bool> TryRecordWebhookEventAsync(ProviderWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        dbContext.ProviderWebhookEvents.Add(webhookEvent);
        try { await dbContext.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateException) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public async Task UpdateAsync(DomainPayment payment, CancellationToken cancellationToken)
    {
        dbContext.Payments.Update(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
