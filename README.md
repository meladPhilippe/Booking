# Booking Payment Module

Sprint 1 provides a development-only, provider-neutral payment API. It uses a simulated card provider and SQL Server; it is not a production payment service.

## Run locally

The Payment API runs on the host. SQL Server and the separate Stripe sandbox demo run through Docker Compose.

The Development API starts at `https://localhost:5001` and `http://localhost:5000`. If this is the first HTTPS launch, trust and export the .NET development certificate:

```bash
dotnet dev-certs https --trust
export DEMO_HTTPS_PFX_PASSWORD='choose-a-local-password'
mkdir -p "$HOME/.aspnet/https"
dotnet dev-certs https -ep "$HOME/.aspnet/https/Payment.Api.pfx" -p "$DEMO_HTTPS_PFX_PASSWORD"
```

Keep `DEMO_HTTPS_PFX_PASSWORD` in your shell or an ignored `.env` file. It is used only by the demo container to read the mounted local certificate.

```bash
docker compose up --build
export Persistence__ConnectionString='Server=localhost,14333;Database=BookingPayments;User Id=sa;Password=your-local-sql-password;TrustServerCertificate=True'
dotnet run --project src/Payment.Api
```

Create a payment (the default simulation outcome is successful):

```bash
curl -X POST http://localhost:5000/api/v1/payments \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: payment-demo-001' \
  -H 'X-Simulated-Outcome: RequiresAction' \
  -d '{"merchantReference":"reservation-123","amount":500.00,"currency":"EGP","paymentMethod":"Card","returnUrl":"https://merchant.example/return","cancelUrl":"https://merchant.example/cancel"}'
```

Use the returned `paymentId` with `GET /api/v1/payments/{paymentId}`. `X-Simulated-Outcome` is accepted in Development only.

## Stripe sandbox checkout demo

Never place Stripe credentials in `appsettings.json`. Export test credentials in a terminal, then start the API:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export PaymentProviders__Card=Stripe
export Stripe__SelectedProfile=Sandbox
export Stripe__Profiles__Sandbox__SecretKey='sk_test_...'
export Stripe__Profiles__Sandbox__PublishableKey='pk_test_...'
export Stripe__Profiles__Sandbox__WebhookSigningSecret='whsec_...'
export Stripe__Profiles__Sandbox__ReturnUrlOrigins__0='https://localhost:5173'
dotnet run --project src/Payment.Api
```

In another terminal, forward Stripe test events. Copy the listener's current `whsec_...` value into `Stripe__Profiles__Sandbox__WebhookSigningSecret` before starting the API:

```bash
stripe listen --forward-thin-to http://localhost:5000/api/v1/webhooks/stripe --thin-events '*'
```

Open `https://localhost:5173/` and use Stripe test cards. The demo calls the host-run API at `https://localhost:5001`; it receives only Stripe's publishable key and Checkout client secret. Do not pause a debugger inside the webhook request; Stripe CLI will time out while the request is suspended.

Run all automated tests with `dotnet test Booking.sln` from this directory.

## Local release candidate and observability

Sprint 6 provides a Docker Compose `release` profile with the Payment API, migration job, OpenTelemetry Collector, Prometheus, Tempo, Loki, and Grafana. Copy `.env.example` to the ignored `.env`, provide sandbox credentials, then run:

```bash
docker compose --profile release up --build
```

See [the local release runbook](docs/sprint-6-local-release.md) for health checks, Grafana usage, release verification, rollback, and secret-rotation procedures. Before sharing or committing changes, run `bash scripts/scan-secrets.sh`.
