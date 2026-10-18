# Sprint 4 — PayPal JS SDK Checkout Implementation

## Delivered design

PayPal is a distinct `PaymentMethod`, mapped explicitly to the PayPal provider; Card remains mapped to Stripe. The module creates a one-time PayPal Order and returns `{ type: "paypal_order", data: { orderId } }`. The consuming frontend renders PayPal JavaScript SDK Buttons, obtains approval, then calls `POST /api/v1/payments/{paymentId}/capture` with its own idempotency key.

This was selected over a browser redirect flow because PayPal’s official JS SDK flow keeps buyer approval in the button UI while capture stays server-side. Approval alone never marks the local payment successful.

## Configuration

Supply credentials from environment or a secret store, never source control:

```json
"PayPal": {
  "BaseUrl": "https://api-m.sandbox.paypal.com",
  "ClientId": "...",
  "ClientSecret": "...",
  "WebhookId": "...",
  "ReturnUrlOrigins": [ "https://merchant.example" ]
}
```

Create-payment requests require independently validated HTTPS `returnUrl` and `cancelUrl`. Both are included in the local idempotency fingerprint.

## Capture and webhooks

The capture endpoint is merchant-scoped and only permits a PayPal payment in `RequiresAction` or `Processing`. It claims `CapturePayment` idempotency before calling PayPal’s Orders capture API. `COMPLETED`, `PENDING`, decline, and ambiguous results map to shared module statuses.

`POST /api/v1/webhooks/paypal` verifies the received event using PayPal’s verification endpoint and configured webhook ID. Verified events are deduplicated in the shared webhook inbox and map `PAYMENT.CAPTURE.COMPLETED`, `PAYMENT.CAPTURE.PENDING`, `PAYMENT.CAPTURE.DENIED`, and approval reversal into legal local transitions. Unknown or duplicate events never create a payment.

## Sandbox demo and references

1. Configure a PayPal sandbox REST app and registered webhook URL.
2. Create a PayPal module payment and render JS SDK Buttons with `nextAction.data.orderId`.
3. On approval, call the module capture endpoint with `Idempotency-Key`.
4. Verify the queried status and a verified `PAYMENT.CAPTURE.COMPLETED` webhook both show `Succeeded`.
5. Run a sandbox negative capture and verify `Failed`; replay capture and verify no second capture occurs.

- [PayPal Standard Checkout](https://developer.paypal.com/platforms/checkout/standard/integrate)
- [PayPal webhook verification](https://developer.paypal.com/api/rest/webhooks/rest/)
- [PayPal checkout webhook events](https://developer.paypal.com/payment-methods/webhooks/)

Pay Later, Venmo, cards through PayPal, subscriptions, authorization-only flows, refunds, and automatic capture remain deferred.
