using Payment.Application.Abstractions;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Providers;

public sealed class SimulatedPaymentProvider(SimulationOptions options) : IPaymentProvider
{
    public string Name => "Simulated";

    public bool Supports(PaymentMethod method) => method == PaymentMethod.Card;

    public Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var selected = request.SimulatedOutcome ?? options.DefaultOutcome;
        var outcome = selected.Trim().ToLowerInvariant() switch
        {
            "succeeded" or "success" => new ProviderPaymentOutcome(
                PaymentStatus.Succeeded,
                $"sim_{request.PaymentId:N}"
            ),
            "declined" or "failed" => new ProviderPaymentOutcome(
                PaymentStatus.Failed,
                $"sim_{request.PaymentId:N}"
            ),
            "requiresaction" or "requires_action" => new ProviderPaymentOutcome(
                PaymentStatus.RequiresAction,
                $"sim_{request.PaymentId:N}",
                new ProviderNextAction(
                    "simulated",
                    new Dictionary<string, string> { ["token"] = "simulated-client-secret" }
                )
            ),
            "timeout" or "unknown" or "indeterminate" => new ProviderPaymentOutcome(
                PaymentStatus.RequiresReconciliation,
                null
            ),
            _ => throw new InvalidOperationException(
                "Simulation outcome must be Succeeded, Declined, RequiresAction, Timeout, or Unknown."
            ),
        };
        return Task.FromResult(outcome);
    }

    public Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerReference,
        CancellationToken cancellationToken
    ) => Task.FromResult(new ProviderPaymentOutcome(PaymentStatus.Succeeded, providerReference));
}
