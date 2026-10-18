# Architecture and Integration

## Core Design Principles and Decisions

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

## Keep Payment Initiation and Outcome Propagation Separate

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

## Proposed Solution Structure

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
