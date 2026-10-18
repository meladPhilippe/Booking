using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using DomainPaymentMethod = Payment.Domain.Enums.PaymentMethod;

namespace Payment.Infrastructure.Providers.Stripe;

/// <summary>
/// Stripe-only adapter that creates embedded Checkout Sessions for Card payments.
/// </summary>
/// <remarks>
/// Stripe SDK types and the secret key stay in Infrastructure. The local payment ID is used for
/// Stripe request idempotency and reconciliation metadata; the returned Checkout Session ID is
/// stored as the provider reference. See <see href="https://docs.stripe.com/api/checkout/sessions/create">Create a Checkout Session</see>
/// and <see href="https://docs.stripe.com/api/idempotent_requests">Stripe idempotent requests</see>.
/// </remarks>
public sealed class StripePaymentProvider(StripeOptions options) : IPaymentProvider
{
    public string Name => "Stripe";

    public bool Supports(DomainPaymentMethod method) => method == DomainPaymentMethod.Card;

    /// <summary>
    /// Creates a one-time, embedded, card-only Checkout Session and returns the client secret as
    /// a provider-neutral customer action.
    /// </summary>
    public async Task<ProviderPaymentOutcome> CreateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var service = new SessionService(new StripeClient(options.SecretKey));
        try
        {
            var session = await service.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    UiMode = "embedded",
                    ReturnUrl = request.ReturnUrl,
                    ClientReferenceId = request.PaymentId.ToString(),
                    PaymentMethodTypes = ["card"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["payment_id"] = request.PaymentId.ToString(),
                    },
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            ["payment_id"] = request.PaymentId.ToString(),
                        },
                    },
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Amount.Currency.ToLowerInvariant(),
                                UnitAmount = request.AmountInMinorUnits,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.MerchantReference,
                                },
                            },
                        },
                    ],
                },
                new RequestOptions { IdempotencyKey = request.PaymentId.ToString("N") },
                cancellationToken
            );
            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                return new ProviderPaymentOutcome(PaymentStatus.RequiresReconciliation, session.Id);
            return new ProviderPaymentOutcome(
                PaymentStatus.RequiresAction,
                session.Id,
                new ProviderNextAction(
                    "embedded_checkout",
                    new Dictionary<string, string> { ["clientSecret"] = session.ClientSecret }
                )
            );
        }
        catch (StripeException exception)
            when (exception.StripeError?.Type is "card_error" or "invalid_request_error")
        {
            return new ProviderPaymentOutcome(PaymentStatus.Failed, null);
        }
    }

    /// <summary>
    /// Rejects the PayPal-specific capture capability; Stripe Checkout confirms its own payment flow.
    /// </summary>
    public Task<ProviderPaymentOutcome> CaptureAsync(
        Guid paymentId,
        string providerReference,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Stripe embedded checkout does not use the capture flow.");
}
