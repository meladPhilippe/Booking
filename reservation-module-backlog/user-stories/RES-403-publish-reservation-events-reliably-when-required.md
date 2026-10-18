# RES-403 — Publish reservation events reliably when required

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Should  
**Planned sprint:** Sprint 4  
**Status:** Proposed  
**Story type:** User story

## User story
As a downstream consumer, I want reservation events delivered reliably, so that notifications and integrations can react to committed reservation changes without missing events.

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
}.get("RES-403", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given an event-producing reservation transaction commits, when event publication is enabled, then the event is eventually available for delivery.
2. Given the process fails after database commit but before publishing, when recovery runs, then the event is not silently lost.
3. Given the same event is delivered more than once, when a consumer processes it, then duplicate business effects are prevented.
4. Given publication repeatedly fails, when retry limits are reached, then the failure is observable and operational recovery is documented.

## Technical notes and constraints
- Use a transactional outbox if reliable DB-change/event publication is a requirement.
- This story may be deferred if MVP has no asynchronous event consumers; document the decision explicitly.
- Do not introduce a message broker solely to satisfy an architectural preference.

## Dependencies
- Persistence transaction boundary and identified event consumers.

## Test guidance
- Failure-injection tests around commit/publish boundary and duplicate consumer delivery.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
