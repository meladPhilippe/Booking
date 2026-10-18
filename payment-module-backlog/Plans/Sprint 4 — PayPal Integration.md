# Sprint 4 — PayPal Integration

Implement PayPal as an explicit payment method using PayPal JavaScript SDK Buttons and server-side Orders capture. The Payment API creates a local idempotent operation and PayPal Order, returns only a provider-neutral `paypal_order` action, and captures only after buyer approval through a merchant-scoped idempotent endpoint.

PayPal REST, OAuth, Orders, and webhook-signature verification stay in the Infrastructure PayPal adapter. Webhooks reuse the durable event inbox and legal payment-state transitions. The implementation guide at `docs/sprints/sprint-4-implementation-plan.md` documents configuration, sandbox verification, the selected JS SDK Buttons flow, the rejected redirect alternative, event mappings, and deferred capability scope.
