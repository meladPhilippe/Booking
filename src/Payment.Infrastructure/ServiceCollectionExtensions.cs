using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Providers;
using Payment.Infrastructure.Providers.Stripe;
using Payment.Infrastructure.Providers.PayPal;

namespace Payment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var persistence = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new PersistenceOptions();
        var currencies = configuration.GetSection(CurrencyRuleOptions.SectionName).Get<CurrencyRuleOptions>() ?? new CurrencyRuleOptions();
        var simulation = configuration.GetSection(SimulationOptions.SectionName).Get<SimulationOptions>() ?? new SimulationOptions();
        var merchant = configuration.GetSection(DevelopmentMerchantOptions.SectionName).Get<DevelopmentMerchantOptions>() ?? new DevelopmentMerchantOptions();
        var stripe = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>() ?? new StripeOptions();
        var providerOptions = configuration.GetSection(PaymentProviderOptions.SectionName).Get<PaymentProviderOptions>() ?? new PaymentProviderOptions();
        var payPal = configuration.GetSection(PayPalOptions.SectionName).Get<PayPalOptions>() ?? new PayPalOptions();
        var outcomeDelivery = configuration.GetSection(OutcomeDeliveryOptions.SectionName).Get<OutcomeDeliveryOptions>() ?? new OutcomeDeliveryOptions();
        if (string.IsNullOrWhiteSpace(persistence.ConnectionString)) throw new InvalidOperationException("Persistence:ConnectionString must be configured.");
        if (currencies.Supported.Count == 0) throw new InvalidOperationException("At least one currency rule must be configured.");

        var selectedStripeProfile = SelectStripeProfile(stripe);
        services.AddSingleton(persistence).AddSingleton(currencies).AddSingleton(simulation).AddSingleton(merchant).AddSingleton(stripe).AddSingleton(selectedStripeProfile).AddSingleton(providerOptions).AddSingleton(payPal).AddSingleton(outcomeDelivery).AddSingleton<PaymentMetrics>();
        services.AddSingleton<IStripeGateway, StripeGateway>();
        services.AddHttpClient("PayPal", client =>
        {
            client.BaseAddress = new Uri(payPal.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient("PaymentOutcomeDelivery", client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer(persistence.ConnectionString));
        services.AddSingleton<ICurrencyRules, ConfiguredCurrencyRules>();
        services.AddSingleton<IMerchantContext, DevelopmentMerchantContext>();
        services.AddSingleton<IReturnUrlPolicy, ConfiguredReturnUrlPolicy>();
        if (isDevelopment)
        {
            services.AddSingleton<IPaymentProvider, SimulatedPaymentProvider>();
            if (string.Equals(providerOptions.Card, "Stripe", StringComparison.OrdinalIgnoreCase))
            {
                ValidateStripeProfile(selectedStripeProfile, "Sandbox", "sk_test_", requirePublishableKey: true);
                services.AddSingleton<IPaymentProvider, StripeProvider>();
            }
        }
        else
        {
            if (!string.Equals(providerOptions.Card, "Stripe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PaymentProviders:Card must be Stripe outside Development.");
            ValidateStripeProfile(selectedStripeProfile, "Live", "sk_live_", requirePublishableKey: false);
            if (string.IsNullOrWhiteSpace(payPal.ClientId) || string.IsNullOrWhiteSpace(payPal.ClientSecret) || string.IsNullOrWhiteSpace(payPal.WebhookId) || payPal.ReturnUrlOrigins.Count == 0)
                throw new InvalidOperationException("Stripe and PayPal credentials, webhook secrets/IDs, and allowed return URL origins must be configured outside Development.");
            services.AddSingleton<IPaymentProvider, StripeProvider>();
            services.AddSingleton<IPaymentProvider, PayPalPaymentProvider>();
        }
        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<StripeWebhookHandler>();
        services.AddScoped<PayPalWebhookHandler>();
        services.AddHostedService<OutcomeOutboxDispatcher>();
        if (string.Equals(persistence.Provider, "EfCore", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        else if (string.Equals(persistence.Provider, "Dapper", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentRepository>(_ => new DapperPaymentRepository(persistence.ConnectionString));
        else
            throw new InvalidOperationException("Persistence:Provider must be EfCore or Dapper.");
        return services;
    }

    private static SelectedStripeProfile SelectStripeProfile(StripeOptions options)
    {
        var match = options.Profiles.FirstOrDefault(pair => string.Equals(pair.Key, options.SelectedProfile, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(match.Key))
            throw new InvalidOperationException($"Stripe profile '{options.SelectedProfile}' is not configured.");
        return new SelectedStripeProfile(match.Key, match.Value);
    }

    private static void ValidateStripeProfile(SelectedStripeProfile profile, string requiredName, string keyPrefix, bool requirePublishableKey)
    {
        if (!string.Equals(profile.Name, requiredName, StringComparison.OrdinalIgnoreCase)
            || !profile.Options.SecretKey.StartsWith(keyPrefix, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(profile.Options.WebhookSigningSecret)
            || profile.Options.ReturnUrlOrigins.Count == 0
            || (requirePublishableKey && (profile.Options.PublishableKey is not { } publishableKey || !publishableKey.StartsWith("pk_test_", StringComparison.Ordinal))))
            throw new InvalidOperationException($"Stripe profile '{requiredName}' must include a {keyPrefix} key, webhook signing secret, required publishable key, and allowed return URL origins.");
    }
}
