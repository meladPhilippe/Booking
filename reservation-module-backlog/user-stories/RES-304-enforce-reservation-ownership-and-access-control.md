# RES-304 — Enforce reservation ownership and access control

**Epic:** EPIC-RES-001 — Reliable Assigned-Seat Reservation  
**Priority:** Must  
**Planned sprint:** Sprint 3  
**Status:** Proposed  
**Story type:** User story

## User story
As a customer, I want to access only reservations I am authorized to view or change, so that reservation details and operations are protected.

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
}.get("RES-304", "Delivers a required reservation capability.")

## Acceptance criteria
1. Given an authenticated customer requests their own reservation, when authorization succeeds, then permitted details are returned.
2. Given a customer requests another customer's reservation, when no authorized role/policy permits access, then the API denies access without leaking private details.
3. Given an unauthenticated request invokes a protected operation, when authentication is required, then access is denied.
4. Given an authorized support/admin role performs a privileged operation, when the role is valid, then the operation is audited.

## Technical notes and constraints
- Derive customer identity from trusted authentication claims, not arbitrary client-supplied owner IDs.
- Define admin/support authorization separately from customer authorization.
- Avoid logging payment-sensitive or unnecessary personal information.

## Dependencies
- Authentication/identity provider and authorization policy.

## Test guidance
- API integration tests for owner, other customer, anonymous, and privileged support cases.

## Definition of Done
- Acceptance criteria pass and are demonstrated.
- Automated tests are added at the appropriate unit, application, API, or persistence level.
- Code is reviewed and follows the agreed architecture boundaries.
- API contracts, configuration, and operational behavior are documented where changed.
- No known critical security or data-integrity defect remains open.
- Story is integrated into the main branch and the build/test pipeline passes.
