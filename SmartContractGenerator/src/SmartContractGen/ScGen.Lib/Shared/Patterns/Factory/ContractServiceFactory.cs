namespace ScGen.Lib.Shared.Patterns.Factory;

public sealed class ContractServiceFactory(IServiceProvider serviceProvider) : IContractServiceFactory
{
    public IContractGenerate GetGenerator(SmartContractLanguage language) => language switch
    {
        SmartContractLanguage.Solidity => serviceProvider.GetRequiredService<IEthereumContractGenerate>(),
        SmartContractLanguage.Rust => serviceProvider.GetRequiredService<ISolanaContractGenerate>(),
        SmartContractLanguage.Scrypto => serviceProvider.GetRequiredService<IRadixContractGenerate>(),
        _ => throw new NotSupportedException(Messages.NotSupportedScGen)
    };

    public IContractCompile GetCompiler(SmartContractLanguage language) => language switch
    {
        SmartContractLanguage.Solidity => serviceProvider.GetRequiredService<IEthereumContractCompile>(),
        SmartContractLanguage.Rust => serviceProvider.GetRequiredService<ISolanaContractCompile>(),
        SmartContractLanguage.Scrypto => serviceProvider.GetRequiredService<IRadixContractCompile>(),
        _ => throw new NotSupportedException(Messages.NotSupportedScGen)
    };

    public IContractDeploy GetDeployer(SmartContractLanguage language) => language switch
    {
        SmartContractLanguage.Solidity => serviceProvider.GetRequiredService<IEthereumContractDeploy>(),
        SmartContractLanguage.Rust => serviceProvider.GetRequiredService<ISolanaContractDeploy>(),
        SmartContractLanguage.Scrypto => serviceProvider.GetRequiredService<IRadixContractDeploy>(),
        _ => throw new NotSupportedException(Messages.NotSupportedScGen)
    };
}