# Reservation Module — Sprint Plan

**Planning baseline:** 4 sprints × 2 weeks  
**Important:** Sprint allocation is a proposed sequence. The Scrum Team should estimate and re-plan using actual capacity and discovered risks.

## Delivery strategy
Deliver a thin, testable vertical slice early, then harden it for concurrent use and payment outcomes. Do not postpone concurrency design until the end: its contract and data model must be established before the first reservation can be considered correct.

## Sprint 1 — Domain foundation and availability

### Sprint goal
Establish the domain vocabulary and API capability to identify inventory occurrences and query available assigned seats.

### Candidate stories
- `RES-101` Define inventory occurrence and seat identity.
- `RES-102` Model reservation lifecycle and valid transitions.
- `RES-103` Query available seats for an inventory occurrence.
- `RES-104` Establish solution structure, dependency boundaries, and automated test foundation.

### Demonstrable outcome
A client can query seat availability for a flight, train journey, or stadium event fixture using the common model. Domain tests validate reservation state transitions.

### Key tasks / decisions
- Confirm whether inventory is locally managed or supplied externally.
- Define stable identifiers and customer identity representation.
- Establish API error conventions and validation approach.
- Confirm database technology and test strategy.

### Sprint review checklist
- Show availability for at least one fixture from each target category.
- Demonstrate domain tests for invalid transitions.
- Explain which behaviors are common and which are data/policy variation.

### Exit criteria
The team can identify an occurrence and seat consistently, and availability results are explicitly documented as advisory rather than a guarantee.

---

## Sprint 2 — Atomic holds and reservation creation

### Sprint goal
Allow a customer to acquire a temporary hold safely, even when requests contend for the same seat.

### Candidate stories
- `RES-201` Create a reservation by atomically holding a seat.
- `RES-202` Calculate and persist the agreed price.
- `RES-203` Make reservation creation idempotent.
- `RES-204` Persist inventory and reservation state with SQL Server.

### Demonstrable outcome
A customer can request a hold and receive reservation ID, Held status, expiration, price, and currency. Competing requests for the same seat cannot both acquire it.

### Key tasks / decisions
- Implement a conditional database operation or equivalent transactional concurrency mechanism.
- Define unique constraints and transaction boundaries.
- Define idempotency-key scope, retention, and request-payload mismatch behavior.
- Add integration tests against a real SQL Server test instance/container where feasible.

### Sprint review checklist
- Show successful hold creation.
- Run concurrent attempts for the same seat and show only one succeeds.
- Retry the same idempotency key and show no duplicate reservation.
- Show a different seat can be held independently.

### Exit criteria
Seat ownership correctness is enforced by persistence behavior, not merely by an application-level availability check.

---

## Sprint 3 — Payment outcome, confirmation, and recovery

### Sprint goal
Complete the core booking path by coordinating reservation state with payment outcomes safely.

### Candidate stories
- `RES-301` Initiate payment through an application-owned abstraction.
- `RES-302` Process payment outcomes idempotently and confirm eligible reservations.
- `RES-303` Handle payment failure, timeout, and late success.
- `RES-304` Enforce reservation ownership and access control.

### Demonstrable outcome
A held reservation can be confirmed once after a valid successful payment outcome. Failure and uncertain outcomes follow an explicit policy, and customers cannot read or modify other customers' reservations.

### Key tasks / decisions
- Define payment request/correlation identifiers and provider event uniqueness.
- Choose whether payment is synchronous, webhook-driven, or message-driven.
- Define bounded hold extension and late-payment compensation behavior.
- Ensure callback authenticity is validated at the integration boundary.
- Add audit correlation without logging sensitive payment data.

### Sprint review checklist
- Show success, failure, duplicate notification, and late-success scenarios.
- Show that payment initiation alone does not confirm a reservation.
- Show access denied for a different customer.
- Demonstrate safe behavior for repeated confirmation attempts.

### Exit criteria
Payment and reservation states remain distinct and recoverable; duplicate outcomes do not create duplicate business effects.

---

## Sprint 4 — Expiration, cancellation, reliability, and release readiness

### Sprint goal
Make the reservation lifecycle operationally complete and demonstrate production-oriented reliability.

### Candidate stories
- `RES-401` Expire held reservations and release seats safely.
- `RES-402` Cancel a reservation under defined business rules.
- `RES-403` Publish reservation events reliably when asynchronous integration is enabled.
- `RES-404` Add operational diagnostics and end-to-end verification.

### Demonstrable outcome
Expired holds release seats without affecting newer owners. Cancellation follows the agreed policy. The team can diagnose a reservation using correlated logs and run the key failure-path tests.

### Key tasks / decisions
- Choose a hosted worker/scheduler approach and retry policy.
- Decide whether a transactional outbox is required for MVP integrations.
- Define cancellation/refund policy and operational ownership.
- Run load/concurrency tests and document deployment/configuration requirements.
- Review API contract, threat model, logging, and support runbook.

### Sprint review checklist
- Show an expired seat becoming available.
- Show that an extended or confirmed hold is not incorrectly expired.
- Show cancellation and any required payment/refund handoff.
- Demonstrate health checks, structured logs, and end-to-end tests.

### Exit criteria
All agreed MVP acceptance criteria pass, critical risks have mitigation or explicit acceptance, and operations know how to investigate uncertain payment outcomes.

---

## Cross-sprint quality practices
- Review SOLID boundaries during code review; avoid abstractions without a real reason.
- Add tests with each story rather than deferring testing to Sprint 4.
- Include cancellation, expiry, retries, and authorization in acceptance discussions.
- Use database-backed integration tests for concurrency claims.
- Keep secrets out of source control and logs.
- Treat monitoring, API documentation, migrations, and deployment configuration as part of delivery.

## Backlog prioritization
1. Seat identity, lifecycle, availability, and atomic hold.
2. Price snapshot and idempotent creation.
3. Payment outcome and confirmation.
4. Expiration, cancellation, and recovery.
5. Outbox/messaging, advanced reporting, and additional policies when justified.

## Planning caveat
If real payment-provider integration, external inventory APIs, or complex pricing are required in the MVP, re-estimate and re-plan. Do not squeeze those dependencies into the plan without discovery.
