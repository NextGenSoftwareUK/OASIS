using System;
using System.Threading;
using System.Threading.Tasks;
using FenceProviderType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IHolonMigrationFenceStorageProvider
{
    public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, NeoHolonMigrationFence.Action.Acquire, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken cancellationToken = default)
        => Fence(request, NeoHolonMigrationFence.Action.Read, Guid.Empty, cancellationToken);
    public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid ownerToken, CancellationToken cancellationToken = default)
        => Fence(request, NeoHolonMigrationFence.Action.Cancel, ownerToken, cancellationToken);
    private async Task<OASISResult<HolonMigrationFenceObservation>> Fence(HolonMigrationFenceRequest request,
        NeoHolonMigrationFence.Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); await _lifecycleGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!IsProviderActivated || _driver == null || _storageDriverEndpoint == null)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "Activate the existing native driver before fence operations." };
            if (request == null || request.ExpectedStorage.Authority != FenceProviderType.Neo4jOASIS)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_MIGRATION_FENCE_OPTIONS_INVALID", Message = "An exact Neo physical-store scope is required." };
            var native = new NeoHolonReplicationOutbox(_driver, request.ExpectedStorage, _storageDriverEndpoint);
            return await native.ExecuteMigrationFenceAsync(request, action, owner, token).ConfigureAwait(false);
        }
        finally { _lifecycleGate.Release(); }
    }
}
