using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Payment.Domain.Enums;

namespace Payment.Application.Payments;

public static class Idempotency
{
    public const string CreatePaymentOperation = "CreatePayment";
    public const string CapturePaymentOperation = "CapturePayment";
    public const int FingerprintVersion = 1;

    public static string ValidateKey(string? key)
    {
        var normalized = key?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new PaymentValidationException(
                "idempotency_key_required",
                "The Idempotency-Key header is required."
            );
        if (normalized.Length > 128 || normalized.Any(char.IsControl))
            throw new PaymentValidationException(
                "invalid_idempotency_key",
                "The Idempotency-Key header is invalid."
            );
        return normalized;
    }

    public static string CreateFingerprint(
        string merchantId,
        string merchantReference,
        decimal amount,
        string currency,
        PaymentMethod method,
        string returnUrl,
        string cancelUrl
    )
    {
        var value = string.Join(
            "|",
            FingerprintVersion,
            merchantId.Trim(),
            merchantReference.Trim(),
            amount.ToString("G29", CultureInfo.InvariantCulture),
            currency.Trim().ToUpperInvariant(),
            method,
            returnUrl.Trim(),
            cancelUrl.Trim()
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
