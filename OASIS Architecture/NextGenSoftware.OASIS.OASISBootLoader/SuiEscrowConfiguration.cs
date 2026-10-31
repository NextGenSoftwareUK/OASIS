using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class SuiEscrowConfiguration
{
    internal static SuiTokenEscrowConfiguration Create(SuiOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.IdBoundTokenEscrow == null) return null;
        var selected = new SuiTokenEscrowConfiguration(settings.IdBoundTokenEscrow.EscrowObjectId);
        selected.Validate();
        return selected;
    }
}
