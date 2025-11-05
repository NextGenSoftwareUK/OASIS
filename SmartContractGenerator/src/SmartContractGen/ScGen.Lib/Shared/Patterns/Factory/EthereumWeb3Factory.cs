namespace ScGen.Lib.Shared.Patterns.Factory;

public class EthereumWeb3Factory : IEthereumWeb3Factory
{

    public Web3 CreateWeb3(string rpcUrl) => new(rpcUrl);

    public Web3 CreateWeb3WithAccount(string rpcUrl, string privateKey)
        => new(new Nethereum.Web3.Accounts.Account(privateKey),rpcUrl);
}