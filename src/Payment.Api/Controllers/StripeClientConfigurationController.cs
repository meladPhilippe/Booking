using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/client-config/stripe")]
public sealed class StripeClientConfigurationController(
    IWebHostEnvironment environment,
    PaymentProviderOptions providers,
    SelectedStripeProfile stripe
) : ControllerBase
{
    /// <summary>Returns the test publishable key needed by the local embedded-Checkout demo.</summary>
    [HttpGet]
    public ActionResult<StripeClientConfiguration> Get()
    {
        if (!environment.IsDevelopment()
            || !string.Equals(providers.Card, "Stripe", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(stripe.Name, "Sandbox", StringComparison.OrdinalIgnoreCase)
            || stripe.Options.PublishableKey is not { } publishableKey
            || !publishableKey.StartsWith("pk_test_", StringComparison.Ordinal))
            return NotFound();

        return Ok(new StripeClientConfiguration(publishableKey));
    }
}

public sealed record StripeClientConfiguration(string PublishableKey);
