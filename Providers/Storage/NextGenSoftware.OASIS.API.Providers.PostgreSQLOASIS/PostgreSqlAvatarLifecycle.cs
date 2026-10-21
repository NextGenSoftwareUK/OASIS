using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private static bool IsLifecycleConflict(Exception cause)=>cause is StorageLifecycleConflictException||cause is PostgresException {SqlState:"40001" or "40P01"};
    private static async Task<T?> ReadVisibleAvatarRecordAsync<T>(Guid id,string json,int version) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var document=StoredDocument(json);
        var current=await RecordHistory.ReadAsync(id,document,0,Core.Serialization.StoredRecordJson.DeserializeStored<T>);
        // Current deletion visibility applies to all ordinary profile reads, including
        // numbered requests. Archives remain durable, not a way to expose a deleted account.
        if(current.IsDeleted)return null;
        return version==0||current.Version==version?current:await RecordHistory.ReadAsync(id,document,version,Core.Serialization.StoredRecordJson.DeserializeStored<T>);
    }

    private async Task<OASISResult<bool>> DeleteAvatarRecordAsync(Guid id,string selector,bool? email,bool soft)
    {
        var result=new OASISResult<bool>(false);
        var acknowledged=false;var committedId=id;
        try{
            if(email.HasValue){if(string.IsNullOrWhiteSpace(selector))throw new ArgumentException("A nonblank avatar selector is required.",nameof(selector));}
            else if(id==Guid.Empty)throw new ArgumentException("A nonempty avatar identity is required.",nameof(id));
            var commit=await DeleteAvatarTransactionAsync(id,selector,email,soft);acknowledged=true;committedId=commit.Id;
            result.Result=true;result.IsDeleted=true;result.DeletedCount=1;result.IsSaved=commit.Changed>0;result.SavedCount=soft?commit.Changed:0;
            if(commit.Tombstone!=null)AvatarTombstoneEvidence.Attach(result,commit.Tombstone);
        }catch(Exception cause){
            if(acknowledged){cause=new PostgreSqlWriteReconciliationException(committedId,cause);result.Result=false;result.IsDeleted=false;result.IsSaved=false;result.DeletedCount=0;result.SavedCount=0;}
            OASISErrorHandling.HandleError(ref result,"PostgreSQL avatar lifecycle failed.",cause);
            if(cause is StorageIdentityAmbiguousException)result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is KeyNotFoundException)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
            else if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            else if(IsLifecycleConflict(cause))result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private async Task<(Guid Id,int Changed,AvatarTombstone? Tombstone)> DeleteAvatarTransactionAsync(Guid id,string selector,bool? email,bool soft)
    {
        var mutationStarted=false;var committing=false;var committed=false;
        try{
            await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            if(email.HasValue){
                // Unique identity selection and row locks share one native snapshot.
                // Never delete every row matching an arbitrary username/email predicate.
                var selected=await StoredRecordIdentityLookup.SelectAsync<IAvatar>(selector,0,
                    _=>ReadAvatarCandidatesAsync(connection,transaction),row=>email.Value?row.Email:row.Username);
                if(selected.IsError){if(selected.Exception!=null)ExceptionDispatchInfo.Capture(selected.Exception).Throw();throw new KeyNotFoundException(selected.Message);}
                id=selected.Result.Id;
            }
            var profileBody=await ReadLockedRecordAsync(connection,transaction,"oasis_avatars",id);
            if(profileBody==null)throw new KeyNotFoundException("Avatar identity was not found.");
            var profile=await ReadKeyedStoredRecordAsync<Avatar>(id,profileBody,0);
            var detailBody=await ReadLockedRecordAsync(connection,transaction,"oasis_avatar_details",id);
            var detail=detailBody==null?null:await ReadKeyedStoredRecordAsync<AvatarDetail>(id,detailBody,0);
            var changed=0;
            AvatarTombstone? tombstone=null;
            if(soft){
                var date=profile.IsDeleted?profile.DeletedDate:DateTime.UtcNow;
                var actor=profile.IsDeleted?profile.DeletedByAvatarId:Guid.Empty; // This interface supplies no actor.
                tombstone=new AvatarTombstone(id,date,actor);
                if(NeedsTombstone(profile,date,actor)){
                    MarkTombstone(profile,date,actor);
                    await WriteTombstoneAsync<IAvatar>(connection,transaction,"oasis_avatars",profile,profileBody,()=>mutationStarted=true);changed++;
                }
                if(detail!=null&&NeedsTombstone(detail,date,actor)){
                    MarkTombstone(detail,date,actor);
                    await WriteTombstoneAsync<IAvatarDetail>(connection,transaction,"oasis_avatar_details",detail,detailBody!,()=>mutationStarted=true);changed++;
                }
            }else{
                if(detailBody!=null){await DeleteObservedRecordAsync(connection,transaction,"oasis_avatar_details",id,detailBody,()=>mutationStarted=true);changed++;}
                await DeleteObservedRecordAsync(connection,transaction,"oasis_avatars",id,profileBody,()=>mutationStarted=true);changed++;
            }
            committing=true;await transaction.CommitAsync();committed=true;return (id,changed,tombstone);
        }catch(Exception cause)when(committed||mutationStarted&&cause is NpgsqlException and not PostgresException||committing&&cause is not PostgresException){
            throw new PostgreSqlWriteReconciliationException(id,cause);
        }
    }

    private static async Task<OASISResult<IEnumerable<IAvatar>>> ReadAvatarCandidatesAsync(NpgsqlConnection connection,NpgsqlTransaction transaction)
    {
        var rows=new List<IAvatar>();
        await using var command=new NpgsqlCommand("SELECT id,data_json FROM oasis_avatars WHERE is_deleted=FALSE",connection,transaction);
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()){
            var row=await ReadKeyedStoredRecordAsync<Avatar>(reader.GetGuid(0),reader.GetString(1),0);
            if(!row.IsDeleted)rows.Add(row);
        }
        return new OASISResult<IEnumerable<IAvatar>>{Result=rows,IsLoaded=true,LoadedCount=rows.Count};
    }

    private static bool NeedsTombstone(IHolonBase record,DateTime date,Guid actor)
        =>!record.IsDeleted||record.IsActive||record.DeletedDate!=date||record.DeletedByAvatarId!=actor;
    private static void MarkTombstone(IHolonBase record,DateTime date,Guid actor)
    {record.IsActive=false;record.DeletedDate=date;record.DeletedByAvatarId=actor;}

    private async Task<T> WriteTombstoneAsync<T>(NpgsqlConnection connection,NpgsqlTransaction transaction,string table,T record,string original,Action mutationStarting) where T:class,IHolonBase
    {
        var nativeFlag=table=="oasis_avatar_details"?"":"is_deleted=TRUE,modified_date=NOW(),";
        await using var command=new NpgsqlCommand($"UPDATE {table} SET {nativeFlag}data_json=@data_json WHERE id=@id",connection,transaction);
        command.Parameters.AddWithValue("id",record.Id);command.Parameters.AddWithValue("data_json",string.Empty);
        return await WriteObservedRecordInTransactionAsync(command,record,original,static()=>{},mutationStarting:mutationStarting);
    }

    private static async Task DeleteObservedRecordAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,string table,Guid id,string original,Action mutationStarting)
    {
        await using var command=new NpgsqlCommand($"DELETE FROM {table} WHERE id=@id RETURNING data_json",connection,transaction);
        command.Parameters.AddWithValue("id",id);mutationStarting();
        if(await command.ExecuteScalarAsync() is not string acknowledged||!string.Equals(acknowledged,original,StringComparison.Ordinal))
            throw new InvalidDataException("PostgreSQL delete did not acknowledge the exact locked original record.");
    }
}
