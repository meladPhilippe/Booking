using Payment.Application.Abstractions;

namespace Payment.Infrastructure;

public sealed class ConfiguredReturnUrlPolicy(SelectedStripeProfile stripe, PayPalOptions payPal) : IReturnUrlPolicy
{
    private readonly HashSet<string> _origins = stripe.Options.ReturnUrlOrigins.Concat(payPal.ReturnUrlOrigins)
        .Select(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : string.Empty)
        .Where(origin => origin.Length > 0)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool IsAllowed(Uri returnUrl) => _origins.Contains(returnUrl.GetLeftPart(UriPartial.Authority));
}
