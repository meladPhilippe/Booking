# RES-401 — Expire held reservations and release seats safely

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 4  
**Status:** Proposed  
**Story type:** User story

## User story
As the system, I want expired holds to release their seats, so that abandoned reservations do not block inventory indefinitely.

## Business value
{
"RES-101": "Creates a consistent identity model so the same seat label on different occurrences cannot be confused.",
"RES-102": "Prevents invalid booking transitions and makes the lifecycle predictable.",
"RES-103": "Lets customers make an informed seat choice while preserving the distinction between viewing and acquiring a seat.",
"RES-104": "Creates a maintainable, testable foundation for the delivery team.",
"RES-201": "Prevents conflicting customers from both acquiring the same seat.",
"RES-202": "Makes the agreed offer transparent and stable.",
"RES-203": "Protects customers and the business from duplicate operations caused by retries.",
"RES-204": "Provides durable, cross-instance reservation correctness.",
"RES-301": "Connects booking to payment without coupling the domain to a provider.",
"RES-302": "Turns payment outcomes into safe, repeatable reservation transitions.",
"RES-303": "Reduces the risk of charging a customer without a valid booking or recovery path.",
"RES-304": "Protects customer data and reservation operations.",
"RES-401": "Returns abandoned seats to inventory without disrupting newer owners.",
"RES-402": "Supports the reservation lifecycle and policy-compliant inventory release.",
"RES-403": "Prevents committed business changes from being lost to downstream integrations when reliable events are required.",
"RES-404": "Makes the module supportable and gives the team evidence for release readiness."
}.get("RES-401", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a Held reservation has expired and has not been extended or confirmed, when expiration is processed, then it becomes Expired and the seat becomes available atomically.
2. Given a hold has been extended or its reservation has been confirmed, when an old expiration job runs, then it does not expire the current reservation or release its seat.
3. Given the expiration job retries the same item, when it runs again, then no duplicate business effect occurs.
4. Given the worker encounters a transient failure, when it retries according to policy, then processing is observable and does not corrupt ownership.

## Technical notes and constraints
- Use a background worker/scheduler and conditional state transitions.
- Compare expiry against a consistent UTC instant using an injectable clock in domain/application tests.
- Batch processing and indexing should be designed for expected inventory volume.

## Dependencies
- RES-201 and RES-204; hold expiration model.

## Test guidance
- Tests for expired, extended, confirmed, and newly re-acquired seat cases; retry worker safely.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
