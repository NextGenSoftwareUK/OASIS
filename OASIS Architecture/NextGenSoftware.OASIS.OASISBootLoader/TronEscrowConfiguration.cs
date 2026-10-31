using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class TronEscrowConfiguration
{
    internal static TronIdBoundEscrowConfiguration Create(TronIdBoundEscrowSettings settings)
        => settings == null ? null : new(settings.ContractAddress, settings.RuntimeSha256);
}
