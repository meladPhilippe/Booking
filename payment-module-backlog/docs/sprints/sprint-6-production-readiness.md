# Sprint 6 — Production Readiness

**Sprint goal:** Demonstrate that the module can be operated safely, diagnosed, deployed repeatably, and integrated by a host application.

## PAY-020 — Add audit trail, structured logs, metrics, and correlation
**Priority:** Must

**Story**  
As an operator/support engineer, I want to trace a payment across the host-facing entry point, database, provider adapter, and webhook/event pipeline, so that failures can be investigated without exposing sensitive data.

**Acceptance criteria**
- A correlation ID and module payment ID connect related logs where available.
- Important lifecycle actions—creation, provider call outcome, webhook processing, refund, reconciliation, and manual intervention—are auditable according to agreed retention/access policies.
- Metrics cover payment outcomes, provider errors/timeouts, webhook verification/deduplication failures, reconciliation backlog/age/attempt exhaustion, worker claim failures, and refund outcomes.
- Logs do not contain credentials, raw card data, security codes, access tokens, or unredacted sensitive provider payloads.
- Dashboard/alert thresholds are agreed with the service owner before release.

## PAY-021 — Complete security and resilience verification
**Priority:** Must

**Story**  
As the service owner, I want security and failure-mode tests, so that common integration faults do not cause data leakage or duplicate money movement.

**Acceptance criteria**
- Tests cover unauthorized cross-merchant access, webhook signature rejection, request validation, secret redaction, duplicate API calls, duplicate webhooks, out-of-order events, and concurrency conflicts.
- Provider HTTP calls use explicit timeouts and bounded retry policies appropriate to each operation; non-idempotent operations are not blindly retried.
- Database migration, startup, provider-unavailable, database-unavailable, and secret-missing behaviours are exercised.
- Dependency/security scanning and code review gates meet the team's agreed baseline.
- A focused load test validates the agreed target workload and records results; performance targets are defined with the product/service owner rather than guessed.

## PAY-022 — Package, deploy, and document the module in the chosen host model
**Priority:** Must

**Story**  
As an operator/integrator, I want repeatable packaging/deployment and clear integration/runbook documentation, so that the payment module can be configured and supported in either a modular monolith or an independent deployment.

**Acceptance criteria**
- The chosen hosting model is documented and deployment is repeatable: deploy the host application containing the module, or deploy the independent Payment API, as selected.
- Database migration/rollback approach is documented and tested in a representative environment.
- Readiness/liveness behaviour is documented and verified.
- An integration guide includes authentication, create/query/refund contracts, idempotency key rules, statuses, redirects/return handling, webhook/event behaviour, and error handling.
- Runbook covers provider outage, stuck/indeterminate payments and refunds, worker retry exhaustion, webhook failures, reconciliation, secret rotation, and support escalation.
- Production uses real provider credentials only from the approved secret store; simulated providers are disabled.

## PAY-023 — Execute business acceptance and release readiness
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
