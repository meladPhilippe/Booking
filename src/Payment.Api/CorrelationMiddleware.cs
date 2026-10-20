using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Payment.Api;

public sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!Guid.TryParse(correlationId, out _)) correlationId = Guid.NewGuid().ToString("D");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("payment.correlation_id", correlationId);
        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
            await next(context);
    }
}
