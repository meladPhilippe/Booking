using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using DomainPaymentMethod = Payment.Domain.Enums.PaymentMethod;

namespace Payment.Infrastructure.Providers.PayPal;

public sealed class PayPalPaymentProvider(IHttpClientFactory httpClientFactory, PayPalOptions options) : IPaymentProvider, IRefundProvider, IReconciliationProvider
{
    public string Name => "PayPal";
    public bool Supports(DomainPaymentMethod method) => method == DomainPaymentMethod.PayPal;
    public bool SupportsRefunds(DomainPaymentMethod method) => method == DomainPaymentMethod.PayPal;

    public async Task<ProviderPaymentOutcome> CreateAsync(ProviderPaymentRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        var token = await GetAccessTokenAsync(client, cancellationToken);
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[] { new { custom_id = request.PaymentId.ToString(), reference_id = request.MerchantReference, amount = new { currency_code = request.Amount.Currency.ToUpperInvariant(), value = request.Amount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } } },
            payment_source = new { paypal = new { experience_context = new { return_url = request.ReturnUrl, cancel_url = request.CancelUrl, user_action = "PAY_NOW" } } }
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders") { Content = JsonContent.Create(body) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("PayPal-Request-Id", request.PaymentId.ToString("N"));
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return new ProviderPaymentOutcome(response.StatusCode is System.Net.HttpStatusCode.UnprocessableEntity or System.Net.HttpStatusCode.BadRequest ? PaymentStatus.Failed : PaymentStatus.RequiresReconciliation, null);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var orderId = document.RootElement.GetProperty("id").GetString()!;
        return new ProviderPaymentOutcome(PaymentStatus.RequiresAction, orderId, new ProviderNextAction("paypal_order", new Dictionary<string, string> { ["orderId"] = orderId }));
    }

    public async Task<ProviderPaymentOutcome> CaptureAsync(Guid paymentId, string providerReference, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        var token = await GetAccessTokenAsync(client, cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{providerReference}/capture");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("PayPal-Request-Id", $"cap-{paymentId:N}");
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return new ProviderPaymentOutcome(response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity ? PaymentStatus.Failed : PaymentStatus.RequiresReconciliation, providerReference);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var status = document.RootElement.GetProperty("status").GetString();
        var captureId = TryGetCaptureId(document.RootElement);
        return new ProviderPaymentOutcome(status == "COMPLETED" ? PaymentStatus.Succeeded : status == "PENDING" ? PaymentStatus.Processing : PaymentStatus.RequiresReconciliation, providerReference, null, captureId);
    }

    public async Task<ProviderRefundOutcome> RefundAsync(ProviderRefundRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        var token = await GetAccessTokenAsync(client, cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/v2/payments/captures/{request.ProviderPaymentReference}/refund")
        {
            Content = JsonContent.Create(new { amount = new { currency_code = request.Amount.Currency.ToUpperInvariant(), value = request.Amount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), }, custom_id = request.RefundId.ToString() })
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("PayPal-Request-Id", $"ref-{request.RefundId:N}");
        message.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new ProviderRefundOutcome(response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.UnprocessableEntity ? RefundStatus.Failed : RefundStatus.RequiresReconciliation, null);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var status = document.RootElement.TryGetProperty("status", out var statusValue) ? statusValue.GetString() : null;
        return new ProviderRefundOutcome(status == "COMPLETED" ? RefundStatus.Succeeded : status is "FAILED" or "DENIED" ? RefundStatus.Failed : RefundStatus.Pending, document.RootElement.GetProperty("id").GetString());
    }

    public async Task<ProviderReconciliationOutcome> ReconcileAsync(string providerReference, string? providerPaymentReference, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        var token = await GetAccessTokenAsync(client, cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/v2/checkout/orders/{providerReference}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var status = document.RootElement.GetProperty("status").GetString();
        return new ProviderReconciliationOutcome(status == "COMPLETED" ? PaymentStatus.Succeeded : status == "APPROVED" ? PaymentStatus.RequiresAction : PaymentStatus.Processing, providerPaymentReference ?? TryGetCaptureId(document.RootElement));
    }

    private static string? TryGetCaptureId(JsonElement root)
    {
        if (!root.TryGetProperty("purchase_units", out var units) || units.GetArrayLength() == 0) return null;
        var unit = units[0];
        if (!unit.TryGetProperty("payments", out var payments) || !payments.TryGetProperty("captures", out var captures) || captures.GetArrayLength() == 0) return null;
        return captures[0].TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private async Task<string> GetAccessTokenAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token") { Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}")));
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}
