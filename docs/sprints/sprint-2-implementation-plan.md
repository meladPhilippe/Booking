# Sprint 2 — Implementation Plan

## Goal

Make payment creation durable and safe under client retries. A create request now claims a merchant-scoped idempotency key in SQL before the provider boundary is crossed, so duplicate requests resolve to one logical payment.

## Delivered scope

- **PAY-006:** Payment reads are merchant-scoped. Both EF Core and Dapper use the same SQL Server schema, with EF Core migrations as the schema authority.
- **PAY-007:** `POST /api/v1/payments` requires the `Idempotency-Key` header. The key is persisted with a SHA-256 request fingerprint and a unique SQL constraint over merchant, operation, and key.
- **PAY-008:** A payment is persisted as `Processing` before provider invocation. Provider errors that occur after dispatch are represented as `RequiresReconciliation`; an HTTP replay never invokes the provider again.
- **PAY-009:** Existing request validation remains provider-neutral. Idempotency errors are stable Problem Details responses. URL and metadata fields are not yet part of the public API, so they cannot reach a provider or persistence layer.

## Architecture and data changes

```text
POST /api/v1/payments + Idempotency-Key
              |
              v
PaymentService validates request and key
              |
              v
IPaymentRepository.CreateOrGetAsync
              |
              +-- SQL transaction: Payments (Processing) + IdempotencyOperations
              |
              +-- matching existing operation -> return existing payment
              +-- differing fingerprint -> controlled conflict
              |
              v
Provider invocation only for the newly claimed operation
              |
              v
Optimistic-concurrency update to final/reconcilable payment status
```

The migration `202610180002_AddIdempotencyOperations` adds:

- `IdempotencyOperations`, linked one-to-one to `Payments`.
- A unique `(MerchantId, Operation, IdempotencyKey)` constraint.
- A stored fingerprint version and SHA-256 fingerprint, rather than raw request metadata.
- Merchant/payment lookup indexes and `rowversion` optimistic concurrency support.

The EF Core and Dapper repository adapters both implement the transactional claim. The unique constraint, rather than in-process locking, decides concurrent duplicate requests.

## HTTP contract

```http
POST /api/v1/payments
Idempotency-Key: client-generated-opaque-key
Content-Type: application/json
```

The request body remains `merchantReference`, `amount`, `currency`, and `paymentMethod`. The configured/development merchant context supplies the merchant scope; a client cannot supply it in the body.

| Situation | Response |
|---|---|
| First valid request | `201 Created` |
| Matching replay after completion | `200 OK`, `Idempotency-Replayed: true` |
| Matching replay while original is processing | `202 Accepted`, `Idempotency-Replayed: true`, `Retry-After: 2` |
| Same key with a different material request | `409 Conflict`, `idempotency_key_reused` |
| Missing key | `400 Bad Request`, `idempotency_key_required` |
| Invalid key | `400 Bad Request`, `invalid_idempotency_key` |

`GET /api/v1/payments/{paymentId}` now queries in the current merchant scope. A cross-merchant ID is indistinguishable from a missing payment and returns `404`.

## Outcome safety

- Definitive simulated outcomes continue to map to `Succeeded`, `Failed`, or `RequiresAction`.
- Simulated timeout/unknown outcomes map to `RequiresReconciliation`.
- An exception from provider invocation, unless the caller cancelled the request before dispatch, also maps to `RequiresReconciliation`.
- A SQL transaction covers only local payment and idempotency writes; it cannot atomically include a provider call. Reconciliation, provider lookups, webhooks, and automatic retry are later-sprint work.

## Verification

- Application tests prove a matching replay returns the original payment and calls the provider once.
- Application tests prove changed amount under the same key returns `idempotency_key_reused`.
- Existing domain, simulator, and API-contract tests remain green.

Run the suite:

```bash
dotnet test Booking.sln --no-restore
```

## Deferred work

- Production authentication and the final merchant-claim convention.
- Public return/cancel URLs and metadata contract, including allow-list and metadata-size policies.
- Real provider idempotency, webhooks, provider lookup/reconciliation worker, refunds, and consuming-application outcome propagation.
