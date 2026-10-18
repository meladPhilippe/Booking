# Generic Payment Module — Epic, User Stories, and Sprint Plan

**Status:** Proposed backlog for refinement  
**Target platform:** .NET 10 / ASP.NET Core  
**Planning horizon:** Six 2-week sprints, preceded by a short discovery and readiness activity  
**Delivery style:** Vertical slices; simulated providers first, then sandbox-backed Stripe and PayPal integrations

---

## 1. Epic

### EPIC PAY-EPIC-01 — Deliver a reusable, provider-independent payment module

**Epic statement**  
As a product/application team, we want a reusable payment module that can initiate, track, and refund payments through supported payment methods and providers, so that reservation, ticketing, event, and e-commerce applications can accept payments without embedding provider-specific payment logic in their business workflows.

### Business outcomes

1. A consuming application can request a payment and correlate it to its own business record without the payment module owning reservations, seats, events, carts, or orders.
2. The same application-facing payment contract can work with Stripe and PayPal integrations.
3. Payment outcomes remain accurate when providers use redirects, asynchronous confirmation, webhooks, timeouts, or retries.
4. Retrying the same logical payment operation does not create a second charge.
5. The module can be tested without real provider calls or real-money transactions.
6. Provider-specific details remain in infrastructure adapters, while business rules and payment lifecycle remain in the core.

### Epic-level success criteria

- A sample client can create a payment, continue the provider checkout flow, and query the payment status without knowing provider SDK types.
- Sandbox end-to-end scenarios pass for Stripe and PayPal.
- Duplicate API requests with the same scoped idempotency key return/reconcile the same logical operation.
- Valid provider webhooks are authenticated, deduplicated, and applied through allowed state transitions; invalid signatures are rejected.
- A provider timeout is not automatically treated as a definitive payment failure.
- Full and partial refunds are tracked for provider/payment combinations that support them.
- Unit tests do not require live provider accounts; adapter tests use sandbox or controlled HTTP fixtures.
- No raw card number or card security code is stored or logged by the module.
- API authentication, secrets management, operational monitoring, and a deployment/runbook are in place before production release.

---

## 2. Product boundaries and working assumptions

These assumptions should be confirmed during discovery. They make the backlog estimable without tying the module to a particular host application.

### In scope for the first production release

- Provider-independent payment initiation and status-query contracts, exposed through HTTP APIs, in-process application calls, or both depending on the host model.
- A generic merchant/business reference, such as `reservation-123`, `event-order-456`, or `cart-789`; the module does not model or manage those business objects.
- Card checkout through Stripe; PayPal checkout through PayPal.
- Apple Pay / Google Pay only where they can be enabled through the selected Stripe checkout integration and are supported by the merchant account, country, currency, device, and checkout configuration. Confirm availability during discovery before committing to a release promise.
- Payment persistence, lifecycle/state transitions, idempotency, provider webhook verification and deduplication, and handling of uncertain outcomes.
- Payment status query and full/partial refunds where supported by the selected integration.
- Simulated provider implementations for deterministic development and tests.
- .NET 10, automated tests, configuration/secrets management, logging, metrics, deployment, and operational documentation.

### Explicitly outside the first release unless discovery changes the priority

- Owning reservations, inventory, seat holds, event capacity, carts, order totals, taxes, or fulfilment.
- Subscriptions/recurring billing, payouts, split payments/marketplace transfers, saved cards, and payment-method vaulting.
- Building a fraud engine or dispute/chargeback management workflow.
- Automatic failover between multiple processors for the same payment method. The design should allow provider selection to evolve, but the first release can use an explicit mapping such as Card → Stripe and PayPal → PayPal.
- A universal authorization/capture workflow for every payment type. Model these as distinct capabilities and add them where a confirmed business use case requires them.
- Supporting every wallet or provider simply because the abstraction permits it. Add an integration only after provider capability, country, currency, and merchant-account eligibility are verified.

### Architectural assumptions

- The payment capability is a logical module, not a prescribed deployment unit. It may be hosted inside a modular monolith or deployed as an independent API/service. Keep Domain, Application, provider/persistence Infrastructure, and host-facing integration adapters separated so the deployment choice does not change the payment business rules.
- The host application's trusted backend supplies the amount, currency, and business reference. The module must not trust browser-submitted pricing as authoritative. The host application remains responsible for checking that the customer is entitled to pay that amount for the referenced business transaction.
- Checkout uses provider-hosted or provider-tokenized UI. The module never accepts or stores raw card data unless a separately approved compliance design explicitly requires it.
- The payment API is authenticated for consuming applications. Provider webhook endpoints are authenticated through each provider's signature-verification mechanism.
- The first routing configuration is static and explicit. Payment method and provider remain separate concepts, even if the initial configuration maps one method to one provider.
- A two-week sprint is a planning assumption, not a delivery commitment. Estimates should be assigned by the delivery team after story refinement and technical discovery.

---

## 3. Core design principles and decisions

