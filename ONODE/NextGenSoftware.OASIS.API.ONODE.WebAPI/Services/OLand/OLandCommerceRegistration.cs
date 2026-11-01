using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

public static class OLandCommerceRegistration
{
    public static void AddOLandCommerce(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var settings = OASISDNAManager.OASISDNA?.OASIS?.OLand
                ?? throw new InvalidOperationException("OLAND requires loaded OASIS DNA settings.");
            // Standard deployment overrides (OLAND__Chains__SolanaOASIS__TreasuryAddress, etc.).
            configuration.GetSection("OLAND").Bind(settings);
            return settings;
        });
        services.AddSingleton<IOLandOrderStore>(s => new MongoOLandOrderStore(new MongoClient(SubscriptionMongoConfiguration.ConnectionString(configuration)),
            s.GetRequiredService<OLandCommerceSettings>().MongoDatabase));
        services.AddSingleton<IOLandPaymentGateway>(s =>
        {
            var settings = s.GetRequiredService<OLandCommerceSettings>();
            return new OLandPaymentGateway(settings, configuration, () => ProviderManager.Instance.GetAllRegisteredProviders(),
                new CoinGeckoExchangeRateService(null, settings.CoinGeckoDemoApiKey));
        });
        services.AddSingleton<IOLandNftIssuer, OLandNftIssuer>();
        services.AddHostedService<OLandFulfilmentWorker>();
        services.AddSingleton(s => new OLandCommerceService(s.GetRequiredService<OLandCommerceSettings>(), s.GetRequiredService<IOLandOrderStore>(),
            s.GetRequiredService<IOLandPaymentGateway>(), s.GetRequiredService<IOLandNftIssuer>(), async count =>
            {
                var quote = await new OLandManager(null, Guid.Empty).GetOlandPriceAsync(count);
                if (quote.IsError) throw new OLandCommerceException("OLAND_PRICE_UNAVAILABLE", quote.Message);
                return quote.Result;
            }, () => ProviderManager.Instance.GetAllRegisteredProviders()));
    }
}
