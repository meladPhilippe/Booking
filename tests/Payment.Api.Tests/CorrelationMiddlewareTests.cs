using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Payment.Api;

namespace Payment.Api.Tests;

public sealed class CorrelationMiddlewareTests
{
    [Fact]
    public async Task Generates_and_returns_a_correlation_id_when_one_is_not_supplied()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(Guid.TryParse(context.Response.Headers[CorrelationMiddleware.HeaderName], out _));
        Assert.Equal(context.Response.Headers[CorrelationMiddleware.HeaderName], context.TraceIdentifier);
    }

    [Fact]
    public async Task Preserves_a_valid_caller_correlation_id()
    {
        var correlationId = Guid.NewGuid().ToString("D");
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationMiddleware.HeaderName] = correlationId;
        var middleware = new CorrelationMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, context.Response.Headers[CorrelationMiddleware.HeaderName]);
        Assert.Equal(correlationId, context.TraceIdentifier);
    }
}
