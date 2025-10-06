using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Context;
using NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Model;
using OASIS_Onion.Repository.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Repository
{
    public class SearchDataRepository : ISearchDataRepository
    {
        private readonly SearchContext _dbContext = null;

        public SearchDataRepository(IOptions<Settings> settings)
        {
            _dbContext = new SearchContext(settings);
        }

        public async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            try
            {
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
                FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.SearchQuery.ToLower() + "/"));
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
                IEnumerable<SearchData> data = (await this._dbContext.Notes.FindAsync(filter)).ToEnumerable();

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