1. **Keep host-domain concepts out of the payment domain.** Store a merchant/client identifier and merchant reference rather than `Reservation`, `Seat`, `Event`, `Cart`, or `Order` entities.
2. **Separate payment method from payment provider.** `Card` is a method; Stripe is a provider. PayPal may be both a customer-facing method and a provider integration, but the concepts should not be conflated in the core model.
3. **Separate workflow from integrations.** The application service coordinates the use case; method strategies express method-specific behaviour; provider adapters translate to/from external provider APIs.
4. **Prefer capability-specific interfaces.** Do not force every provider to implement refund, cancel, authorize, and capture if a provider/method combination does not support the operation. Check capability before dispatching an operation.
5. **Model outcomes explicitly.** Distinguish a definitive decline/failure from a timeout or unknown outcome. A lost response does not prove that the provider did not charge the customer.
6. **Persist before crossing the provider boundary.** Establish the local operation and idempotency record before making an external call. A local database transaction cannot atomically commit an external provider operation.
7. **Treat webhooks as at-least-once and potentially out of order.** Verify signatures, deduplicate deliveries, and enforce legal state transitions with concurrency protection.
8. **Use provider-neutral API contracts.** Provider-specific IDs are provider references, not the payment module's primary payment ID. Provider-specific checkout data should be exposed only where the client needs it and through a documented response shape.
9. **Test the core in isolation.** Fake providers are for deterministic tests and development, not a silent production fallback.
10. **Do not promise a fallback provider silently.** Unsupported methods and unavailable providers return explicit, controlled outcomes.

---

## 4. Keep payment initiation and outcome propagation separate

These are separate integration concerns, not necessarily separate services or deployments.

### A. Initiation: the business application asks the payment module to start an operation

- In a modular monolith, the business application can call a payment application contract in-process.
- With an independently deployed module, the caller can invoke an HTTP API.
- The module validates the request, persists the payment operation/idempotency record, and starts the provider flow.
- The response may be `Pending`, `RequiresAction`, or another non-final state, along with a checkout/next-action payload where needed. A successful HTTP response means the request was handled; it does not automatically mean the customer paid.

### B. Inbound provider notification: Stripe/PayPal tells the payment module what happened

A provider webhook is sent **to the payment module**. The module verifies the provider signature/authenticity, deduplicates the provider event, correlates it to a payment, and applies a legal state transition. A browser return URL is not proof of payment. The module must also handle missing, duplicated, and out-of-order provider notifications, with reconciliation for uncertain outcomes.

### C. Outcome propagation: the payment module tells the business application what happened

After a payment-state change is durably recorded, the module makes that outcome available to the business domain. Examples include: confirm a seat reservation, issue a ticket, or mark an order paid. These business actions remain owned by the consuming application.

- **Modular monolith:** publish an internal application/integration event or invoke a well-defined host integration handler. Keep module boundaries explicit; do not pass payment infrastructure types or mutate another domain's entities directly. If an event must survive process failure or coordinate with a separate transaction, use a durable outbox rather than relying on an in-memory event alone.
- **Independent API/service:** publish through a message broker or send a signed outbound merchant webhook. A status-query endpoint remains a fallback. Use durable delivery, retries, event IDs, and consumer deduplication where the mechanism is asynchronous.
- **Polling:** the business application can query payment status when the use case permits it. Polling is simple, but has latency and repeated-request costs; it should not be the only recovery strategy for workflows that require prompt, reliable notification.

These three paths are deliberately distinct: **business app → payment module** (initiation/query), **provider → payment module** (provider webhook), and **payment module → business app** (outcome propagation). They can use different protocols and can be hosted in one process or across services.

### Deployment-neutral boundary

Treat `Payment.Application` as the reusable payment use-case boundary. HTTP controllers, internal method calls, provider webhook endpoints, message publishers, and merchant-webhook delivery are adapters/composition choices. A monolith may host the module in the same process and database deployment; a standalone deployment may host it behind HTTP and separate persistence. In both cases, the payment core should not depend on ticketing/order/reservation entities, and business modules should integrate through explicit contracts.

---

## 5. Proposed solution structure

```text
PaymentSolution/
├── src/
│   ├── Payment.Domain/
│   │   ├── Entities/Payment.cs
│   │   ├── Entities/Refund.cs                  # if separate refund lifecycle is needed
│   │   ├── ValueObjects/Money.cs
│   │   ├── Enums/PaymentStatus.cs
│   │   └── Exceptions/
│   ├── Payment.Application/
│   │   ├── Abstractions/
│   │   │   ├── IPaymentMethod.cs
│   │   │   ├── IPaymentProvider.cs
│   │   │   ├── IPaymentRepository.cs
│   │   │   ├── IPaymentMethodResolver.cs
│   │   │   ├── IRefundProcessor.cs              # capability-specific
│   │   │   ├── IProviderWebhookHandler.cs
│   │   │   └── IPaymentEventPublisher.cs        # or outbox abstraction
│   │   ├── Payments/CreatePayment/
│   │   ├── Payments/GetPayment/
│   │   ├── Payments/RefundPayment/
│   │   └── Services/
│   ├── Payment.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Providers/Stripe/
│   │   ├── Providers/PayPal/
│   │   └── Messaging/                           # if needed for status events
│   └── Payment.Api/
│       ├── Controllers/PaymentsController.cs
│       ├── Controllers/ProviderWebhooksController.cs
│       └── Program.cs
└── tests/
    ├── Payment.Domain.Tests/
    ├── Payment.Application.Tests/
    ├── Payment.Infrastructure.Tests/
    └── Payment.Api.Tests/
```

