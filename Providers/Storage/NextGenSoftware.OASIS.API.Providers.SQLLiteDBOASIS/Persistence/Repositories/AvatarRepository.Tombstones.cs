using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class AvatarRepository
    {
        private static Guid TombstoneActor(string stored) => string.IsNullOrEmpty(stored) ? Guid.Empty : Guid.Parse(stored);

        public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
            => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        {
            var result=new OASISResult<bool>();
            var persistenceEntered=false;
            var commitEntered=false;
            try
            {
                ArgumentNullException.ThrowIfNull(tombstone);
                var id=tombstone.AvatarId.ToString();
                var actor=tombstone.DeletedByAvatarId.ToString();
                using var transaction=await _dbContext.Database.BeginTransactionAsync();
                var hasDetail=false;
                var changed=false;
                try
                {
                    var avatar=await _dbContext.Avatars.AsNoTracking().SingleOrDefaultAsync(row=>row.Id==id)
                        ?? throw new KeyNotFoundException("SQLite tombstone application requires an existing avatar.");
                    var detail=await _dbContext.AvatarDetails.AsNoTracking().SingleOrDefaultAsync(row=>row.Id==id);
                    hasDetail=detail != null;
                    bool Conflicts(DateTime deleted,string by) => deleted != DateTime.MinValue
                        && (deleted != tombstone.DeletedAtUtc || TombstoneActor(by) != tombstone.DeletedByAvatarId);
                    if(Conflicts(avatar.DeletedDate,avatar.DeletedByAvatarId) || detail != null && Conflicts(detail.DeletedDate,detail.DeletedByAvatarId))
                        throw new StorageLifecycleConflictException("SQLite replica has a conflicting committed tombstone.");
                    var already=avatar.DeletedDate==tombstone.DeletedAtUtc && avatar.DeletedDateKind==(int)DateTimeKind.Utc && TombstoneActor(avatar.DeletedByAvatarId)==tombstone.DeletedByAvatarId && !avatar.IsActive
                        && (detail == null || detail.DeletedDate==tombstone.DeletedAtUtc && detail.DeletedDateKind==(int)DateTimeKind.Utc && TombstoneActor(detail.DeletedByAvatarId)==tombstone.DeletedByAvatarId && !detail.IsActive);
                    if(!already)
                    {
                        persistenceEntered=true;
                        if(await _dbContext.Avatars.Where(row=>row.Id==id).ExecuteUpdateAsync(set=>set
                            .SetProperty(row=>row.DeletedDate,tombstone.DeletedAtUtc).SetProperty(row=>row.DeletedDateKind,(int)DateTimeKind.Utc).SetProperty(row=>row.DeletedByAvatarId,actor).SetProperty(row=>row.IsActive,false)) != 1)
                            throw new InvalidOperationException("SQLite avatar changed during tombstone application.");
                        if(hasDetail && await _dbContext.AvatarDetails.Where(row=>row.Id==id).ExecuteUpdateAsync(set=>set
                            .SetProperty(row=>row.DeletedDate,tombstone.DeletedAtUtc).SetProperty(row=>row.DeletedDateKind,(int)DateTimeKind.Utc).SetProperty(row=>row.DeletedByAvatarId,actor).SetProperty(row=>row.IsActive,false)) != 1)
                            throw new InvalidOperationException("SQLite avatar detail changed during tombstone application.");
                        changed=true;
                    }
                    commitEntered=true;
                    await transaction.CommitAsync();
                }
                catch(Exception cause)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        if(!commitEntered) persistenceEntered=false;
                    }
                    catch(Exception rollback) { throw new AggregateException("SQLite tombstone application and rollback failed.",cause,rollback); }
                    throw;
                }
                result.Result=true; result.IsDeleted=true; result.DeletedCount=1; result.IsSaved=changed;
                AvatarTombstoneEvidence.Attach(result,tombstone);
                // Direct lifecycle SQL must not leave stale lifecycle fields in this context.
                foreach(var entry in _dbContext.ChangeTracker.Entries().Where(entry=>entry.State is EntityState.Unchanged or EntityState.Modified).ToList())
                {
                    if(entry.Entity is not AvatarModel && (!hasDetail || entry.Entity is not AvatarDetailModel)) continue;
                    if((string)entry.Property("Id").CurrentValue != id) continue;
                    foreach(var field in new[] {("DeletedDate",(object)tombstone.DeletedAtUtc),("DeletedDateKind",(object)(int)DateTimeKind.Utc),("DeletedByAvatarId",(object)actor),("IsActive",(object)false)})
                    {
                        var property=entry.Property(field.Item1); property.CurrentValue=field.Item2; property.OriginalValue=field.Item2; property.IsModified=false;
                    }
                }
            }
            catch(Exception cause)
            {
                if(result.Result)
                {
                    result.IsWarning=true; result.WarningCount++; result.Exception=cause;
                    result.Message="SQLite tombstone committed but tracker synchronization failed: "+cause.Message;
                }
                else
                {
                    if(persistenceEntered || commitEntered)
                        cause=new SqliteMutationReconciliationException(tombstone.AvatarId,cause);
                    OASISErrorHandling.HandleError(ref result,"SQLite committed tombstone application failed: "+cause.Message,cause);
                    if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
                }
            }
            return result;
        }
    }
}
