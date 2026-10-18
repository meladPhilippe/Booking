namespace Payment.Domain.ValueObjects;

public sealed class Money
{
    private Money() { }

    private Money(decimal value, string currency)
    {
        Value = value;
        Currency = currency;
    }

    public decimal Value { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public static Money Create(decimal value, string currency, int decimalPlaces)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(currency));
        if (decimalPlaces is < 0 or > 4)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

        var normalized = currency.Trim().ToUpperInvariant();
        if (decimal.Round(value, decimalPlaces) != value)
            throw new ArgumentException($"{normalized} permits at most {decimalPlaces} decimal places.", nameof(value));

        return new Money(value, normalized);
    }
}
