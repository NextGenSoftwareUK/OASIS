using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    private async Task<OASISResult<bool>> DeleteCurrentAvatarAsync(string key,bool soft)
    {
        var result=new OASISResult<bool>(false);
        var acknowledged=new List<string>();Guid id=Guid.Empty;
        try
        {
            if(!Guid.TryParse(key,out id) || id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos avatar GUID is required.",nameof(key));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return activation;
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var avatar=await avatarRepository.GetByIdAsync(id.ToString());
            if(avatar==null) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Cosmos avatar not found.";return result;}
            var detail=await avatarDetailRepository.GetByIdAsync(id.ToString());
            if(soft)
            {
                // Plan both payloads before the first write. No actor is supplied by this interface.
                var date=avatar.IsActive || avatar.DeletedDate==default ? DateTime.UtcNow:avatar.DeletedDate;
                var actor=avatar.IsActive ? Guid.Empty:avatar.DeletedByAvatarId;
                var snapshot=(IAvatar)OasisJson.Deserialize(OasisJson.Serialize(avatar),typeof(Avatar));
                snapshot.IsActive=false;snapshot.DeletedDate=date;snapshot.DeletedByAvatarId=actor;
                var detailSnapshot=detail==null ? null:(IAvatarDetail)OasisJson.Deserialize(OasisJson.Serialize(detail),typeof(AvatarDetail));
                if(detailSnapshot!=null) {detailSnapshot.IsActive=false;detailSnapshot.DeletedDate=date;detailSnapshot.DeletedByAvatarId=actor;}
                if(avatar.IsActive || avatar.DeletedDate==default)
                {
                    if(await avatarRepository.ReplaceIfUnchangedAsync(avatar,snapshot)==null) throw new CosmosMutationReconciliationException(id.ToString(),new System.IO.InvalidDataException("Cosmos avatar tombstone lacks acknowledged record."));
                    acknowledged.Add("avatarItems");
                }
                if(detailSnapshot!=null && (detail.IsActive || detail.DeletedDate!=date || detail.DeletedByAvatarId!=actor))
                {
                    if(await avatarDetailRepository.ReplaceIfUnchangedAsync(detail,detailSnapshot)==null) throw new CosmosMutationReconciliationException(id.ToString(),new System.IO.InvalidDataException("Cosmos detail tombstone lacks acknowledged record."));
                    acknowledged.Add("avatarDetailItems");
                }
            }
            else
            {
                // Preserve detail-first ordering; cross-container partial outcomes remain explicit.
                if(detail!=null) {await avatarDetailRepository.DeleteIfUnchangedAsync(detail);acknowledged.Add("avatarDetailItems");}
                await avatarRepository.DeleteIfUnchangedAsync(avatar);acknowledged.Add("avatarItems");
            }
            result.Result=true;result.IsSaved=acknowledged.Count>0;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause)
        {
            var error=acknowledged.Count>0 ? new CosmosAvatarLifecycleException(id,acknowledged,cause):cause;
            OASISErrorHandling.HandleError(ref result,error.Message,error);
            if(cause is StorageLifecycleConflictException || cause is Microsoft.Azure.Cosmos.CosmosException conflict && conflict.StatusCode==System.Net.HttpStatusCode.PreconditionFailed)
                result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private async Task<OASISResult<bool>> DeleteCurrentAvatarSelectorAsync(string selector,bool email,bool soft)
    {
        var loaded=await ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,selector,row=>email ? row.Email:row.Username,0);
        if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(loaded);
        return await DeleteCurrentAvatarAsync(loaded.Result.Id.ToString(),soft);
    }

    private async Task<OASISResult<IHolon>> DeleteCurrentHolonAsync(string key)
    {
        var result=await ReadCurrentKeyAsync<IHolon>(()=>holonRepository,key,0);
        if(result.IsError) return result;
        var record=result.Result;
        result.Result=null;result.IsLoaded=false;
        try
        {
            await holonRepository.DeleteIfUnchangedAsync(record);
            result.Result=record;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageLifecycleConflictException||cause is Microsoft.Azure.Cosmos.CosmosException conflict&&conflict.StatusCode==System.Net.HttpStatusCode.PreconditionFailed)
                result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
        return result;
    }

    private async Task<OASISResult<T>> ReadCurrentSelectorAsync<T>(Func<IRepository<T>> repository,string selector,Func<T,string> field,int version) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonempty Cosmos identity selector is required.",nameof(selector));
            var loaded=await ReadCurrentListAsync(repository,version);
            if(loaded.IsError)
            {
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,T>(loaded);
                if(loaded.Exception is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
                return result;
            }
            var matches=loaded.Result.Where(row=>string.Equals(field(row),selector,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if(matches.Length>1) throw new StorageIdentityAmbiguousException();
            if(matches.Length==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Cosmos record selector not found.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportCurrentOwnedAsync(Guid owner,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(owner==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.",nameof(owner));
            var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var pool=await ReadGraphPoolAsync(loaded,version,true,true);
            if(pool.IsError)return pool;
            var project=HolonGraphProjection.Create(pool.Result,true,true,0,0,true,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>row.CreatedByAvatarId==owner || row.ParentHolonId==owner).Select(project).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportCurrentSelectorAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            var loaded=await ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,selector,row=>email ? row.Email:row.Username,version);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(loaded);
            return await ExportCurrentOwnedAsync(loaded.Result.Id,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCurrentMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Nonempty metadata pairs and keys are required.");
            if(!Enum.IsDefined(typeof(MetaKeyValuePairMatchMode),mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if(initialDepth<0) throw new ArgumentOutOfRangeException(nameof(initialDepth));
            var search=new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>(pairs),MetaKeyValuePairMatchMode=mode};
            StoredRecordSearch.Validate(search,version,depth,supportsHistory:true);
            var loaded=await ReadCurrentHolonsAsync(type,children,recursive,depth,initialDepth,fromProvider,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            result.Result=records.Where(row=>StoredRecordSearch.Matches(row,false,search,records)).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCurrentParentAsync(string key,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(!Guid.TryParse(key,out var id) || id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos parent GUID key is required.",nameof(key));
            if(!Enum.IsDefined(typeof(HolonType),type))throw new ArgumentOutOfRangeException(nameof(type));
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var pool=await ReadGraphPoolAsync(loaded,version,children,fromProvider);
            if(pool.IsError)return pool;
            var project=HolonGraphProjection.Create(pool.Result,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type)).Select(project).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCurrentHolonsAsync(HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(!Enum.IsDefined(typeof(HolonType),type))throw new ArgumentOutOfRangeException(nameof(type));
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var pool=await ReadGraphPoolAsync(loaded,version,children,fromProvider);
            if(pool.IsError)return pool;
            var project=HolonGraphProjection.Create(pool.Result,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private Task<OASISResult<IEnumerable<IHolon>>> ReadGraphPoolAsync(OASISResult<IEnumerable<IHolon>> roots,int version,bool children,bool fromProvider)
        => version>0 && children && fromProvider ? ReadCurrentListAsync<IHolon>(()=>holonRepository,0) : Task.FromResult(roots);

    private async Task<OASISResult<IHolon>> ReadCurrentHolonAsync(Guid id,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        var result=await ReadCurrentIdAsync<IHolon>(()=>holonRepository,id,version,depth);
        if(result.IsError) return result;
        try
        {
            IEnumerable<IHolon> records=new[] {result.Result};
            if(children && fromProvider)
            {
                var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,0);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                records=loaded.Result;
            }
            var project=HolonGraphProjection.Create(records,children,recursive,depth,0,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=project(result.Result);
        }
        catch(Exception cause) {result.Result=null;result.IsLoaded=false;OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private Task<OASISResult<IHolon>> ReadCurrentHolonKeyAsync(string key,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return ReadCurrentHolonAsync(id,children,recursive,depth,fromProvider,version);
        var result=new OASISResult<IHolon>();var cause=new ArgumentException("A nonempty Cosmos OASIS GUID key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);return Task.FromResult(result);
    }

    private async Task<OASISResult<IEnumerable<T>>> ReadCurrentListAsync<T>(Func<IRepository<T>> repository,int version) where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<T>>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var store=repository() ?? throw new InvalidOperationException("Cosmos repository was not initialized.");
            var rows=(version==0 ? store.GetList() : store.GetList(version)) ?? throw new System.IO.InvalidDataException("Cosmos collection read returned no collection.");
            result.Result=rows.Where(row=>row.IsActive).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<T>> SaveCurrentRecordAsync<T>(T record,Func<IRepository<T>> repository) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            ArgumentNullException.ThrowIfNull(record);
            var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<T>.Capture(record);
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,T>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var store=repository() ?? throw new InvalidOperationException("Cosmos repository was not initialized.");
            publication.ValidateUnchanged();
            var saved=await store.UpsertAsync(publication.Proposal);
            if(saved==null) throw new CosmosMutationReconciliationException(record.Id.ToString(),new System.IO.InvalidDataException("Cosmos save returned no acknowledged record."));
            try{publication.ValidateUnchanged();}
            catch(Exception cause){throw new CosmosMutationReconciliationException(saved.Id.ToString(),cause,acknowledged:true);}
            record.Id=saved.Id;record.Version=saved.Version;record.VersionId=saved.VersionId;record.PreviousVersionId=saved.PreviousVersionId;
            record.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=saved.Id.ToString();
            result.Result=saved;result.IsSaved=true;
        }
        catch(Exception cause) {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageLifecycleConflictException || cause is Microsoft.Azure.Cosmos.CosmosException conflict
                && conflict.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.PreconditionFailed)
                result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private Task<OASISResult<T>> ReadCurrentKeyAsync<T>(Func<IRepository<T>> repository,string key,int version,int depth=0) where T:IHolonBase
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return ReadCurrentIdAsync(repository,id,version,depth);
        var result=new OASISResult<T>();var cause=new ArgumentException("A nonempty Cosmos OASIS GUID key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);return Task.FromResult(result);
    }

    private async Task<OASISResult<T>> ReadCurrentIdAsync<T>(Func<IRepository<T>> repository,Guid id,int version,int depth=0) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos OASIS GUID identity is required.",nameof(id));
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(depth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,T>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var row=version==0 ? await repository().GetByIdAsync(id.ToString()) : await repository().GetByIdAsync(id.ToString(),version);
            if(row==null || !row.IsActive)
            {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Cosmos OASIS record not found.";
            }
            else {result.Result=row;result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
