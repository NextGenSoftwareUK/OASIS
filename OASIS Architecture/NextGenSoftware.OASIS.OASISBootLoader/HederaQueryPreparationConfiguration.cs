using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class HederaQueryPreparationConfiguration
{
    // The shipped DNA templates carry an unset placeholder (no nodes, zero ceilings).
    // That means "paid queries not enabled", exactly like a missing section. Any
    // partially filled policy is still passed through and rejected by Capture().
    internal static bool IsUnset(HashgraphStorageQueryPreparationSettings settings)
        => settings == null || ((settings.AuthorizedNodeAccountIds == null || settings.AuthorizedNodeAccountIds.Length == 0)
            && settings.MaxPaymentTinybars == 0 && settings.MaxTransactionFeeTinybars == 0);

    internal static HederaStorageQueryPreparation Create(HashgraphStorageQueryPreparationSettings settings)
        => IsUnset(settings) ? null : new HederaStorageQueryPreparation
        {
            AuthorizedNodeAccountIds = settings.AuthorizedNodeAccountIds?.ToArray(),
            MaxPaymentTinybars = settings.MaxPaymentTinybars,
            MaxTransactionFeeTinybars = settings.MaxTransactionFeeTinybars,
            MaximumRequestsPerInvocation = settings.MaximumRequestsPerInvocation,
            PersistenceTimeoutMs = settings.PersistenceTimeoutMs
        };
}
