using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private async Task<OASISResult<IHolon>> DeleteHolonRecordAsync(Guid id)
    {
        var result=new OASISResult<IHolon>();var acknowledged=false;
        try{
            if(id==Guid.Empty)throw new ArgumentException("A nonempty holon identity is required.",nameof(id));
            var commit=await DeleteHolonTransactionAsync(id);acknowledged=true;
            result.Result=commit.Record;result.IsDeleted=true;result.DeletedCount=1;result.IsSaved=commit.Changed>0;result.SavedCount=commit.Changed;
        }catch(Exception cause){
            if(acknowledged){cause=new PostgreSqlWriteReconciliationException(id,cause);result.Result=null;result.IsDeleted=false;result.IsSaved=false;result.DeletedCount=0;result.SavedCount=0;}
            OASISErrorHandling.HandleError(ref result,"PostgreSQL holon deletion failed.",cause);
            if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            else if(cause is KeyNotFoundException)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
            else if(IsLifecycleConflict(cause))result.ErrorCode=Core.Helpers.StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private async Task<(IHolon Record,int Changed)> DeleteHolonTransactionAsync(Guid id)
    {
        var mutationStarted=false;var committing=false;var committed=false;
        try{
            await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            var original=await ReadLockedRecordAsync(connection,transaction,"oasis_holons",id);
            if(original==null)throw new KeyNotFoundException("Holon identity was not found.");
            IHolon record=await ReadKeyedStoredRecordAsync<Holon>(id,original,0);
            var date=record.IsDeleted?record.DeletedDate:DateTime.UtcNow;var actor=record.IsDeleted?record.DeletedByAvatarId:Guid.Empty;
            var changed=0;
            if(NeedsTombstone(record,date,actor)){
                MarkTombstone(record,date,actor);
                record=await WriteTombstoneAsync(connection,transaction,"oasis_holons",record,original,()=>mutationStarted=true);changed=1;
            }
            committing=true;await transaction.CommitAsync();committed=true;return (record,changed);
        }catch(Exception cause)when(committed||mutationStarted&&cause is NpgsqlException and not PostgresException||committing&&cause is not PostgresException){
            throw new PostgreSqlWriteReconciliationException(id,cause);
        }
    }
}
