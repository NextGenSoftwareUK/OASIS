using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    internal static bool IsPrivateRecordAbsent(EntityOASIS entity)
        =>entity!=null && entity.EntityId==0 && entity.ExternalId?.Length==32 && entity.ExternalId.All(value=>value==0) && entity.Info?.Length==0;
    private async Task<OASISResult<IHolon>> ReadPrivateHolonGraphAsync(Guid id,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        var result=new OASISResult<IHolon>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core identity is required.",nameof(id));
            if(depth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            if(children && fromProvider)
            {
                // Root and descendants must come from the same pinned collection block.
                var loaded=await ReadPrivateHolonsAsync(HolonType.All,true,recursive,depth,0,true,version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                var root=loaded.Result.SingleOrDefault(row=>row.Id==id);
                if(root==null) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core holon not found.";return result;}
                result.Result=root;result.IsLoaded=true;
                return result;
            }
            result=await ReadPrivateGuidAsync<IHolon>(id,version,index=>_web3CoreOASIS.GetHolonByIdAsync(index),row=>DecodePrivateEntity<Holon>("holon",row,version));
            if(result.IsError) return result;
            var project=HolonGraphProjection.Create(new[] {result.Result},children,recursive,depth,0,false,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            result.Result=project(result.Result);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.Result=null;result.IsLoaded=false;}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportPrivateOwnedAsync(Guid owner,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(owner==Guid.Empty) throw new ArgumentException("A nonempty export avatar identity is required.",nameof(owner));
            var loaded=await ReadPrivateHolonsAsync(HolonType.All,true,true,0,0,true,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            result.Result=records.Where(row=>row.CreatedByAvatarId==owner || row.ParentHolonId==owner).ToArray();result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportPrivateSelectorAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try{
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            // Identity and exported holon histories have independent version counters.
            var loaded=email ? await LoadAvatarByEmailAsync(selector,0):await LoadAvatarByUsernameAsync(selector,0);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(loaded);
            return await ExportPrivateOwnedAsync(loaded.Result.Id,version);
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadPrivateParentAsync(string key,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(!Guid.TryParse(key,out var id) || id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core parent GUID is required.",nameof(key));
            var loaded=await ReadPrivateHolonsAsync(type,children,recursive,depth,initialDepth,fromProvider,version);
            if(loaded.IsError) return loaded;
            loaded.Result=loaded.Result.Where(row=>row.ParentHolonId==id).ToArray();return loaded;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadPrivateMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Nonempty metadata pairs and keys are required.");
            var search=new Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>(pairs),MetaKeyValuePairMatchMode=mode};
            StoredRecordSearch.Validate(search,version,depth,supportsHistory:true);
            var loaded=await ReadPrivateHolonsAsync(type,children,recursive,depth,initialDepth,fromProvider,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();loaded.Result=records.Where(row=>StoredRecordSearch.Matches(row,false,search,records)).ToArray();return loaded;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<T>> ReadPrivateSelectorAsync<T>(string selector,Func<T,string> field,Func<Task<OASISResult<IEnumerable<T>>>> load) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonempty Web3Core selector is required.",nameof(selector));
            var loaded=await load();
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,T>(loaded);
            var matches=loaded.Result.Where(row=>string.Equals(field(row),selector,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if(matches.Length>1) throw new StorageIdentityAmbiguousException();
            if(matches.Length==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core selector not found.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;}
        return result;
    }

    private Task<OASISResult<T>> ReadPrivateKeyAsync<T>(string key,Func<Guid,Task<OASISResult<T>>> load)
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return load(id);
        var result=new OASISResult<T>();var cause=new ArgumentException("A nonempty Web3Core GUID storage key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);return Task.FromResult(result);
    }

    private async Task<OASISResult<T>> ReadPrivateGuidAsync<T>(Guid id,int version,Func<uint,Task<EntityOASIS>> load,Func<EntityOASIS,T> decode) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core identity is required.",nameof(id));
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            var entity=await load((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()));
            if(IsPrivateRecordAbsent(entity))
            {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core record not found.";return result;}
            var record=decode(entity);
            if(record==null){result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core version not found.";return result;}
            if(record.Id!=id) throw new System.IO.InvalidDataException("Web3Core payload differs from requested full GUID.");
            if(!record.IsActive) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core record not active.";return result;}
            result.Result=record;result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<T>>> ReadPrivateCollectionAsync<T>(string kind,Func<EntityOASIS,T> decode,int version) where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            var entities=await _web3CoreOASIS.ReadCollectionAsync(kind);
            result.Result=entities.Select(decode).Where(row=>row!=null&&row.IsActive).ToArray();result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadPrivateHolonsAsync(HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            if(_web3CoreOASIS==null)throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            // One pinned collection supplies historical roots and independently current descendants.
            var entities=await _web3CoreOASIS.ReadCollectionAsync("holon");
            var current=entities.Select(row=>DecodePrivateEntity<Holon>("holon",row)).Where(row=>row.IsActive).Cast<IHolon>().ToList();
            var records=version==0?current:entities.Select(row=>DecodePrivateEntity<Holon>("holon",row,version)).Where(row=>row!=null&&row.IsActive).Cast<IHolon>().ToList();
            var project=HolonGraphProjection.Create(fromProvider?current:records,children,recursive,depth,initialDepth,fromProvider,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            result.Result=records.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToArray();result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
