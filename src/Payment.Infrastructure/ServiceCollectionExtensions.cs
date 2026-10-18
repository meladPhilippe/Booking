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
        var payPal = configuration.GetSection(PayPalOptions.SectionName).Get<PayPalOptions>() ?? new PayPalOptions();
        if (string.IsNullOrWhiteSpace(persistence.ConnectionString)) throw new InvalidOperationException("Persistence:ConnectionString must be configured.");
        if (currencies.Supported.Count == 0) throw new InvalidOperationException("At least one currency rule must be configured.");

        services.AddSingleton(persistence).AddSingleton(currencies).AddSingleton(simulation).AddSingleton(merchant).AddSingleton(stripe).AddSingleton(payPal);
        services.AddHttpClient("PayPal", client => client.BaseAddress = new Uri(payPal.BaseUrl));
        services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer(persistence.ConnectionString));
        services.AddSingleton<ICurrencyRules, ConfiguredCurrencyRules>();
        services.AddSingleton<IMerchantContext, DevelopmentMerchantContext>();
        services.AddSingleton<IReturnUrlPolicy, ConfiguredReturnUrlPolicy>();
        if (isDevelopment)
            services.AddSingleton<IPaymentProvider, SimulatedPaymentProvider>();
        else
        {
            if (string.IsNullOrWhiteSpace(stripe.SecretKey) || string.IsNullOrWhiteSpace(stripe.WebhookSigningSecret) || stripe.ReturnUrlOrigins.Count == 0 || string.IsNullOrWhiteSpace(payPal.ClientId) || string.IsNullOrWhiteSpace(payPal.ClientSecret) || string.IsNullOrWhiteSpace(payPal.WebhookId) || payPal.ReturnUrlOrigins.Count == 0)
                throw new InvalidOperationException("Stripe and PayPal credentials, webhook secrets/IDs, and allowed return URL origins must be configured outside Development.");
            services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
            services.AddSingleton<IPaymentProvider, PayPalPaymentProvider>();
        }
        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<StripeWebhookHandler>();
        services.AddScoped<PayPalWebhookHandler>();
        if (string.Equals(persistence.Provider, "EfCore", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        else if (string.Equals(persistence.Provider, "Dapper", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentRepository>(_ => new DapperPaymentRepository(persistence.ConnectionString));
        else
            throw new InvalidOperationException("Persistence:Provider must be EfCore or Dapper.");
        return services;
    }
}
