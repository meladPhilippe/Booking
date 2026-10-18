using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Logging;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Providers.Stripe;

/// <summary>
/// Verifies and applies Stripe webhook notifications to local payment state.
/// </summary>
/// <remarks>
/// Signature verification uses the unmodified payload and <c>Stripe-Signature</c> header.
/// Thin events are followed by a Stripe API read of the related Session or PaymentIntent. Events
/// are persisted and deduplicated before local state is changed. See
/// <see href="https://docs.stripe.com/webhooks?lang=dotnet">Receive Stripe events in .NET</see>.
/// </remarks>
public sealed class StripeWebhookHandler(StripeOptions options, IPaymentRepository repository, TimeProvider clock, ILogger<StripeWebhookHandler> logger)
{
    /// <summary>
    /// Authenticates a Stripe event, resolves the current provider outcome, deduplicates its event ID,
    /// and applies only a legal payment-state transition.
    /// </summary>
    /// <exception cref="StripeWebhookSignatureException">Thrown when Stripe signature verification fails.</exception>
    public async Task HandleAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        try
        {
            new StripeClient(options.SecretKey).ParseEventNotification(payload, signature, options.WebhookSigningSecret);
        }
        catch (StripeException exception)
        {
            throw new StripeWebhookSignatureException(exception);
        }
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? throw new StripeException("Stripe event is missing an id.");
        var eventType = root.GetProperty("type").GetString() ?? throw new StripeException("Stripe event is missing a type.");
        var relatedId = GetRelatedObjectId(root);
        var (paymentId, status) = await ResolveOutcomeAsync(eventType, relatedId, cancellationToken);
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            logger.LogWarning("Stripe event {StripeEventId} references unknown local payment {PaymentId}", eventId, paymentId);
            await repository.TryRecordWebhookEventAsync(ProviderWebhookEvent.Create("Stripe", eventId, eventType, relatedId, clock.GetUtcNow()), cancellationToken);
            return;
        }
        if (!await repository.TryRecordWebhookEventAsync(ProviderWebhookEvent.Create("Stripe", eventId, eventType, relatedId, clock.GetUtcNow()), cancellationToken))
            return;
        if (payment.Status != status && DomainPayment.CanTransition(payment.Status, status))
        {
            payment.ApplyOutcome(status, null, clock.GetUtcNow());
            await repository.UpdateAsync(payment, cancellationToken);
        }
        logger.LogInformation("Processed Stripe event {StripeEventId} for payment {PaymentId}", eventId, payment.Id);
    }

    /// <summary>
    /// Fetches the current related Stripe resource because thin-event payloads do not contain a full
    /// authoritative resource snapshot.
    /// </summary>
    private async Task<(Guid PaymentId, PaymentStatus Status)> ResolveOutcomeAsync(string eventType, string relatedId, CancellationToken cancellationToken)
    {
        var client = new StripeClient(options.SecretKey);
        if (eventType.StartsWith("checkout.session.", StringComparison.Ordinal))
        {
            var session = await new SessionService(client).GetAsync(relatedId, cancellationToken: cancellationToken);
            var paymentId = ParsePaymentId(session.ClientReferenceId ?? GetMetadata(session.Metadata));
            var status = eventType switch
            {
                "checkout.session.completed" when session.PaymentStatus == "paid" => PaymentStatus.Succeeded,
                "checkout.session.async_payment_succeeded" => PaymentStatus.Succeeded,
                "checkout.session.async_payment_failed" => PaymentStatus.Failed,
                "checkout.session.expired" => PaymentStatus.Cancelled,
                _ => PaymentStatus.Processing
            };
            return (paymentId, status);
        }
        var intent = await new PaymentIntentService(client).GetAsync(relatedId, cancellationToken: cancellationToken);
        return (ParsePaymentId(GetMetadata(intent.Metadata)), eventType switch
        {
            "payment_intent.succeeded" => PaymentStatus.Succeeded,
            "payment_intent.payment_failed" => PaymentStatus.Failed,
            "payment_intent.requires_action" => PaymentStatus.RequiresAction,
            _ => PaymentStatus.Processing
        });
    }

    private static string GetRelatedObjectId(JsonElement root)
    {
        if (root.TryGetProperty("related_object", out var related) && related.TryGetProperty("id", out var id)) return id.GetString() ?? throw new StripeException("Related object id is missing.");
        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("object", out var obj) && obj.TryGetProperty("id", out var objectId)) return objectId.GetString() ?? throw new StripeException("Related object id is missing.");
        throw new StripeException("Stripe event has no related object id.");
    }

    private static string? GetMetadata(Dictionary<string, string>? metadata) => metadata is not null && metadata.TryGetValue("payment_id", out var paymentId) ? paymentId : null;
    private static Guid ParsePaymentId(string? value) => Guid.TryParse(value, out var paymentId) ? paymentId : throw new StripeException("Stripe object is not correlated to a payment.");
}
