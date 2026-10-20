using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.Api;
using Payment.Application.Payments;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<PaymentDbContext>(tags: ["ready"]);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PaymentService>();
builder.Services.AddPaymentInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

var resource = ResourceBuilder.CreateDefault().AddService("payment-api");
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resource);
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService("payment-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(PaymentMetrics.MeterName)
        .AddPrometheusExporter()
        .AddOtlpExporter());

if (builder.Environment.IsDevelopment())
{
    var demoOrigin = builder.Configuration["DevelopmentDemo:AllowedOrigin"] ?? "https://localhost:5173";
    builder.Services.AddCors(options => options.AddPolicy("DevelopmentDemo", policy => policy
        .WithOrigins(demoOrigin)
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type", "Idempotency-Key")));
}

var app = builder.Build();
app.UseMiddleware<CorrelationMiddleware>();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, code, title) = exception switch
    {
        PaymentValidationException validation => (StatusCodes.Status400BadRequest, validation.Code, validation.Message),
        PaymentConflictException conflict => (StatusCodes.Status409Conflict, conflict.Code, conflict.Message),
        PaymentNotFoundException => (StatusCodes.Status404NotFound, "payment_not_found", "Payment was not found."),
        _ => (StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred.")
    };
    await Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code }).ExecuteAsync(context);
}));

if (app.Environment.IsDevelopment())
    app.UseCors("DevelopmentDemo");

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.MigrateAsync();
    return;
}

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint("/metrics");
app.Run();

public partial class Program;
