using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface ISearchRepository
    {
        Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchTerm);
        OASISResult<ISearchResults> Search(ISearchParams searchTerm);
    }
}