using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure;
using Payment.Infrastructure.Providers.PayPal;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/paypal")]
public sealed class PayPalWebhooksController(PayPalWebhookHandler handler, PaymentMetrics metrics, ILogger<PayPalWebhooksController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase
        );
        var accepted = await handler.HandleAsync(payload, headers, cancellationToken);
        metrics.Webhook("PayPal", accepted ? "accepted" : "signature_rejected");
        if (!accepted) logger.LogWarning("Rejected PayPal webhook after signature verification");
        return accepted ? Ok() : BadRequest();
    }
}