The exact project count can be adjusted to the repository and deployment model. Keep dependency direction clear: Domain has no Application/Infrastructure dependency; Application depends on Domain and defines required abstractions; Infrastructure implements abstractions; API composes the application and infrastructure.

### Proposed provider-neutral API concepts

**Create payment request** (illustrative; refine as an API contract story):

```json
{
  "merchantReference": "reservation-123",
  "amount": 125.00,
  "currency": "USD",
  "paymentMethod": "Card",
  "returnUrl": "https://merchant.example/payment/return",
  "cancelUrl": "https://merchant.example/payment/cancel",
  "metadata": {
    "customerReference": "customer-456"
  }
}
```

The caller sends an idempotency key in a request header. Authentication identifies the consuming merchant/client; the request must not be allowed to impersonate a different merchant by setting a body field. The response includes the module's `paymentId`, current status, and a documented next-action/checkout payload when required. The exact payload differs by flow but should not leak SDK-specific object graphs.

**Generic payment record fields:** `PaymentId`, `MerchantId`, `MerchantReference`, `Amount`, `Currency`, `PaymentMethod`, `Provider`, `ProviderReference`, `Status`, timestamps, version/concurrency token, and safe metadata. Keep provider credentials and sensitive payment data out of this record.

### Initial state model

Use explicit transitions rather than arbitrary status assignment. A reasonable starting vocabulary is:

- `Pending`
- `RequiresAction`
- `Processing`
- `Authorized` (only for a flow that explicitly authorizes)
- `Succeeded`
- `Failed` (only when the failure is definitive)
- `Cancelled`
- `RefundPending`
- `PartiallyRefunded`
- `Refunded`
- `RequiresReconciliation` (or another explicit indeterminate representation)

The exact transitions must be agreed against the chosen provider flows. In particular, authorization is not capture, capture is not settlement, and a refund is a separate operation. Do not represent a timeout as `Failed` unless the provider confirms that no successful payment occurred.

---

## 6. Backlog and sprint plan

### Preparation before Sprint 1 — discovery and readiness

Time-box this activity; do not let it become an open-ended design phase.

**Tasks / decisions**

- Confirm merchant legal entity/country, provider account access, supported settlement currencies, and the intended Stripe/PayPal sandbox accounts.
- Verify whether card, Apple Pay, and Google Pay are enabled/eligible for the target merchant and checkout flow; decide whether wallets are part of the first release or a follow-up.
- Agree the consuming application's trust model, service-to-service authentication, and how amounts are calculated/validated by the host application.
- Decide how the host application will initiate payments (in-process application contract or HTTP API) and learn about later outcomes (status query, internal event, message broker, or signed outbound webhook). Treat these as separate choices; neither determines whether the module must be separately deployed.
- Confirm SQL database/deployment platform, secret store, logging/metrics stack, and whether there is an existing event bus.
- Create Architecture Decision Records (ADRs) for deployment shape, provider-neutral payment contract, idempotency, state model, and webhook processing.
- Obtain sandbox credentials and confirm a minimal successful sandbox transaction path for each provider.

**Exit criteria**

- Provider account/eligibility blockers are known rather than discovered in the final sprint.
- Initial API contract and status terminology are agreed.
- Product owner confirms MVP vs. next-release boundaries.
- Stories for Sprint 1 are refined, testable, and small enough to complete.

---

## Sprint 1 — Foundation and provider-independent vertical slice

**Sprint goal:** Establish the solution boundaries and demonstrate one complete payment use case with a deterministic simulated provider, without a real charge.

### PAY-001 — Create solution skeleton and engineering quality gates
**Priority:** Must

**Story**  
As a development team, we want a clean .NET solution with clear project boundaries and automated quality checks, so that core business logic stays independent of APIs, databases, and provider SDKs.

**Acceptance criteria**
- Given the repository is checked out, when the documented build/test command runs, then all projects compile and the test suite runs in CI.
- Domain does not reference API, Infrastructure, provider SDKs, or persistence implementation packages.
- Application depends only on Domain plus appropriate framework abstractions.
- Formatting/analyzer/test failures fail the CI build according to agreed repository standards.
- Configuration examples contain placeholders only; no real provider secrets are committed.

### PAY-002 — Define generic payment domain and lifecycle rules
**Priority:** Must

**Story**  
As the payment module, I want a provider-neutral payment model with explicit invariants and status transitions, so that all integrations follow consistent business rules.

**Acceptance criteria**
- Payment contains a module-generated payment ID, merchant/client reference, money, method, provider identity/reference, status, timestamps, and concurrency version as appropriate.
- Payment does not depend on order, reservation, seat, event, or cart domain classes.
- Negative/zero amounts, unsupported currencies, invalid currency precision, and invalid transitions are rejected according to agreed rules.
- Authorization, successful capture/payment, refund, cancellation, and indeterminate provider outcomes are not treated as interchangeable states.
- Unit tests cover valid and invalid transitions; currency support/precision rules are documented and configurable where appropriate.

