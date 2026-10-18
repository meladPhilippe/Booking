# Sprint 3 — Stripe Embedded Checkout Implementation

## Delivered design

Card payments use Stripe Checkout Sessions in `payment` + `embedded` mode. `POST /api/v1/payments` now requires an allow-listed HTTPS `returnUrl` and returns a provider-neutral `nextAction` object with `type: embedded_checkout` and a Checkout Session client secret. The consuming frontend mounts that secret with Stripe.js; this module never accepts card data.

Stripe SDK usage is isolated in `Payment.Infrastructure/Providers/Stripe`. The module sends Stripe the local payment ID as both `client_reference_id` and safe metadata, uses it as Stripe's outbound idempotency key, and saves the `cs_...` Session ID as the provider reference.

## Configuration

Provide these secrets through environment variables or the deployment secret store; do not commit them:

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "WebhookSigningSecret": "whsec_...",
  "PublishableKey": "pk_test_...",
  "ReturnUrlOrigins": [ "https://merchant.example" ]
}
```

Outside Development, startup requires a secret key, webhook signing secret, and at least one allowed return-url origin. Development continues to use the simulated provider.

## Webhooks and state

`POST /api/v1/webhooks/stripe` reads the raw body, validates `Stripe-Signature`, parses a thin event, retrieves the current Stripe Checkout Session or PaymentIntent, and maps it to a local outcome. Webhook events are deduplicated in `ProviderWebhookEvents` by `(Provider, ExternalEventId)`; raw payloads and secrets are never stored.

Handled events: `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `checkout.session.expired`, `payment_intent.succeeded`, `payment_intent.payment_failed`, `payment_intent.processing`, and `payment_intent.requires_action`. Legal domain transitions prevent stale events from moving a terminal payment backwards.

## Sandbox demo

1. Configure Stripe test credentials and permitted frontend return origin.
2. Start SQL Server and the API, then run `stripe listen --forward-thin-to localhost:<port>/api/v1/webhooks/stripe --thin-events "*"`; place the printed signing secret in local secret configuration.
3. Create a Card payment with `Idempotency-Key` and `returnUrl`; use the returned `nextAction.clientSecret` with `stripe.initEmbeddedCheckout`.
4. Complete a successful test-card checkout, then query the module payment and verify the webhook updated it to `Succeeded`.
5. Repeat with a Stripe declined test card and verify `Failed`; replay the original module request and verify it does not create another Checkout Session.

## References and deferred work

- [Stripe Checkout Session API](https://docs.stripe.com/api/checkout/sessions/create)
- [Stripe webhook verification for .NET](https://docs.stripe.com/webhooks?lang=dotnet)
- [Stripe test cards and sandbox testing](https://docs.stripe.com/testing?numbers-or-method-or-token=tokens)

Wallets, saved cards, subscriptions, manual capture, refunds, automatic reconciliation, and public authentication remain later-sprint work.
