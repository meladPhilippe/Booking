namespace Payment.Domain.Entities;

public sealed class OutcomeOutboxMessage
{
    private OutcomeOutboxMessage() { }

    private OutcomeOutboxMessage(Guid id, string eventType, string payload, DateTimeOffset now)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredAt = now;
        NextAttemptAt = now;
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static OutcomeOutboxMessage Create(string eventType, string payload, DateTimeOffset now) => new(Guid.NewGuid(), eventType, payload, now);
    public void MarkDelivered(DateTimeOffset now) => DeliveredAt = now;
    public void ScheduleRetry(DateTimeOffset nextAttemptAt, string safeError) { AttemptCount++; NextAttemptAt = nextAttemptAt; LastError = safeError; }
}
