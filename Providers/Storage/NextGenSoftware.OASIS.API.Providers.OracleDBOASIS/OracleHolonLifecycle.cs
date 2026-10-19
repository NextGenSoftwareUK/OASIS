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
            Holon row;
            await using(var select=CreateCommand("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                var json=await select.ExecuteScalarAsync();
                if(json==null||json==DBNull.Value)throw new InvalidOperationException("Oracle holon to delete was not found.");
                row=Deserialize<Holon>(json.ToString()!);
                if(row.Id!=id)throw new InvalidDataException("Oracle holon identity projection does not match its canonical record.");
            }
            if(softDelete){row.IsActive=false;if(!row.IsDeleted)row.DeletedDate=DateTime.UtcNow;}
            var sql=softDelete?"UPDATE OASIS_HOLONS SET IS_DELETED=1,MODIFIED_DATE=SYSTIMESTAMP,DATA_JSON=:Json WHERE ID=:Id":"DELETE FROM OASIS_HOLONS WHERE ID=:Id";
            await using var command=CreateCommand(sql,connection);command.Transaction=transaction;
            command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
            if(softDelete)command.Parameters.Add("Json",OracleDbType.Clob).Value=Serialize(row);
            dispatched=true;if(await command.ExecuteNonQueryAsync()!=1)throw new InvalidDataException("Oracle locked holon changed unexpectedly.");
            await transaction.CommitAsync();result.Result=row;
        }catch(Exception cause){
            result.Exception=dispatched?new OracleLifecycleReconciliationException(id,cause):cause;
            OASISErrorHandling.HandleError(ref result,"Oracle holon lifecycle operation failed; inspect the original exception.");
        }
        return result;
    }
}
