using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Payment.Api.Controllers;
using Payment.Infrastructure;

namespace Payment.Api.Tests;

public sealed class ContractTests
{
    [Fact]
    public void Create_contract_contains_only_provider_neutral_fields()
    {
        var fields = typeof(CreatePaymentInput).GetProperties().Select(property => property.Name).ToArray();
        Assert.Equal(["MerchantReference", "Amount", "Currency", "PaymentMethod", "ReturnUrl", "CancelUrl"], fields);
    }

    [Fact]
    public void Stripe_client_config_returns_only_test_publishable_key_for_development_sandbox_profile()
    {
        var controller = new StripeClientConfigurationController(
            new TestEnvironment(Environments.Development),
            new PaymentProviderOptions { Card = "Stripe" },
            new SelectedStripeProfile("Sandbox", new StripeProfileOptions { PublishableKey = "pk_test_demo" }));

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var configuration = Assert.IsType<StripeClientConfiguration>(ok.Value);
        Assert.Equal("pk_test_demo", configuration.PublishableKey);
    }

    [Fact]
    public void Stripe_client_config_is_hidden_outside_development()
    {
        var controller = new StripeClientConfigurationController(
            new TestEnvironment(Environments.Production),
            new PaymentProviderOptions { Card = "Stripe" },
            new SelectedStripeProfile("Sandbox", new StripeProfileOptions { PublishableKey = "pk_test_demo" }));

        var result = controller.Get();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private sealed class TestEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Payment.Api.Tests";
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
