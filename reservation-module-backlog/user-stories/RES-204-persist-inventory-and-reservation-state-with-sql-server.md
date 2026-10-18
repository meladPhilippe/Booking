# RES-204 — Persist inventory and reservation state with SQL Server

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 2  
**Status:** Proposed  
**Story type:** User story

## User story
As the system, I want inventory and reservation changes persisted consistently, so that seat ownership survives restarts and is correct across application instances.

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
}.get("RES-204", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a reservation transaction succeeds, when data is read back, then reservation and inventory ownership agree.
2. Given a persistence operation fails before commit, when the operation is retried, then no partial ownership change remains.
3. Given two application instances contend for one seat, when they issue concurrent hold requests, then database-enforced concurrency guarantees still allow at most one successful owner.

## Technical notes and constraints
- Define schema, indexes, foreign keys, migrations, and transaction boundaries.
- Use SQL Server integration tests for transactional and concurrency behavior.
- Keep EF Core details inside Infrastructure.

## Dependencies
- RES-101 and RES-102; schema must support atomic ownership.

## Test guidance
- SQL Server integration tests for rollback, constraints, and concurrent updates.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
