# Sprint 5 — Refunds, Reconciliation, and Integration

This is the implementation record for PAY-016 through PAY-018.

The module now tracks refunds separately from payments, protects refund creation with idempotency and remaining-balance checks, reconciles only indeterminate payments by querying provider state, and records payment/refund outcomes in a transactional outbox. The default host model is an independent Payment API that sends signed HTTPS merchant callbacks; the host can always query the payment API as its source of truth.

See the detailed engineering guide in [Sprint 5 implementation plan](../../docs/sprints/sprint-5-implementation-plan.md).

PAY-019 cancellation/void is explicitly deferred because the first-release Stripe Checkout and PayPal CAPTURE flows do not share a safe common cancellation operation.
