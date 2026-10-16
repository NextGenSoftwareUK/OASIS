using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS
{
    private async Task<OASISResult<IEnumerable<T>>> ReadNearbyAsync<T>(long latitude,long longitude,int radius,
        Func<Task<OASISResult<IEnumerable<T>>>> load,HolonType type=HolonType.All) where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            var query=new StoredRecordLocationQuery(latitude,longitude,radius);
            if(!Enum.IsDefined(typeof(HolonType),type)) throw new ArgumentOutOfRangeException(nameof(type));
            var loaded=await load();if(loaded.IsError) return loaded;
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,IEnumerable<T>>(loaded);
            result.Result=query.Select(loaded.Result,row=>row.IsActive && !row.IsDeleted && (type==HolonType.All || row.HolonType==type));
            result.IsLoaded=true;
        }
        catch(Exception cause) {result.Result=null;result.IsLoaded=false;OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportOwnedHolonsAsync(Guid owner,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try {
            if(owner==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var loaded=await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);
            if(loaded.IsError) return loaded;
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IEnumerable<IHolon>>(loaded);
            result.Result=loaded.Result.Where(row=>row.CreatedByAvatarId==owner || row.ParentHolonId==owner).ToList();
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportSelectedAvatarAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try {
            ValidateDetailVersion(version);
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
            var loaded=email ? await LoadAvatarByEmailAsync(selector,version):await LoadAvatarByUsernameAsync(selector,version);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(loaded);
            return await ExportOwnedHolonsAsync(loaded.Result.Id,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<ISearchResults>> ReadSearchAsync(ISearchParams search,bool children,bool recursive,int maxDepth,bool continueOnError,int version)
    {
        var result=new OASISResult<ISearchResults>();
        try {
            StoredRecordSearch.Validate(search,version,maxDepth);
            var groups=search.SearchGroups ?? new List<ISearchGroupBase>();
            var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
            var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
            var avatars=new List<IAvatar>();var holons=new List<IHolon>();
            if(wantsAvatars) {
                var loaded=await LoadAllAvatarsAsync(version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                avatars=loaded.Result.ToList();
            }
            if(wantsHolons || search.ParentId!=Guid.Empty) {
                var loaded=await LoadAllHolonsAsync(HolonType.All,children,recursive,maxDepth,0,continueOnError,true,version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);
                holons=loaded.Result.ToList();
            }
            var found=new SearchResults();
            found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,search,holons)));
            if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,search,holons)));
            found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
            result.Result=found;result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,
        HolonType type,bool children,bool recursive,int maxDepth,int initialDepth,bool continueOnError,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Nonempty metadata pairs and keys are required.");
            var search=new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>(pairs),MetaKeyValuePairMatchMode=mode};
            StoredRecordSearch.Validate(search,version,maxDepth);
            var loaded=await LoadAllHolonsAsync(type,children,recursive,maxDepth,initialDepth,continueOnError,fromProvider,version);
            if(loaded.IsError) return loaded;
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IEnumerable<IHolon>>(loaded);
            var rows=loaded.Result.ToList();
            result.Result=rows.Where(row=>StoredRecordSearch.Matches(row,false,search,rows)).ToList();result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
