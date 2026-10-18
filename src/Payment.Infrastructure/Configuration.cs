namespace Payment.Infrastructure;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public string Provider { get; init; } = "EfCore";
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class CurrencyRuleOptions
{
    public const string SectionName = "Currencies";
    public List<CurrencyRule> Supported { get; init; } = [];
}

public sealed class CurrencyRule
{
    public string Code { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; }
}

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";
    public string DefaultOutcome { get; init; } = "Succeeded";
}

public sealed class DevelopmentMerchantOptions
{
    public const string SectionName = "DevelopmentMerchant";
    public string Id { get; init; } = string.Empty;
}

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSigningSecret { get; init; } = string.Empty;
    public string? PublishableKey { get; init; }
    public List<string> ReturnUrlOrigins { get; init; } = [];
}

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";
    public string BaseUrl { get; init; } = "https://api-m.sandbox.paypal.com";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string WebhookId { get; init; } = string.Empty;
    public List<string> ReturnUrlOrigins { get; init; } = [];
}
