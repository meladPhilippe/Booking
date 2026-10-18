# RES-303 — Handle payment failure, timeout, and late success

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 3  
**Status:** Proposed  
**Story type:** User story

## User story
As the business, I want uncertain payment outcomes handled explicitly, so that customers are not charged without a recoverable booking outcome.

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
}.get("RES-303", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given payment definitively fails, when the outcome is processed, then the reservation follows the agreed failure/hold policy.
2. Given payment times out or the response is unknown, when the system handles the result, then it does not assume payment failed without reconciliation or a defined policy.
3. Given payment succeeds after the seat can no longer be allocated, when the outcome is processed, then the system invokes the agreed compensation/recovery path and does not confirm a conflicting reservation.
4. Given an active payment attempt requires additional time, when the bounded extension policy permits it, then the hold can be extended without exceeding the configured limit.

## Technical notes and constraints
- Product must decide whether to extend holds during payment and define maximum extension.
- Define refund/compensation ownership and retry behavior.
- Add reconciliation for outcomes that remain uncertain.

## Dependencies
- RES-301 and RES-302; business decisions for hold extension and compensation.

## Test guidance
- Tests for definitive failure, timeout/unknown, bounded extension, and compensation path.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
