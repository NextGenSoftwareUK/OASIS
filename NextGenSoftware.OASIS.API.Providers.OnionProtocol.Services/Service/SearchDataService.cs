using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class SearchDataService : EntityService<SearchData>, ISearchDataService
    {
        private ISearchDataRepository _SearchDataRepository;

        public SearchDataService(ISearchDataRepository SearchDataRepository) : base((DbModel.Interfaces.IEntityRepository<SearchData>)SearchDataRepository)
        {
            this._SearchDataRepository = SearchDataRepository;
        }
    }
}