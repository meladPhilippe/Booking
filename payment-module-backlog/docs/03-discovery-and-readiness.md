# Discovery and Readiness

### Discovery and Readiness Before Sprint 1

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
