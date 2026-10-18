# Sprint 3 — Stripe Integration

**Sprint goal:** Complete a sandbox-backed Stripe card payment end to end, including asynchronous confirmation and safe status updates.

## PAY-010 — Implement Stripe provider adapter for card checkout
**Priority:** Must

**Story**  
As a customer, I want to pay by card through Stripe's supported checkout flow, so that my payment can be processed without the module handling raw card data.

**Acceptance criteria**
- Stripe SDK/API calls live only in the Stripe infrastructure adapter.
- The application workflow creates the provider operation using the persisted local payment/idempotency context.
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

**Sprint 3 demo:** A test client starts a Stripe sandbox card checkout; the resulting payment status becomes accurate through the API and/or a verified webhook, including a failed/declined scenario.

---
