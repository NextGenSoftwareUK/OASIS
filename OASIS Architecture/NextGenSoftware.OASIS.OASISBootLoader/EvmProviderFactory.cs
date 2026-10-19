using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

// Constructor/configuration wiring only: no activation, deployment or network I/O.
internal static class EvmProviderFactory
{
    internal static IOASISStorageProvider CreateArbitrum(ArbitrumOASISProviderSettings settings,string mode,string overrideConnectionString)
    {
        if(settings==null) throw new ArgumentNullException(nameof(settings));
        var endpoint=overrideConnectionString??settings.ConnectionString;
        if(mode==HyperDriveModes.V2)
        {
            if(settings.ChainId<=0) throw new ArgumentOutOfRangeException(nameof(settings.ChainId));
            // Existing ContractAddress must identify a compatible Web3Core deployment;
            // normal activation verifies native protocol. Never retry a legacy layout.
            return new ArbitrumOASIS_Web3Core(endpoint,settings.ChainPrivateKey,settings.ContractAddress,settings.ChainId)
            {TokenEscrowContractAddress=settings.TokenEscrowContractAddress,IsVersionControlEnabled=settings.IsVersionControlEnabled};
        }
        return new ArbitrumOASIS(endpoint,settings.ChainPrivateKey,settings.ChainId,settings.ContractAddress);
    }

    internal static PolygonOASIS CreatePolygon(PolygonOASISProviderSettings settings,string overrideConnectionString)
    {
        if(settings==null) throw new ArgumentNullException(nameof(settings));
        return new PolygonOASIS(overrideConnectionString??settings.ConnectionString,settings.ChainPrivateKey,settings.ContractAddress,settings.ChainId)
        {TokenEscrowContractAddress=settings.TokenEscrowContractAddress,IsVersionControlEnabled=settings.IsVersionControlEnabled};
    }
}
