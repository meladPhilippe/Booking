using Payment.Domain.Entities;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Application.Abstractions;

public interface IPaymentRepository
{
    Task<IdempotentCreateResult> CreateOrGetAsync(
        DomainPayment payment,
        IdempotencyOperation operation,
        CancellationToken cancellationToken
    );
    Task<IdempotentCreateResult> ClaimOperationAsync(
        IdempotencyOperation operation,
        CancellationToken cancellationToken
    );
    Task UpdateAsync(DomainPayment payment, CancellationToken cancellationToken);
    Task<DomainPayment?> GetAsync(
        string merchantId,
        Guid paymentId,
        CancellationToken cancellationToken
    );
    Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<bool> TryRecordWebhookEventAsync(
        ProviderWebhookEvent webhookEvent,
        CancellationToken cancellationToken
    );
    Task<RefundClaimResult> CreateRefundOrGetAsync(
        Refund refund,
        IdempotencyOperation operation,
        decimal reservedAmount,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Refund persistence is not configured.");
    Task<Refund?> GetRefundAsync(
        string merchantId,
        Guid refundId,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Refund persistence is not configured.");
    Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Refund persistence is not configured.");
    Task<IReadOnlyList<Refund>> GetRefundsAsync(
        string merchantId,
        Guid paymentId,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Refund persistence is not configured.");
    Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Refund persistence is not configured.");
    Task<IReadOnlyList<OutcomeOutboxMessage>> GetDueOutboxMessagesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Outcome delivery is not configured.");
    Task UpdateOutboxMessageAsync(
        OutcomeOutboxMessage message,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Outcome delivery is not configured.");
}
