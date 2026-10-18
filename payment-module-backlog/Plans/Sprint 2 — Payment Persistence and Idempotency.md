# Sprint 2 — Payment Persistence and Idempotency

## Goal

Make payment creation durable, merchant-scoped, and safe to retry before any real provider integration. A duplicate request must resolve to one logical payment operation, including when duplicate requests race or the API restarts.

This sprint delivers PAY-006 through PAY-009 from the maintained backlog. It builds on the Sprint 1 `Payment.*` solution and preserves its provider-neutral boundary.

## Scope and outcome

At the end of the sprint, a caller sends an idempotency key with `POST /api/v1/payments`. The module atomically claims that key for the authenticated/configured merchant and a canonical representation of the request. It persists the payment and idempotency record before invoking the provider. A replay with the same merchant, key, and payload returns the existing operation; a replay with changed material fields receives a stable conflict response.

The API can retrieve a payment only in the current merchant scope. Provider timeout or an unknown result remains recoverable (`Processing` or `RequiresReconciliation`), never an automatic assertion that the payment failed or that a retry is safe.

## In scope

- **PAY-006 — Persist and retrieve payment records**
  - Complete repository contract coverage for create, update, and merchant-scoped lookup using both EF Core and Dapper against the existing SQL Server schema.
  - Keep EF Core migrations as the only schema authority.
  - Use the existing SQL Server `rowversion` column for optimistic concurrency and translate stale updates into a controlled application error.
  - Add the indexes and constraints required by merchant-scoped reads and idempotency claims.

- **PAY-007 — Make payment creation idempotent**
  - Require `Idempotency-Key` on every create request. The header is trimmed, opaque, and limited to 128 characters; missing or invalid values return a stable validation error.
  - Scope idempotency by `(MerchantId, Operation = CreatePayment, IdempotencyKey)`.
  - Persist a canonical request fingerprint (SHA-256 over a versioned, invariant representation) containing merchant ID, merchant reference, amount in invariant decimal format, upper-case currency, payment method, and the relevant return/cancel URLs and metadata when those fields are introduced.
  - Place a unique database constraint on that scope. It is the final arbiter for concurrent requests; application locking alone is not sufficient.
  - Persist the claimed idempotency record and associated payment in one local database transaction before crossing the provider boundary.
  - Return the stored/current payment response for a matching replay. Return `409 idempotency_key_reused` for a fingerprint mismatch. If the original request has claimed the key but its payment is still being initiated, return `202 Accepted` with the existing payment representation and a documented `Processing`/`Pending` status.

- **PAY-008 — Handle provider outcomes without unsafe assumptions**
  - Persist a new payment in `Processing` before calling the provider; do not expose a durable `Pending` record as a completed attempt.
  - Map definitive provider success, decline, and required action through existing legal domain transitions.
  - Map timeout, cancellation after dispatch, lost response, or unknown completion to `RequiresReconciliation` (or retain `Processing` only while the request is known to be in flight). Do not replay the provider call from an HTTP retry.
  - Record enough non-sensitive provider attempt information to reconcile later: provider name, module payment ID, provider reference when known, outcome category, and timestamps. A reconciliation worker, real-provider queries, webhooks, and blind automated replay are deferred.
  - Make the local transaction/provider boundary visible in code and documentation: a database transaction cannot atomically commit an external provider operation.

- **PAY-009 — Validate payment request and safe metadata**
  - Retain amount, configured-currency, method, and merchant-reference validation from Sprint 1; make error codes and Problem Details shapes stable.
  - Add optional `returnUrl`, `cancelUrl`, and metadata only if the host needs them this sprint. Validate URLs as absolute HTTPS URLs whose host appears in a configured allow-list. Reject user-info, fragments, IP-literal hosts, and non-default ports unless explicitly configured.
  - Limit metadata to 20 entries, keys of 1–40 `[A-Za-z0-9_.-]` characters, values up to 500 characters, and a 5 KB serialized total. Reject prohibited sensitive-data use; documentation states that metadata must not contain card data, credentials, tokens, or secrets.
  - Document that the consuming trusted backend owns the authoritative business transaction and amount. The payment module validates supplied values but does not establish that an order or reservation is payable.

