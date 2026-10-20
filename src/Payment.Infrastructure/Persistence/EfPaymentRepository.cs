using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using System.Text.Json;
using System.Data;
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
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
        dbContext.OutcomeOutboxMessages.Add(CreatePaymentOutcome(payment));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefundClaimResult> CreateRefundOrGetAsync(Refund refund, IdempotencyOperation operation, decimal reservedAmount, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var alreadyReserved = await dbContext.Refunds
            .Where(x => x.PaymentId == refund.PaymentId && x.Status != RefundStatus.Failed)
            .SumAsync(x => (decimal?)x.Amount.Value, cancellationToken) ?? 0m;
        var payment = await dbContext.Payments.SingleAsync(x => x.Id == refund.PaymentId && x.MerchantId == refund.MerchantId, cancellationToken);
        if (alreadyReserved + reservedAmount > payment.Amount.Value)
            return new RefundClaimResult(IdempotentCreateKind.FingerprintConflict);
        dbContext.Refunds.Add(refund);
        dbContext.IdempotencyOperations.Add(operation);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefundClaimResult(IdempotentCreateKind.Created, refund);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.IdempotencyOperations.AsNoTracking().SingleOrDefaultAsync(x => x.MerchantId == operation.MerchantId && x.Operation == operation.Operation && x.Key == operation.Key, cancellationToken);
            if (existing is null) throw;
            if (existing.FingerprintVersion != operation.FingerprintVersion || existing.Fingerprint != operation.Fingerprint)
                return new RefundClaimResult(IdempotentCreateKind.FingerprintConflict);
            var existingRefund = existing.RefundId is null ? null : await dbContext.Refunds.AsNoTracking().SingleOrDefaultAsync(x => x.Id == existing.RefundId, cancellationToken);
            return new RefundClaimResult(IdempotentCreateKind.Replay, existingRefund);
        }
    }

    public Task<Refund?> GetRefundAsync(string merchantId, Guid refundId, CancellationToken cancellationToken) =>
        dbContext.Refunds.AsNoTracking().SingleOrDefaultAsync(x => x.MerchantId == merchantId && x.Id == refundId, cancellationToken);

    public Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken) =>
        dbContext.Refunds.SingleOrDefaultAsync(x => x.Id == refundId, cancellationToken);

    public async Task<IReadOnlyList<Refund>> GetRefundsAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken) =>
        await dbContext.Refunds.AsNoTracking().Where(x => x.MerchantId == merchantId && x.PaymentId == paymentId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken)
    {
        dbContext.Refunds.Update(refund);
        dbContext.OutcomeOutboxMessages.Add(CreateRefundOutcome(refund));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutcomeOutboxMessage>> GetDueOutboxMessagesAsync(DateTimeOffset now, int take, CancellationToken cancellationToken) =>
        await dbContext.OutcomeOutboxMessages.Where(x => x.DeliveredAt == null && x.NextAttemptAt <= now).OrderBy(x => x.OccurredAt).Take(take).ToListAsync(cancellationToken);

    public async Task UpdateOutboxMessageAsync(OutcomeOutboxMessage message, CancellationToken cancellationToken)
    {
        dbContext.OutcomeOutboxMessages.Update(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static OutcomeOutboxMessage CreatePaymentOutcome(DomainPayment payment)
    {
        var eventType = $"payment.{payment.Status.ToString().ToLowerInvariant()}";
        var payload = JsonSerializer.Serialize(new { eventId = Guid.NewGuid(), eventType, occurredAt = payment.UpdatedAt, merchantId = payment.MerchantId, paymentId = payment.Id, merchantReference = payment.MerchantReference, paymentStatus = payment.Status.ToString() });
        return OutcomeOutboxMessage.Create(eventType, payload, payment.UpdatedAt);
    }

    private static OutcomeOutboxMessage CreateRefundOutcome(Refund refund)
    {
        var eventType = $"refund.{refund.Status.ToString().ToLowerInvariant()}";
        var payload = JsonSerializer.Serialize(new { eventId = Guid.NewGuid(), eventType, occurredAt = refund.UpdatedAt, merchantId = refund.MerchantId, paymentId = refund.PaymentId, refund = new { refundId = refund.Id, amount = refund.Amount.Value, currency = refund.Amount.Currency, status = refund.Status.ToString() } });
        return OutcomeOutboxMessage.Create(eventType, payload, refund.UpdatedAt);
    }
}
