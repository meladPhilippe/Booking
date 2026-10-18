# Sprint 3 — Stripe Integration

## Objective

Deliver sandbox-backed embedded Stripe Checkout for Card payments and verified, deduplicated Stripe webhooks without exposing raw card data or Stripe SDK types outside Infrastructure.

## Implementation decisions

- Use `Stripe.net` in `Payment.Infrastructure/Providers/Stripe` only.
- Create embedded Checkout Sessions in one-time `payment` mode, card-only, with a validated request `returnUrl`.
- Return `{ type: "embedded_checkout", clientSecret }` as a provider-neutral next action; frontend owns Stripe.js mounting.
- Use local payment ID for Stripe request idempotency, `client_reference_id`, and safe Stripe metadata; persist the `cs_...` session ID locally.
- Verify raw webhook body signatures, accept thin events, fetch current related Stripe resources, and deduplicate with `ProviderWebhookEvents`.
- Map Stripe events only through legal local transitions; terminal outcomes never regress.

## Required configuration and demo

Provide test secret key, webhook signing secret, permitted HTTPS return origins, and optional frontend publishable key through a secret store. Use `stripe listen --forward-thin-to` for local sandbox delivery and Stripe test cards for successful, declined, and authentication flows.

See the repository implementation guide at `docs/sprints/sprint-3-implementation-plan.md` for API behavior, webhook events, exact setup, and official Stripe references.
