using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Abstractions;

public sealed record ProviderPaymentRequest(
    Guid PaymentId,
    string MerchantReference,
    Money Amount,
    long AmountInMinorUnits,
    PaymentMethod Method,
    string ReturnUrl,
    string CancelUrl,
    string? SimulatedOutcome
);
