# RES-103 — Query available seats

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 1  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer, I want to view seats currently available for a specific inventory occurrence, so that I can choose a seat before requesting a hold.

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
}.get("RES-103", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given an existing inventory occurrence, when available seats are requested, then only seats currently considered available are returned.
2. Given a seat held by a non-expired reservation or already confirmed, when availability is queried, then it is not returned as available.
3. Given an unknown inventory occurrence, when availability is queried, then the API returns a not-found response.
4. Given an availability response, when a customer later requests a hold, then the system revalidates and atomically acquires the seat rather than trusting the earlier query.

## Technical notes and constraints
- Availability is advisory and may change immediately after the response.
- Define pagination and filtering only if inventory size requires them.
- Do not make Redis/cache data the authority for seat ownership.

## Dependencies
- RES-101; requires an agreed inventory source.

## Test guidance
- Application/API tests for available, held, confirmed, and unknown inventory cases.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
