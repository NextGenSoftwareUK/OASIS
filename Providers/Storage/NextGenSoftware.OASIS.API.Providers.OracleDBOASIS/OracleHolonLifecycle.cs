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
    private async Task<OASISResult<IHolon>> DeleteHolonRecordAsync(Guid id,bool softDelete)
    {
        var result=new OASISResult<IHolon>();var dispatched=false;
        try{
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync();
            Holon row;string original;
            await using(var select=CreateCommand("SELECT DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                await using var reader=await select.ExecuteReaderAsync();
                if(!await reader.ReadAsync()){RecordMissing(result,"Oracle holon to delete was not found.");return result;}
                original=reader.GetString(0);row=await ReadRecordVersionAsync<Holon>(id,original,0)??throw new InvalidDataException("Oracle locked holon is missing its current version.");
                ValidateNativeHolonProjection(row,reader.GetInt32(1),reader.GetInt32(2),reader.IsDBNull(3)?Guid.Empty:new Guid((byte[])reader.GetValue(3)));
            }
            if(softDelete&&row.IsDeleted&&!row.IsActive){result.Result=row;result.IsDeleted=true;return result;}
            if(softDelete){row.IsActive=false;if(!row.IsDeleted)row.DeletedDate=DateTime.UtcNow;}
            var sql=softDelete?"UPDATE OASIS_HOLONS SET IS_DELETED=1,MODIFIED_DATE=SYSTIMESTAMP,DATA_JSON=:Json WHERE ID=:Id":"DELETE FROM OASIS_HOLONS WHERE ID=:Id";
            await using var command=CreateCommand(sql,connection);command.Transaction=transaction;
            command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
            if(softDelete){
                var staged=await StageRecordHistoryAsync(row,original);
                command.Parameters.Add("Json",OracleDbType.Clob).Value=staged;row=Deserialize<Holon>(staged);
            }
            dispatched=true;if(await command.ExecuteNonQueryAsync()!=1)throw new InvalidDataException("Oracle locked holon changed unexpectedly.");
            await CommitLifecycleTransactionAsync(transaction);result.Result=row;result.IsDeleted=true;result.DeletedCount=1;
        }catch(Exception cause){
            result.Exception=dispatched?new OracleLifecycleReconciliationException(id,cause):cause;
            OASISErrorHandling.HandleError(ref result,"Oracle holon lifecycle operation failed; inspect the original exception.");
        }
        return result;
    }
}
