# Sprint 5 — Refunds, Reconciliation, and Outcome Delivery

Sprint 5 completes the core lifecycle with tracked provider refunds, read-only reconciliation, and durable notifications for the consuming application.

## Implemented scope

- `POST /api/v1/payments/{paymentId}/refunds` requires `Idempotency-Key`, supports full or partial amounts, and returns a separate refund resource.
- Refunds reserve their amount before the provider call, so concurrent requests cannot exceed the original captured amount. Duplicate keys return the original operation without issuing a second refund.
- `Refunds`, `OutcomeOutboxMessages`, `Payments.ProviderPaymentReference`, and `IdempotencyOperations.RefundId` are persisted in migration `202610180005_AddRefundsAndOutbox`.
- Stripe persists its PaymentIntent ID; PayPal persists its captured-payment ID. Those are the provider references required for refunding—not the Checkout Session or PayPal Order IDs.
- `POST /api/v1/payments/{paymentId}/reconcile` performs a read-only provider lookup only for `RequiresReconciliation` payments. It never creates another charge.
- Provider webhook handlers process refund updates as separate records and retain inbox deduplication.
- Payment/refund state changes create provider-neutral JSON events in a transactional SQL outbox. The dispatcher posts the exact event body to the configured HTTPS endpoint with timestamped HMAC-SHA256 headers and retries non-2xx results with bounded exponential backoff.

## Configuration

```json
"OutcomeDelivery": {
  "Endpoint": "https://merchant.example/api/payment-outcomes",
  "SigningSecret": "from-approved-secret-store"
}
```

The dispatcher is inactive until both values are configured. Secrets, client secrets, raw provider webhook bodies, card data, and provider credentials are never placed in the outbox.

## Provider references

Stripe creates refunds against a PaymentIntent and supports partial refunds up to the remaining amount. Subscribe to `refund.created`, `refund.updated`, and `refund.failed` in addition to the payment events. [Stripe Create a Refund](https://docs.stripe.com/api/refunds/create) and [Stripe event types](https://docs.stripe.com/api/events/types).

PayPal refunds a captured payment, not an Order. The successful capture response supplies `purchase_units.payments.captures.id`; the module stores that ID before refunding with deterministic `PayPal-Request-Id`. [PayPal Refund captured payment](https://developer.paypal.com/api/payments/v2/captures-refund) and [PayPal capture response guidance](https://developer.paypal.com/api/orders-v2/track-orders/use/).

## Deferred work

Cancellation/void remains deferred. Stripe Checkout must expire an open Checkout Session rather than cancel its underlying PaymentIntent, while PayPal void applies to authorization flows outside the current immediate-capture scope. Provider sandbox end-to-end verification and authenticated merchant/operations authorization remain release-readiness work.
