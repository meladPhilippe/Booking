using Payment.Application.Abstractions;

namespace Payment.Infrastructure;

public sealed class ConfiguredCurrencyRules(CurrencyRuleOptions options) : ICurrencyRules
{
    private readonly Dictionary<string, int> _rules = options.Supported.ToDictionary(
        x => x.Code.ToUpperInvariant(),
        x => x.DecimalPlaces,
        StringComparer.OrdinalIgnoreCase
    );

    public bool TryGetDecimalPlaces(string currency, out int decimalPlaces) =>
        _rules.TryGetValue(currency.Trim(), out decimalPlaces);
}
