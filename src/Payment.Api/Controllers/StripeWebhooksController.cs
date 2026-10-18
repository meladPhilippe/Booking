using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure.Providers.Stripe;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe")]
public sealed class StripeWebhooksController(StripeWebhookHandler handler) : ControllerBase
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
            return Ok();
        }
        catch (StripeWebhookSignatureException)
        {
            return BadRequest();
        }
    }
}
