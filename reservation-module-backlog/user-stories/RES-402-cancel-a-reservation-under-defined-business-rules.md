# RES-402 — Cancel a reservation under defined business rules

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 4  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer or authorized operator, I want to cancel an eligible reservation, so that inventory is released and any required refund is handled according to policy.

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
}.get("RES-402", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given a reservation is cancellable under policy, when cancellation succeeds, then its state is updated and the inventory/payment consequences are recorded.
2. Given a reservation is not cancellable, when cancellation is requested, then the operation is rejected with a clear reason.
3. Given cancellation is retried, when the reservation is already cancelled, then the operation does not duplicate releases, refunds, or notifications.
4. Given a confirmed reservation requires a refund, when cancellation is processed, then refund processing is coordinated through the payment boundary and tracked separately.

## Technical notes and constraints
- Define cancellation windows, fees, refund rules, and authority before implementation.
- Do not mark a refund successful merely because a refund request was initiated.
- Keep cancellation and payment/refund states distinct.

## Dependencies
- RES-102, RES-204, and cancellation/refund policy.

## Test guidance
- Tests for policy rejection, repeated cancellation, seat release, and refund handoff.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
