using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public sealed record ProviderPaymentOutcome(
    PaymentStatus Status,
    string? ProviderReference,
    ProviderNextAction? NextAction = null,
    string? ProviderPaymentReference = null
);
