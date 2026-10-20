using Payment.Domain.ValueObjects;

namespace Payment.Application.Abstractions;

public sealed record ProviderRefundRequest(
    Guid PaymentId,
    Guid RefundId,
    string ProviderPaymentReference,
    Money Amount,
    long AmountInMinorUnits
);
