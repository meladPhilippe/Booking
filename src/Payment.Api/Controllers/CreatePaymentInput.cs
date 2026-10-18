namespace Payment.Api.Controllers;

public sealed record CreatePaymentInput(
    string MerchantReference,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string ReturnUrl,
    string CancelUrl
);
