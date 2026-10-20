using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    private async Task<OASISResult<ISearchResults>> SearchPrivateRecordsAsync(ISearchParams request,bool children,bool recursive,int depth,int version)
    {
        var result=new OASISResult<ISearchResults>();SearchParams query;
        try{
            StoredRecordSearch.Validate(request,version,depth,supportsHistory:true);
            // Own the supported canonical request/masks before the first native read.
            query=StoredRecordJson.Project<SearchParams>(request);
            StoredRecordSearch.Validate(query,version,depth,supportsHistory:true);
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"Invalid SOLID search request.",error);
            result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";return result;
        }
        try{
            var groups=query.SearchGroups??new List<ISearchGroupBase>();
            var searchAvatars=groups.Count==0||groups.Any(group=>group.SearchAvatars||!group.SearchHolons);
            var searchHolons=groups.Count==0||groups.Any(group=>group.SearchHolons||!group.SearchAvatars);
            var avatars=Array.Empty<IAvatar>();var holons=Array.Empty<IHolon>();
            if(searchAvatars){
                var loaded=await LoadPrivateRecordsAsync<IAvatar>(version);
                if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                avatars=loaded.Result.ToArray();
            }
            if(searchHolons||query.ParentId!=Guid.Empty){
                var loaded=await LoadPrivateRecordsAsync<IHolon>(version);
                if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);
                holons=loaded.Result.ToArray();
            }
            var currentHolons=holons;
            if(version>0&&(searchHolons||query.ParentId!=Guid.Empty)){
                var current=await LoadPrivateRecordsAsync<IHolon>(0);
                if(current.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(current);
                currentHolons=current.Result.ToArray();
            }
            var found=new SearchResults();
            found.SearchResultAvatars.AddRange(avatars.Where(record=>record.DeletedDate==DateTime.MinValue&&StoredRecordSearch.Matches(record,true,query,currentHolons)));
            if(searchHolons){
                var project=HolonGraphProjection.Create(currentHolons.Where(record=>record.DeletedDate==DateTime.MinValue),children,recursive,depth,0,true,
                    record=>StoredRecordJson.DeserializeStored<IHolon>(StoredRecordJson.Serialize(record)));
                found.SearchResultHolons.AddRange(holons.Where(record=>record.DeletedDate==DateTime.MinValue&&StoredRecordSearch.Matches(record,false,query,currentHolons)).Select(project));
            }
            found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
            result.Result=found;
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private search failed.",error);}
        return result;
    }
}
