using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private static async Task<T> ObserveFirestoreMutationAsync<T>(Func<Task<T>> operation,string documentPath)
    {
        try {return await operation();}
        catch(Exception cause) {throw new GoogleFirestoreMutationReconciliationException(documentPath,cause);}
    }

    private async Task<OASISResult<bool>> EnsureDetailStoreAsync(int version)
    {
        if(version!=0) throw new NotSupportedException("Firestore detail reads currently support latest version (0) only.");
        if(!IsProviderActivated) {
            var active=await ActivateProviderAsync();
            if(active.IsError) return active;
        }
        if(!_enableFirestore) throw new InvalidOperationException("Firestore is not enabled.");
        return new OASISResult<bool>(true);
    }

    private async Task<OASISResult<IAvatarDetail>> ReadCanonicalDetailByIdAsync(Guid id,int version)
    {
        var result=new OASISResult<IAvatarDetail>();
        try {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatarDetail>(ready);
            var document=await _firestoreDb.Collection("avatarDetails").Document(id.ToString()).GetSnapshotAsync();
            var detail=document.Exists ? DecodeAvatarDetail(document.ConvertTo<Dictionary<string,object>>(),document.Id):null;
            if(detail==null || !detail.IsActive) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore detail not found.";}
            else {result.Result=detail;result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IAvatarDetail>>> ReadCanonicalDetailsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IAvatarDetail>>();
        try {
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IAvatarDetail>>(ready);
            var snapshot=await _firestoreDb.Collection("avatarDetails").GetSnapshotAsync();
            result.Result=snapshot.Documents.Select(doc=>DecodeAvatarDetail(doc.ConvertTo<Dictionary<string,object>>(),doc.Id)).Where(row=>row.IsActive).Cast<IAvatarDetail>().ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> ReadCanonicalDetailSelectorAsync(string field,string selector,int version)
    {
        var result=new OASISResult<IAvatarDetail>();
        try {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
            if(version!=0) throw new NotSupportedException("Firestore detail selectors currently support latest version (0) only.");
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatarDetail>(ready);
            var snapshot=await _firestoreDb.Collection("avatarDetails").WhereEqualTo(field,selector).WhereEqualTo("isActive",true).Limit(2).GetSnapshotAsync();
            var matches=snapshot.Documents.Select(doc=>DecodeAvatarDetail(doc.ConvertTo<Dictionary<string,object>>(),doc.Id)).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No Firestore detail matches selector.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }

    private static Dictionary<string,object> EncodeAvatarDetail(IAvatarDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        if(detail.Id==Guid.Empty) throw new ArgumentException("A nonempty avatar detail identity is required.");
        return new Dictionary<string,object> {
            ["id"]=detail.Id.ToString(),["username"]=detail.Username,["email"]=detail.Email,
            ["isActive"]=detail.IsActive,["version"]=detail.Version,
            ["createdDate"]=ToFirestoreTimestamp(detail.CreatedDate),["modifiedDate"]=ToFirestoreTimestamp(detail.ModifiedDate),
            ["savedAt"]=ToFirestoreTimestamp(DateTime.UtcNow),["data_json"]=OasisJson.Serialize(detail)
        };
    }

    private static AvatarDetail DecodeAvatarDetail(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore detail lacks canonical data_json; legacy manual mappings require explicit review.");
        var detail=OasisJson.Deserialize<AvatarDetail>(json) ?? throw new InvalidDataException("Firestore detail payload is null.");
        if(detail.Id==Guid.Empty || detail.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore detail identity does not match document key.");
        return detail;
    }
}