### PAY-003 — Define the provider-neutral payment initiation/query contract
**Priority:** Must

**Story**  
As a consuming business application, I want a stable contract to initiate and query payments, so that I do not need to know whether Stripe or PayPal processes the transaction or whether the payment module is hosted in-process or separately.

**Acceptance criteria**
- The application contract defines create-payment request/response, get-payment response, stable error codes, and status vocabulary; an HTTP API adapter may expose the same use cases when required.
- Requests correlate to a caller-owned business record through a generic merchant reference.
- The response uses the module's payment ID and exposes a next-action/checkout payload only when required.
- Provider SDK objects and secrets are not serialized into public API responses.
- If HTTP is used, API versioning and validation-error conventions are documented; the core application contract does not depend on HTTP types.

### PAY-004 — Implement a simulated provider and application workflow
**Priority:** Must

**Story**  
As a developer/tester, I want a deterministic simulated provider, so that we can exercise success, decline, redirect/action-required, timeout, and indeterminate outcomes without contacting a real provider.

**Acceptance criteria**
- The application service depends on abstractions rather than directly constructing provider SDK clients.
- The simulated provider can produce controlled outcomes for automated tests.
- A create-payment request can traverse the selected host-facing entry point → application workflow → resolver → simulated provider → result mapping.
- The simulated provider cannot be selected accidentally in production configuration; startup validation or environment controls prevent silent production use.
- Unit tests prove the same application workflow works with a fake implementation.

### PAY-005 — Add consuming-application authentication and authorization
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

## Sprint 2 — Persistence, safe payment creation, and idempotency

**Sprint goal:** Make the payment workflow durable and safe under retries and concurrent requests before connecting real providers.

### PAY-006 — Persist and retrieve payment records
**Priority:** Must

**Story**  
As a consuming application, I want payment attempts persisted and queryable, so that payment status survives restarts and can be inspected later.

**Acceptance criteria**
- The repository can create, retrieve by payment ID, and retrieve within the authenticated merchant scope.
- Persistence maps money, currency, status, provider references, and timestamps without losing required precision.
- Database migrations/schema setup are repeatable in a clean environment.
- Concurrent state changes are protected using an agreed concurrency strategy.
- Repository integration tests verify round-trip data, merchant scoping, and concurrency behaviour.

### PAY-007 — Make payment creation idempotent
**Priority:** Must

**Story**  
As a customer/consuming application, I want retrying the same create-payment operation to return the same logical payment operation, so that network retries do not cause duplicate charges.

**Acceptance criteria**
- Create-payment requires an idempotency key, or applies another explicitly approved contract for generating one.
- The uniqueness scope includes the authenticated merchant/client and operation; the same key cannot be reused to create a different payment payload silently.
- The idempotency record/payment operation is persisted before the external-provider boundary is crossed.
- Concurrent duplicate requests result in one logical operation; the other request receives the existing/current outcome or a documented in-progress response.
- Reuse of a key with a materially different amount, currency, merchant reference, or method returns a controlled conflict.
- Tests cover concurrent requests, process restart, duplicate requests after success, and duplicate requests while processing.

### PAY-008 — Handle provider outcomes without unsafe assumptions
**Priority:** Must

**Story**  
As a payment system, I want success, decline, retryable failure, and indeterminate outcomes represented distinctly, so that transient faults do not cause duplicate payment attempts.

**Acceptance criteria**
- A definitive decline maps to a failed/declined outcome according to the public contract.
- A timeout or lost provider response can map to `Processing` or `RequiresReconciliation` rather than automatically to `Failed`.
- Provider request, local persistence, and response handling are designed for partial failure; the implementation does not claim one DB transaction can atomically include the external provider.
- Retry policies do not blindly replay non-idempotent provider operations.
- Tests simulate a provider completing an operation while the client times out before seeing the result.

### PAY-009 — Validate payment request and safe metadata
**Priority:** Must

**Story**  
As a consuming application, I want payment requests validated consistently, so that malformed or unsafe requests do not reach a provider.

**Acceptance criteria**
- Amount, currency, method, merchant reference, and return/cancel URLs are validated against documented rules.
- URL validation prevents unsafe redirect patterns according to an allow-list or equivalent agreed policy.
- Metadata has documented size/key limits and is not intended for card data, secrets, or other prohibited sensitive information.
- The server treats the amount supplied by the authenticated trusted backend as input to validate, not as evidence that the business transaction is payable; the host application's responsibility is documented.
- Validation errors are stable, actionable, and do not include secrets or internal exception details.

**Sprint 2 demo:** A payment is stored and can be retrieved. Replaying the same idempotency key—including concurrent replays—does not create a second logical operation.

---

## Sprint 3 — Real Stripe card checkout

**Sprint goal:** Complete a sandbox-backed Stripe card payment end to end, including asynchronous confirmation and safe status updates.

### PAY-010 — Implement Stripe provider adapter for card checkout
**Priority:** Must

**Story**  
As a customer, I want to pay by card through Stripe's supported checkout flow, so that my payment can be processed without the module handling raw card data.

