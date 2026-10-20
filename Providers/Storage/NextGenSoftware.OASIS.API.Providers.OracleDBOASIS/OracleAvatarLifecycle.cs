using System;
using System.IO;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    /// <summary>The single native commit boundary shared by Oracle lifecycle operations.</summary>
    protected virtual Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        => transaction.CommitAsync();

    private async Task<OASISResult<bool>> DeleteAvatarPairAsync(string column,OracleDbType type,object value,bool softDelete)
    {
        var result=new OASISResult<bool>();var dispatched=false;var id=Guid.Empty;
        try{
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync();
            Avatar? avatar=null;string? avatarJson=null;string? detailJson=null;
            await using(var select=CreateCommand($"SELECT ID,DATA_JSON,IS_DELETED,USERNAME,EMAIL FROM OASIS_AVATARS WHERE {column}=:Selector AND ROWNUM<=2 FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Selector",type).Value=value;
                await using var reader=await select.ExecuteReaderAsync();
                if(await reader.ReadAsync()){
                    id=new Guid((byte[])reader.GetValue(0));avatarJson=reader.GetString(1);avatar=await ReadRecordVersionAsync<Avatar>(id,avatarJson,0,reader.GetInt32(2),ReadNativeIdentityProjection(reader,3))??throw new InvalidDataException("Oracle locked avatar is missing its current version.");
                    if(avatar.Id!=id)throw new InvalidDataException("Oracle avatar identity projection does not match its canonical record.");
                    if(column!="ID"&&!CanonicalIdentityMatches(avatar,column,value))throw new InvalidDataException("Oracle delete indexed identity differs from its canonical record.");
                    if(await reader.ReadAsync())throw new InvalidDataException("Oracle delete identity selector matched multiple avatars.");
                }
            }
            if(avatar==null)throw new InvalidOperationException("Oracle avatar to delete was not found.");
            AvatarDetail? detail=null;
            await using(var select=CreateCommand("SELECT DATA_JSON,USERNAME,EMAIL FROM OASIS_AVATAR_DETAILS WHERE ID=:Id FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                await using var reader=await select.ExecuteReaderAsync();
                if(await reader.ReadAsync()){detailJson=reader.GetString(0);detail=await ReadRecordVersionAsync<AvatarDetail>(id,detailJson,0,null,ReadNativeIdentityProjection(reader,1))??throw new InvalidDataException("Oracle locked detail is missing its current version.");if(detail.Id!=id)throw new InvalidDataException("Oracle avatar detail identity projection is invalid.");}
            }
            if(softDelete&&avatar.IsDeleted&&!avatar.IsActive&&(detail==null||detail.IsDeleted&&!detail.IsActive&&detail.DeletedDate==avatar.DeletedDate)){
                result.Result=true;return result;
            }
            var deletedAt=avatar.IsDeleted?avatar.DeletedDate:DateTime.UtcNow;
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                IHolonBase? row=table=="OASIS_AVATARS"?avatar:detail;if(row==null)continue;
                if(softDelete){row.IsActive=false;row.DeletedDate=deletedAt;}
                var sql=!softDelete?$"DELETE FROM {table} WHERE ID=:Id":table=="OASIS_AVATARS"
                    ?"UPDATE OASIS_AVATARS SET IS_DELETED=1,MODIFIED_DATE=SYSTIMESTAMP,DATA_JSON=:Json WHERE ID=:Id"
                    :"UPDATE OASIS_AVATAR_DETAILS SET DATA_JSON=:Json WHERE ID=:Id";
                await using var command=CreateCommand(sql,connection);command.Transaction=transaction;
                command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                if(softDelete)command.Parameters.Add("Json",OracleDbType.Clob).Value=row is Avatar storedAvatar
                    ?await StageRecordHistoryAsync(storedAvatar,avatarJson):await StageRecordHistoryAsync((AvatarDetail)row,detailJson);
                dispatched=true;if(await command.ExecuteNonQueryAsync()!=1)throw new InvalidDataException("Oracle locked lifecycle row changed unexpectedly.");
            }
            await CommitLifecycleTransactionAsync(transaction);result.Result=true;
        }catch(Exception cause){
            result.Exception=dispatched?new OracleLifecycleReconciliationException(id,cause):cause;
            OASISErrorHandling.HandleError(ref result,"Oracle avatar lifecycle operation failed; inspect the original exception.");
        }
        return result;
    }
}

public sealed class OracleLifecycleReconciliationException:Exception,INativeMutationReconciliationRequired
{
    public Guid RecordId{get;}
    public string OriginalTransactionId=>string.Empty;
    public OracleLifecycleReconciliationException(Guid id,Exception cause)
        :base("Oracle lifecycle outcome requires reconciliation before replay.",cause)=>RecordId=id;
}
