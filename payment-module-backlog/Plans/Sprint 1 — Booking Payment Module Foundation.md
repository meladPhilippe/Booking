# Sprint 1 — Booking Payment Module Foundation

## Summary

Create a new `Booking/Booking.sln` alongside the existing backlog folders. It will contain a reusable, provider-neutral `Payment.*` module with a SQL-backed HTTP API and simulated payment provider.

Sprint 1 proves payment creation and lookup only. It does not integrate with `Ticketing-Solution`, move real money, process webhooks, support refunds, or provide production authentication.

## Solution and documentation

```text
Booking/
├── Booking.sln
├── src/
│   ├── Payment.Domain/
│   ├── Payment.Application/
│   ├── Payment.Infrastructure/
│   └── Payment.Api/
├── tests/
│   ├── Payment.Domain.Tests/
│   ├── Payment.Application.Tests/
│   ├── Payment.Infrastructure.Tests/
│   └── Payment.Api.Tests/
├── docs/
│   └── sprints/
│       └── sprint-1-implementation-plan.md
└── compose.yaml
```

- Add `sprint-1-implementation-plan.md` as the team’s implementation guide: scope, architecture decisions, work slices, API contract, test plan, demo steps, and open decisions.
- Keep user stories focused on business behavior; add a `🔴 Open decisions / blockers` section in the sprint plan instead of placing technical details into every story.
- Add build, test, formatting, analyzer, and dependency-direction checks.

## Implementation slices

1. **Solution foundation — PAY-001**
   - Create the solution, projects, test projects, local SQL Server Compose setup, and sample configuration.
   - Enforce `Domain ← Application ← Infrastructure/API`.
   - Ensure secrets are placeholders only.

2. **Payment domain — PAY-002**
   - Add a `Payment` aggregate with payment ID, merchant reference, configured merchant identity, amount/currency, method, provider/reference, timestamps, concurrency version, and status.
   - Add `Money` and configurable ISO currency rules, including decimal precision validation.
   - Implement guarded transitions for `Pending`, `Processing`, `RequiresAction`, `Succeeded`, `Failed`, `Cancelled`, and `RequiresReconciliation`.

3. **Application contract — PAY-003**
   - Add provider-neutral create-payment and get-payment use cases.
   - Define stable validation and business error codes.
   - Define ports for payment persistence, merchant context, provider resolution, and payment providers.
   - Keep reservation, order, seat, event, and provider-SDK objects out of the contract.

4. **Persistence — early Sprint 2 capability included by decision**
   - Use SQL Server from Sprint 1.
   - Make EF Core migrations the sole schema authority.
   - Implement EF Core and Dapper repository adapters against the same schema.
   - Select the active repository through `Persistence:Provider` configuration; fail startup for invalid settings.
   - Do not implement idempotency yet; every valid create request is a new payment operation.

5. **Simulated payment workflow — PAY-004**
   - Add a simulated `Card` provider only.
   - Default outcome comes from configuration.
   - Allow `X-Simulated-Outcome` only in Development for per-request outcomes.
   - Map outcomes consistently: success → `Succeeded`; decline → `Failed`; action-required → `RequiresAction`; timeout/unknown → `RequiresReconciliation`.
   - Prevent simulated-provider startup outside Development.

6. **HTTP adapter**
   - Add `POST /api/v1/payments`.
   - Add `GET /api/v1/payments/{paymentId}`.
   - Return only module payment IDs, neutral statuses, optional next-action data, and stable errors through Problem Details.
   - Use a configured Development merchant context rather than a request-body merchant ID.

## Deferred and visibly documented

Add the following to `docs/sprints/sprint-1-implementation-plan.md`:

```md
## 🔴 Open decisions / deferred scope

- PAY-005 authentication and authorization are deferred.
  The API is Development-only and must not be publicly deployed.
- Confirm supported production currencies and their decimal scales.
- Confirm the future merchant identity provider and claim convention.
- Idempotency, webhooks, refunds, real providers, outcome events,
  and Ticketing/Reservation integration are later-sprint work.
```

## Test plan and demo

- Domain tests for money validation and every permitted/forbidden state transition.
- Application tests using fake provider and repository implementations.
- Repository contract tests run against SQL Server for both EF Core and Dapper adapters.
- API tests for create/read behavior, invalid input, configured currencies, all simulated outcomes, and neutral response shapes.
- Startup tests proving Development-only features cannot run in Production.
- Demo: start the local stack, create a simulated payment through HTTP, receive its module payment ID and status, then retrieve it by ID.

## Defaults

- Solution: `Booking.sln`
- Projects: generic `Payment.*`
- Host boundary: HTTP API first
- Storage: SQL Server
- Schema source: EF Core migrations
- Repository selection: EF Core or Dapper by configuration
- Currency support: configurable ISO rules; EGP may be the local default
- Authentication: explicitly deferred; no Ticketing integration in Sprint 1
