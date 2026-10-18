# Booking Payment Module

Sprint 1 provides a development-only, provider-neutral payment API. It uses a simulated card provider and SQL Server; it is not a production payment service.

## Run locally

```bash
docker compose up -d
dotnet run --project src/Payment.Api
```

Create a payment (the default simulation outcome is successful):

```bash
curl -X POST http://localhost:5000/api/v1/payments \
  -H 'Content-Type: application/json' \
  -H 'X-Simulated-Outcome: RequiresAction' \
  -d '{"merchantReference":"reservation-123","amount":500.00,"currency":"EGP","paymentMethod":"Card"}'
```

Use the returned `paymentId` with `GET /api/v1/payments/{paymentId}`. `X-Simulated-Outcome` is accepted in Development only.

Run all automated tests with `dotnet test Booking.sln` from this directory.
