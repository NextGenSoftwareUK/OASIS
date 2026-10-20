using System;
using System.IO;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    protected virtual Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        =>command.ExecuteNonQueryAsync();

    private async Task<T> WriteCanonicalRecordAsync<T>(T proposed,string table,PlannedHolonWrite? plannedHolon=null) where T:class,IHolonBase
    {
        var expected=table=="OASIS_AVATARS"?typeof(Avatar):table=="OASIS_AVATAR_DETAILS"?typeof(AvatarDetail):table=="OASIS_HOLONS"?typeof(Holon):null;
        if(expected!=typeof(T))throw new ArgumentException("Canonical Oracle record type must match its table.");
        var dispatched=false;var creating=false;
        try{
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync();
            // Match deletion's profile-then-detail lock order. The related tombstone
            // check and the proposed record write belong to this SAME transaction.
            if(!proposed.IsDeleted&&typeof(T)==typeof(AvatarDetail))
                await RejectRelatedTombstoneAsync<Avatar>(connection,transaction,"OASIS_AVATARS",proposed.Id);
            string? previous=null;Holon? observedHolon=null;int? nativeAvatarDeleted=null;
            var validateHolon=typeof(T)==typeof(Holon);
            var validateAvatar=typeof(T)==typeof(Avatar);
            await using(var select=CreateCommand($"SELECT DATA_JSON{(validateHolon?",HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID":validateAvatar?",IS_DELETED":"")} FROM {table} WHERE ID=:Id FOR UPDATE",connection)){
                select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=proposed.Id.ToByteArray();
                await using var reader=await select.ExecuteReaderAsync();
                if(await reader.ReadAsync()){
                    previous=reader.GetString(0);
                    if(validateAvatar)nativeAvatarDeleted=reader.GetInt32(1);
                    if(validateHolon){
                        var stored=await ReadRecordVersionAsync<Holon>(proposed.Id,previous,0)??throw new InvalidDataException("Oracle locked holon is missing its current version.");
                        observedHolon=stored;
                        ValidateNativeHolonProjection(stored,reader.GetInt32(1),reader.GetInt32(2),reader.IsDBNull(3)?Guid.Empty:new Guid((byte[])reader.GetValue(3)));
                    }
                }
            }
            if(validateHolon&&proposed is Holon holon){
                var write=plannedHolon??new PlannedHolonWrite(holon,holon);
                holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(write,HolonChildRoleWriteKind.SourceSave,previous!=null,observedHolon?.ChildRoleManifestJson);
            }
            creating=previous==null;var json=await StageRecordHistoryAsync(proposed,previous,nativeAvatarDeleted);var staged=Deserialize<T>(json);
            if(!proposed.IsDeleted&&typeof(T)==typeof(Avatar))
                await RejectRelatedTombstoneAsync<AvatarDetail>(connection,transaction,"OASIS_AVATAR_DETAILS",proposed.Id);
            await using var command=CreateCanonicalWriteCommand(staged,json,table,creating,connection,transaction);
            dispatched=true;if(await ExecuteCanonicalWriteAsync(command)!=1)throw new InvalidDataException("Oracle locked canonical writer did not affect exactly one record.");
            await CommitLifecycleTransactionAsync(transaction);return staged;
        }catch(OracleException cause)when(creating&&cause.Number==1){
            throw new InvalidDataException("Oracle create-only record conflicted with an existing native identity.",cause);
        }catch(Exception cause)when(dispatched){throw new OracleLifecycleReconciliationException(proposed.Id,cause);}
    }

    private async Task RejectRelatedTombstoneAsync<T>(OracleConnection connection,OracleTransaction transaction,string table,Guid id) where T:class,IHolonBase
    {
        if(!(typeof(T)==typeof(Avatar)&&table=="OASIS_AVATARS"||typeof(T)==typeof(AvatarDetail)&&table=="OASIS_AVATAR_DETAILS"))
            throw new ArgumentException("Related Oracle identity type must match its fixed table.");
        var validateAvatar=typeof(T)==typeof(Avatar);
        await using var select=CreateCommand($"SELECT DATA_JSON{(validateAvatar?",IS_DELETED":"")} FROM {table} WHERE ID=:Id FOR UPDATE",connection);
        select.Transaction=transaction;select.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
        await using var reader=await select.ExecuteReaderAsync();
        if(await reader.ReadAsync()){
            var related=await ReadRecordVersionAsync<T>(id,reader.GetString(0),0,validateAvatar?reader.GetInt32(1):null)??throw new InvalidDataException("Oracle related identity is missing its current canonical record.");
            if(related.IsDeleted)throw new Core.Helpers.StorageLifecycleConflictException("Active save conflicts with a committed related Oracle identity tombstone.");
        }
    }

    // SQL/projection binding has one owner; callers own connection, transaction and acknowledgement.
    private static OracleCommand CreateCanonicalWriteCommand(IHolonBase staged,string json,string table,bool creating,OracleConnection connection,OracleTransaction transaction)
    {
            var expected=staged is Avatar?"OASIS_AVATARS":staged is AvatarDetail?"OASIS_AVATAR_DETAILS":staged is Holon?"OASIS_HOLONS":null;
            if(expected==null||table!=expected)throw new ArgumentException("Canonical Oracle record type must match its table.");
            var identity=staged is IAvatar||staged is IAvatarDetail;
            var columns=identity?"ID,USERNAME,EMAIL,DATA_JSON":"ID,PARENT_HOLON_ID,HOLON_TYPE,DATA_JSON";
            var values=identity?":Id,:Username,:Email,:Json":":Id,:ParentId,:HolonType,:Json";
            if(table!="OASIS_AVATAR_DETAILS"){columns+=",IS_DELETED";values+=",:Deleted";}
            var updates=identity?"USERNAME=:Username,EMAIL=:Email,DATA_JSON=:Json":"PARENT_HOLON_ID=:ParentId,HOLON_TYPE=:HolonType,DATA_JSON=:Json";
            if(table!="OASIS_AVATAR_DETAILS")updates+=",IS_DELETED=:Deleted,MODIFIED_DATE=SYSTIMESTAMP";
            // Missing-key writes are INSERT-only. A concurrent creator must never be overwritten by MERGE.
            var sql=creating?$"INSERT INTO {table} ({columns}) VALUES ({values})":$"UPDATE {table} SET {updates} WHERE ID=:Id";
            var command=CreateCommand(sql,connection);
            try{
            command.Transaction=transaction;
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
            return command;
            }catch{command.Dispose();throw;}
    }
}
