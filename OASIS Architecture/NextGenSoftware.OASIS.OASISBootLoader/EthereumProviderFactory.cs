using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

// Constructor/configuration only: never activates, deploys, or modifies hosted DNA.
internal static class EthereumProviderFactory
{
    internal static EthereumOASIS Create(EthereumOASISProviderSettings settings,string overrideConnectionString)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new EthereumOASIS(overrideConnectionString??settings.ConnectionString,settings.ChainPrivateKey,settings.ChainId,settings.ContractAddress)
        {TokenEscrowContractAddress=settings.TokenEscrowContractAddress,IsVersionControlEnabled=settings.IsVersionControlEnabled};
    }
}
