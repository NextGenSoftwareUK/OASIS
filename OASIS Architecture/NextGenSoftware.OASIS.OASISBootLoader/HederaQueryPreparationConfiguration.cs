using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class HederaQueryPreparationConfiguration
{
    internal static HederaStorageQueryPreparation Create(HashgraphStorageQueryPreparationSettings settings)
        => settings == null ? null : new HederaStorageQueryPreparation
        {
            AuthorizedNodeAccountIds = settings.AuthorizedNodeAccountIds?.ToArray(),
            MaxPaymentTinybars = settings.MaxPaymentTinybars,
            MaxTransactionFeeTinybars = settings.MaxTransactionFeeTinybars,
            MaximumRequestsPerInvocation = settings.MaximumRequestsPerInvocation,
            PersistenceTimeoutMs = settings.PersistenceTimeoutMs
        };
}
