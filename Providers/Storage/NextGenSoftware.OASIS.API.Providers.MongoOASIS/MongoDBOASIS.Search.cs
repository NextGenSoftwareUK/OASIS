using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Holon=NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<ISearchResults>> SearchValidatedAsync(ISearchParams query,bool children,bool recursive,int depth,int version,bool synchronous)
    {
        var result=new OASISResult<ISearchResults>();
        try {StoredRecordSearch.Validate(query,version,depth,supportsHistory:true);}
        catch(Exception cause) when(cause is ArgumentException or NotSupportedException)
        {
            OASISErrorHandling.HandleError(ref result,"Invalid Mongo search request: "+cause.Message,cause);
            result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            return result;
        }
        try
        {
            if(!IsProviderActivated)
            {
                var activated=synchronous ? ActivateProvider():await ActivateProviderAsync().ConfigureAwait(false);
                if(activated.IsError || !activated.Result)
                {
                    OASISErrorHandling.HandleError(ref result,"Mongo search activation failed: "+activated.Message,activated.Exception);
                    result.ErrorCode=activated.ErrorCode;return result;
                }
            }
            result=synchronous ? _searchRepository.Search(query):await _searchRepository.SearchAsync(query).ConfigureAwait(false);
            if(result==null) throw new InvalidDataException("Mongo search repository returned no result envelope.");
            if(result.IsError) return result;
            var found=result.Result ?? throw new InvalidDataException("Mongo search repository returned no result content.");
            if(found.SearchResultHolons==null || found.SearchResultAvatars==null)
                throw new InvalidDataException("Mongo search repository returned missing result collections.");
            // Version selects matching current root records, not an archive or global graph snapshot.
            var roots=found.SearchResultHolons.Where(row=>version==0 || row.Version==version).ToList();
            var project=HolonGraphProjection.Create(roots,children,recursive,depth,0,false,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            found.SearchResultHolons=roots.Select(project).ToList();
            found.SearchResultAvatars=found.SearchResultAvatars.Where(row=>version==0 || row.Version==version).ToList();
            found.NumberOfResults=found.SearchResultHolons.Count+found.SearchResultAvatars.Count;
            result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            result ??= new OASISResult<ISearchResults>();
            result.Result=null;result.IsLoaded=false;
            OASISErrorHandling.HandleError(ref result,"Mongo search failed: "+cause.Message,cause);
        }
        return result;
    }
}
