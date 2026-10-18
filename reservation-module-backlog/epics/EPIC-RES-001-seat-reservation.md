# EPIC-RES-001 — Reliable Assigned-Seat Reservation

**Priority:** Must have for MVP  
**Business owner:** Product Owner / Business Sponsor  
**Technical owner:** Tech Lead  
**Status:** Proposed  
**Target:** MVP

## Epic statement
As a customer, I want to find and reserve an assigned seat for a specific flight, train journey, or stadium event, so that I can obtain a reliable booking without another customer being confirmed for the same seat.

## Business outcome
Deliver a reusable reservation capability with a common lifecycle and inventory model while allowing transport- or event-specific pricing and policies to be added through well-defined abstractions.

## In scope
- Occurrence-specific inventory and assigned-seat identity.
- Seat availability query.
- Atomic temporary seat hold with expiration.
- Reservation lifecycle and customer ownership.
- Price calculation through a replaceable strategy.
- Payment initiation boundary and payment-outcome handling.
- Confirmation, cancellation, hold expiration, and seat release.
- Idempotent handling of retried requests and duplicate outcomes.
- SQL Server persistence and API endpoints.
- Automated domain, application, integration, concurrency, and API tests.
- Basic observability and operational diagnostics.

## Out of scope for MVP
- General-admission / unassigned tickets.
- Hotel rooms, parking spaces, and arbitrary resource booking.
- Complex loyalty, bundles, coupons, and group seating.
- A microservices deployment or mandatory message broker.
- A real payment-provider integration unless separately agreed.
- Seat-map rendering and frontend implementation.
- Multi-currency conversion and tax-engine integration unless required by the business.
- Waitlists and automatic seat reassignment.

## Actors
- **Customer:** searches availability, requests a hold, pays, views or cancels a reservation.
- **Reservation API client:** submits requests and retries safely.
- **Payment module/provider:** processes payment and reports outcomes.
- **Operations/support:** investigates failed or inconsistent reservation/payment workflows.
- **Background processor:** expires eligible holds and releases inventory.

## Core business rules
1. A seat belongs to a specific inventory occurrence for reservation purposes.
2. The reservation identity includes the inventory occurrence and seat.
3. At most one active owner may hold or confirm a given seat in an occurrence.
4. Availability results are advisory; successful atomic hold acquisition is the authoritative claim.
5. A hold has an expiration instant and cannot be confirmed after expiry unless an explicit valid extension exists.
6. A confirmed reservation cannot be silently overwritten by another reservation.
7. Reservation and payment states are separate. Payment initiation is not payment success.
8. Duplicate requests and duplicate payment outcomes must not create duplicate business effects.
9. A reservation records the agreed price and currency under the chosen price-lock policy.
10. Seat release, cancellation, refunds, and late payment recovery must follow explicit business rules.
11. All times used for expiry comparisons are consistent instants; persistence and APIs should use UTC/offset-aware values.
12. The system must not expose one customer's reservation details to another customer.

## MVP workflow
1. Customer queries available seats for an inventory occurrence.
2. Customer requests a seat hold.
3. The system atomically acquires the seat and creates a Held reservation with expiry and agreed price.
4. The customer initiates payment.
5. The payment outcome is processed idempotently.
6. A valid successful payment confirms the reservation; failure, timeout, or late success follows the defined recovery policy.
7. Cancellation or expiry releases the seat only when the reservation still owns it and release is permitted.

## Acceptance criteria for the epic
- A customer can query availability for an inventory occurrence.
- A seat can be held and the response includes reservation identity, status, expiry, price, and currency.
- Concurrent attempts for the same seat cannot both succeed as active holds.
- A valid payment outcome can confirm a held reservation once.
- Expired holds are released without releasing a seat that has since been acquired by another reservation.
- Retries and duplicate payment notifications do not duplicate reservations, confirmations, or refunds.
- A new inventory type that follows the common rules can use the workflow without rewriting the application orchestration.
- Automated tests demonstrate lifecycle, concurrency, authorization, and failure-path behavior.

## Success measures
- **Correctness:** zero duplicate active ownership for the same inventory/seat pair in concurrency tests.
- **Reliability:** retrying the same idempotency key returns the original operation result rather than creating another reservation.
- **Traceability:** reservation operations and payment references can be correlated in logs without exposing sensitive payment data.
- **Extensibility:** flight, train, and stadium scenarios use the common workflow; differences are isolated to metadata, policy, or pricing where justified.
- **Testability:** business rules can be tested without SQL Server, HTTP, or a payment provider.

## Key risks and mitigations
| Risk | Mitigation |
|---|---|
| Two customers acquire the same seat | Atomic conditional update/locking and database constraints; concurrency integration tests. |
| Payment succeeds after hold expiry | Explicit bounded hold-extension and late-payment compensation policy; reconciliation path. |
| Duplicate callbacks or retries | Idempotency keys, unique provider event identifiers, and idempotent state transitions. |
| Background job acts on stale data | Conditional transition based on current status, expiry, and ownership. |
| Pricing changes during checkout | Store agreed price and currency at the defined price-lock point. |
| Transport-specific rules leak into common flow | Domain/application abstractions; add specializations only for real variation. |
| Overengineering before requirements are known | Modular monolith first; introduce messaging/distributed components only when justified. |

## Dependencies and decisions to resolve
- Customer identity source and authorization claims.
- Exact hold duration and whether payment processing extends the hold.
- Price-lock moment and whether prices include tax/fees.
- Cancellation policy and refund ownership.
- Payment outcome contract, signature verification, retry behavior, and event identifiers.
- Inventory source: locally managed inventory or external provider.
- Required throughput and seat contention expectations.
- Audit, retention, and privacy requirements.

## Definition of Ready (epic-level)
- Business rules and unresolved decisions have named owners.
- MVP scope is accepted.
- Payment and inventory boundaries are agreed.
- Non-functional expectations are measurable enough for testing.
- Team has refined stories and identified external dependencies.

## Definition of Done (epic-level)
- All Must-have stories are Done and accepted.
- End-to-end assigned-seat workflow is demonstrated.
- Concurrency and duplicate-delivery tests pass.
- API documentation and operational runbook are available.
- Security, logging, and failure recovery have been reviewed.
- Known limitations and deferred scope are documented.
