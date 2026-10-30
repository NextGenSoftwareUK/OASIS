using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

internal static class EvmCqlDemoProviders
{
    // Exercise the actual shipping factory. The explicit local chain identity is
    // also part of the authenticated encryption context, not just a display label.
    internal static OASISStorageProviderBase CreateRootstock(string rpc, string key, string contract)
        => (OASISStorageProviderBase)EvmProviderFactory.CreateRootstock(
            new RootstockOASISProviderSettings
            {
                ConnectionString = rpc, ChainPrivateKey = key,
                ContractAddress = contract, ChainId = 31338
            }, overrideConnectionString: null!);

    internal static OASISStorageProviderBase CreateAvalanche(string rpc, string key, string contract)
        => (OASISStorageProviderBase)EvmProviderFactory.CreateAvalanche(
            new AvalancheOASISProviderSettings
            {
                RpcEndpoint = rpc,
                ChainPrivateKey = key,
                ContractAddress = contract,
                ChainId = "31338"
            },
            overrideConnectionString: null!);

    // Base's central BootLoader registration directly constructs this shipped
    // Web3Core class. Keep its real authority and isolated native contract.
    internal static OASISStorageProviderBase CreateBase(string rpc, string key, string contract)
        => new NextGenSoftware.OASIS.API.Providers.BaseOASIS.BaseOASIS(rpc, key, 31338, contract)
            { IsVersionControlEnabled = false };

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
