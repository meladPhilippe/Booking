# Provider Routing and Reconciliation Hardening

## Purpose

This follow-on plan makes provider choice immutable for an existing payment and
makes indeterminate provider operations recoverable. It supplements, rather
than replaces, the Sprint 1 through Sprint 6 delivery plans.

## Shared design decisions

- A **payment method** is customer-facing (`Card`, `PayPal`); a **provider** is
  the external processor (`stripe`, `paypal`, `simulated`). They are separate
  concepts.
- `ProviderKey` is a stable machine identifier, persisted at creation. It is
  not a display name and is used for every later provider call on that record.
- Current configuration chooses a provider only for a **new** payment. Refund,
  capture, webhook recovery, and reconciliation resolve the persisted key.
- Use `ProviderOperationId` for a Checkout Session or PayPal Order and
  `ProviderPaymentId` for a Stripe PaymentIntent or PayPal Capture. Do not use
  `Settlement` for these identifiers.
- Development registers only `SimulatedPaymentProvider`; production registers
  only approved live providers. A missing route is an explicit unsupported
  payment method, never a fallback.

## Plan A — Provider routing and adapter contracts

1. Replace `PaymentProviders:Card` with `PaymentRouting:Methods`, a map from
   `PaymentMethod` name to provider key. Validate at startup that every route
   resolves to a registered provider and that environment rules are met.
2. Persist `ProviderKey` on every payment and refund. Migrate existing values
   (`Stripe` → `stripe`, `PayPal` → `paypal`, `Simulated` → `simulated`).
3. Replace method-only provider resolution with a registry that supports:

   ```csharp
   ResolveForNewPayment(PaymentMethod method);
   ResolveByKey(string providerKey);
   ```

4. Keep `IPaymentProvider` to initiation only. Model optional operations using
   `ICaptureProvider`, `IRefundProvider`,
   `IPaymentReconciliationProvider`, and
   `IRefundReconciliationProvider`.
5. Resolve capability interfaces after `ResolveByKey`; remove method-based
   `Supports`/`SupportsRefunds` checks.
6. Keep `IStripeGateway` as the Stripe anti-corruption layer and introduce an
   equivalent `IPayPalGateway`, moving PayPal HTTP/OAuth/protocol work out of
   `PayPalPaymentProvider`.
7. Prove with tests that a route change affects only new payments: an older
   Stripe payment is still refunded and reconciled by Stripe.

## Plan B — Scheduled reconciliation and safe recovery

1. Record reconciliation scheduling data for payments and refunds: attempt
   count, next-attempt time, deadline, last safe error, and a lease/claim.
2. Persist a durable provider idempotency key and enough immutable request data
   to reproduce an initiation request.
3. For an uncertain create with no external ID, recover by replaying the same
   provider request with its original idempotency key. This retrieves the
   original provider operation; it must not create a second charge.
4. For records with external IDs, query the provider's authoritative payment or
   refund state. Apply only legal, non-regressive local transitions.
5. Add a hosted worker that claims due work in batches, uses bounded exponential
   backoff, and is safe when multiple service instances run.
6. Alert and expose an operations queue when the attempt/deadline budget is
   exhausted; never silently turn an unknown outcome into `Failed`.
7. Test retry recovery, lost webhooks, concurrent webhook/worker updates,
   duplicate worker claims, refund recovery, and exhausted retries.

## Suggested delivery order

Deliver Plan A through persisted provider-key routing first. Plan B depends on
that invariant: a worker must reconcile an old payment through the provider
that created it, not through whatever provider today's method route selects.
