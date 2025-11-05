namespace ScGen.Lib.Shared.Extensions.DI;

public static class ScGenServices
{
    public static IServiceCollection AddScGenServices(this IServiceCollection services)
    {
        services.AddScoped<IEthereumContractGenerate, EthereumContractGenerate>();
        services.AddScoped<IEthereumContractCompile, EthereumContractCompile>();
        services.AddScoped<IEthereumContractDeploy, EthereumContractDeploy>();
        services.AddScoped<IContractGenerate>(x
            => x.GetRequiredService<IEthereumContractGenerate>());
        services.AddScoped<IContractCompile>(x
            => x.GetRequiredService<IEthereumContractCompile>());
        services.AddScoped<IContractDeploy>(x
            => x.GetRequiredService<IEthereumContractDeploy>());

        services.AddScoped<ISolanaContractGenerate, SolanaContractGenerate>();
        services.AddScoped<ISolanaContractCompile, SolanaContractCompile>();
        services.AddScoped<ISolanaContractDeploy, SolanaContractDeploy>();
        services.AddScoped<IContractGenerate>(x
            => x.GetRequiredService<ISolanaContractGenerate>());
        services.AddScoped<IContractCompile>(x
            => x.GetRequiredService<ISolanaContractCompile>());
        services.AddScoped<IContractDeploy>(x
            => x.GetRequiredService<ISolanaContractDeploy>());


        services.AddScoped<IRadixContractGenerate, RadixContractGenerate>();
        services.AddScoped<IRadixContractCompile, RadixContractCompile>();
        services.AddScoped<IRadixContractDeploy, RadixContractDeploy>();
        services.AddScoped<IContractGenerate>(x
            => x.GetRequiredService<IRadixContractGenerate>());
        services.AddScoped<IContractCompile>(x
            => x.GetRequiredService<IRadixContractCompile>());
        services.AddScoped<IContractDeploy>(x
            => x.GetRequiredService<IRadixContractDeploy>());


        services.AddSingleton<IEthereumWeb3Factory, EthereumWeb3Factory>();
        services.AddScoped<IContractServiceFactory, ContractServiceFactory>();
        services.AddScoped<IPropertyExtractorService, PropertyExtractorService>();
        services.AddScoped<IX402PaymentService, X402PaymentService>();
        services.AddSingleton<ICreditsService, CreditsService>();
        services.AddSingleton(Handlebars.Create());
        return services;
    }
}