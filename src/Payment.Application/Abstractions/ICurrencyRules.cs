namespace Payment.Application.Abstractions;

public interface ICurrencyRules
{
    bool TryGetDecimalPlaces(string currency, out int decimalPlaces);
}
