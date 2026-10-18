# RES-302 — Process payment outcomes idempotently

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 3  
**Status:** Proposed  
**Story type:** User story

## User story
As the system, I want payment outcomes processed safely, so that successful payment confirms an eligible reservation once and duplicate notifications do not repeat effects.

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
}.get("RES-302", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a valid successful payment outcome for an eligible Held reservation, when it is processed, then the reservation becomes Confirmed once.
2. Given the same provider event is delivered more than once, when each delivery is processed, then only one business transition/effect occurs.
3. Given an outcome references an unknown payment or reservation, when it is received, then it is safely rejected or quarantined and observable.
4. Given a payment success arrives for an expired or no-longer-owned seat, when it is processed, then the late-payment recovery policy runs instead of stealing the seat.

## Technical notes and constraints
- Verify callback signature/authenticity at the integration boundary where applicable.
- Store provider event identifiers with a uniqueness guarantee.
- Separate payment status from reservation status and record correlation identifiers.

## Dependencies
- RES-301; provider event/correlation contract.

## Test guidance
- Tests for duplicate events, unknown references, valid success, and late success.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
