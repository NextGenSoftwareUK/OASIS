using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface ISearchRepository
    {
        Task<ISearchResults> SearchAsync(ISearchParams searchTerm);
    }
}