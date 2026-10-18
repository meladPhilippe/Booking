# RES-203 — Make reservation creation idempotent

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 2  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer, I want retries after a timeout to return the original reservation result, so that a network issue does not create duplicate reservations.

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
}.get("RES-203", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a request with an idempotency key succeeds, when the same key and equivalent request are retried, then the original result is returned and no second reservation is created.
2. Given the same key is reused with a materially different request, when it is submitted, then the request is rejected according to the documented policy.
3. Given two concurrent requests use the same key, when processed, then only one logical operation is created.

## Technical notes and constraints
- Define key scope, retention, payload fingerprinting, and replay response behavior.
- Persist idempotency state with suitable uniqueness constraints.
- Do not rely on an in-memory dictionary in a multi-instance deployment.

## Dependencies
- RES-204 for durable idempotency in a multi-instance deployment.

## Test guidance
- Integration tests for replay, payload mismatch, and concurrent use of the same key.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