## Explicitly deferred

- Production authentication/authorization and the final merchant-claim convention (PAY-005). Development merchant context remains the temporary boundary.
- Stripe, PayPal, webhooks, provider-side idempotency implementation, provider lookup/reconciliation, refunds, outcome publishing, and Reservation/Ticketing integration.
- A background reconciliation worker. This sprint records indeterminate results safely; it does not resolve them automatically.

## Design decisions

| Area | Decision |
|---|---|
| Idempotency transport | Required `Idempotency-Key` request header; never accepted in the request body. |
| Key ownership | Merchant context, not a client-supplied merchant ID, defines the scope. |
| Duplicate success | Return `200 OK` with the original response and `Idempotency-Replayed: true`; first accepted create remains `201 Created`. |
| Duplicate in progress | Return `202 Accepted` with the current payment response, `Location`, and `Retry-After: 2`; no second provider call. |
| Changed replay | Return `409 Conflict`, code `idempotency_key_reused`; do not disclose another merchant's record. |
| Fingerprint | Versioned SHA-256 of canonical, material request fields; store the hash, not raw metadata. |
| Local atomicity | Create the payment and idempotency record in one SQL transaction, then commit before provider dispatch. |
| Concurrency | Unique SQL constraint for claiming a key; `rowversion` for subsequent payment updates. |
| Indeterminate provider result | Persist `RequiresReconciliation`; never turn it into `Failed` merely because the caller timed out. |

## Data and contract changes

### Database migration

Create a new EF Core migration that introduces an `IdempotencyOperations` table (or equivalent clearly named aggregate table):

```text
Id                         uniqueidentifier primary key
MerchantId                 nvarchar(128) not null
Operation                  nvarchar(64) not null             -- CreatePayment
IdempotencyKey             nvarchar(128) not null
RequestFingerprintVersion  int not null
RequestFingerprint         char(64) not null                  -- SHA-256 hex
PaymentId                  uniqueidentifier not null unique FK Payments(Id)
CreatedAt / UpdatedAt      datetimeoffset not null
RowVersion                 rowversion not null

UNIQUE (MerchantId, Operation, IdempotencyKey)
INDEX  (MerchantId, PaymentId)
INDEX  Payments (MerchantId, Id)
```

Use foreign-key and uniqueness constraints in the migration rather than relying on repository behavior. EF Core owns this migration; Dapper consumes the resulting schema.

### Application ports

- Replace the unscoped `GetAsync(paymentId)` repository method with `GetAsync(merchantId, paymentId)`.
- Add a transactional idempotent-create port that can either atomically create the payment plus operation, or return the existing operation. It must report three outcomes: `Created`, `Replay`, and `FingerprintConflict`.
- Do not make the application service coordinate separate `AddAsync` and idempotency writes; that would reintroduce the race.
- Add a typed `PaymentConcurrencyException` and map it deliberately. Do not leak `DbUpdateConcurrencyException` or SQL exceptions through the API.

### HTTP contract

```http
POST /api/v1/payments
Idempotency-Key: 4c00f56c-...
Content-Type: application/json
```

The existing request body remains provider-neutral. Add URL and metadata fields only together with their validation and documentation. The response keeps the Sprint 1 neutral `PaymentResponse` shape; it must not include a fingerprint, database version, provider SDK object, or secret.

`GET /api/v1/payments/{paymentId}` returns `404` when the payment does not exist **or is outside the current merchant scope**. It must not reveal cross-merchant existence.

Problem Details errors:

| Status | Code | Condition |
|---:|---|---|
| 400 | `idempotency_key_required` | Header is absent or blank. |
| 400 | `invalid_idempotency_key` | Header exceeds limits or violates allowed opaque-key rules. |
| 400 | `invalid_return_url`, `invalid_cancel_url`, `invalid_metadata` | Optional unsafe field validation fails. |
| 409 | `idempotency_key_reused` | Same merchant/key, materially different request. |
| 409 | `payment_concurrency_conflict` | A concurrent state update cannot be applied safely. |

