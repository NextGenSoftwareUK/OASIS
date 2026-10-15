using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public partial class AvatarRepository
    {
        public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
            => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        {
            var result=new OASISResult<bool>();
            try
            {
                if(tombstone == null) throw new ArgumentNullException(nameof(tombstone));
                using var session=await _dbContext.MongoClient.StartSessionAsync();
                session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority));
                bool commitAttempted=false;
                try
                {
                    var avatars=await _dbContext.Avatar.Find(session,row=>row.HolonId==tombstone.AvatarId).Limit(2).ToListAsync();
                    var details=await _dbContext.AvatarDetail.Find(session,row=>row.HolonId==tombstone.AvatarId).Limit(2).ToListAsync();
                    if(avatars.Count>1 || details.Count>1) throw new StorageIdentityAmbiguousException();
                    var avatar=avatars.SingleOrDefault() ?? throw new KeyNotFoundException("Mongo tombstone application requires an existing avatar.");
                    var detail=details.SingleOrDefault();
                    Guid Actor(HolonBase row) => string.IsNullOrEmpty(row.DeletedByAvatarId) ? Guid.Empty : Guid.Parse(row.DeletedByAvatarId);
                    bool Conflicts(HolonBase row) => row != null && row.DeletedDate != DateTime.MinValue
                        && (row.DeletedDate != tombstone.DeletedAtUtc || Actor(row) != tombstone.DeletedByAvatarId);
                    if(Conflicts(avatar) || Conflicts(detail)) throw new StorageLifecycleConflictException("Mongo replica has a conflicting committed tombstone.");
                    bool Matches(HolonBase row) => row == null || row.DeletedDate==tombstone.DeletedAtUtc && Actor(row)==tombstone.DeletedByAvatarId && !row.IsActive;
                    var already=Matches(avatar) && Matches(detail);
                    if(!already)
                    {
                        var actor=tombstone.DeletedByAvatarId.ToString();
                        var avatarPatch=SoftDeletionPatch<Avatar>(tombstone.DeletedAtUtc,actor,true);
                        var written=await _dbContext.Avatar.UpdateOneAsync(session,row=>row.Id==avatar.Id,avatarPatch);
                        if(!written.IsAcknowledged || written.MatchedCount!=1) throw new InvalidOperationException("Mongo avatar lifecycle patch was not acknowledged.");
                        if(detail != null)
                        {
                            var detailPatch=SoftDeletionPatch<AvatarDetail>(tombstone.DeletedAtUtc,actor,true);
                            var detailWritten=await _dbContext.AvatarDetail.UpdateOneAsync(session,row=>row.Id==detail.Id,detailPatch);
                            if(!detailWritten.IsAcknowledged || detailWritten.MatchedCount!=1) throw new InvalidOperationException("Mongo detail lifecycle patch was not acknowledged.");
                        }
                    }
                    commitAttempted=!already;
                    await session.CommitTransactionAsync();
                    result.Result=true; result.IsDeleted=true; result.DeletedCount=1; result.IsSaved=!already;
                    AvatarTombstoneEvidence.Attach(result,tombstone);
                }
                catch(Exception cause)
                {
                    Exception evidence=commitAttempted ? new MongoTransactionReconciliationException(session.ServerSession.Id.ToJson(),session.WrappedCoreSession.CurrentTransaction.TransactionNumber,cause) : cause;
                    OASISErrorHandling.HandleError(ref result,"Mongo committed tombstone application failed.",evidence);
                    if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
                    if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
                    if(session.IsInTransaction) await session.AbortTransactionAsync();
                }
            }
            catch(Exception cause)
            {
                var evidence=result.Exception == null ? cause : new AggregateException(result.Exception,cause);
                OASISErrorHandling.HandleError(ref result,"Mongo tombstone session failed.",evidence);
                result.Result=false;
            }
            return result;
        }
    }
}
