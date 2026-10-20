using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure;

public sealed class OutcomeOutboxDispatcher(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory, OutcomeDeliveryOptions options, TimeProvider clock, ILogger<OutcomeOutboxDispatcher> logger, PaymentMetrics metrics) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchAsync(stoppingToken); }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested) { logger.LogError(exception, "Outcome outbox dispatch cycle failed."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(options.SigningSecret)) return;
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        foreach (var message in await repository.GetDueOutboxMessagesAsync(clock.GetUtcNow(), 20, cancellationToken))
        {
            try
            {
                var timestamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SigningSecret), Encoding.UTF8.GetBytes($"{timestamp}.{message.Payload}")));
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(message.Payload, Encoding.UTF8, "application/json") };
                request.Headers.Add("X-Payment-Timestamp", timestamp);
                request.Headers.Add("X-Payment-Signature", $"v1={signature}");
                using var response = await httpClientFactory.CreateClient("PaymentOutcomeDelivery").SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var deliveredAt = clock.GetUtcNow();
                    message.MarkDelivered(deliveredAt);
                    metrics.OutboxDelivered(message.EventType, deliveredAt - message.OccurredAt);
                    logger.LogInformation("Delivered payment outcome event {OutcomeEventType} {OutcomeMessageId}", message.EventType, message.Id);
                }
                else
                {
                    var reason = $"HTTP {(int)response.StatusCode}";
                    message.ScheduleRetry(NextAttempt(message.AttemptCount), reason);
                    metrics.OutboxFailed(message.EventType, reason, clock.GetUtcNow() - message.OccurredAt);
                    logger.LogWarning("Payment outcome delivery failed for {OutcomeEventType} {OutcomeMessageId}: {OutcomeReason}", message.EventType, message.Id, reason);
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                var reason = exception.GetType().Name;
                message.ScheduleRetry(NextAttempt(message.AttemptCount), reason);
                metrics.OutboxFailed(message.EventType, reason, clock.GetUtcNow() - message.OccurredAt);
                logger.LogWarning("Payment outcome delivery threw for {OutcomeEventType} {OutcomeMessageId}: {OutcomeReason}", message.EventType, message.Id, reason);
            }
            await repository.UpdateOutboxMessageAsync(message, cancellationToken);
        }
    }

    private DateTimeOffset NextAttempt(int attempts) => clock.GetUtcNow().AddSeconds(Math.Min(3600, Math.Pow(2, Math.Min(attempts + 1, 10))));
}
