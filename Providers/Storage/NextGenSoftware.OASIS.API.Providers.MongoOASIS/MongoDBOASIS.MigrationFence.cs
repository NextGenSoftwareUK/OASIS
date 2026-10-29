using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IHolonMigrationFenceStorageProvider
{
    public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, MongoHolonMigrationFence.Action.Acquire, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, MongoHolonMigrationFence.Action.Read, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid ownerToken, CancellationToken cancellationToken = default)
        => Fence(request, MongoHolonMigrationFence.Action.Cancel, ownerToken, cancellationToken);
    private Task<OASISResult<HolonMigrationFenceObservation>> Fence(HolonMigrationFenceRequest request,
        MongoHolonMigrationFence.Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native store before migration fence operations." });
        return _holonRepository.ExecuteMigrationFenceAsync(request, action, owner, token);
    }
}
