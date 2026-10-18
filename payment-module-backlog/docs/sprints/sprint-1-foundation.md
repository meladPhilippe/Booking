# Sprint 1 — Foundation and Provider-Independent Vertical Slice

**Sprint goal:** Establish the solution boundaries and demonstrate one complete payment use case with a deterministic simulated provider, without a real charge.

## PAY-001 — Create solution skeleton and engineering quality gates
**Priority:** Must

**Story**  
As a development team, we want a clean .NET solution with clear project boundaries and automated quality checks, so that core business logic stays independent of APIs, databases, and provider SDKs.

**Acceptance criteria**
- Given the repository is checked out, when the documented build/test command runs, then all projects compile and the test suite runs in CI.
- Domain does not reference API, Infrastructure, provider SDKs, or persistence implementation packages.
- Application depends only on Domain plus appropriate framework abstractions.
- Formatting/analyzer/test failures fail the CI build according to agreed repository standards.
- Configuration examples contain placeholders only; no real provider secrets are committed.

## PAY-002 — Define generic payment domain and lifecycle rules
**Priority:** Must

**Story**  
As the payment module, I want a provider-neutral payment model with explicit invariants and status transitions, so that all integrations follow consistent business rules.

**Acceptance criteria**
- Payment contains a module-generated payment ID, merchant/client reference, money, method, provider identity/reference, status, timestamps, and concurrency version as appropriate.
- Payment does not depend on order, reservation, seat, event, or cart domain classes.
- Negative/zero amounts, unsupported currencies, invalid currency precision, and invalid transitions are rejected according to agreed rules.
- Authorization, successful capture/payment, refund, cancellation, and indeterminate provider outcomes are not treated as interchangeable states.
- Unit tests cover valid and invalid transitions; currency support/precision rules are documented and configurable where appropriate.

## PAY-003 — Define the provider-neutral payment initiation/query contract
**Priority:** Must

**Story**  
As a consuming business application, I want a stable contract to initiate and query payments, so that I do not need to know whether Stripe or PayPal processes the transaction or whether the payment module is hosted in-process or separately.

**Acceptance criteria**
- The application contract defines create-payment request/response, get-payment response, stable error codes, and status vocabulary; an HTTP API adapter may expose the same use cases when required.
- Requests correlate to a caller-owned business record through a generic merchant reference.
- The response uses the module's payment ID and exposes a next-action/checkout payload only when required.
- Provider SDK objects and secrets are not serialized into public API responses.
- If HTTP is used, API versioning and validation-error conventions are documented; the core application contract does not depend on HTTP types.

## PAY-004 — Implement a simulated provider and application workflow
**Priority:** Must

**Story**  
As a developer/tester, I want a deterministic simulated provider, so that we can exercise success, decline, redirect/action-required, timeout, and indeterminate outcomes without contacting a real provider.

**Acceptance criteria**
- The application service depends on abstractions rather than directly constructing provider SDK clients.
- The simulated provider can produce controlled outcomes for automated tests.
- A create-payment request can traverse the selected host-facing entry point → application workflow → resolver → simulated provider → result mapping.
- The simulated provider cannot be selected accidentally in production configuration; startup validation or environment controls prevent silent production use.
- Unit tests prove the same application workflow works with a fake implementation.

## PAY-005 — Add consuming-application authentication and authorization
**Priority:** Must

**Story**  
As the module owner, I want API calls authenticated and scoped to a consuming merchant/client, so that one application cannot read or operate on another application's payments.

**Acceptance criteria**
- The chosen trust/authentication mechanism is applied at the entry boundary: HTTP/service-to-service authentication for remote callers, or the host application's authenticated user/merchant context for in-process calls.
- A client can access only payments belonging to its authenticated merchant/client scope.
- The merchant identity used for scoping comes from the authenticated principal/configuration, not an untrusted request-body field.
- Unauthenticated and out-of-scope requests return controlled responses without disclosing whether another merchant's payment exists.
- Authentication/authorization tests cover allowed and denied access.

**Sprint 1 demo:** A sample consuming business flow initiates a simulated payment through the agreed host-facing contract, receives a module payment ID/status, and reads the payment without any provider-specific types in its code. Demonstrate through HTTP if that adapter is part of the chosen host, or through an in-process contract for a modular monolith.

---
