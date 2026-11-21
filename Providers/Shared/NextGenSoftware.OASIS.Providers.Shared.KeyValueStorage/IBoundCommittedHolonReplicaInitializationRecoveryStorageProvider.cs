using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Initialization and receipt observation fenced by the reviewed physical target.</summary>
public interface IBoundCommittedHolonReplicaInitializationRecoveryStorageProvider : ICommittedHolonReplicaInitializationRecoveryStorageProvider
{
    Task<OASISResult<IEnumerable<IHolon>>> InitializeBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default);
    Task<OASISResult<HolonReplicaInitializationOutcome>> ReadBoundReplicaInitializationOutcomeAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default);
}
