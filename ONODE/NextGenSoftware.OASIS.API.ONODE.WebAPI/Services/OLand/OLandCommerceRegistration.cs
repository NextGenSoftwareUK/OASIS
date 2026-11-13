using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
            return new OLandPaymentGateway(settings, configuration, () => ConfiguredProviders(settings),
                string.IsNullOrWhiteSpace(settings.CoinGeckoDemoApiKey)
                    ? CoinGeckoExchangeRateService.ForPublicMarketQuotes()
                    : new CoinGeckoExchangeRateService(null, settings.CoinGeckoDemoApiKey));
        });
        services.AddSingleton<IOLandNftIssuer, OLandNftIssuer>();
        services.AddHostedService<OLandFulfilmentWorker>();
        services.AddSingleton(s => new OLandCommerceService(s.GetRequiredService<OLandCommerceSettings>(), s.GetRequiredService<IOLandOrderStore>(),
            s.GetRequiredService<IOLandPaymentGateway>(), s.GetRequiredService<IOLandNftIssuer>(), async count =>
            {
                var quote = await new OLandManager(null, Guid.Empty).GetOlandPriceAsync(count);
                if (quote.IsError) throw new OLandCommerceException("OLAND_PRICE_UNAVAILABLE", quote.Message);
                return quote.Result;
            }, () => ConfiguredProviders(s.GetRequiredService<OLandCommerceSettings>())));
    }

    private static readonly object providerGate = new();
    private static IEnumerable<IOASISProvider> ConfiguredProviders(OLandCommerceSettings settings)
    {
        lock (providerGate)
        {
            if (settings.Enabled)
                foreach (string name in settings.Chains.Keys)
                {
                    if (!Enum.TryParse<ProviderType>(name, false, out var type))
                        throw new OLandCommerceException("OLAND_CONFIGURATION_INVALID", "An OLAND ownership chain is not a recognized OASIS provider.");
                    if (ProviderManager.Instance.GetAllRegisteredProviders().Any(p => p.ProviderType.Value == type)) continue;
                    // Initialize NFT chains independently of the storage-only boot list.
                    var registered = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.RegisterProvider(type,
                        activateProviderIfOASISProviderBootTypeIsHot: false);
                    if (registered == null || registered.IsError || registered.Result == null)
                        throw new OLandCommerceException("OLAND_CONFIGURATION_INVALID", "An OLAND ownership provider could not be initialized. Check its server configuration.");
                }
            return ProviderManager.Instance.GetAllRegisteredProviders().ToArray();
        }
    }
}
