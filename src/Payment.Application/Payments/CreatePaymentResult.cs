namespace Payment.Application.Payments;

public sealed record CreatePaymentResult(PaymentResponse Payment, bool IsReplay, bool IsInProgress);
