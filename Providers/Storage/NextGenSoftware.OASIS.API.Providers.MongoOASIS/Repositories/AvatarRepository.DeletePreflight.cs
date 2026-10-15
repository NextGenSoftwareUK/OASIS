using System;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class AvatarRepository
{
    private static UpdateDefinition<T> SoftDeletionPatch<T>(DateTime deletedAt,string actor,bool includeActor) where T:HolonBase
    {
        long? ticks=deletedAt.Ticks % TimeSpan.TicksPerMillisecond==0 ? null:deletedAt.Ticks;
        var patch=Builders<T>.Update.Set(row=>row.DeletedDate,deletedAt).Set(row=>row.DeletedDateExactUtcTicks,ticks).Set(row=>row.IsActive,false);
        return includeActor ? patch.Set(row=>row.DeletedByAvatarId,actor):patch;
    }

    private sealed class MissingDeletionIdentityException(string message):InvalidOperationException(message);

    private static OASISResult<bool> MissingDeletionIdentity() => new() {IsError=true,
        ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Avatar deletion selector matched no persisted identity."};

    private static void HandleDeletionSessionFailure(ref OASISResult<bool> result,Exception cause)
    {
        if(result.IsDeleted && !result.IsError)
        {
            result.IsWarning=true;result.WarningCount++;
            result.Exception=cause;result.Message="Mongo deletion committed, but session cleanup failed.";
            return;
        }
        Exception evidence=result.Exception==null ? cause:new AggregateException(result.Exception,cause);
        if(result.ErrorCode==OASISResultErrorCodes.RecordNotFound)
        {
            // Cleanup cannot turn known absence into permission to delete a stale replica.
            evidence=new AggregateException(new MissingDeletionIdentityException(result.Message),cause);
            result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        OASISErrorHandling.HandleError(ref result,"MongoDB avatar deletion session failed.",evidence);
        result.Result=false;result.IsDeleted=false;
    }
}
