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
