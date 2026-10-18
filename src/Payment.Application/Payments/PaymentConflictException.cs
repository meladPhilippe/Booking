namespace Payment.Application.Payments;

public sealed class PaymentConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
