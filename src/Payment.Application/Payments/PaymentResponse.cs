using Payment.Domain.Enums;
using Payment.Application.Abstractions;

namespace Payment.Application.Payments;

public sealed record PaymentResponse(
    Guid PaymentId,
    string MerchantReference,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string Provider,
    string? ProviderReference,
    PaymentStatus Status,
    ProviderNextAction? NextAction,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
