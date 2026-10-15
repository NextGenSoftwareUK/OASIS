using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ExportCanonicalOwnedHolonsAsync(Guid owner,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(owner==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var loaded=await LoadAllHolonsAsync(loadChildren:false,version:version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var project=HolonGraphProjection.Create(records,true,true,0,0,true,source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>row.CreatedByAvatarId==owner || row.ParentHolonId==owner).Select(project).ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportCanonicalSelectedAvatarAsync(string selector,bool email,int version)
    {
        var loaded=await ReadCanonicalAvatarSelectorAsync(selector,email,version);
        if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(loaded);
        return await ExportCanonicalOwnedHolonsAsync(loaded.Result.Id,version);
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCanonicalMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,
        HolonType type,bool children,bool recursive,int depth,int initialDepth,bool continueOnError,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Nonempty metadata pairs and keys are required.");
            if(initialDepth<0) throw new ArgumentOutOfRangeException(nameof(initialDepth));
            var search=new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>(pairs),MetaKeyValuePairMatchMode=mode};
            StoredRecordSearch.Validate(search,version,depth);
            var loaded=await LoadAllHolonsAsync(type,children,recursive,depth,initialDepth,continueOnError,fromProvider,version);
            if(loaded.IsError) return loaded;
            var rows=loaded.Result.ToList();
            result.Result=rows.Where(row=>StoredRecordSearch.Matches(row,false,search,rows)).ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private Task<OASISResult<IEnumerable<IHolon>>> ReadCanonicalParentHolonsAsync(string key,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool continueOnError,bool fromProvider,int version)
    {
        if (Guid.TryParse(key,out var id) && id != Guid.Empty)
            return ReadCanonicalParentHolonsAsync(id,type,children,recursive,depth,initialDepth,continueOnError,fromProvider,version);
        var invalid = new OASISResult<IEnumerable<IHolon>>();
        var cause = new ArgumentException("A nonempty GUID Firestore parent storage key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref invalid,cause.Message,cause);
        return Task.FromResult(invalid);
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCanonicalParentHolonsAsync(Guid id,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool continueOnError,bool fromProvider,int version)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty parent identity is required.",nameof(id));
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded = await LoadAllHolonsAsync(loadChildren:false,version:version);
            if(loaded.IsError) return loaded;
            var records = loaded.Result.ToList();
            var project = HolonGraphProjection.Create(records,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result = records.Where(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type)).Select(project).ToList();
            result.IsLoaded = true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IHolon>> ReadCanonicalHolonByIdAsync(Guid id,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        var result=new OASISResult<IHolon>();
        try {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty holon identity is required.");
            if(depth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(ready);
            var document=await _firestoreDb.Collection("holons").Document(id.ToString()).GetSnapshotAsync();
            var holon=document.Exists ? DecodeHolon(document.ConvertTo<Dictionary<string,object>>(),document.Id):null;
            if(holon==null || !holon.IsActive) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore holon not found.";}
            else
            {
                IEnumerable<IHolon> records = new[] { holon };
                if (children && fromProvider)
                {
                    var loaded = await LoadAllHolonsAsync(loadChildren:false,version:version);
                    if (loaded.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                    records = loaded.Result;
                }
                var project = HolonGraphProjection.Create(records,children,recursive,depth,0,fromProvider,
                    source => OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                result.Result=project(holon);
                result.IsLoaded=true;
            }
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private static Holon DecodeHolon(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore holon lacks canonical data_json; old mappings require explicit review.");
        var holon=OasisJson.Deserialize<Holon>(json) ?? throw new InvalidDataException("Firestore holon payload is null.");
        if(holon.Id==Guid.Empty || holon.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore holon identity does not match document key.");
        return holon;
    }
}
