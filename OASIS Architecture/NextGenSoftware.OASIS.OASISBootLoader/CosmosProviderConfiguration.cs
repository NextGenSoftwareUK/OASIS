using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class CosmosProviderConfiguration
{
    internal static CosmosBlockChainOASIS Create(CosmosBlockChainOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var limits=settings.MaximumFeeUnitsByDenomination==null?null:new CosmosNativeFeeLimits(settings.MaximumFeeUnitsByDenomination);
        var variable=settings.StorageEncryptionKeyEnvironmentVariable;var escrow=CosmosEscrowConfiguration.Create(settings);
        return limits==null
            ?new CosmosBlockChainOASIS(settings.RpcEndpoint??"https://cosmos-rpc.polkachu.com",settings.ChainId??"cosmoshub-4",settings.PrivateKey??"",settings.ContractAddress??"",
                settings.AddressPrefix??"cosmos",settings.GasPrice??"0.025uatom",settings.NativeDenom,settings.NativeDecimals,
                ()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(variable),escrow)
            :CosmosBlockChainOASIS.CreateWithNativeFeeLimits(limits,settings.RpcEndpoint??"https://cosmos-rpc.polkachu.com",settings.ChainId??"cosmoshub-4",settings.PrivateKey??"",settings.ContractAddress??"",
                settings.AddressPrefix??"cosmos",settings.GasPrice??"0.025uatom",settings.NativeDenom,settings.NativeDecimals,
                ()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(variable),escrow);
    }
}
