# RES-101 — Define inventory occurrence and seat identity

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 1  
**Status:** Proposed  
**Story type:** User story

## User story
As a reservation customer, I want seats to be identified within a specific flight, train journey, or stadium event occurrence, so that a seat on one occurrence is distinct from the same seat on another occurrence.

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
}.get("RES-101", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given two different inventory occurrences that both contain seat `12A`, when the system identifies each seat, then the identities are distinct by inventory occurrence plus seat identifier.
2. Given an inventory occurrence, when a seat is requested using its identifier, then the seat is resolved only within that occurrence.
3. Given an unsupported or nonexistent inventory occurrence, when it is queried, then the API returns the agreed not-found response.

## Technical notes and constraints
- Define `InventoryId` and `SeatId` as stable domain identifiers.
- Do not make physical seat identity globally unique unless a shared seat-layout model is a confirmed requirement.
- Document occurrence types as metadata or domain-specific models only where needed.

## Dependencies
- None. Establishes identifiers used by the remaining stories.

## Test guidance
- Unit tests for identity equality and occurrence separation; API tests for unknown inventory.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
