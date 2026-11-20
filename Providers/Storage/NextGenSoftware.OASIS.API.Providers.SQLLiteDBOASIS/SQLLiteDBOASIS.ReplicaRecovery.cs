using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IBoundCommittedHolonReplicaInitializationRecoveryStorageProvider
{
    public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken cancellationToken=default)
        =>WithHolonRepositoryAsync(repository => repository.ReadReplicaInitializationOutcomeAsync(original,cancellationToken));

    public Task<OASISResult<IEnumerable<IHolon>>> InitializeBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request,CosmicStorageIdentity expectedStorage,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        return Task.FromResult(WithHolonRepository(repository => repository.InitializeCommittedHolonReplica(request,cancellationToken,expectedStorage)));
    }

    public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadBoundReplicaInitializationOutcomeAsync(
        CommittedHolonReplicaRequest request,CosmicStorageIdentity expectedStorage,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        return WithHolonRepositoryAsync(repository => repository.ReadReplicaInitializationOutcomeAsync(request,cancellationToken,expectedStorage));
    }
}
