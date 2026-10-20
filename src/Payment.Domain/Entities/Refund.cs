using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public sealed class Refund
{
    private Refund() { }

    private Refund(Guid id, Guid paymentId, string merchantId, Money amount, string provider, DateTimeOffset now)
    {
        Id = id;
        PaymentId = paymentId;
        MerchantId = merchantId;
        Amount = amount;
        Provider = provider;
        Status = RefundStatus.Pending;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public RefundStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Refund Create(Guid paymentId, string merchantId, Money amount, string provider, DateTimeOffset now)
    {
        if (paymentId == Guid.Empty) throw new ArgumentException("Payment identity is required.", nameof(paymentId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant identity is required.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        return new Refund(Guid.NewGuid(), paymentId, merchantId.Trim(), amount, provider.Trim(), now);
    }

    public void ApplyOutcome(RefundStatus status, string? providerReference, DateTimeOffset now)
    {
        if (!CanTransition(Status, status)) throw new InvalidOperationException($"Cannot transition refund from {Status} to {status}.");
        Status = status;
        ProviderReference = providerReference ?? ProviderReference;
        UpdatedAt = now;
    }

    public static bool CanTransition(RefundStatus current, RefundStatus next) => current switch
    {
        RefundStatus.Pending => next is RefundStatus.Succeeded or RefundStatus.Failed or RefundStatus.RequiresReconciliation,
        RefundStatus.RequiresReconciliation => next is RefundStatus.Pending or RefundStatus.Succeeded or RefundStatus.Failed,
        _ => false
    };
}
