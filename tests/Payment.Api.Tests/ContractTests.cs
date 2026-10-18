using Payment.Api.Controllers;

namespace Payment.Api.Tests;

public sealed class ContractTests
{
    [Fact]
    public void Create_contract_contains_only_provider_neutral_fields()
    {
        var fields = typeof(CreatePaymentInput).GetProperties().Select(property => property.Name).ToArray();
        Assert.Equal(["MerchantReference", "Amount", "Currency", "PaymentMethod", "ReturnUrl", "CancelUrl"], fields);
    }
}
