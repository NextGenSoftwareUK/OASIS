using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : IAvatarTombstoneStorageProvider
{
    public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
        => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

    public async Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
    {
        if(tombstone!=null) return await DeleteCanonicalAvatarAsync(tombstone.AvatarId,true,tombstone);
        var result=new OASISResult<bool>();
        var cause=new ArgumentNullException(nameof(tombstone));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);
        return result;
    }

    private static bool MatchesTombstone(IHolonBase record,AvatarTombstone tombstone)
        => !record.IsActive && record.DeletedDate==tombstone.DeletedAtUtc && record.DeletedByAvatarId==tombstone.DeletedByAvatarId;

    private Task<OASISResult<bool>> DeleteCanonicalAvatarKeyAsync(string key,bool softDelete)
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return DeleteCanonicalAvatarAsync(id,softDelete);
        var result=new OASISResult<bool>();
        var cause=new ArgumentException("A nonempty GUID Firestore avatar storage key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);
        return Task.FromResult(result);
    }

    private async Task<OASISResult<bool>> DeleteCanonicalAvatarSelectorAsync(string selector,bool email,bool softDelete)
    {
        var loaded=await ReadCanonicalAvatarSelectorAsync(selector,email,0);
        if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(loaded);
        return await DeleteCanonicalAvatarAsync(loaded.Result.Id,softDelete);
    }

    private async Task<OASISResult<bool>> DeleteCanonicalAvatarAsync(Guid id,bool softDelete,AvatarTombstone supplied=null)
    {
        var result=new OASISResult<bool>();
        bool mutationPrepared=false;
        string paths=string.Empty;
        AvatarTombstone committed=null;
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var createHistory=IsVersionControlEnabled;
            var ready=await EnsureDetailStoreAsync(0);
            if(ready.IsError) return ready;
            RequireSingleAttemptFirestoreCommit(_firestoreDb);
            var reference=_firestoreDb.Collection("avatars").Document(id.ToString());
            var detailReference=_firestoreDb.Collection("avatarDetails").Document(id.ToString());
            paths=reference.Path+";"+detailReference.Path;
            var exists=await _firestoreDb.RunTransactionAsync(async transaction=>
            {
                var snapshot=await transaction.GetSnapshotAsync(reference);
                var detailSnapshot=await transaction.GetSnapshotAsync(detailReference);
                if(!snapshot.Exists) return false;
                var avatar=DecodeAvatar(snapshot.ConvertTo<Dictionary<string,object>>(),snapshot.Id);
                var detail=detailSnapshot.Exists ? DecodeAvatarDetail(detailSnapshot.ConvertTo<Dictionary<string,object>>(),detailSnapshot.Id):null;
                if(softDelete)
                {
                    var patch=supplied ?? (avatar.IsDeleted
                        ? new AvatarTombstone(id,avatar.DeletedDate,avatar.DeletedByAvatarId)
                        : new AvatarTombstone(id,DateTime.UtcNow,avatar.DeletedByAvatarId));
                    if(avatar.IsDeleted && !MatchesTombstone(avatar,patch) || detail!=null && detail.IsDeleted && !MatchesTombstone(detail,patch))
                        throw new StorageLifecycleConflictException("Firestore stored tombstone differs from requested committed lifecycle state.");
                    committed=patch;
                    if(MatchesTombstone(avatar,patch) && (detail==null || MatchesTombstone(detail,patch))) return true;
                    avatar.IsActive=false;avatar.DeletedDate=patch.DeletedAtUtc;avatar.DeletedByAvatarId=patch.DeletedByAvatarId;
                    var avatarUpdate=await StageFirestoreLifecycleUpdateAsync(avatar,snapshot.ToDictionary(),snapshot.Id,createHistory);
                    Dictionary<string,object> detailUpdate=null;
                    if(detail!=null)
                    {
                        detail.IsActive=false;detail.DeletedDate=patch.DeletedAtUtc;detail.DeletedByAvatarId=patch.DeletedByAvatarId;
                        detailUpdate=await StageFirestoreLifecycleUpdateAsync(detail,detailSnapshot.ToDictionary(),detailSnapshot.Id,createHistory);
                    }
                    transaction.Update(reference,avatarUpdate);
                    if(detailUpdate!=null) transaction.Update(detailReference,detailUpdate);
                }
                else
                {
                    transaction.Delete(reference);
                    if(detailSnapshot.Exists) transaction.Delete(detailReference);
                }
                mutationPrepared=true;
                return true;
            },Google.Cloud.Firestore.TransactionOptions.ForMaxAttempts(1));
            if(!exists) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore avatar not found.";return result;}
            result.Result=true;result.IsSaved=mutationPrepared;
            result.IsDeleted=true;result.DeletedCount=1;
            if(softDelete) AvatarTombstoneEvidence.Attach(result,committed);
        }
        catch(Grpc.Core.RpcException cause) when(mutationPrepared && (cause.StatusCode==Grpc.Core.StatusCode.FailedPrecondition || cause.StatusCode==Grpc.Core.StatusCode.Aborted || cause.StatusCode==Grpc.Core.StatusCode.NotFound))
        {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        catch(Exception cause)
        {
            var error=mutationPrepared ? new GoogleFirestoreMutationReconciliationException(paths,cause):cause;
            OASISErrorHandling.HandleError(ref result,error.Message,error);
            if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private static Avatar DecodeAvatar(Dictionary<string,object> fields,string documentId)
        => ReadFirestoreHistoryAsync<Avatar>(fields,documentId,0).GetAwaiter().GetResult();

    private async Task<OASISResult<IEnumerable<IAvatar>>> ReadCanonicalAvatarsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IAvatar>>();
        try {
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IAvatar>>(ready);
            var snapshot=await _firestoreDb.Collection("avatars").GetSnapshotAsync();
            result.Result=snapshot.Documents.Select(doc=>ReadFirestoreHistoryAsync<Avatar>(doc.ToDictionary(),doc.Id,version).GetAwaiter().GetResult()).Where(row=>row!=null&&row.DeletedDate==DateTime.MinValue).Cast<IAvatar>().ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IAvatar>> ReadCanonicalAvatarAsync(Guid id,int version)
    {
        var result=new OASISResult<IAvatar>();
        try {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatar>(ready);
            var document=await _firestoreDb.Collection("avatars").Document(id.ToString()).GetSnapshotAsync();
            var avatar=document.Exists ? await ReadFirestoreHistoryAsync<Avatar>(document.ToDictionary(),document.Id,version):null;
            if(avatar==null || avatar.DeletedDate!=DateTime.MinValue) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore avatar not found.";}
            else {result.Result=avatar;result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IAvatar>> ReadCanonicalAvatarSelectorAsync(string selector,bool email,int version)
        => await ReadCanonicalAvatarSelectorAsync(selector,row=>string.Equals(email ? row.Email:row.Username,selector,StringComparison.OrdinalIgnoreCase),version);

    private async Task<OASISResult<IAvatar>> ReadCanonicalAvatarSelectorAsync(string selector,Func<IAvatar,bool> predicate,int version,bool credentialsLatestOnly=false)
    {
        var result=new OASISResult<IAvatar>();
        try {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            if(credentialsLatestOnly && version!=0)throw new NotSupportedException("Historical credentials must not authenticate; only current version zero is permitted.");
            ValidateFirestoreVersion(version);
            var loaded=await ReadCanonicalAvatarsAsync(version);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,IAvatar>(loaded);
            if(loaded.Result==null) throw new InvalidDataException("Firestore avatar selector returned no collection.");
            var matches=loaded.Result.Where(predicate).Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No Firestore avatar matches selector.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            if(cause is ArgumentException || cause is NotSupportedException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
