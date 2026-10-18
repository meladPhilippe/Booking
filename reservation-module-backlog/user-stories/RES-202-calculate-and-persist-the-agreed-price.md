# RES-202 — Calculate and persist the agreed price

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 2  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer, I want the price and currency for my held seat to be clear, so that later pricing changes do not silently alter the offer I accepted.

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
}.get("RES-202", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a valid hold request, when the pricing strategy returns a valid amount and currency, then the agreed price and currency are stored with the reservation.
2. Given no applicable price can be calculated, when a hold is requested, then the system follows the defined pricing error policy and does not create an incorrectly priced reservation.
3. Given a price changes after the agreed price-lock point, when the reservation is retrieved, then its stored agreed price remains unchanged.

## Technical notes and constraints
- Define the price-lock moment and currency rules before implementation.
- Use a `Money` value object or equivalent amount-plus-currency type.
- Keep pricing calculation separate from persistence and reservation lifecycle logic.

## Dependencies
- RES-101 and pricing policy decision.

## Test guidance
- Unit tests for pricing strategy selection, currency, invalid pricing, and immutable price snapshot.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