**Acceptance criteria**
- Stripe SDK/API calls live only in the Stripe infrastructure adapter.
- The application workflow creates the provider operation using the persisted local payment/idempotency context.
- The checkout response contains only the provider-approved redirect/client-action information the client needs.
- Provider IDs are stored as provider references and are not substituted for the module's payment ID.
- Declines, validation errors, provider unavailability, and uncertain responses map to documented provider-neutral outcomes.
- Sandbox tests demonstrate a successful flow and a declined/failed flow without moving real money.
- No PAN/CVV or secret key is persisted or logged.

### PAY-011 — Process Stripe webhooks safely
**Priority:** Must

**Story**  
As the payment module, I want to receive authenticated Stripe payment events, so that the local payment state reflects provider-confirmed outcomes even when the browser does not return or the original request times out.

**Acceptance criteria**
- The endpoint verifies the provider's webhook signature using the documented verification mechanism and configured secret.
- Invalid signatures are rejected before the event changes payment state.
- Event identity is persisted/deduplicated so a repeated delivery does not repeat the business effect.
- The handler maps provider events to provider-neutral outcomes and applies only valid state transitions.
- Duplicate and out-of-order events do not move a payment backwards from a later valid state.
- Processing is concurrency-safe and safely retryable if local persistence temporarily fails.
- Sandbox/fixture tests cover valid signature, invalid signature, duplicate event, unknown payment reference, and out-of-order event.

### PAY-012 — Add Stripe configuration, health visibility, and diagnostics
**Priority:** Must

**Story**  
As an operator, I want Stripe configuration validated and integration failures diagnosable, so that a misconfigured integration is not mistaken for a customer payment decline.

**Acceptance criteria**
- API keys and webhook secrets are provided through approved secret configuration, never committed in source.
- Startup/configuration validation detects missing mandatory configuration in the relevant environment.
- Logs include correlation/payment IDs and provider request/correlation IDs when safe, but redact secrets and payment-sensitive data.
- Provider operational errors are distinguishable from customer declines in logs/metrics.
- Health/readiness behaviour reflects required local dependencies and configuration; a transient provider outage does not falsely mark every existing payment as failed.

**Sprint 3 demo:** A test client starts a Stripe sandbox card checkout; the resulting payment status becomes accurate through the API and/or a verified webhook, including a failed/declined scenario.

---

## Sprint 4 — Real PayPal checkout

**Sprint goal:** Add PayPal without rewriting the application workflow or leaking PayPal concepts into the domain/application API.

### PAY-013 — Implement PayPal checkout and capture adapter
**Priority:** Must

**Story**  
As a customer, I want to pay through PayPal, so that the module supports a second real payment option through the same provider-neutral contract.

**Acceptance criteria**
- PayPal API/SDK types are isolated in the PayPal infrastructure adapter.
- The flow supports the required create/approval/return/capture steps for the chosen PayPal integration.
- The API represents customer action/redirect requirements in a provider-neutral response shape.
- The local payment operation and idempotency context are persisted before external operations that can move money.
- Approval is not treated as successful capture unless the chosen flow's confirmed semantics say the funds have been captured.
- PayPal decline, expiration, timeout, provider error, and success are mapped to documented neutral outcomes.
- Sandbox end-to-end tests demonstrate successful and unsuccessful payment flows.

### PAY-014 — Process PayPal webhooks safely
**Priority:** Must

**Story**  
As the payment module, I want to authenticate and process PayPal webhook notifications, so that delayed or asynchronous PayPal outcomes update the correct payment safely.

**Acceptance criteria**
- Webhook authenticity is verified using PayPal's supported verification mechanism.
- Event deduplication, correlation to the local payment, state-transition validation, and concurrency protection use the shared webhook-processing rules.
- Duplicate/out-of-order events do not double-apply payment or refund effects.
- An uncorrelated event is logged safely and handled according to a documented operational policy; it does not create an arbitrary payment record.
- Tests cover valid/invalid verification, duplicated notifications, and status ordering.

### PAY-015 — Resolve method/provider independently and validate capability support
**Priority:** Must

**Story**  
As the payment module, I want payment-method selection separated from provider implementation, so that the core workflow can support more providers later without being rewritten.

**Acceptance criteria**
- A dedicated resolver selects the requested method strategy using a stable identifier.
- Initial configuration explicitly maps supported methods to Stripe or PayPal; no silent fallback to a different method/provider occurs.
- The method and provider are recorded separately where they represent different concepts.
- Unsupported method/provider/capability combinations return a controlled error before attempting an unsupported operation.
- Adding a new provider implementation does not require adding provider-specific branches to the main application workflow.

**Sprint 4 demo:** The same create/status API supports a Stripe card checkout and PayPal checkout; both are backed by real sandbox integrations and map results into the shared payment state model.

---

## Sprint 5 — Refunds, reconciliation, and application integration

**Sprint goal:** Complete the primary payment lifecycle and make delayed/uncertain outcomes recoverable and usable by the consuming application.

### PAY-016 — Process full and partial refunds
**Priority:** Must

**Story**  
As an authorized consuming application, I want to request full or partial refunds for eligible payments, so that the customer can receive money back without the host application calling provider APIs directly.

