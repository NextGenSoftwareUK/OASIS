namespace ScGen.Lib.Shared.Patterns.Factory;

public interface IContractServiceFactory
{
    IContractGenerate GetGenerator(SmartContractLanguage language);
    IContractCompile GetCompiler(SmartContractLanguage language);
    IContractDeploy GetDeployer(SmartContractLanguage language);
}