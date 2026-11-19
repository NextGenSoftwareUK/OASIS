using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS:IAvatarProfileStorageProvider
{
    private sealed class ProfileRefusal(string code,string message):InvalidOperationException(message)
    { public string Code { get; }=code; }

    private sealed class ProfileRow
    {
        public string Title,FirstName,LastName,Username,Email,Address,Portrait;
        public int Version;
        public Guid VersionId;
        public object NativeVersionId;
        public bool Active,Deleted;
    }

    private static OASISResult<AvatarProfileSnapshot> ProfileFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};

    private static async Task<ProfileRow> ReadProfileRowAsync(SqliteConnection connection,SqliteTransaction transaction,
        Guid id,bool detail,CancellationToken token,bool includePortrait=false)
    {
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText=detail
            ? "SELECT Username,Email,Version,VersionId,IsActive,DeletedDate,Address"+(includePortrait?",Portrait":"")+" FROM AvatarDetail WHERE Id=$id"
            : "SELECT Username,Email,Version,VersionId,IsActive,DeletedDate,Title,FirstName,LastName FROM Avatar WHERE Id=$id";
        command.Parameters.AddWithValue("$id",id.ToString());
        using var reader=await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if(!await reader.ReadAsync(token).ConfigureAwait(false))
            throw new ProfileRefusal(AvatarProfileErrors.NotFound,"Both existing profile records are required.");
        string Text(int ordinal)=>reader.IsDBNull(ordinal)?null:reader.GetString(ordinal);
        var row=new ProfileRow{Username=Text(0),Email=Text(1),Version=reader.GetInt32(2),VersionId=reader.GetGuid(3),
            NativeVersionId=reader.GetValue(3),Active=reader.GetBoolean(4),Deleted=reader.GetDateTime(5)!=DateTime.MinValue};
        if(detail){row.Address=Text(6);if(includePortrait)row.Portrait=Text(7);}
        else {row.Title=Text(6);row.FirstName=Text(7);row.LastName=Text(8);}
        if(await reader.ReadAsync(token).ConfigureAwait(false))throw new InvalidDataException("A profile id identifies multiple native records.");
        return row;
    }

    private static void RequireProfileRevision(ProfileRow row,AvatarProfileRevision expected)
    {
        if(row.Version!=expected.Version || row.VersionId!=expected.VersionId)
            throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The profile changed. Reload it before saving.");
    }

    private static async Task<bool> PatchProfileRowAsync(SqliteConnection connection,SqliteTransaction transaction,
        Guid id,ProfileRow observed,Dictionary<string,string> fields,bool detail,DateTime modified,Guid nextVersionId,CancellationToken token)
    {
        if(fields.Count==0)return false;
        using var command=connection.CreateCommand();command.Transaction=transaction;
        var assignments=new List<string>();
        foreach(var field in fields)
        {
            // Names come exclusively from the fixed profile/derived schema below, never from
            // user JSON, metadata or a caller-supplied SQL identifier.
            var parameter="$field"+assignments.Count;
            assignments.Add("\""+field.Key+"\"="+parameter);
            command.Parameters.AddWithValue(parameter,field.Value);
        }
        assignments.Add("ModifiedDate=$modified");assignments.Add("ModifiedDateKind=$utc");
        assignments.Add("ModifiedByAvatarId=$actor");assignments.Add("PreviousVersionId=$observedId");
        assignments.Add("Version=$nextVersion");assignments.Add("VersionId=$nextId");
        command.Parameters.AddWithValue("$modified",modified);
        command.Parameters.AddWithValue("$utc",(int)DateTimeKind.Utc);
        command.Parameters.AddWithValue("$actor",id.ToString("D"));
        command.Parameters.AddWithValue("$observedId",observed.NativeVersionId);
        command.Parameters.AddWithValue("$observedVersion",observed.Version);
        command.Parameters.AddWithValue("$nextVersion",checked(observed.Version+1));
        command.Parameters.AddWithValue("$nextId",nextVersionId);
        command.Parameters.AddWithValue("$id",id.ToString());
        command.CommandText="UPDATE "+(detail?"AvatarDetail":"Avatar")+" SET "+string.Join(",",assignments)
            +" WHERE Id=$id AND Version=$observedVersion AND VersionId=$observedId";
        if(await command.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1)
            throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The profile revision changed before persistence.");
        return true;
    }

    public async Task<OASISResult<AvatarProfileSnapshot>> ApplyAvatarProfileAsync(Guid authenticatedAvatarId,
        AvatarProfileMutation mutation,CancellationToken cancellationToken=default)
    {
        if(mutation==null)return ProfileFailure(AvatarProfileErrors.Invalid,"A profile edit is required.");
        if(authenticatedAvatarId==Guid.Empty || authenticatedAvatarId!=mutation.AvatarId)
            return ProfileFailure(AvatarProfileErrors.Forbidden,"An avatar may edit only its own profile.");
        if(!IsProviderActivated)return ProfileFailure(AvatarProfileErrors.StorageUnavailable,"The profile storage provider is not active.");
        var result=new OASISResult<AvatarProfileSnapshot>();
        try
        {
            var settings=new SqliteConnectionStringBuilder(_ownConnectionString){Pooling=false};
            // Activation owns schema creation. A vanished file must not silently
            // create a different empty authority; preserve explicit read-only mode.
            if(settings.Mode==SqliteOpenMode.ReadWriteCreate)settings.Mode=SqliteOpenMode.ReadWrite;
            using var connection=new SqliteConnection(settings.ToString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            using var transaction=connection.BeginTransaction(deferred:false);
            var commitEntered=false;
            try
            {
                var account=await ReadProfileRowAsync(connection,transaction,mutation.AvatarId,false,cancellationToken).ConfigureAwait(false);
                var detail=await ReadProfileRowAsync(connection,transaction,mutation.AvatarId,true,cancellationToken,mutation.Fields.ContainsKey(AvatarProfileProperty.Portrait)).ConfigureAwait(false);
                if(!account.Active || account.Deleted || detail.Deleted)
                    throw new ProfileRefusal(AvatarProfileErrors.Forbidden,"The account is not active.");
                RequireProfileRevision(account,mutation.AvatarRevision);RequireProfileRevision(detail,mutation.DetailRevision);
                var accountFields=new Dictionary<string,string>();var detailFields=new Dictionary<string,string>();
                void Set(Dictionary<string,string> fields,string name,string previous,string value)
                {if(previous!=value)fields.Add(name,value);}
                foreach(var field in mutation.Fields)
                {
                    switch(field.Key)
                    {
                        case AvatarProfileProperty.Title:Set(accountFields,"Title",account.Title,field.Value);break;
                        case AvatarProfileProperty.FirstName:Set(accountFields,"FirstName",account.FirstName,field.Value);break;
                        case AvatarProfileProperty.LastName:Set(accountFields,"LastName",account.LastName,field.Value);break;
                        case AvatarProfileProperty.Username:
                            Set(accountFields,"Username",account.Username,field.Value);Set(detailFields,"Username",detail.Username,field.Value);break;
                        case AvatarProfileProperty.Email:
                            Set(accountFields,"Email",account.Email,field.Value);Set(detailFields,"Email",detail.Email,field.Value);break;
                        case AvatarProfileProperty.Address:Set(detailFields,"Address",detail.Address,field.Value);break;
                        case AvatarProfileProperty.Portrait:Set(detailFields,"Portrait",detail.Portrait,field.Value);break;
                        default:throw new ProfileRefusal(AvatarProfileErrors.Invalid,"Only editable profile fields are accepted.");
                    }
                }
                if(accountFields.ContainsKey("FirstName") || accountFields.ContainsKey("LastName"))
                    accountFields.Add("Name",new Core.Holons.Avatar
                    {
                        FirstName=mutation.Fields.TryGetValue(AvatarProfileProperty.FirstName,out var first)?first:account.FirstName,
                        LastName=mutation.Fields.TryGetValue(AvatarProfileProperty.LastName,out var last)?last:account.LastName
                    }.FullName);
                var modified=mutation.ModifiedAtUtc;
                var accountChanged=await PatchProfileRowAsync(connection,transaction,mutation.AvatarId,account,accountFields,false,modified,mutation.AvatarNextVersionId,cancellationToken).ConfigureAwait(false);
                var detailChanged=await PatchProfileRowAsync(connection,transaction,mutation.AvatarId,detail,detailFields,true,modified,mutation.DetailNextVersionId,cancellationToken).ConfigureAwait(false);
                account=await ReadProfileRowAsync(connection,transaction,mutation.AvatarId,false,cancellationToken).ConfigureAwait(false);
                detail=await ReadProfileRowAsync(connection,transaction,mutation.AvatarId,true,cancellationToken,mutation.Fields.ContainsKey(AvatarProfileProperty.Portrait)).ConfigureAwait(false);
                if(account.Username!=detail.Username || account.Email!=detail.Email)
                    throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The account profile identities are inconsistent.");
                var snapshot=new AvatarProfileSnapshot(mutation.AvatarId,account.Title,account.FirstName,account.LastName,
                    account.Username,account.Email,detail.Address,new(account.Version,account.VersionId),new(detail.Version,detail.VersionId));
                commitEntered=accountChanged || detailChanged;
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                result.Result=snapshot;result.IsSaved=accountChanged || detailChanged;
                result.SavedCount=(accountChanged?1:0)+(detailChanged?1:0);
            }
            catch(Exception cause)
            {
                var code=AvatarProfileErrors.StorageUnavailable;
                Exception evidence=cause;
                if(commitEntered){code=AvatarProfileErrors.CommitUnknown;evidence=new SqliteMutationReconciliationException(mutation.AvatarId,cause);}
                else if(cause is ProfileRefusal refusal)code=refusal.Code;
                else if(cause is SqliteException native && native.SqliteExtendedErrorCode is 2067 or 1555)code=AvatarProfileErrors.IdentityConflict;
                else if(cause is SqliteException busy && busy.SqliteErrorCode is 5 or 6)code=AvatarProfileErrors.Conflict;
                try{await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);}
                catch(Exception cleanup)
                {
                    evidence=new SqliteMutationReconciliationException(mutation.AvatarId,new AggregateException(evidence,cleanup));
                    code=AvatarProfileErrors.CommitUnknown;
                }
                result=ProfileFailure(code,"The profile edit was not acknowledged.",evidence);
            }
        }
        catch(Exception cause)
        {
            var evidence=result.Exception==null?cause:new AggregateException(result.Exception,cause);
            var code=result.ErrorCode??(cause is SqliteException busy && busy.SqliteErrorCode is 5 or 6
                ?AvatarProfileErrors.Conflict:AvatarProfileErrors.StorageUnavailable);
            result=ProfileFailure(code,"The profile storage session failed.",evidence);
        }
        return result;
    }
}