**Acceptance criteria**
- Refund requests reference the module payment ID, amount, and an idempotency key.
- The domain validates that the payment is eligible for a refund and that the requested amount does not exceed the remaining refundable amount.
- Refunds are modeled/tracked as separate operations or records, including provider reference, amount, currency, status, and timestamps as needed.
- Only provider/payment combinations with the required refund capability are offered the operation.
- A refund request is not reported as completed until the provider's confirmed outcome meets the agreed semantics; pending outcomes remain pending.
- Duplicate refund requests do not refund the same amount twice.
- Sandbox/fixture tests cover full refund, partial refund, exceeding the remaining amount, duplicate requests, provider failure, and asynchronous confirmation where applicable.

### PAY-017 — Reconcile indeterminate payment outcomes
**Priority:** Must

**Story**  
As an operator/system, I want an indeterminate payment to be checked against the provider's authoritative state, so that the module can recover safely after timeouts or missed webhooks.

**Acceptance criteria**
- Payments in an agreed indeterminate/stale state are queryable by a reconciliation process or controlled operations endpoint.
- The reconciler uses provider references and supported provider lookup APIs; it does not create a new charge as a way to discover the original outcome.
- Reconciliation is idempotent and cannot overwrite a newer valid state with an older result.
- Retries use bounded/backoff policies and distinguish transient transport errors from definitive provider outcomes.
- Exhausted or ambiguous cases are visible for operational intervention rather than silently marked failed.
- Tests cover provider timeout after a successful charge, lost webhook followed by reconciliation, and concurrent webhook/reconciliation updates.

### PAY-018 — Define payment outcome propagation for the chosen host model
**Priority:** Must for defining the contract; delivery adapter depends on the deployment/integration model

**Story**  
As a consuming business application, I want to learn reliably when a payment outcome changes, so that I can confirm a reservation, issue a ticket, or continue order processing without embedding provider-specific logic in my business workflow.

**Acceptance criteria**
- A provider-neutral payment outcome contract identifies the payment ID, merchant/client scope, merchant reference, event ID, status, and relevant timestamps without exposing secrets or sensitive payment data.
- Initiating a payment, querying its current status, receiving inbound provider webhooks, and propagating outcomes to the business application are documented as distinct interactions.
- The outcome propagation adapter is selected for the host model: an in-process application event/handler for a modular monolith; or a durable message/event delivery mechanism or signed outbound webhook for an independently deployed module. A host may also use polling where suitable.
- Payment outcome notifications are emitted only after the corresponding payment-state change is durably committed. Use a transactional outbox where needed to prevent a database commit and external message publication from getting out of sync.
- Asynchronous delivery is treated as at-least-once; consumers deduplicate by event ID and tolerate retries/out-of-order delivery. Internal synchronous handlers must also handle failures and transaction boundaries explicitly.
- If outbound webhooks are selected, signatures, retry/backoff, delivery visibility, secret rotation, and replay protection are specified and tested.
- The consuming application can query the module's current payment state as a fallback/source of truth; a browser redirect or return URL alone is never treated as authoritative proof of payment.
- Tests cover repeated delivery, propagation failure/retry, and the host integration's handling of success, failure, and indeterminate outcomes.

### PAY-019 — Support cancellation/void where provider capability and state allow
**Priority:** Should

**Story**  
As an authorized consuming application, I want to cancel an eligible in-progress payment or void an authorization, so that abandoned or no-longer-valid payment attempts can be handled explicitly.

**Acceptance criteria**
- Cancellation/void is distinct from refund and only appears for states and provider/method combinations where it is supported.
- The domain rejects cancellation after an incompatible terminal state.
- Provider cancellation outcomes follow the same idempotency, timeout, and reconciliation rules as other money-moving operations.
- Unsupported operations return a controlled capability error.
- Unit and sandbox/fixture tests cover valid cancellation, invalid state, duplicate request, and uncertain provider outcome.

**Sprint 5 demo:** A completed payment can be queried and refunded; uncertain outcomes can be reconciled; the consuming application's notification/polling path is demonstrated.

---

## Sprint 6 — Production hardening, security, and release

**Sprint goal:** Demonstrate that the module can be operated safely, diagnosed, deployed repeatably, and integrated by a host application.

### PAY-020 — Add audit trail, structured logs, metrics, and correlation
**Priority:** Must

**Story**  
As an operator/support engineer, I want to trace a payment across the host-facing entry point, database, provider adapter, and webhook/event pipeline, so that failures can be investigated without exposing sensitive data.

**Acceptance criteria**
- A correlation ID and module payment ID connect related logs where available.
- Important lifecycle actions—creation, provider call outcome, webhook processing, refund, reconciliation, and manual intervention—are auditable according to agreed retention/access policies.
- Metrics cover payment outcomes, provider errors/timeouts, webhook verification/deduplication failures, reconciliation backlog, and refund outcomes.
- Logs do not contain credentials, raw card data, security codes, access tokens, or unredacted sensitive provider payloads.
- Dashboard/alert thresholds are agreed with the service owner before release.

### PAY-021 — Complete security and resilience verification
**Priority:** Must

