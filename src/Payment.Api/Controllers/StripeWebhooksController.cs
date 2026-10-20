using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure;
using Payment.Infrastructure.Providers.Stripe;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe")]
public sealed class StripeWebhooksController(StripeWebhookHandler handler, PaymentMetrics metrics, ILogger<StripeWebhooksController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature)) return BadRequest();
        try
        {
            await handler.HandleAsync(payload, signature, cancellationToken);
            metrics.Webhook("Stripe", "accepted");
            return Ok();
        }
        catch (StripeWebhookSignatureException)
        {
            metrics.Webhook("Stripe", "signature_rejected");
            logger.LogWarning("Rejected Stripe webhook with an invalid signature");
            return BadRequest();
        }
    }
}
