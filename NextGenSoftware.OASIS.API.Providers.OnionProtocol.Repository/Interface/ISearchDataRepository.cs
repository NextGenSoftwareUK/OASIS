using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface
{
    public interface ISearchDataRepository : IEntityRepository<SearchData>
    {
        Task<ISearchResults> SearchAsync(ISearchParams searchTerm);
    }
}