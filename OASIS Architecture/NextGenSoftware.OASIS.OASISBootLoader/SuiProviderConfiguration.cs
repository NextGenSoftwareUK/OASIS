using System;
using System.Globalization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class SuiProviderConfiguration
{
    internal static SuiOASIS Create(SuiOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        SuiNativeFeeLimits limits=null;
        var ceiling=settings.MaximumGasBudgetMist;
        if(ceiling!=null){
            if(!ulong.TryParse(ceiling,NumberStyles.None,CultureInfo.InvariantCulture,out var value)||value==0
                ||value.ToString(CultureInfo.InvariantCulture)!=ceiling)
                throw new ArgumentException("Sui MaximumGasBudgetMist must be canonical positive u64 decimal text.",nameof(settings));
            limits=new SuiNativeFeeLimits(value);
        }
        var storageKeyVariable=settings.StorageEncryptionKeyEnvironmentVariable;
        var escrow=SuiEscrowConfiguration.Create(settings);
        // Explicit configured/unconfigured modes; never catch a failed configured creation and downgrade.
        return limits==null
            ?new SuiOASIS(settings.RpcEndpoint??"https://fullnode.mainnet.sui.io:443",settings.Network??"mainnet",settings.ChainId??"",
                settings.ContractAddress??"",settings.PrivateKey??"",settings.StorageObjectId??"",settings.CurrencyObjectId??"",
                ()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(storageKeyVariable),escrow)
            :SuiOASIS.CreateWithNativeFeeLimits(limits,settings.RpcEndpoint??"https://fullnode.mainnet.sui.io:443",settings.Network??"mainnet",settings.ChainId??"",
                settings.ContractAddress??"",settings.PrivateKey??"",settings.StorageObjectId??"",settings.CurrencyObjectId??"",
                ()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(storageKeyVariable),escrow);
    }
}
