# Sprint 1 — Implementation Plan

## Goal

Deliver a reusable, provider-neutral payment create/read workflow through an HTTP API, backed by SQL Server and a deterministic simulated provider. No real payment is created.

## Included work

- **PAY-001:** `Payment.Domain`, `Payment.Application`, `Payment.Infrastructure`, and `Payment.Api` form a one-way dependency graph. Test projects, analyzers, nullable references, warning-as-error, and local Compose support are included.
- **PAY-002:** `Payment`, `Money`, configurable ISO currency precision, and explicit payment-state transitions are implemented in the domain.
- **PAY-003:** Application contracts expose create and get-payment operations, stable business error codes, and provider-neutral responses.
- **PAY-004:** A simulated Card provider produces success, decline, action-required, timeout, and indeterminate outcomes without external calls.

## Architecture decisions

- The host boundary is HTTP: `POST /api/v1/payments` and `GET /api/v1/payments/{paymentId}`.
- SQL Server is used from Sprint 1. EF Core migrations are the sole schema authority.
- EF Core and Dapper repositories target the same schema; `Persistence:Provider` selects `EfCore` or `Dapper` at startup.
- A configured Development merchant context supplies merchant identity. It is not authentication.
- Supported currency codes and their scales are configured under `Currencies:Supported`.
- The simulated provider is registered only in Development. Its default outcome is configuration-driven; `X-Simulated-Outcome` is a Development-only per-request override.

## Work slices

1. Create the solution, projects, test projects, local SQL Server Compose service, configuration samples, and build/test commands.
2. Implement domain validation and lifecycle tests before adapters.
3. Add application ports for persistence, merchant context, currency rules, provider resolution, and provider invocation.
4. Add the EF migration, EF repository, Dapper repository, and runtime selection validation.
5. Add the simulated provider, HTTP controller, Problem Details error mapping, and local demo instructions.

## API contract

`POST /api/v1/payments` accepts `merchantReference`, `amount`, `currency`, and `paymentMethod`. It returns the module payment ID, neutral status, module/provider references, timestamps, and optional next action. It never exposes provider SDK types or credentials.

`GET /api/v1/payments/{paymentId}` returns the same neutral representation. Idempotency is intentionally not implemented yet, so each valid create request produces a new payment record.

## Test and demo evidence

- Domain tests cover money and legal/illegal status transitions.
- Application tests use fake ports to prove provider-neutral workflow and error behavior.
- Infrastructure tests cover deterministic simulator mappings. Repository contract tests should run against the Compose SQL Server in CI once a database test fixture is configured.
- API contract tests verify that request fields remain provider-neutral.
- Demo: start Compose, run the API, create a simulated payment, and retrieve its returned payment ID.

## 🔴 Open decisions / deferred scope

- **PAY-005 is deferred.** The API has no real authentication/authorization and must not be deployed publicly.
- Confirm production currencies and decimal scales.
- Confirm the future merchant identity provider and claim convention.
- Idempotency, webhooks, refunds, real providers, outcome events, and Ticketing/Reservation integration belong to later sprints.
