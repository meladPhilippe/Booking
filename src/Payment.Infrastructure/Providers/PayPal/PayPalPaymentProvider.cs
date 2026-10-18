using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Domain.Enums;
using DomainPaymentMethod = Payment.Domain.Enums.PaymentMethod;

namespace Payment.Infrastructure.Providers.PayPal;

public sealed class PayPalPaymentProvider(IHttpClientFactory httpClientFactory, PayPalOptions options) : IPaymentProvider
{
    public string Name => "PayPal";
    public bool Supports(DomainPaymentMethod method) => method == DomainPaymentMethod.PayPal;

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
        return new ProviderPaymentOutcome(status == "COMPLETED" ? PaymentStatus.Succeeded : status == "PENDING" ? PaymentStatus.Processing : PaymentStatus.RequiresReconciliation, providerReference);
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
