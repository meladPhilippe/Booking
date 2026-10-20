# Sprint 3 — Stripe Integration

**Sprint goal:** Complete a sandbox-backed Stripe card payment end to end, including asynchronous confirmation and safe status updates.

## PAY-010 — Implement Stripe provider adapter for card checkout
**Priority:** Must

**Story**
As a customer, I want to pay by card through Stripe's supported checkout flow, so that my payment can be processed without the module handling raw card data.

**Acceptance criteria**
- Stripe SDK/API calls live only in the Stripe infrastructure adapter.
- The application workflow creates the provider operation using the persisted local payment/idempotency context.
- Stripe is selected for a new Card payment by configuration, but every later Stripe operation is selected by the payment's persisted `ProviderKey` (`stripe`), never by the current Card route.
- The checkout response contains only the provider-approved redirect/client-action information the client needs.
- Provider IDs are stored as provider references and are not substituted for the module's payment ID.
- Declines, validation errors, provider unavailability, and uncertain responses map to documented provider-neutral outcomes.
- Sandbox tests demonstrate a successful flow and a declined/failed flow without moving real money.
- No PAN/CVV or secret key is persisted or logged.

## PAY-011 — Process Stripe webhooks safely
**Priority:** Must

**Story**  
As the payment module, I want to receive authenticated Stripe payment events, so that the local payment state reflects provider-confirmed outcomes even when the browser does not return or the original request times out.

**Acceptance criteria**
- The endpoint verifies the provider's webhook signature using the documented verification mechanism and configured secret.
- Invalid signatures are rejected before the event changes payment state.
- Event identity is persisted/deduplicated so a repeated delivery does not repeat the business effect.
- The handler maps provider events to provider-neutral outcomes and applies only valid state transitions.
- Duplicate and out-of-order events do not move a payment backwards from a later valid state.
- Processing is concurrency-safe and safely retryable if local persistence temporarily fails.
- Sandbox/fixture tests cover valid signature, invalid signature, duplicate event, unknown payment reference, and out-of-order event.

## PAY-012 — Add Stripe configuration, health visibility, and diagnostics
**Priority:** Must

**Story**  
As an operator, I want Stripe configuration validated and integration failures diagnosable, so that a misconfigured integration is not mistaken for a customer payment decline.

**Acceptance criteria**
- API keys and webhook secrets are provided through approved secret configuration, never committed in source.
- Startup/configuration validation detects missing mandatory configuration in the relevant environment.
- Logs include correlation/payment IDs and provider request/correlation IDs when safe, but redact secrets and payment-sensitive data.
- Provider operational errors are distinguishable from customer declines in logs/metrics.
- Health/readiness behaviour reflects required local dependencies and configuration; a transient provider outage does not falsely mark every existing payment as failed.

## PAY-013 — Add a Stripe Sandbox Provider
**Priority:** Must

**Story**
As a developer, I want a selectable Stripe sandbox provider for card payments and refunds, so that I can validate the real Stripe integration safely without handling card data or affecting live payments.

**Acceptance criteria**
- `StripeProvider` implements initiation plus only the optional capabilities it supports: refund and payment/refund reconciliation; embedded Checkout does not require a generic capture capability.
- It uses Stripe test-mode embedded Checkout, local payment/refund IDs as durable idempotency keys, and provider-neutral outcomes; Checkout Session (`ProviderOperationId`) and PaymentIntent (`ProviderPaymentId`) references are persisted separately.
- If creation times out before the Checkout Session ID is received, recovery can safely repeat the original request using its persisted Stripe idempotency key and recover the original external operation.
- Full and partial refunds use the persisted PaymentIntent reference and are safely idempotent.
- Sandbox startup requires an `sk_test_` key, webhook signing secret, and allowed return origins; no credential or card data is committed, persisted, or logged.
- Verified sandbox webhooks are deduplicated under the sandbox provider identity and apply only legal payment/refund state transitions.
- Automated tests cover provider selection, checkout outcomes, refunds, webhook deduplication, and configuration rejection.

## PAY-014 — Add an integrated Stripe sandbox checkout demo
**Priority:** Should

**Story**
As a developer, I want a local HTTPS checkout demo that uses the payment API and Stripe Embedded Checkout, so that I can validate sandbox payments, webhook outcomes, and refunds in a browser without exposing secret credentials.

**Acceptance criteria**
- The existing API serves a Development-only HTTPS demo using only vanilla browser assets and the Stripe test publishable key.
- The demo creates a Card payment through the existing API, mounts only an `embedded_checkout` next action, and shows the local payment status until it reaches a terminal state.
- It shows a full or partial refund control only for a succeeded payment and displays the resulting refund status.
- A Development-only client-configuration endpoint returns only `pk_test_...` while the Sandbox Stripe profile is selected; it never returns API or webhook secrets.
- The checked-in configuration contains no Stripe credentials and documents environment-variable setup, the local development certificate, and Stripe CLI forwarding.

**Sprint 3 demo:** With `PaymentRouting:Methods:Card=stripe`, the Sandbox Stripe profile selected, and test credentials supplied through a secret store, a test client starts an embedded Stripe checkout; a verified webhook makes the payment status accurate for success and decline, then a full or partial test-mode refund is issued and observed through the API.

---
