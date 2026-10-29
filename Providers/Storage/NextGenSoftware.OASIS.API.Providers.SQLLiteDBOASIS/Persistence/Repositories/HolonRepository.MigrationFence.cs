using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Acquire, Guid.Empty, token);
    public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Read, Guid.Empty, token);
    public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid ownerToken, CancellationToken token = default)
        => Fence(request, SqliteHolonMigrationFence.Action.Cancel, ownerToken, token);

    private Task<OASISResult<HolonMigrationFenceObservation>> Fence(HolonMigrationFenceRequest request,
        SqliteHolonMigrationFence.Action action, Guid owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try { return Task.FromResult(SqliteHolonMigrationFence.Execute(_dbContext.Database.GetDbConnection().ConnectionString, request, action, owner, token)); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception cause) { return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true,
            ErrorCount = 1, ErrorCode = "COSMIC_MIGRATION_FENCE_NOT_READY", Message = "The native repository is unavailable.", Exception = cause }); }
    }
}
