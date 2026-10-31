using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class CosmosEscrowConfiguration
{
    internal static CosmosTokenEscrowConfiguration Create(CosmosBlockChainOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.IdBoundTokenEscrow == null) return null;
        var selected = new CosmosTokenEscrowConfiguration(settings.IdBoundTokenEscrow.ContractAddress,
            settings.IdBoundTokenEscrow.CodeSha256);
        selected.Validate(settings.ContractAddress);
        return selected;
    }
}
