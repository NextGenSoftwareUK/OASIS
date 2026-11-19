using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

internal static class EvmCqlDemoProviders
{
    // Compile/link the actual BootLoader factory so acceptance proves the V2
    // shipping selection, rather than a separately constructed legacy provider.
    internal static OASISStorageProviderBase CreateArbitrum(string rpc, string key, string contract)
    {
        var provider = EvmProviderFactory.CreateArbitrum(new ArbitrumOASISProviderSettings
        {
            ConnectionString = rpc,
            ChainPrivateKey = key,
            ContractAddress = contract,
            ChainId = 31338,
            IsVersionControlEnabled = false
        }, HyperDriveModes.V2, null!);
        Assert.IsInstanceOfType(provider, typeof(NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.ArbitrumOASIS_Web3Core));
        return (OASISStorageProviderBase)provider;
    }
}
