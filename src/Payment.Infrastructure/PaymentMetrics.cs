using System.Diagnostics.Metrics;

namespace Payment.Infrastructure;

public sealed class PaymentMetrics
{
    public const string MeterName = "Booking.Payment";
    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _providerFailures;
    private readonly Counter<long> _outboxDeliveries;
    private readonly Counter<long> _outboxFailures;
    private readonly Counter<long> _paymentOutcomes;
    private readonly Counter<long> _refundOutcomes;
    private readonly Counter<long> _webhookEvents;
    private readonly Histogram<double> _outboxAge;

    public PaymentMetrics()
    {
        _providerFailures = _meter.CreateCounter<long>("payment_provider_failures_total");
        _outboxDeliveries = _meter.CreateCounter<long>("payment_outbox_deliveries_total");
        _outboxFailures = _meter.CreateCounter<long>("payment_outbox_failures_total");
        _paymentOutcomes = _meter.CreateCounter<long>("payment_outcomes_total");
        _refundOutcomes = _meter.CreateCounter<long>("payment_refund_outcomes_total");
        _webhookEvents = _meter.CreateCounter<long>("payment_webhook_events_total");
        _outboxAge = _meter.CreateHistogram<double>("payment_outbox_age_seconds");
    }

    public void ProviderFailure(string provider, string operation, string reason) =>
        _providerFailures.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", reason));

    public void OutboxDelivered(string eventType, TimeSpan age)
    {
        _outboxDeliveries.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        _outboxAge.Record(age.TotalSeconds, new KeyValuePair<string, object?>("event_type", eventType));
    }

    public void OutboxFailed(string eventType, string reason, TimeSpan age)
    {
        _outboxFailures.Add(1, new KeyValuePair<string, object?>("event_type", eventType), new KeyValuePair<string, object?>("reason", reason));
        _outboxAge.Record(age.TotalSeconds, new KeyValuePair<string, object?>("event_type", eventType));
    }

    public void PaymentOutcome(string provider, string status) =>
        _paymentOutcomes.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("status", status));

    public void RefundOutcome(string status) =>
        _refundOutcomes.Add(1, new KeyValuePair<string, object?>("status", status));

    public void Webhook(string provider, string result) =>
        _webhookEvents.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("result", result));
}
