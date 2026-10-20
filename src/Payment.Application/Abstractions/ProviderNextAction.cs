namespace Payment.Application.Abstractions;

public sealed record ProviderNextAction(string Type, IReadOnlyDictionary<string, string> Data);
