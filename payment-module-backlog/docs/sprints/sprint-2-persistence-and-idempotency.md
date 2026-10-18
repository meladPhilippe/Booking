# Sprint 2 — Persistence and Idempotency

**Sprint goal:** Make the payment workflow durable and safe under retries and concurrent requests before connecting real providers.

## PAY-006 — Persist and retrieve payment records
**Priority:** Must

**Story**  
As a consuming application, I want payment attempts persisted and queryable, so that payment status survives restarts and can be inspected later.

**Acceptance criteria**
- The repository can create, retrieve by payment ID, and retrieve within the authenticated merchant scope.
- Persistence maps money, currency, status, provider references, and timestamps without losing required precision.
- Database migrations/schema setup are repeatable in a clean environment.
- Concurrent state changes are protected using an agreed concurrency strategy.
- Repository integration tests verify round-trip data, merchant scoping, and concurrency behaviour.

## PAY-007 — Make payment creation idempotent
**Priority:** Must

**Story**  
As a customer/consuming application, I want retrying the same create-payment operation to return the same logical payment operation, so that network retries do not cause duplicate charges.

**Acceptance criteria**
- Create-payment requires an idempotency key, or applies another explicitly approved contract for generating one.
- The uniqueness scope includes the authenticated merchant/client and operation; the same key cannot be reused to create a different payment payload silently.
- The idempotency record/payment operation is persisted before the external-provider boundary is crossed.
- Concurrent duplicate requests result in one logical operation; the other request receives the existing/current outcome or a documented in-progress response.
- Reuse of a key with a materially different amount, currency, merchant reference, or method returns a controlled conflict.
- Tests cover concurrent requests, process restart, duplicate requests after success, and duplicate requests while processing.

## PAY-008 — Handle provider outcomes without unsafe assumptions
**Priority:** Must

**Story**  
As a payment system, I want success, decline, retryable failure, and indeterminate outcomes represented distinctly, so that transient faults do not cause duplicate payment attempts.

**Acceptance criteria**
- A definitive decline maps to a failed/declined outcome according to the public contract.
- A timeout or lost provider response can map to `Processing` or `RequiresReconciliation` rather than automatically to `Failed`.
- Provider request, local persistence, and response handling are designed for partial failure; the implementation does not claim one DB transaction can atomically include the external provider.
- Retry policies do not blindly replay non-idempotent provider operations.
- Tests simulate a provider completing an operation while the client times out before seeing the result.

## PAY-009 — Validate payment request and safe metadata
**Priority:** Must

**Story**  
As a consuming application, I want payment requests validated consistently, so that malformed or unsafe requests do not reach a provider.

**Acceptance criteria**
- Amount, currency, method, merchant reference, and return/cancel URLs are validated against documented rules.
- URL validation prevents unsafe redirect patterns according to an allow-list or equivalent agreed policy.
- Metadata has documented size/key limits and is not intended for card data, secrets, or other prohibited sensitive information.
- The server treats the amount supplied by the authenticated trusted backend as input to validate, not as evidence that the business transaction is payable; the host application's responsibility is documented.
- Validation errors are stable, actionable, and do not include secrets or internal exception details.

**Sprint 2 demo:** A payment is stored and can be retrieved. Replaying the same idempotency key—including concurrent replays—does not create a second logical operation.

---
