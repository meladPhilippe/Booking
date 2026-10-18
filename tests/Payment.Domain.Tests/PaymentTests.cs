using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Domain.Tests;

public sealed class PaymentTests
{
    [Fact]
    public void Pending_payment_can_succeed()
    {
        var payment = DomainPayment.Create("merchant", "booking-1", Money.Create(100m, "EGP", 2), PaymentMethod.Card, "Simulated", DateTimeOffset.UtcNow);
        payment.ApplyOutcome(PaymentStatus.Succeeded, "sim_1", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void Terminal_payment_cannot_transition()
    {
        var payment = DomainPayment.Create("merchant", "booking-1", Money.Create(100m, "EGP", 2), PaymentMethod.Card, "Simulated", DateTimeOffset.UtcNow);
        payment.ApplyOutcome(PaymentStatus.Succeeded, "sim_1", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => payment.ApplyOutcome(PaymentStatus.Failed, null, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Money_rejects_non_positive_values(decimal amount) => Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(amount, "EGP", 2));

    [Fact]
    public void Money_rejects_invalid_precision() => Assert.Throws<ArgumentException>(() => Money.Create(10.123m, "EGP", 2));
}
