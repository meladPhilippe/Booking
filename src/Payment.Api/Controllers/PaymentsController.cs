using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Payment.Application.Payments;
using Payment.Domain.Enums;
using Payment.Infrastructure;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController(
    PaymentService paymentService,
    IWebHostEnvironment environment,
    PaymentMetrics metrics,
    ILogger<PaymentsController> logger
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
        ObservePayment(result.Payment);
        logger.LogInformation("Payment create completed for {PaymentId} with provider {PaymentProvider} and status {PaymentStatus}; replay={IdempotencyReplay}", result.Payment.PaymentId, result.Payment.Provider, result.Payment.Status, result.IsReplay);
        if (result.IsReplay)
        {
            Response.Headers["Idempotency-Replayed"] = "true";
            if (result.IsInProgress)
            {
                Response.Headers.RetryAfter = "2";
                return AcceptedAtAction(
                    nameof(Get),
                    new { paymentId = result.Payment.PaymentId },
                    result.Payment
                );
            }
            return Ok(result.Payment);
        }
        return CreatedAtAction(
            nameof(Get),
            new { paymentId = result.Payment.PaymentId },
            result.Payment
        );
    }

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> Get(
        Guid paymentId,
        CancellationToken cancellationToken
    ) => Ok(await paymentService.GetAsync(paymentId, cancellationToken));

    [HttpPost("{paymentId:guid}/capture")]
    public async Task<ActionResult<PaymentResponse>> Capture(
        Guid paymentId,
        CancellationToken cancellationToken
    )
    {
        var payment = await paymentService.CaptureAsync(paymentId, Request.Headers["Idempotency-Key"].ToString(), cancellationToken);
        ObservePayment(payment);
        logger.LogInformation("Payment capture completed for {PaymentId} with status {PaymentStatus}", paymentId, payment.Status);
        return Ok(payment);
    }

    [HttpPost("{paymentId:guid}/refunds")]
    [ProducesResponseType<RefundResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RefundResponse>> Refund(
        Guid paymentId,
        RefundPaymentInput input,
        CancellationToken cancellationToken
    )
    {
        var refund = await paymentService.RefundAsync(
            paymentId,
            new RefundPaymentCommand(input.Amount, Request.Headers["Idempotency-Key"].ToString()),
            cancellationToken
        );
        metrics.RefundOutcome(refund.Status.ToString());
        Activity.Current?.SetTag("payment.id", paymentId.ToString());
        Activity.Current?.SetTag("refund.id", refund.RefundId.ToString());
        Activity.Current?.SetTag("refund.status", refund.Status.ToString());
        logger.LogInformation("Refund {RefundId} completed for payment {PaymentId} with status {RefundStatus}", refund.RefundId, paymentId, refund.Status);
        return CreatedAtAction(nameof(GetRefunds), new { paymentId }, refund);
    }

    [HttpGet("{paymentId:guid}/refunds")]
    public async Task<ActionResult<IReadOnlyList<RefundResponse>>> GetRefunds(
        Guid paymentId,
        CancellationToken cancellationToken
    ) => Ok(await paymentService.GetRefundsAsync(paymentId, cancellationToken));

    [HttpPost("{paymentId:guid}/reconcile")]
    public async Task<ActionResult<PaymentResponse>> Reconcile(
        Guid paymentId,
        CancellationToken cancellationToken
    )
    {
        var payment = await paymentService.ReconcileAsync(paymentId, cancellationToken);
        ObservePayment(payment);
        logger.LogInformation("Payment reconciliation completed for {PaymentId} with status {PaymentStatus}", paymentId, payment.Status);
        return Ok(payment);
    }

    private void ObservePayment(PaymentResponse payment)
    {
        metrics.PaymentOutcome(payment.Provider, payment.Status.ToString());
        Activity.Current?.SetTag("payment.id", payment.PaymentId.ToString());
        Activity.Current?.SetTag("payment.provider", payment.Provider);
        Activity.Current?.SetTag("payment.status", payment.Status.ToString());
    }
}
