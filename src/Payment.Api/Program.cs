using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payments;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentDbContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PaymentService>();
builder.Services.AddPaymentInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();
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

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.MigrateAsync();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

public partial class Program;
