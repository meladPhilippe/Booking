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

## PAY-005 — Secure multi-merchant payment API access
**Priority:** Must

**Story**  
As the payment-module owner, I want the Payment API authenticated and authorized for trusted consuming backends, so that each backend can operate only on payments for its authorized merchant and business function, while shoppers can safely use guest or signed-in checkout through the storefront.

**Acceptance criteria**
- Every Payment API endpoint except provider webhook endpoints and health probes requires an OAuth/OIDC client-credential bearer JWT.
- JWT validation verifies issuer, audience, expiry, client identity, operation scope, and merchant/tenant claim.
- Every payment is bound to the merchant identity from the validated token; merchant identity is never accepted from request bodies, browser input, or caller-controlled headers.
- The storefront backend can create, retrieve, and capture payments only for its merchant. The admin backend can retrieve, refund, and reconcile payments only for its merchant.
- A caller outside its permitted operation scope receives a controlled `403` response. A caller attempting to access another merchant's payment receives the existing controlled not-found response without confirmation that the payment exists.
- Browsers never receive Payment API credentials. Guest checkout ownership is validated by the storefront's short-lived checkout session; the storefront calls the Payment API with its own service token.
- Provider webhooks remain exempt from JWT authentication but require valid Stripe or PayPal signature verification.
- Audit records identify the calling backend client ID, merchant ID, operation, payment ID, and the initiating staff actor ID when supplied by the trusted admin backend; they never contain access tokens or sensitive payment/provider data.
- Authentication and authorization tests cover valid tokens, missing/expired/invalid tokens, invalid tenant scope, invalid operation scope, cross-merchant access, guest checkout boundaries, and webhook signature rejection.

**Sprint 1 demo:** A sample consuming business flow initiates a simulated payment through the agreed host-facing contract, receives a module payment ID/status, and reads the payment without any provider-specific types in its code. Demonstrate through HTTP if that adapter is part of the chosen host, or through an in-process contract for a modular monolith.

---
