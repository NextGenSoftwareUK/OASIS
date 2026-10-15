using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
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

    private async Task<OASISResult<bool>> DeleteCanonicalAvatarAsync(Guid id,bool softDelete)
    {
        var result=new OASISResult<bool>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var ready=await EnsureDetailStoreAsync(0);
            if(ready.IsError) return ready;
            var reference=_firestoreDb.Collection("avatars").Document(id.ToString());
            var snapshot=await reference.GetSnapshotAsync();
            if(!snapshot.Exists) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore avatar not found.";return result;}
            var avatar=DecodeAvatar(snapshot.ConvertTo<Dictionary<string,object>>(),snapshot.Id);
            var condition=Google.Cloud.Firestore.Precondition.LastUpdated(snapshot.UpdateTime.Value);
            if(softDelete)
            {
                if(!avatar.IsActive) {result.Result=true;return result;}
                avatar.IsActive=false;
                avatar.DeletedDate=DateTime.UtcNow;
                var update=new Dictionary<string,object> { ["isActive"]=false,["data_json"]=OasisJson.Serialize(avatar) };
                await ObserveFirestoreMutationAsync(()=>reference.UpdateAsync(update,condition),reference.Path);
            }
            else await ObserveFirestoreMutationAsync(()=>reference.DeleteAsync(condition),reference.Path);
            result.Result=true;result.IsSaved=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private static Avatar DecodeAvatar(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore avatar lacks canonical data_json.");
        var avatar=OasisJson.Deserialize<Avatar>(json) ?? throw new InvalidDataException("Firestore avatar payload is null.");
        if(avatar.Id==Guid.Empty || avatar.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore avatar identity does not match document key.");
        return avatar;
    }

    private async Task<OASISResult<IEnumerable<IAvatar>>> ReadCanonicalAvatarsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IAvatar>>();
        try {
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IAvatar>>(ready);
            var snapshot=await _firestoreDb.Collection("avatars").GetSnapshotAsync();
            result.Result=snapshot.Documents.Select(doc=>DecodeAvatar(doc.ConvertTo<Dictionary<string,object>>(),doc.Id)).Where(row=>row.IsActive).Cast<IAvatar>().ToList();
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
            var avatar=document.Exists ? DecodeAvatar(document.ConvertTo<Dictionary<string,object>>(),document.Id):null;
            if(avatar==null || !avatar.IsActive) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore avatar not found.";}
            else {result.Result=avatar;result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IAvatar>> ReadCanonicalAvatarSelectorAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IAvatar>();
        try {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
            var loaded=await ReadCanonicalAvatarsAsync(version);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,IAvatar>(loaded);
            var matches=loaded.Result.Where(row=>string.Equals(email ? row.Email:row.Username,selector,StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No Firestore avatar matches selector.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }
}
