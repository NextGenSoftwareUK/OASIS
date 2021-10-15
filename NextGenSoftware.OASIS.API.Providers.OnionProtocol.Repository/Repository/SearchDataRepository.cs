using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Repository
{
    public class SearchDataRepository : EntityRepository<SearchData, IEntityProvider<SearchData>>, ISearchDataRepository
    {
        private IEntityProvider<SearchData> _testProvider;

        public SearchDataRepository(IEntityProvider<SearchData> testProvider) : base(testProvider)
        {
            this._testProvider = testProvider;
        }

        public Task<long> Count(MongoDBOASIS.Entities.SearchData instance)
        {
            throw new NotImplementedException();
        }

        public Task<MongoDBOASIS.Entities.SearchData> Create(MongoDBOASIS.Entities.SearchData instance)
        {
            throw new NotImplementedException();
        }

        public void Delete(MongoDBOASIS.Entities.SearchData instance)
        {
            throw new NotImplementedException();
        }

        public async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            try
            {
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
                FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.SearchQuery.ToLower() + "/"));
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
                IEnumerable<SearchData> data = await this._testProvider.GetAllAsync(filter);

                if (data != null)
                {
                    List<string> results = new List<string>();

                    foreach (SearchData dataObj in data)
                        results.Add(dataObj.Data);

                    return new SearchResults() { SearchResultStrings = results };
                }
                else
                    return null;
            }
            catch
            {
                throw;
            }
        }

        public Task<MongoDBOASIS.Entities.SearchData> Update(MongoDBOASIS.Entities.SearchData instance)
        {
            throw new NotImplementedException();
        }

        Task<MongoDBOASIS.Entities.SearchData> IEntityRepository<MongoDBOASIS.Entities.SearchData>.Get(Guid id)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<MongoDBOASIS.Entities.SearchData>> IEntityRepository<MongoDBOASIS.Entities.SearchData>.GetAll()
        {
            throw new NotImplementedException();
        }
    }
}