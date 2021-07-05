using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class SearchRepository : ISearchRepository
    {
        private MongoDbContext _dbContext;

        public SearchRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            try
            {
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
                FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.SearchQuery.ToLower() + "/"));
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
                IEnumerable<SearchData> data = await _dbContext.SearchData.Find(filter).ToListAsync();

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
    }
}