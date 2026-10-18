using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure.Providers.PayPal;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/paypal")]
public sealed class PayPalWebhooksController(PayPalWebhookHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return await handler.HandleAsync(payload, headers, cancellationToken) ? Ok() : BadRequest();
    }
}
