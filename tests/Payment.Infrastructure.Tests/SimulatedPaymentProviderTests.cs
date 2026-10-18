using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure;
using Payment.Infrastructure.Providers;

namespace Payment.Infrastructure.Tests;

public sealed class SimulatedPaymentProviderTests
{
    [Theory]
    [InlineData("Succeeded", PaymentStatus.Succeeded)]
    [InlineData("Declined", PaymentStatus.Failed)]
    [InlineData("RequiresAction", PaymentStatus.RequiresAction)]
    [InlineData("Timeout", PaymentStatus.RequiresReconciliation)]
    public async Task Maps_controlled_outcomes(string selectedOutcome, PaymentStatus expected)
    {
        var provider = new SimulatedPaymentProvider(new SimulationOptions { DefaultOutcome = "Succeeded" });
        var outcome = await provider.CreateAsync(new ProviderPaymentRequest(Guid.NewGuid(), "test", Money.Create(10m, "EGP", 2), 1000, PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", selectedOutcome), CancellationToken.None);
        Assert.Equal(expected, outcome.Status);
    }
}
