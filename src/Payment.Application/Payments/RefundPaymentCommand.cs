namespace Payment.Application.Payments;

public sealed record RefundPaymentCommand(decimal Amount, string IdempotencyKey);
