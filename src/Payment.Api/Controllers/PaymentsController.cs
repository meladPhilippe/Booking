using Microsoft.AspNetCore.Mvc;
using Payment.Application.Payments;
using Payment.Domain.Enums;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController(
    PaymentService paymentService,
    IWebHostEnvironment environment
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentResponse>> Create(
        CreatePaymentInput input,
        CancellationToken cancellationToken
    )
    {
        if (!Enum.TryParse<PaymentMethod>(input.PaymentMethod, true, out var method))
            throw new PaymentValidationException(
                "unsupported_method",
                "The requested payment method is not supported."
            );

        string? outcome = null;
        if (Request.Headers.TryGetValue("X-Simulated-Outcome", out var configuredOutcome))
        {
            if (!environment.IsDevelopment())
                throw new PaymentValidationException(
                    "simulation_not_allowed",
                    "Simulation overrides are not allowed."
                );
            outcome = configuredOutcome.ToString();
        }

        var result = await paymentService.CreateAsync(
            new CreatePaymentCommand(
                input.MerchantReference,
                input.Amount,
                input.Currency,
                method,
                input.ReturnUrl,
                input.CancelUrl,
                Request.Headers["Idempotency-Key"].ToString(),
                outcome
            ),
            cancellationToken
        );
        if (result.IsReplay)
        {
            Response.Headers["Idempotency-Replayed"] = "true";
            if (result.IsInProgress)
            {
                Response.Headers.RetryAfter = "2";
                return AcceptedAtAction(nameof(Get), new { paymentId = result.Payment.PaymentId }, result.Payment);
            }
            return Ok(result.Payment);
        }
        return CreatedAtAction(nameof(Get), new { paymentId = result.Payment.PaymentId }, result.Payment);
    }

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> Get(
        Guid paymentId,
        CancellationToken cancellationToken
    ) => Ok(await paymentService.GetAsync(paymentId, cancellationToken));

    [HttpPost("{paymentId:guid}/capture")]
    public async Task<ActionResult<PaymentResponse>> Capture(Guid paymentId, CancellationToken cancellationToken) =>
        Ok(await paymentService.CaptureAsync(paymentId, Request.Headers["Idempotency-Key"].ToString(), cancellationToken));
}
