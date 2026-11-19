using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS:IAvatarProfileStorageProvider
{
    private sealed class ProfileRefusal(string code,string message):InvalidOperationException(message)
    {public string Code {get;}=code;}

    private static OASISResult<AvatarProfileSnapshot> ProfileFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};

    private static void RequireProfileRevision(IHolonBase observed,AvatarProfileRevision expected)
    {
        if(observed.Version!=expected.Version || observed.VersionId!=expected.VersionId)
            throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The profile changed. Reload it before saving.");
    }

    private static string ProfileFailureCode(Exception cause)
    {
        if(cause==null)return null;
        if(cause is ProfileRefusal refusal)return refusal.Code;
        if(cause is StorageIdentityAmbiguousException)return OASISResultErrorCodes.AmbiguousIdentity;
        if(cause is ClientException constraint && constraint.Code=="Neo.ClientError.Schema.ConstraintValidationFailed")
            return AvatarProfileErrors.IdentityConflict;
        if(cause is StorageLifecycleConflictException)return AvatarProfileErrors.Conflict;
        if(cause is AggregateException group)
            foreach(var inner in group.InnerExceptions)
            {var code=ProfileFailureCode(inner);if(code!=null)return code;}
        return ProfileFailureCode(cause.InnerException);
    }

    private static async Task<T> WriteProfileSnapshotAsync<T>(IAsyncTransaction transaction,string kind,T record,
        string expected,IReadOnlyCollection<string> changed,Func<INode,T> read,bool retainHistory,DateTime modified,Guid nextVersionId,
        CancellationToken cancellation) where T:class,IHolonBase
    {
        if(changed.Count==0)return record;
        cancellation.ThrowIfCancellationRequested();
        // The locked original, including unknown native JSON fields, is the
        // historical snapshot. Never archive the already edited runtime object.
        if(retainHistory)await ArchiveRecordVersionAsync(transaction,kind,record,expected).ConfigureAwait(false);
        record.PreviousVersionId=record.VersionId;record.Version=checked(record.Version+1);record.VersionId=nextVersionId;
        record.ModifiedDate=modified;record.ModifiedByAvatarId=record.Id;
        var fields=new HashSet<string>(changed,StringComparer.Ordinal)
        {nameof(IHolonBase.Version),nameof(IHolonBase.VersionId),nameof(IHolonBase.PreviousVersionId),
            nameof(IHolonBase.ModifiedDate),nameof(IHolonBase.ModifiedByAvatarId)};
        if(kind=="Avatar" && (fields.Contains(nameof(IAvatar.Title)) || fields.Contains(nameof(IAvatar.FirstName)) || fields.Contains(nameof(IAvatar.LastName))))
        {
            fields.Add(nameof(IAvatar.Name));fields.Add(nameof(IAvatar.FullName));fields.Add(nameof(IAvatar.FullNameWithTitle));
        }
        var original=OasisJson.ParseStoredObject(expected);
        var proposal=OasisJson.ParseStoredObject(OasisJson.Serialize(record));
        foreach(var name in fields)
        {
            if(!proposal.TryGetValue(name,StringComparison.Ordinal,out var value))
                throw new InvalidDataException("The canonical profile field is absent from its stored projection.");
            original[name]=value.DeepClone();
        }
        var username=kind=="Avatar"?((IAvatar)record).Username:((IAvatarDetail)record).Username;
        var email=kind=="Avatar"?((IAvatar)record).Email:((IAvatarDetail)record).Email;
        // Only two fixed record kinds are admitted. All user values are parameters.
        HistoryRecordKind(kind);
        if(kind!="Avatar" && kind!="AvatarDetail")throw new ArgumentException("A profile account record kind is required.");
        cancellation.ThrowIfCancellationRequested();
        var rows=await(await transaction.RunAsync("MATCH (a:"+kind+" {Id:$id}) WHERE a.SnapshotJson=$expected "
            +"SET a.SnapshotJson=$snapshot,a.Username=$username,a.Email=$email,a.ModifiedDate=$modified RETURN a",
            new{id=record.Id.ToString(),expected,snapshot=original.ToString(Formatting.None),username,email,modified}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if(rows.Count!=1)throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The native profile identity changed before persistence.");
        var acknowledged=read(rows[0]["a"].As<INode>());
        RequireAcknowledgedRecord(record,acknowledged);
        return acknowledged;
    }

    public async Task<OASISResult<AvatarProfileSnapshot>> ApplyAvatarProfileAsync(Guid authenticatedAvatarId,
        AvatarProfileMutation mutation,CancellationToken cancellationToken=default)
    {
        if(mutation==null)return ProfileFailure(AvatarProfileErrors.Invalid,"A profile edit is required.");
        if(authenticatedAvatarId==Guid.Empty || authenticatedAvatarId!=mutation.AvatarId)
            return ProfileFailure(AvatarProfileErrors.Forbidden,"An avatar may edit only its own profile.");
        if(cancellationToken.IsCancellationRequested)
            return ProfileFailure("AVATAR_PROFILE_CANCELLED","The profile edit was cancelled.",new OperationCanceledException(cancellationToken));
        var retainHistory=IsVersionControlEnabled;
        var result=await ExecuteOwnedNeoMutationAsync<AvatarProfileSnapshot>(async transaction=>
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Reuse the identity writer's lock order and transaction owner. A
            // concurrent ordinary account save cannot slip between observation
            // and the exact-field snapshot replacement.
            var accountRead=await ReadLockedIdentityAsync(transaction,"Avatar",mutation.AvatarId,ReadAvatarNode).ConfigureAwait(false);
            var detailRead=await ReadLockedIdentityAsync(transaction,"AvatarDetail",mutation.AvatarId,ReadDetailNode).ConfigureAwait(false);
            var account=accountRead.Previous;var detail=detailRead.Previous;
            if(account==null || detail==null)throw new ProfileRefusal(AvatarProfileErrors.NotFound,"Both existing profile records are required.");
            if(!account.IsActive || account.IsDeleted || detail.IsDeleted)
                throw new ProfileRefusal(AvatarProfileErrors.Forbidden,"The account is not active.");
            RequireProfileRevision(account,mutation.AvatarRevision);RequireProfileRevision(detail,mutation.DetailRevision);
            var accountFields=new List<string>();var detailFields=new List<string>();
            foreach(var field in mutation.Fields)
            {
                switch(field.Key)
                {
                    case AvatarProfileProperty.Title:
                        if(account.Title!=field.Value){account.Title=field.Value;accountFields.Add(nameof(IAvatar.Title));}break;
                    case AvatarProfileProperty.FirstName:
                        if(account.FirstName!=field.Value){account.FirstName=field.Value;accountFields.Add(nameof(IAvatar.FirstName));}break;
                    case AvatarProfileProperty.LastName:
                        if(account.LastName!=field.Value){account.LastName=field.Value;accountFields.Add(nameof(IAvatar.LastName));}break;
                    case AvatarProfileProperty.Username:
                        if(account.Username!=field.Value){account.Username=field.Value;accountFields.Add(nameof(IAvatar.Username));}
                        if(detail.Username!=field.Value){detail.Username=field.Value;detailFields.Add(nameof(IAvatarDetail.Username));}break;
                    case AvatarProfileProperty.Email:
                        if(account.Email!=field.Value){account.Email=field.Value;accountFields.Add(nameof(IAvatar.Email));}
                        if(detail.Email!=field.Value){detail.Email=field.Value;detailFields.Add(nameof(IAvatarDetail.Email));}break;
                    case AvatarProfileProperty.Address:
                        if(detail.Address!=field.Value){detail.Address=field.Value;detailFields.Add(nameof(IAvatarDetail.Address));}break;
                    case AvatarProfileProperty.Portrait:
                        if(detail.Portrait!=field.Value){detail.Portrait=field.Value;detailFields.Add(nameof(IAvatarDetail.Portrait));}break;
                    default:throw new ProfileRefusal(AvatarProfileErrors.Invalid,"Only editable profile fields are accepted.");
                }
            }
            var modified=mutation.ModifiedAtUtc;
            account=await WriteProfileSnapshotAsync(transaction,"Avatar",account,accountRead.Expected,accountFields,ReadAvatarNode,retainHistory,modified,mutation.AvatarNextVersionId,cancellationToken).ConfigureAwait(false);
            detail=await WriteProfileSnapshotAsync(transaction,"AvatarDetail",detail,detailRead.Expected,detailFields,ReadDetailNode,retainHistory,modified,mutation.DetailNextVersionId,cancellationToken).ConfigureAwait(false);
            if(account.Username!=detail.Username || account.Email!=detail.Email)
                throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The account profile identities are inconsistent.");
            cancellationToken.ThrowIfCancellationRequested();
            var changed=accountFields.Count>0 || detailFields.Count>0;
            return(new OASISResult<AvatarProfileSnapshot>
            {
                Result=new(mutation.AvatarId,account.Title,account.FirstName,account.LastName,account.Username,account.Email,detail.Address,
                    new(account.Version,account.VersionId),new(detail.Version,detail.VersionId)),
                IsSaved=changed,SavedCount=(accountFields.Count>0?1:0)+(detailFields.Count>0?1:0)
            },changed);
        }).ConfigureAwait(false);
        if(result.IsError)
        {
            result.ErrorCode=NativeMutationReconciliation.IsRequired(result.Exception)?AvatarProfileErrors.CommitUnknown
                :ProfileFailureCode(result.Exception)??AvatarProfileErrors.StorageUnavailable;
            result.Message="The profile edit was not acknowledged.";
        }
        return result;
    }
}
