using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IHolonMigrationMutationStorageProvider, NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IHolonMigrationReplicaAdoptionStorageProvider
{
    public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Acquire, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Read, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid ownerToken, CancellationToken cancellationToken = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Cancel, ownerToken, cancellationToken);

    public Task<OASISResult<HolonMigrationFenceObservation>> BeginHolonMigrationMutationAsync(HolonMigrationMutationStartRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native store before mutation phase admission." });
        if (request == null) return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
            ErrorCode = "COSMIC_MIGRATION_FENCE_OPTIONS_INVALID", Message = "An exact immutable native mutation start request is required." });
        return WithHolonRepositoryAsync(repository => repository.BeginHolonMigrationMutationAsync(request, cancellationToken));
    }

    public Task<OASISResult<HolonMigrationFenceObservation>> AdoptPreparedHolonReplicaAsync(
        NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonMigrationReplicaAdoptionPlan plan,
        Guid owner, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing store before native adoption." });
        return WithHolonRepositoryAsync(repository => repository.AdoptPreparedHolonReplicaAsync(plan, owner, token));
    }

    private Task<OASISResult<HolonMigrationFenceObservation>> Fence(HolonMigrationFenceRequest request,
        SqliteHolonMigrationFence.Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native store before migration fence operations." });
        return action switch {
            SqliteHolonMigrationFence.Action.Acquire => WithHolonRepositoryAsync(repository => repository.AcquireHolonMigrationFenceAsync(request, token)),
            SqliteHolonMigrationFence.Action.Read => WithHolonRepositoryAsync(repository => repository.ReadHolonMigrationFenceAsync(request, token)),
            _ => WithHolonRepositoryAsync(repository => repository.CancelHolonMigrationFenceAsync(request, owner, token))
        };
    }
}
