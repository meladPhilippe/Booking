# Sprint 5 — Refunds, Reconciliation, and Integration

This is the implementation record for PAY-016 through PAY-018.

The module now tracks refunds separately from payments, protects refund creation with idempotency and remaining-balance checks, reconciles indeterminate payments and refunds, and records payment/refund outcomes in a transactional outbox. Reconciliation resolves the provider key persisted on the record, queries authoritative provider state when an external ID is known, and safely replays an uncertain create only with its original durable idempotency key. A scheduled worker uses claims, bounded retries, and alerting for exhausted cases. The default host model is an independent Payment API that sends signed HTTPS merchant callbacks; the host can always query the payment API as its source of truth.

See the detailed engineering guide in [Sprint 5 implementation plan](../../docs/sprints/sprint-5-implementation-plan.md).

PAY-019 cancellation/void is explicitly deferred because the first-release Stripe Checkout and PayPal CAPTURE flows do not share a safe common cancellation operation.
