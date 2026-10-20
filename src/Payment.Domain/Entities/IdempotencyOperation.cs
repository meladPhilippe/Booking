namespace Payment.Domain.Entities;

public sealed class IdempotencyOperation
{
    private IdempotencyOperation() { }

    private IdempotencyOperation(Guid id, string merchantId, string operation, string key, int fingerprintVersion, string fingerprint, Guid paymentId, Guid? refundId, DateTimeOffset now)
    {
        Id = id;
        MerchantId = merchantId;
        Operation = operation;
        Key = key;
        FingerprintVersion = fingerprintVersion;
        Fingerprint = fingerprint;
        PaymentId = paymentId;
        RefundId = refundId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public int FingerprintVersion { get; private set; }
    public string Fingerprint { get; private set; } = string.Empty;
    public Guid PaymentId { get; private set; }
    public Guid? RefundId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static IdempotencyOperation Create(string merchantId, string operation, string key, int fingerprintVersion, string fingerprint, Guid paymentId, DateTimeOffset now, Guid? refundId = null)
    {
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant identity is required.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation is required.", nameof(operation));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Idempotency key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(fingerprint)) throw new ArgumentException("Request fingerprint is required.", nameof(fingerprint));
        return new IdempotencyOperation(Guid.NewGuid(), merchantId.Trim(), operation, key.Trim(), fingerprintVersion, fingerprint, paymentId, refundId, now);
    }

    public static IdempotencyOperation Rehydrate(Guid id, string merchantId, string operation, string key, int fingerprintVersion, string fingerprint, Guid paymentId, Guid? refundId, DateTimeOffset createdAt, DateTimeOffset updatedAt, byte[] rowVersion) =>
        new(id, merchantId, operation, key, fingerprintVersion, fingerprint, paymentId, refundId, createdAt) { UpdatedAt = updatedAt, RowVersion = rowVersion };
}
