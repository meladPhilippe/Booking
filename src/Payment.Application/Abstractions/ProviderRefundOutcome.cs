using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public sealed record ProviderRefundOutcome(RefundStatus Status, string? ProviderReference);
