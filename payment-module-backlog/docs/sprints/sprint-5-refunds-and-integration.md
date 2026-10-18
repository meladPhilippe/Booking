# Sprint 5 — Refunds, Reconciliation, and Application Integration

**Sprint goal:** Complete the primary payment lifecycle and make delayed/uncertain outcomes recoverable and usable by the consuming application.

## PAY-016 — Process full and partial refunds
**Priority:** Must

**Story**  
As an authorized consuming application, I want to request full or partial refunds for eligible payments, so that the customer can receive money back without the host application calling provider APIs directly.

**Acceptance criteria**
- Refund requests reference the module payment ID, amount, and an idempotency key.
- The domain validates that the payment is eligible for a refund and that the requested amount does not exceed the remaining refundable amount.
- Refunds are modeled/tracked as separate operations or records, including provider reference, amount, currency, status, and timestamps as needed.
- Only provider/payment combinations with the required refund capability are offered the operation.
- A refund request is not reported as completed until the provider's confirmed outcome meets the agreed semantics; pending outcomes remain pending.
- Duplicate refund requests do not refund the same amount twice.
- Sandbox/fixture tests cover full refund, partial refund, exceeding the remaining amount, duplicate requests, provider failure, and asynchronous confirmation where applicable.

## PAY-017 — Reconcile indeterminate payment outcomes
**Priority:** Must

**Story**  
As an operator/system, I want an indeterminate payment to be checked against the provider's authoritative state, so that the module can recover safely after timeouts or missed webhooks.

**Acceptance criteria**
- Payments in an agreed indeterminate/stale state are queryable by a reconciliation process or controlled operations endpoint.
- The reconciler uses provider references and supported provider lookup APIs; it does not create a new charge as a way to discover the original outcome.
- Reconciliation is idempotent and cannot overwrite a newer valid state with an older result.
- Retries use bounded/backoff policies and distinguish transient transport errors from definitive provider outcomes.
- Exhausted or ambiguous cases are visible for operational intervention rather than silently marked failed.
- Tests cover provider timeout after a successful charge, lost webhook followed by reconciliation, and concurrent webhook/reconciliation updates.

## PAY-018 — Define payment outcome propagation for the chosen host model
**Priority:** Must for defining the contract; delivery adapter depends on the deployment/integration model

**Story**  
As a consuming business application, I want to learn reliably when a payment outcome changes, so that I can confirm a reservation, issue a ticket, or continue order processing without embedding provider-specific logic in my business workflow.

**Acceptance criteria**
- A provider-neutral payment outcome contract identifies the payment ID, merchant/client scope, merchant reference, event ID, status, and relevant timestamps without exposing secrets or sensitive payment data.
- Initiating a payment, querying its current status, receiving inbound provider webhooks, and propagating outcomes to the business application are documented as distinct interactions.
- The outcome propagation adapter is selected for the host model: an in-process application event/handler for a modular monolith; or a durable message/event delivery mechanism or signed outbound webhook for an independently deployed module. A host may also use polling where suitable.
- Payment outcome notifications are emitted only after the corresponding payment-state change is durably committed. Use a transactional outbox where needed to prevent a database commit and external message publication from getting out of sync.
- Asynchronous delivery is treated as at-least-once; consumers deduplicate by event ID and tolerate retries/out-of-order delivery. Internal synchronous handlers must also handle failures and transaction boundaries explicitly.
- If outbound webhooks are selected, signatures, retry/backoff, delivery visibility, secret rotation, and replay protection are specified and tested.
- The consuming application can query the module's current payment state as a fallback/source of truth; a browser redirect or return URL alone is never treated as authoritative proof of payment.
- Tests cover repeated delivery, propagation failure/retry, and the host integration's handling of success, failure, and indeterminate outcomes.

## PAY-019 — Support cancellation/void where provider capability and state allow
**Priority:** Should

**Story**  
As an authorized consuming application, I want to cancel an eligible in-progress payment or void an authorization, so that abandoned or no-longer-valid payment attempts can be handled explicitly.

**Acceptance criteria**
- Cancellation/void is distinct from refund and only appears for states and provider/method combinations where it is supported.
- The domain rejects cancellation after an incompatible terminal state.
- Provider cancellation outcomes follow the same idempotency, timeout, and reconciliation rules as other money-moving operations.
- Unsupported operations return a controlled capability error.
- Unit and sandbox/fixture tests cover valid cancellation, invalid state, duplicate request, and uncertain provider outcome.

**Sprint 5 demo:** A completed payment can be queried and refunded; uncertain outcomes can be reconciled; the consuming application's notification/polling path is demonstrated.

---
