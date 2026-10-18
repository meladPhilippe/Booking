namespace Payment.Domain.Entities;

public sealed class ProviderWebhookEvent
{
    private ProviderWebhookEvent() { }
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string RelatedObjectId { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static ProviderWebhookEvent Create(string provider, string externalEventId, string eventType, string relatedObjectId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), Provider = provider, ExternalEventId = externalEventId, EventType = eventType, RelatedObjectId = relatedObjectId, ReceivedAt = now, ProcessedAt = now
    };
}
