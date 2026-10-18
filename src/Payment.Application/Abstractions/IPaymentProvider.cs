using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Abstractions;

public sealed record ProviderPaymentRequest(
    Guid PaymentId,
    string MerchantReference,
    Money Amount,
    long AmountInMinorUnits,
    PaymentMethod Method,
    string ReturnUrl,
    string CancelUrl,
    string? SimulatedOutcome
);

public sealed record ProviderNextAction(string Type, IReadOnlyDictionary<string, string> Data);

public sealed record ProviderPaymentOutcome(
    PaymentStatus Status,
    string? ProviderReference,
    ProviderNextAction? NextAction = null
);

public interface IPaymentProvider
{
    string Name { get; }
    bool Supports(PaymentMethod method);
    Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    );
    Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerReference,
        CancellationToken cancellationToken
    );
}
