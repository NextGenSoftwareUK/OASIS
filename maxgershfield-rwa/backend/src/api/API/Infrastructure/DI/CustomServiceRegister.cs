using Application.Contracts;
using Infrastructure.ImplementationContract;

namespace API.Infrastructure.DI;

/// <summary>
/// This class provides an extension method to register custom services into the application's dependency injection (DI) container.
/// It includes a variety of services, such as user and role management, network services, exchange rate services, and others.
/// </summary>
public static class CustomServiceRegister
{
    /// <summary>
    /// Registers a set of custom services required by the application.
    /// The services registered include user management, role management, network-related services, identity services, 
    /// exchange rate services, and wallet account management services.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to register services.</param>
    /// <returns>The WebApplicationBuilder instance with the custom services registered, enabling method chaining.</returns>
    public static WebApplicationBuilder AddCustomServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<Seeder>();

        builder.Services.AddScoped<IRoleService, RoleService>();

        builder.Services.AddScoped<IUserService, UserService>();

        builder.Services.AddScoped<IOrderService, OrderService>();

        builder.Services.AddScoped<INetworkService, NetworkService>();

        builder.Services.AddScoped<IIdentityService, IdentityService>();

        builder.Services.AddScoped<IUserRoleService, UserRoleService>();

        builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

        builder.Services.AddScoped<IWalletLinkedAccountService, WalletLinkedAccountService>();

        builder.Services.AddScoped<IRwaTokenService, RwaTokenService>();

        builder.Services.AddScoped<IRwaTokenPriceHistoryService, RwaTokenPriceHistoryService>();

        builder.Services.AddScoped<ISolShiftIntegrationService, SolShiftIntegrationService>();
        builder.Services.AddScoped<INftPurchaseService, NftPurchaseService>();
        builder.Services.AddScoped<IRwaTokenOwnershipTransferService, RwaTokenOwnershipTransferService>();
        
        builder.Services.AddScoped<IOASISAuthService, OASISAuthService>();

        builder.Services.AddScoped<IFractionalNFTService, FractionalNFTService>();

        builder.Services.AddScoped<IPriceAdjustmentService, PriceAdjustmentService>();

        // RWA Oracle Services
        builder.Services.AddScoped<ICorporateActionService, CorporateActionService>();
        builder.Services.AddScoped<IEquityPriceService, EquityPriceService>();

        // Corporate Action Data Sources
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<Infrastructure.ExternalServices.CorporateActions.ICorporateActionDataSource, Infrastructure.ExternalServices.CorporateActions.AlphaVantageCorporateActionSource>();
        builder.Services.AddSingleton<Infrastructure.ExternalServices.CorporateActions.ICorporateActionDataSource, Infrastructure.ExternalServices.CorporateActions.IexCloudCorporateActionSource>();
        builder.Services.AddSingleton<Infrastructure.ExternalServices.CorporateActions.ICorporateActionDataSource, Infrastructure.ExternalServices.CorporateActions.PolygonCorporateActionSource>();

        // Equity Price Data Sources
        builder.Services.AddSingleton<Infrastructure.ExternalServices.EquityPrices.IEquityPriceDataSource, Infrastructure.ExternalServices.EquityPrices.AlphaVantagePriceSource>();
        builder.Services.AddSingleton<Infrastructure.ExternalServices.EquityPrices.IEquityPriceDataSource, Infrastructure.ExternalServices.EquityPrices.IexCloudPriceSource>();
        builder.Services.AddSingleton<Infrastructure.ExternalServices.EquityPrices.IEquityPriceDataSource, Infrastructure.ExternalServices.EquityPrices.PolygonPriceSource>();

        // Memory cache for equity prices
        builder.Services.AddMemoryCache();

        // Funding Rate Calculation Services
        builder.Services.AddScoped<IVolatilityService, VolatilityService>();
        builder.Services.AddScoped<ILiquidityService, LiquidityService>();
        builder.Services.AddScoped<IFundingRateService, FundingRateService>();

        // Risk Management Services
        builder.Services.AddScoped<IRiskWindowService, RiskWindowService>();
        builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
        builder.Services.AddScoped<IRiskRecommendationService, RiskRecommendationService>();

        // On-Chain Funding Rate Publishing Services
        RegisterOnChainFundingRateServices(builder.Services, builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Register on-chain funding rate publishing services for multi-chain support
    /// </summary>
    private static void RegisterOnChainFundingRateServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register Solana-specific services
        services.AddScoped<Infrastructure.Blockchain.Solana.SolanaPdaManager>();
        
        // Register Solana publisher (scoped to get fresh instances)
        // Note: Uses existing IRpcClient registered in BridgeRegister
        services.AddScoped<Infrastructure.Blockchain.Solana.SolanaOnChainFundingPublisher>();
        services.AddScoped<IOnChainFundingPublisher>(sp => 
            sp.GetRequiredService<Infrastructure.Blockchain.Solana.SolanaOnChainFundingPublisher>());

        // Register factory (singleton since it manages all publishers)
        // Must be registered after publishers so they're available for injection
        services.AddSingleton<IOnChainFundingPublisherFactory, Infrastructure.Blockchain.OnChainFundingPublisherFactory>();

        // Note: Ethereum, Arbitrum, Polygon publishers can be added here when implemented
        // They will be automatically picked up by the factory based on configuration
    }
}