## Implementation slices

1. **Lock the contract and domain vocabulary**
   - Add idempotency command/result types, error codes, canonical fingerprinting, and merchant-scoped lookup contracts.
   - Decide and test the status mapping for simulated success, decline, action-required, timeout, and lost response.
   - Update the API input/header binding and Problem Details mapping without accepting provider-specific fields.

2. **Migrate and implement durable claim storage**
   - Model the idempotency operation and migration in `PaymentDbContext`.
   - Implement the EF Core atomic create-or-replay path with a transaction and unique-constraint conflict handling.
   - Implement the equivalent Dapper path, including parameterized SQL and transaction handling.
   - Keep `PaymentDbContext` migration tooling as the sole schema author.

3. **Make the service safe across the provider boundary**
   - Claim and persist before provider invocation.
   - On provider result, re-read/update with `rowversion`; map stale writes and unknown outcomes safely.
   - Return the correct created, replayed, in-progress, conflict, and indeterminate responses. Ensure an idempotent replay never invokes the provider.

4. **Validation, documentation, and operational clarity**
   - Implement URL and metadata validation if those fields are exposed this sprint; otherwise document their deferred public-contract status rather than adding unused storage.
   - Document amount ownership, metadata restrictions, header behavior, status semantics, and the reconciliation hand-off in the API README/OpenAPI comments.
   - Update local demo instructions with replay and concurrent-request examples.

5. **Prove both persistence adapters**
   - Run shared repository-contract tests against local SQL Server for EF Core and Dapper.
   - Ensure migration applies cleanly to an empty database and existing Sprint 1 database.

## Test plan and acceptance evidence

- **Domain/application:** fingerprint canonicalization; missing, valid, and mismatched keys; one provider call for repeated requests; matching replay after success; restart-safe replay using a fresh repository/service instance; merchant scoping; every provider outcome mapping.
- **Concurrency:** two simultaneous identical creates yield one `Payments` row, one idempotency row, and one provider call. Concurrent same-key/different-payload requests yield one winner and one controlled `409`. Concurrent state updates raise the controlled concurrency outcome.
- **Repository integration:** run each contract test against EF Core and Dapper with SQL Server; prove money/currency precision, timestamps, row version updates, migration repeatability, unique constraint enforcement, and cross-merchant non-disclosure.
- **API:** `201` first create, `200` matching replay, `202` in-progress replay, `409` mismatched replay, merchant-scoped `404`, stable Problem Details, required header handling, validation boundaries, and no provider-specific type in JSON.
- **Partial failure:** simulate provider completion followed by client timeout/lost response; persisted payment is `RequiresReconciliation` (or documented active `Processing`), and a replay does not create another provider attempt.

## Definition of done and demo

- The migration is checked in, applies repeatedly to a clean SQL Server database, and works with both configured repository adapters.
- Every create request requires and persists an idempotency claim before provider dispatch.
- The database, not process memory, guarantees one logical operation under concurrent duplicate creates.
- All listed application, integration, and API tests pass for both EF Core and Dapper configurations.
- Documentation explains the temporary development merchant boundary, amount ownership, metadata prohibition, replay semantics, and indeterminate outcome behavior.

Demo against the local stack:

1. Submit a Card payment with an `Idempotency-Key`; receive `201 Created` and the module payment ID.
2. Submit the identical request with the same key; receive `200 OK`, `Idempotency-Replayed: true`, and the same payment ID without another simulated provider call.
3. Submit the same key with a changed amount; receive `409 idempotency_key_reused`.
4. Fire two identical requests concurrently; show one payment/operation in SQL and one provider invocation.
5. Simulate an unknown/timeout outcome; show `RequiresReconciliation` and demonstrate that retrying the HTTP request returns the original operation rather than charging again.