**Story**  
As the service owner, I want security and failure-mode tests, so that common integration faults do not cause data leakage or duplicate money movement.

**Acceptance criteria**
- Tests cover unauthorized cross-merchant access, webhook signature rejection, request validation, secret redaction, duplicate API calls, duplicate webhooks, out-of-order events, and concurrency conflicts.
- Provider HTTP calls use explicit timeouts and bounded retry policies appropriate to each operation; non-idempotent operations are not blindly retried.
- Database migration, startup, provider-unavailable, database-unavailable, and secret-missing behaviours are exercised.
- Dependency/security scanning and code review gates meet the team's agreed baseline.
- A focused load test validates the agreed target workload and records results; performance targets are defined with the product/service owner rather than guessed.

### PAY-022 — Package, deploy, and document the module in the chosen host model
**Priority:** Must

**Story**  
As an operator/integrator, I want repeatable packaging/deployment and clear integration/runbook documentation, so that the payment module can be configured and supported in either a modular monolith or an independent deployment.

**Acceptance criteria**
- The chosen hosting model is documented and deployment is repeatable: deploy the host application containing the module, or deploy the independent Payment API, as selected.
- Database migration/rollback approach is documented and tested in a representative environment.
- Readiness/liveness behaviour is documented and verified.
- An integration guide includes authentication, create/query/refund contracts, idempotency key rules, statuses, redirects/return handling, webhook/event behaviour, and error handling.
- Runbook covers provider outage, stuck/indeterminate payments, webhook failures, reconciliation, secret rotation, and support escalation.
- Production uses real provider credentials only from the approved secret store; simulated providers are disabled.

### PAY-023 — Execute business acceptance and release readiness
**Priority:** Must

**Story**  
As the product owner, I want end-to-end acceptance scenarios completed, so that the first consuming application can integrate with confidence.

**Acceptance criteria**
- Product/business representatives approve card success, card decline, PayPal success/failure, user action/redirect, timeout/indeterminate outcome, duplicate create request, webhook retry, refund, and status-query scenarios.
- The consuming application confirms that it—not the payment module—owns reservation/order/seat inventory rules and reacts correctly to payment state changes.
- Known limitations and unsupported provider/method/currency combinations are published.
- No open release-blocking security, duplicate-payment, data-integrity, or reconciliation defects remain.
- Release decision and rollback/disable-provider plan are documented.

**Sprint 6 demo/release gate:** A representative host application completes accepted sandbox scenarios through both providers, while automated tests and operational runbooks demonstrate safe recovery paths.

---

## 7. Sprint summary and dependency map

| Sprint | Goal | Main stories | Exit outcome |
|---|---|---|---|
| Preparation | Resolve unknowns early | Provider eligibility, authentication, API contract, ADRs | MVP is agreed and sandbox access is ready |
| 1 | Establish architecture and simulated vertical slice | PAY-001–PAY-005 | Generic payment contract works through the selected host entry point using a safe fake provider |
| 2 | Make operations durable and retry-safe | PAY-006–PAY-009 | Persistence, validation, and idempotent create are verified |
| 3 | Complete Stripe card checkout | PAY-010–PAY-012 | Stripe sandbox flow and verified Stripe webhooks work |
| 4 | Add PayPal without changing core workflow | PAY-013–PAY-015 | PayPal sandbox flow and verified PayPal webhooks work |
| 5 | Complete lifecycle and integration behaviour | PAY-016–PAY-019 | Refunds, reconciliation, notifications, and optional cancellation work |
| 6 | Prove production readiness | PAY-020–PAY-023 | Security, operations, documentation, UAT, and release gate pass |

**Critical dependencies:**

- Domain/state model and API contract precede provider adapters.
- Persistence and idempotency precede real money-moving calls.
- Common webhook rules precede provider-specific event handlers.
- Each provider adapter must have sandbox credentials, verified capabilities, and test scenarios before its story is considered done.
- Refund support depends on successful/captured payment state and provider capability verification.
- Production release depends on reconciliation, observability, security review, and a consuming-application acceptance test.

---

## 8. Definition of Ready (DoR)

A story is ready for sprint planning when:

- The user/business outcome is clear and the story has a single accountable owner.
- Acceptance criteria are observable and testable.
- Dependencies and provider-specific assumptions are identified.
- API contract, state transitions, and failure semantics are documented where relevant.
- Required sandbox credentials, secrets access, test fixtures, and environment access are available or explicitly tracked as blockers.
- Security/privacy implications are considered, especially for payment data, merchant scoping, redirects, webhooks, and logs.
- The team has estimated the story and split it if it cannot reasonably complete inside a sprint.

## 9. Definition of Done (DoD)

A story is done when:

- Code is reviewed and follows agreed coding conventions.
- Unit tests cover domain/application behaviour; integration tests cover persistence/provider mappings where appropriate.
- Relevant failure paths are tested, not only the happy path.
- API contracts, configuration, migrations, and integration documentation are updated.
- Logs/metrics are safe and useful; no secrets or prohibited payment data are exposed.
- CI passes, security scanning is clean to the agreed threshold, and no known blocker remains.
- A demo proves the acceptance criteria in a reproducible environment.
- Any operational impact, unsupported capability, and rollback consideration is documented.

