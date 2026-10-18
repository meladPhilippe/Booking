namespace Payment.Application.Payments;

public sealed class PaymentNotFoundException(Guid paymentId) : Exception("Payment was not found.")
{
    public Guid PaymentId { get; } = paymentId;
}
