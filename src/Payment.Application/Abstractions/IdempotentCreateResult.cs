using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Application.Abstractions;

public sealed record IdempotentCreateResult(
    IdempotentCreateKind Kind,
    DomainPayment? Payment = null
);