---

## 10. Key risks and mitigations

| Risk | Why it matters | Mitigation / owner action |
|---|---|---|
| Provider account/country/currency eligibility blocks a desired method | A technically complete adapter may not be usable by the actual merchant | Validate legal entity, account, settlement currency, and method availability during preparation; record exclusions explicitly |
| Provider timeout after the charge succeeds | Retrying blindly can charge the customer twice | Persist operations first, use idempotency, model indeterminate outcomes, reconcile against provider state |
| Duplicate or out-of-order webhooks | Payment state or refund totals can become incorrect | Verify signatures, persist event IDs, enforce transition rules, use concurrency control, and test re-delivery |
| The module assumes it owns order/reservation truth | Can accept the wrong amount or fulfil the wrong business item | Keep host references generic; document that the host backend validates payable amount and fulfilment entitlement |
| Generic abstraction becomes over-engineered | More layers can delay the first working integration | Implement only abstractions with a demonstrated boundary; start with explicit routing and add dynamic routing only when needed |
| Provider-specific semantics leak into the domain/API | Future provider additions become breaking changes | Map provider responses to agreed neutral outcomes; isolate SDK models in infrastructure |
| Secret or payment-sensitive data is logged | Security and compliance exposure | Central redaction, safe structured logging, secret store, no raw card handling, security tests |
| Webhook or event delivery is unavailable | Host app may not learn that payment completed | Keep status query as a fallback; add durable event/outbox or signed webhook delivery based on deployment choice |
| A provider capability is assumed but not actually available | Refunds, wallets, or authorization may not work for a particular account/region | Maintain a capability matrix per method/provider/account and reject unsupported operations explicitly |

---

## 11. Prioritization recommendation

### Must have for the first production release

- Provider-neutral payment create/status API and generic merchant reference.
- Explicit payment domain/state model and money/currency validation.
- Authenticated merchant scoping.
- Durable persistence and concurrency control.
- Idempotent payment initiation and refund operations.
- Simulated provider for tests plus real Stripe card and PayPal checkout adapters.
- Verified and deduplicated Stripe and PayPal webhooks.
- Correct handling of declines, timeouts, asynchronous confirmation, and reconciliation.
- Full/partial refund support where verified for the configured provider/payment path.
- Safe configuration, logging, monitoring, deployment, integration guide, and acceptance tests.

### Should have, based on delivery capacity and host-app needs

- Durable payment status notification to the host application, using an outbox/event bus or signed merchant webhook.
- Cancellation/void where supported by the provider and valid payment state.
- Wallets through Stripe, only after eligibility and checkout requirements are confirmed.

### Later releases

- Multiple providers for the same payment method and configurable routing/failover.
- A universal authorize-then-capture workflow if a real business case requires seat/booking hold semantics.
- Additional wallet or local payment methods.
- Saved payment methods, subscriptions, split payments, payouts, disputes, and advanced fraud tooling.

---

## 12. Scrum Master / Tech Lead operating recommendations

- Keep each sprint demonstrable with a working vertical slice; avoid spending multiple sprints only creating abstractions.
- Review risks/dependencies in refinement: provider account eligibility, webhook exposure, authentication, currency rules, and host-app notification are not minor implementation details.
- Maintain separate acceptance criteria for business behaviour, provider integration, and operational behaviour.
- Use a capability matrix rather than assuming all providers support all operations.
- Treat the idempotency, indeterminate-outcome, webhook, and reconciliation paths as core payment features—not post-MVP polish.
- Do not score a story as done merely because the provider returns HTTP 2xx; verify the provider business status and the corresponding local state.
- Use fake providers for deterministic tests, provider sandbox accounts for adapter/UAT verification, and never run automated tests against live money-moving credentials.
- Re-plan after Sprint 2 using actual throughput and integration complexity. Provider onboarding and account eligibility may change the forecast.

## 13. First refinement meeting — decisions to close

1. Which hosting shape is preferred for the first consumer: a module inside a modular monolith, or an independently deployed Payment API? Keep this as a deployment decision; the domain/application contracts should support either. Separately, decide the required outcome-propagation mechanism for that host (in-process event, message broker, outbound webhook, or polling).
2. Which country/legal entity and settlement currencies will the merchant use, and are Stripe/PayPal accounts approved for them?
3. Should first release include Apple Pay/Google Pay via Stripe, or is card + PayPal enough for the first release?
4. Is immediate capture sufficient, or must reservation/ticket scenarios authorize funds and capture later?
5. Which authentication mechanism and deployment/secrets/observability infrastructure already exist?
6. Is full/partial refund required at launch, and who is allowed to initiate refunds?
7. What response-time, availability, retention, audit, and support expectations are required for launch?

**Recommended baseline:** Start with a provider-independent payment application contract and state model, use a fake provider to prove the workflow, implement Stripe and PayPal as isolated adapters, and make idempotency, verified inbound provider webhooks, and reconciliation release-critical. Keep HTTP controllers, in-process calls, outbound notifications, and deployment composition as adapters around the same core, so the module can run inside a modular monolith or as an independent API without changing its payment business rules.
