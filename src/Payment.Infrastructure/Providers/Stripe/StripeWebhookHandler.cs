using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Stripe;
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
public sealed class StripeWebhookHandler(
    IStripeGateway gateway,
    IPaymentRepository repository,
    TimeProvider clock,
    ILogger<StripeWebhookHandler> logger
)
{
    /// <summary>
    /// Authenticates a Stripe event, resolves the current provider outcome, deduplicates its event ID,
    /// and applies only a legal payment-state transition.
    /// </summary>
    /// <exception cref="StripeWebhookSignatureException">Thrown when Stripe signature verification fails.</exception>
    public async Task HandleAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken
    )
    {
        try
        {
            gateway.VerifyWebhook(payload, signature);
        }
        catch (StripeException exception)
        {
            throw new StripeWebhookSignatureException(exception);
        }
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId =
            root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Stripe event is missing an id.");
        var eventType =
            root.GetProperty("type").GetString()
            ?? throw new InvalidOperationException("Stripe event is missing a type.");
        var relatedId = GetRelatedObjectId(root);
        if (!IsSupportedEvent(eventType))
        {
            await repository.TryRecordWebhookEventAsync(
                ProviderWebhookEvent.Create(
                    "stripe",
                    eventId,
                    eventType,
                    relatedId,
                    clock.GetUtcNow()
                ),
                cancellationToken
            );
            logger.LogDebug(
                "Ignored unsupported Stripe event {StripeEventId} of type {StripeEventType}",
                eventId,
                eventType
            );
            return;
        }
        if (eventType.StartsWith("refund.", StringComparison.Ordinal))
        {
            await HandleRefundAsync(eventId, eventType, relatedId, cancellationToken);
            return;
        }
        var (paymentId, status, providerPaymentReference) = await ResolveOutcomeAsync(
            eventType,
            relatedId,
            cancellationToken
        );
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            logger.LogWarning(
                "Stripe event {StripeEventId} references unknown local payment {PaymentId}",
                eventId,
                paymentId
            );
            await repository.TryRecordWebhookEventAsync(
                ProviderWebhookEvent.Create(
                    "stripe",
                    eventId,
                    eventType,
                    relatedId,
                    clock.GetUtcNow()
                ),
                cancellationToken
            );
            return;
        }
        if (
            !await repository.TryRecordWebhookEventAsync(
                ProviderWebhookEvent.Create(
                    "stripe",
                    eventId,
                    eventType,
                    relatedId,
                    clock.GetUtcNow()
                ),
                cancellationToken
            )
        )
            return;
        if (payment.Status != status && DomainPayment.CanTransition(payment.Status, status))
        {
            payment.ApplyOutcome(status, null, clock.GetUtcNow(), providerPaymentReference);
            await repository.UpdateAsync(payment, cancellationToken);
        }
        logger.LogInformation(
            "Processed Stripe event {StripeEventId} for payment {PaymentId}",
            eventId,
            payment.Id
        );
    }

    private async Task HandleRefundAsync(
        string eventId,
        string eventType,
        string relatedId,
        CancellationToken cancellationToken
    )
    {
        var providerRefund = await gateway.GetRefundAsync(relatedId, cancellationToken);
        var refundIdText = GetMetadata(providerRefund.Metadata);
        if (!Guid.TryParse(refundIdText, out var refundId))
        {
            await repository.TryRecordWebhookEventAsync(
                ProviderWebhookEvent.Create(
                    "stripe",
                    eventId,
                    eventType,
                    relatedId,
                    clock.GetUtcNow()
                ),
                cancellationToken
            );
            return;
        }
        if (
            !await repository.TryRecordWebhookEventAsync(
                ProviderWebhookEvent.Create(
                    "stripe",
                    eventId,
                    eventType,
                    relatedId,
                    clock.GetUtcNow()
                ),
                cancellationToken
            )
        )
            return;
        var refund = await repository.GetRefundByIdAsync(refundId, cancellationToken);
        if (refund is null)
            return;
        var status = StripeProvider.MapRefund(providerRefund).Status;
        if (
            refund.Status != status
            && Payment.Domain.Entities.Refund.CanTransition(refund.Status, status)
        )
        {
            refund.ApplyOutcome(status, providerRefund.Id, clock.GetUtcNow());
            await repository.UpdateRefundAsync(refund, cancellationToken);
        }
    }

    /// <summary>
    /// Fetches the current related Stripe resource because thin-event payloads do not contain a full
    /// authoritative resource snapshot.
    /// </summary>
    private async Task<(
        Guid PaymentId,
        PaymentStatus Status,
        string? ProviderPaymentReference
    )> ResolveOutcomeAsync(string eventType, string relatedId, CancellationToken cancellationToken)
    {
        if (eventType.StartsWith("checkout.session.", StringComparison.Ordinal))
        {
            var session = await gateway.GetCheckoutSessionAsync(relatedId, cancellationToken);
            var paymentId = ParsePaymentId(
                session.ClientReferenceId ?? GetMetadata(session.Metadata)
            );
            var status = eventType switch
            {
                "checkout.session.completed" when session.PaymentStatus == "paid" =>
                    PaymentStatus.Succeeded,
                "checkout.session.async_payment_succeeded" => PaymentStatus.Succeeded,
                "checkout.session.async_payment_failed" => PaymentStatus.Failed,
                "checkout.session.expired" => PaymentStatus.Cancelled,
                _ => PaymentStatus.Processing,
            };
            return (paymentId, status, session.PaymentIntentId);
        }
        var intent = await gateway.GetPaymentIntentAsync(relatedId, cancellationToken);
        return (
            ParsePaymentId(GetMetadata(intent.Metadata)),
            eventType switch
            {
                "payment_intent.succeeded" => PaymentStatus.Succeeded,
                "payment_intent.payment_failed" => PaymentStatus.Failed,
                "payment_intent.requires_action" => PaymentStatus.RequiresAction,
                _ => PaymentStatus.Processing,
            },
            intent.Id
        );
    }

    private static string GetRelatedObjectId(JsonElement root)
    {
        if (
            root.TryGetProperty("related_object", out var related)
            && related.TryGetProperty("id", out var id)
        )
            return id.GetString()
                ?? throw new InvalidOperationException(
                    "Stripe event related object is missing an id."
                );
        if (
            root.TryGetProperty("data", out var data)
            && data.TryGetProperty("object", out var obj)
            && obj.TryGetProperty("id", out var objectId)
        )
            return objectId.GetString()
                ?? throw new InvalidOperationException(
                    "Stripe event related object is missing an id."
                );
        throw new InvalidOperationException("Stripe event has no related object id.");
    }

    private static bool IsSupportedEvent(string eventType) =>
        eventType
            is "checkout.session.completed"
                or "checkout.session.async_payment_succeeded"
                or "checkout.session.async_payment_failed"
                or "checkout.session.expired"
                or "payment_intent.succeeded"
                or "payment_intent.payment_failed"
                or "payment_intent.processing"
                or "payment_intent.requires_action"
                or "refund.created"
                or "refund.updated";

    private static string? GetMetadata(IReadOnlyDictionary<string, string>? metadata) =>
        metadata is not null && metadata.TryGetValue("payment_id", out var paymentId)
            ? paymentId
            : null;

    private static Guid ParsePaymentId(string? value) =>
        Guid.TryParse(value, out var paymentId)
            ? paymentId
            : throw new InvalidOperationException("Stripe object is not correlated to a payment.");
}
