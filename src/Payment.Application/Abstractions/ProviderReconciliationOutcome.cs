using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public sealed record ProviderReconciliationOutcome(
    PaymentStatus Status,
    string? ProviderPaymentReference = null
);
