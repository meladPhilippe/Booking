using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Providers.PayPal;

public sealed class PayPalWebhookHandler(IHttpClientFactory httpClientFactory, PayPalOptions options, IPaymentRepository repository, TimeProvider clock)
{
    public async Task<bool> HandleAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        var token = await GetAccessTokenAsync(client, cancellationToken);
        using var verification = new HttpRequestMessage(HttpMethod.Post, "/v1/notifications/verify-webhook-signature") { Content = JsonContent.Create(new
        {
            auth_algo = headers["PAYPAL-AUTH-ALGO"].ToString(), cert_url = headers["PAYPAL-CERT-URL"].ToString(), transmission_id = headers["PAYPAL-TRANSMISSION-ID"].ToString(), transmission_sig = headers["PAYPAL-TRANSMISSION-SIG"].ToString(), transmission_time = headers["PAYPAL-TRANSMISSION-TIME"].ToString(), webhook_id = options.WebhookId, webhook_event = JsonSerializer.Deserialize<JsonElement>(payload)
        }) };
        verification.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var verificationResponse = await client.SendAsync(verification, cancellationToken);
        if (!verificationResponse.IsSuccessStatusCode) throw new HttpRequestException("PayPal webhook verification failed.");
        using var verificationJson = JsonDocument.Parse(await verificationResponse.Content.ReadAsStringAsync(cancellationToken));
        if (verificationJson.RootElement.GetProperty("verification_status").GetString() != "SUCCESS") return false;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString()!;
        var eventType = root.GetProperty("event_type").GetString()!;
        var resource = root.GetProperty("resource");
        var paymentIdText = resource.TryGetProperty("custom_id", out var customId) ? customId.GetString() : null;
        if (!Guid.TryParse(paymentIdText, out var paymentId)) return true;
        var relatedId = resource.TryGetProperty("supplementary_data", out var supplementary) && supplementary.TryGetProperty("related_ids", out var related) && related.TryGetProperty("order_id", out var orderId) ? orderId.GetString()! : resource.GetProperty("id").GetString()!;
        if (!await repository.TryRecordWebhookEventAsync(ProviderWebhookEvent.Create("PayPal", eventId, eventType, relatedId, clock.GetUtcNow()), cancellationToken)) return true;
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null) return true;
        var status = eventType switch
        {
            "PAYMENT.CAPTURE.COMPLETED" => PaymentStatus.Succeeded,
            "PAYMENT.CAPTURE.DENIED" or "CHECKOUT.PAYMENT-APPROVAL.REVERSED" => PaymentStatus.Failed,
            "PAYMENT.CAPTURE.PENDING" => PaymentStatus.Processing,
            _ => PaymentStatus.RequiresAction
        };
        if (payment.Status != status && DomainPayment.CanTransition(payment.Status, status)) { payment.ApplyOutcome(status, null, clock.GetUtcNow()); await repository.UpdateAsync(payment, cancellationToken); }
        return true;
    }

    private async Task<string> GetAccessTokenAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token") { Content = new StringContent("grant_type=client_credentials", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}")));
        using var response = await client.SendAsync(request, cancellationToken); response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); return json.RootElement.GetProperty("access_token").GetString()!;
    }
}
