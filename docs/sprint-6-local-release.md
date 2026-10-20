# Sprint 6 — Local Release Candidate

## Start the stack

Copy `.env.example` to the ignored `.env` file and replace every placeholder with sandbox-only credentials. Rotate the Stripe keys and webhook signing secret that were previously committed before using this stack.

The host-run development API also requires `Persistence__ConnectionString` to be set; connection strings are deliberately not committed.

Run the release profile:

```bash
docker compose --profile release up --build
```

The `payment-migrate` one-shot container applies EF migrations before `payment-api` starts. Grafana is available at `http://localhost:3000`; all other observability services and the Payment API are internal to the Compose network.

## Observability

The Payment API sends traces, metrics, and structured logs through the OpenTelemetry Collector. The collector routes metrics to Prometheus, traces to Tempo, and logs to Loki. In Grafana, query by `CorrelationId` or module payment ID to follow a payment across API, provider, webhook, refund, reconciliation, and outbox events.

`/health/live` checks that the process is running. `/health/ready` also requires SQL Server. `/metrics` is intentionally available only within the Compose network for Prometheus scraping.

## Release checks

- Run `dotnet test Booking.sln` and `bash scripts/scan-secrets.sh`.
- Confirm Grafana receives API traffic, a trace, and a structured log with the same correlation ID.
- Perform Stripe and PayPal sandbox success/failure, duplicate request/webhook, refund, reconciliation, and status-query scenarios.
- Inspect failed outcome delivery in Grafana; restore the target endpoint and confirm bounded retry eventually delivers it.
- Do not sign off a release until external PAY-005 OAuth/OIDC merchant-scoped authorization is deployed and its integration tests pass.
- Record a load-test target before release sign-off; the harness/threshold is deliberately not guessed by this module.

## Incident and rollback runbook

- **Provider outage or timeout:** leave the payment in `RequiresReconciliation`, investigate using correlation/payment ID, then use reconciliation after the provider recovers. Do not replay a money-moving API call manually without its original idempotency key.
- **Webhook failures:** confirm signature configuration and provider delivery history, correct the configuration, then let provider retry or perform the documented reconciliation path.
- **Outcome delivery failures:** inspect `payment_outbox_failures_total`, endpoint status, and retry logs. Correct the consumer endpoint or signing secret; dispatcher retries with bounded exponential backoff.
- **Secret exposure:** revoke/rotate the affected provider key or webhook secret immediately, update the approved secret store/local `.env`, and inspect provider activity.
- **Rollback:** stop `payment-api`, deploy the previous image, and do not roll back database migrations automatically. Review migration compatibility before any manual database rollback.
- **Disable a provider:** remove its configured provider mapping/credentials, restart the API, and publish the temporary unsupported-method limitation to consuming applications.
