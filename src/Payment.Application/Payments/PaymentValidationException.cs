namespace Payment.Application.Payments;

public sealed class PaymentValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
