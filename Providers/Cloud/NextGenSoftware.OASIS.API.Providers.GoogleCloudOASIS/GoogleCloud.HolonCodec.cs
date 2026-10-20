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
    private Task<OASISResult<IHolon>> DeleteCanonicalHolonAsync(string key)
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return DeleteCanonicalHolonAsync(id);
        var result=new OASISResult<IHolon>();
        var cause=new ArgumentException("A nonempty GUID Firestore holon storage key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);
        return Task.FromResult(result);
    }

    private async Task<OASISResult<IHolon>> DeleteCanonicalHolonAsync(Guid id)
    {
        var result=new OASISResult<IHolon>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty holon identity is required.");
            var ready=await EnsureDetailStoreAsync(0);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(ready);
            var reference=_firestoreDb.Collection("holons").Document(id.ToString());
            var snapshot=await reference.GetSnapshotAsync();
            if(!snapshot.Exists) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore holon not found.";return result;}
            var holon=DecodeHolon(snapshot.ConvertTo<Dictionary<string,object>>(),snapshot.Id);
            try {RequireFirestoreAcknowledgement(await reference.DeleteAsync(Google.Cloud.Firestore.Precondition.LastUpdated(snapshot.UpdateTime.Value)));}
            catch(Grpc.Core.RpcException cause) when(cause.StatusCode==Grpc.Core.StatusCode.FailedPrecondition || cause.StatusCode==Grpc.Core.StatusCode.NotFound)
            {
                OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode=StorageLifecycleConflictException.ErrorCode;return result;
            }
            catch(Exception cause) {throw new GoogleFirestoreMutationReconciliationException(reference.Path,cause);}
            result.Result=holon;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportCanonicalOwnedHolonsAsync(Guid owner,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(owner==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var loaded=await LoadAllHolonsAsync(loadChildren:false,version:version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var current=version>0?await LoadAllHolonsAsync(loadChildren:false,version:0):loaded;
            if(current.IsError)return current;
            var project=HolonGraphProjection.Create(current.Result,true,true,0,0,true,source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>row.CreatedByAvatarId==owner).Select(project).ToList();
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
            ValidateFirestoreVersion(version);
            StoredRecordSearch.Validate(search,version,depth,supportsHistory:true);
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
            if(!Enum.IsDefined(typeof(HolonType),type)) throw new ArgumentOutOfRangeException(nameof(type));
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded = await LoadAllHolonsAsync(loadChildren:false,version:version);
            if(loaded.IsError) return loaded;
            var records = loaded.Result.ToList();
            var current=children&&fromProvider&&version>0?await LoadAllHolonsAsync(loadChildren:false,version:0):loaded;
            if(current.IsError)return current;
            var project = HolonGraphProjection.Create(current.Result,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)),rootsAreChildren:true);
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
            var holon=document.Exists ? await ReadFirestoreHistoryAsync<Holon>(document.ToDictionary(),document.Id,version):null;
            if(holon==null || holon.DeletedDate!=DateTime.MinValue) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore holon not found.";}
            else
            {
                IEnumerable<IHolon> records = new[] { holon };
                if (children && fromProvider)
                {
                    var loaded = await LoadAllHolonsAsync(loadChildren:false,version:0);
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
        => ReadFirestoreHistoryAsync<Holon>(fields,documentId,0).GetAwaiter().GetResult();
}
