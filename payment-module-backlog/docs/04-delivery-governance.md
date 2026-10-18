# Delivery Governance and Release Criteria

## Sprint Summary and Dependency Map

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

## Definition of Ready (DoR)

A story is ready for sprint planning when:

- The user/business outcome is clear and the story has a single accountable owner.
- Acceptance criteria are observable and testable.
- Dependencies and provider-specific assumptions are identified.
- API contract, state transitions, and failure semantics are documented where relevant.
- Required sandbox credentials, secrets access, test fixtures, and environment access are available or explicitly tracked as blockers.
- Security/privacy implications are considered, especially for payment data, merchant scoping, redirects, webhooks, and logs.
- The team has estimated the story and split it if it cannot reasonably complete inside a sprint.

## Definition of Done (DoD)

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

## Key Risks and Mitigations

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

## Prioritization Recommendation

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

## Scrum Master / Tech Lead Operating Recommendations

- Keep each sprint demonstrable with a working vertical slice; avoid spending multiple sprints only creating abstractions.
- Review risks/dependencies in refinement: provider account eligibility, webhook exposure, authentication, currency rules, and host-app notification are not minor implementation details.
- Maintain separate acceptance criteria for business behaviour, provider integration, and operational behaviour.
- Use a capability matrix rather than assuming all providers support all operations.
- Treat the idempotency, indeterminate-outcome, webhook, and reconciliation paths as core payment features—not post-MVP polish.
- Do not score a story as done merely because the provider returns HTTP 2xx; verify the provider business status and the corresponding local state.
- Use fake providers for deterministic tests, provider sandbox accounts for adapter/UAT verification, and never run automated tests against live money-moving credentials.
- Re-plan after Sprint 2 using actual throughput and integration complexity. Provider onboarding and account eligibility may change the forecast.

## First Refinement Meeting — Decisions to Close

1. Which hosting shape is preferred for the first consumer: a module inside a modular monolith, or an independently deployed Payment API? Keep this as a deployment decision; the domain/application contracts should support either. Separately, decide the required outcome-propagation mechanism for that host (in-process event, message broker, outbound webhook, or polling).
2. Which country/legal entity and settlement currencies will the merchant use, and are Stripe/PayPal accounts approved for them?
3. Should first release include Apple Pay/Google Pay via Stripe, or is card + PayPal enough for the first release?
4. Is immediate capture sufficient, or must reservation/ticket scenarios authorize funds and capture later?
5. Which authentication mechanism and deployment/secrets/observability infrastructure already exist?
6. Is full/partial refund required at launch, and who is allowed to initiate refunds?
7. What response-time, availability, retention, audit, and support expectations are required for launch?

**Recommended baseline:** Start with a provider-independent payment application contract and state model, use a fake provider to prove the workflow, implement Stripe and PayPal as isolated adapters, and make idempotency, verified inbound provider webhooks, and reconciliation release-critical. Keep HTTP controllers, in-process calls, outbound notifications, and deployment composition as adapters around the same core, so the module can run inside a modular monolith or as an independent API without changing its payment business rules.
