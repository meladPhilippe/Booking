using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public sealed class Payment
{
    private Payment() { }

    private Payment(Guid id, string merchantId, string merchantReference, Money amount, PaymentMethod method, string provider, DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        MerchantReference = merchantReference;
        Amount = amount;
        Method = method;
        Provider = provider;
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public string MerchantReference { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public PaymentMethod Method { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Payment Create(string merchantId, string merchantReference, Money amount, PaymentMethod method, string provider, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant identity is required.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(merchantReference)) throw new ArgumentException("Merchant reference is required.", nameof(merchantReference));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        return new Payment(Guid.NewGuid(), merchantId.Trim(), merchantReference.Trim(), amount, method, provider.Trim(), now);
    }

    public static Payment Rehydrate(Guid id, string merchantId, string merchantReference, Money amount, PaymentMethod method, string provider, string? providerReference, PaymentStatus status, DateTimeOffset createdAt, DateTimeOffset updatedAt, byte[] rowVersion)
    {
        var payment = new Payment(id, merchantId, merchantReference, amount, method, provider, createdAt)
        {
            ProviderReference = providerReference,
            Status = status,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion
        };
        return payment;
    }

    public void ApplyOutcome(PaymentStatus nextStatus, string? providerReference, DateTimeOffset now)
    {
        if (!CanTransition(Status, nextStatus))
            throw new InvalidOperationException($"Cannot transition payment from {Status} to {nextStatus}.");

        Status = nextStatus;
        ProviderReference = providerReference ?? ProviderReference;
        UpdatedAt = now;
    }

    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    public static bool CanTransition(PaymentStatus current, PaymentStatus next) => current switch
    {
        PaymentStatus.Pending => next is PaymentStatus.Processing or PaymentStatus.RequiresAction or PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.RequiresReconciliation,
        PaymentStatus.Processing => next is PaymentStatus.RequiresAction or PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.RequiresReconciliation,
        PaymentStatus.RequiresAction => next is PaymentStatus.Processing or PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.RequiresReconciliation,
        PaymentStatus.RequiresReconciliation => next is PaymentStatus.Processing or PaymentStatus.Succeeded or PaymentStatus.Failed,
        _ => false
    };
}
