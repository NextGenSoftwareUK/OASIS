namespace ScGen.Lib.Shared.Patterns.Factory;

public interface IEthereumWeb3Factory
{
    Web3 CreateWeb3(string rpcUrl);
    Web3 CreateWeb3WithAccount(string rpcUrl, string privateKey);
}