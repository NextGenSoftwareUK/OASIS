using System;
using System.IO;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    protected virtual Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        =>command.ExecuteNonQueryAsync();

    private async Task<T> WriteCanonicalRecordAsync<T>(T proposed,string table) where T:class,IHolonBase
    {
        var expected=table=="OASIS_AVATARS"?typeof(Avatar):table=="OASIS_AVATAR_DETAILS"?typeof(AvatarDetail):table=="OASIS_HOLONS"?typeof(Holon):null;
        if(expected!=typeof(T))throw new ArgumentException("Canonical Oracle record type must match its table.");
        var dispatched=false;var creating=false;
        try{
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync();
            string? previous=null;
            await using(var select=CreateCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=proposed.Id.ToByteArray();
                var value=await select.ExecuteScalarAsync();if(value!=null&&value!=DBNull.Value)previous=value.ToString();
            }
            creating=previous==null;var json=await StageRecordHistoryAsync(proposed,previous);var staged=Deserialize<T>(json);
            var identity=staged is IAvatar||staged is IAvatarDetail;
            var columns=identity?"ID,USERNAME,EMAIL,DATA_JSON":"ID,PARENT_HOLON_ID,HOLON_TYPE,DATA_JSON";
            var values=identity?":Id,:Username,:Email,:Json":":Id,:ParentId,:HolonType,:Json";
            if(table!="OASIS_AVATAR_DETAILS"){columns+=",IS_DELETED";values+=",:Deleted";}
            var updates=identity?"USERNAME=:Username,EMAIL=:Email,DATA_JSON=:Json":"PARENT_HOLON_ID=:ParentId,HOLON_TYPE=:HolonType,DATA_JSON=:Json";
            if(table!="OASIS_AVATAR_DETAILS")updates+=",IS_DELETED=:Deleted,MODIFIED_DATE=SYSTIMESTAMP";
            // Missing-key writes are INSERT-only. A concurrent creator must never be overwritten by MERGE.
            var sql=creating?$"INSERT INTO {table} ({columns}) VALUES ({values})":$"UPDATE {table} SET {updates} WHERE ID=:Id";
            await using var command=CreateCommand(sql,connection);command.Transaction=transaction;
            command.Parameters.Add("Id",OracleDbType.Raw).Value=staged.Id.ToByteArray();command.Parameters.Add("Json",OracleDbType.Clob).Value=json;
            if(identity){
                var username=staged is IAvatar a?a.Username:((IAvatarDetail)staged).Username;
                var email=staged is IAvatar av?av.Email:((IAvatarDetail)staged).Email;
                command.Parameters.Add("Username",OracleDbType.Varchar2).Value=username??"";command.Parameters.Add("Email",OracleDbType.Varchar2).Value=email??"";
            }else{
                var holon=(IHolon)staged;command.Parameters.Add("ParentId",OracleDbType.Raw).Value=holon.ParentHolonId==Guid.Empty?DBNull.Value:holon.ParentHolonId.ToByteArray();
                command.Parameters.Add("HolonType",OracleDbType.Int32).Value=(int)holon.HolonType;
            }
            if(table!="OASIS_AVATAR_DETAILS")command.Parameters.Add("Deleted",OracleDbType.Int32).Value=staged.IsDeleted?1:0;
            dispatched=true;if(await ExecuteCanonicalWriteAsync(command)!=1)throw new InvalidDataException("Oracle locked canonical writer did not affect exactly one record.");
            await CommitLifecycleTransactionAsync(transaction);return staged;
        }catch(OracleException cause)when(creating&&cause.Number==1){
            throw new InvalidDataException("Oracle create-only record conflicted with an existing native identity.",cause);
        }catch(Exception cause)when(dispatched){throw new OracleLifecycleReconciliationException(proposed.Id,cause);}
    }
}
