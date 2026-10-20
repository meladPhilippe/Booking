using Payment.Domain.Entities;

namespace Payment.Application.Abstractions;

public sealed record RefundClaimResult(IdempotentCreateKind Kind, Refund? Refund = null);
