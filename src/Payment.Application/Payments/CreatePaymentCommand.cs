using Payment.Domain.Enums;

namespace Payment.Application.Payments;

public sealed record CreatePaymentCommand(
    string MerchantReference,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string ReturnUrl,
    string CancelUrl,
    string IdempotencyKey,
    string? SimulatedOutcome = null
);
