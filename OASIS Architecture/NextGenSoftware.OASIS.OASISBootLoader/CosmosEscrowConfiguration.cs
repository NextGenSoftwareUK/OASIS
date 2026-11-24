using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class CosmosEscrowConfiguration
{
    internal static CosmosTokenEscrowConfiguration Create(CosmosBlockChainOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        // The DNA templates ship an empty escrow section ("" / "") meaning "no escrow
        // configured"; treat it like a missing section. A half-filled section is still rejected.
        var escrow = settings.IdBoundTokenEscrow;
        if (escrow == null || (string.IsNullOrEmpty(escrow.ContractAddress) && string.IsNullOrEmpty(escrow.CodeSha256)))
            return null;
        var selected = new CosmosTokenEscrowConfiguration(settings.IdBoundTokenEscrow.ContractAddress,
            settings.IdBoundTokenEscrow.CodeSha256);
        selected.Validate(settings.ContractAddress);
        return selected;
    }
}
