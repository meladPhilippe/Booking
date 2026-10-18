# Sprint 4 — PayPal Integration

**Sprint goal:** Add PayPal without rewriting the application workflow or leaking PayPal concepts into the domain/application API.

## PAY-013 — Implement PayPal checkout and capture adapter
**Priority:** Must

**Story**  
As a customer, I want to pay through PayPal, so that the module supports a second real payment option through the same provider-neutral contract.

**Acceptance criteria**
- PayPal API/SDK types are isolated in the PayPal infrastructure adapter.
- The flow supports the required create/approval/return/capture steps for the chosen PayPal integration.
- The API represents customer action/redirect requirements in a provider-neutral response shape.
- The local payment operation and idempotency context are persisted before external operations that can move money.
- Approval is not treated as successful capture unless the chosen flow's confirmed semantics say the funds have been captured.
- PayPal decline, expiration, timeout, provider error, and success are mapped to documented neutral outcomes.
- Sandbox end-to-end tests demonstrate successful and unsuccessful payment flows.

## PAY-014 — Process PayPal webhooks safely
**Priority:** Must

**Story**  
As the payment module, I want to authenticate and process PayPal webhook notifications, so that delayed or asynchronous PayPal outcomes update the correct payment safely.

**Acceptance criteria**
- Webhook authenticity is verified using PayPal's supported verification mechanism.
- Event deduplication, correlation to the local payment, state-transition validation, and concurrency protection use the shared webhook-processing rules.
- Duplicate/out-of-order events do not double-apply payment or refund effects.
- An uncorrelated event is logged safely and handled according to a documented operational policy; it does not create an arbitrary payment record.
- Tests cover valid/invalid verification, duplicated notifications, and status ordering.

## PAY-015 — Resolve method/provider independently and validate capability support
**Priority:** Must

**Story**  
As the payment module, I want payment-method selection separated from provider implementation, so that the core workflow can support more providers later without being rewritten.

**Acceptance criteria**
- A dedicated resolver selects the requested method strategy using a stable identifier.
- Initial configuration explicitly maps supported methods to Stripe or PayPal; no silent fallback to a different method/provider occurs.
- The method and provider are recorded separately where they represent different concepts.
- Unsupported method/provider/capability combinations return a controlled error before attempting an unsupported operation.
- Adding a new provider implementation does not require adding provider-specific branches to the main application workflow.

**Sprint 4 demo:** The same create/status API supports a Stripe card checkout and PayPal checkout; both are backed by real sandbox integrations and map results into the shared payment state model.

---
