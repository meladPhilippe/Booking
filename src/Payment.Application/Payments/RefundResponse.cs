using Payment.Domain.Enums;

namespace Payment.Application.Payments;

public sealed record RefundResponse(Guid RefundId, Guid PaymentId, decimal Amount, string Currency, RefundStatus Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
