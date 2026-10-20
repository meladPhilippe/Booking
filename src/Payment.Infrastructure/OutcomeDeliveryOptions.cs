namespace Payment.Infrastructure;

public sealed class OutcomeDeliveryOptions
{
    public const string SectionName = "OutcomeDelivery";
    public string? Endpoint { get; init; }
    public string? SigningSecret { get; init; }
}
