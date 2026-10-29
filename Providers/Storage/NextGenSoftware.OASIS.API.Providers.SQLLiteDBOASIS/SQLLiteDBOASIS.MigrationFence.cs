using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IHolonMigrationMutationStorageProvider
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
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native store before mutation phase admission." });
        if (request == null) return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
            ErrorCode = "COSMIC_MIGRATION_FENCE_OPTIONS_INVALID", Message = "An exact immutable native mutation start request is required." });
        return _holonRepository.BeginHolonMigrationMutationAsync(request, cancellationToken);
    }

    private Task<OASISResult<HolonMigrationFenceObservation>> Fence(HolonMigrationFenceRequest request,
        SqliteHolonMigrationFence.Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native store before migration fence operations." });
        return action switch {
            SqliteHolonMigrationFence.Action.Acquire => _holonRepository.AcquireHolonMigrationFenceAsync(request, token),
            SqliteHolonMigrationFence.Action.Read => _holonRepository.ReadHolonMigrationFenceAsync(request, token),
            _ => _holonRepository.CancelHolonMigrationFenceAsync(request, owner, token)
        };
    }
}
