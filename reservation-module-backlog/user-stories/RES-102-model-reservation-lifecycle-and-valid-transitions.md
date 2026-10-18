# RES-102 — Model reservation lifecycle and valid transitions

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 1  
**Status:** Proposed  
**Story type:** User story

## User story
As the business, I want reservation states and transitions to be explicit, so that invalid lifecycle changes are rejected consistently.

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
}.get("RES-102", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a Held reservation whose hold is still valid, when a permitted confirmation operation occurs, then it can transition to Confirmed only when the required business condition is satisfied.
2. Given an expired, cancelled, or otherwise ineligible reservation, when confirmation is attempted, then the transition is rejected without changing state.
3. Given a Confirmed reservation, when a duplicate confirmation is processed, then no duplicate business effect occurs.
4. Given an invalid state transition, when it is requested, then a clear domain-level failure is returned.

## Technical notes and constraints
- Keep reservation status separate from payment status.
- Encapsulate transitions in domain methods; do not expose arbitrary public status setters.
- Use an injectable clock for expiry-related tests.

## Dependencies
- RES-101 for inventory and reservation identity.

## Test guidance
- Table-driven domain tests for every allowed and forbidden state transition.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
