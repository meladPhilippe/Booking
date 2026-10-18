# RES-201 — Create reservation by atomically holding a seat

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 2  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer, I want to hold an available seat temporarily, so that other customers cannot acquire it while I complete the booking.

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
}.get("RES-201", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a seat is available, when a valid hold request is processed, then one Held reservation is created with an expiry instant.
2. Given two concurrent requests for the same inventory occurrence and seat, when both attempt to hold it, then at most one succeeds.
3. Given a seat is already held by another customer and the hold is valid, when a new hold is requested, then it is rejected as unavailable.
4. Given a previous hold has expired, when a new request attempts to acquire the seat, then acquisition succeeds only if the expired ownership is safely transitioned/replaced atomically.

## Technical notes and constraints
- Use a conditional database update, locking strategy, or equivalent atomic persistence guarantee.
- Use a uniqueness constraint for the inventory occurrence and seat record; understand that uniqueness alone does not enforce lifecycle ownership.
- Persist reservation and inventory ownership consistently within the transaction boundary.

## Dependencies
- RES-101, RES-102, RES-204. RES-202 must be coordinated if price is locked during hold creation.

## Test guidance
- Database integration tests with parallel requests and multiple application scopes; assert at most one successful owner.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
