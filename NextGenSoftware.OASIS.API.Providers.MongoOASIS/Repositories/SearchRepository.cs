using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
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

        //public async Task<ISearchResults> SearchOLDAsync(ISearchParams searchTerm)
        //{
        //    try
        //    {
        //        //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
        //        FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.SearchQuery.ToLower() + "/"));
        //        //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
        //        IEnumerable<SearchData> data = await _dbContext.SearchData.Find(filter).ToListAsync();

        //        if (data != null)
        //        {
        //            List<string> results = new List<string>();

        //            foreach (SearchData dataObj in data)
        //                results.Add(dataObj.Data);

        //            return new SearchResults() { SearchResultStrings = results };
        //        }
        //        else
        //            return null;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}


        //TODO: This code is a WIP and will ONLY work if SearchParamGroupOperator is OR (it will add each seach group to the search results), if it is AND then it will need to be more complex and combine the various search groups into a unified search. Implementing generic search properly across the full OASIS is a LOT of work! ;-) lol
        //This code is only partially implemented to show how to use the OASIS Search Architecture, it is up to each OASIS Provider how to implement each search depending on how that provider works, for example SQL, Graph, Mongo, Blockchain, IPFS, Holochain, File, etc would all need to be implemented differently!
        //This code will be finished properly later... like many other places! ;-)
        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            List<Avatar> avatars = new List<Avatar>();
            List<Holon> holons = new List<Holon>();
            FilterDefinition<Avatar> avatarFilter = null;
            FilterDefinition<Holon> holonFilter = null;

            try
            {
                foreach (ISearchGroupBase searchGroup in searchParams.SearchGroups)
                {
                    ISearchTextGroup searchTextGroup = searchGroup as ISearchTextGroup;

                    if (searchTextGroup != null)
                    {
                        if (searchTextGroup.SearchAvatars)
                        {
                            if (searchTextGroup.AvatarSerachParams.FirstName)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("FirstName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                //IEnumerable<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
                                //IAsyncCursor<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSerachParams.LastName)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("LastName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSerachParams.Username)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("Username", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSerachParams.Email)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("Email", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            //TODO: Add remaining properties...
                        }

                        if (searchTextGroup.SearchHolons)
                        {
                            if (searchTextGroup.HolonSearchParams.Name)
                            {
                                holonFilter = Builders<Holon>.Filter.Regex("Name", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                            }

                            //TODO: Add remaining properties...
                        }
                    }

                    ISearchDateGroup searchDateGroup = searchGroup as ISearchDateGroup;

                    if (searchDateGroup != null) 
                    {
                        if (searchDateGroup.PreviousSearchGroupOperator == Core.Enums.SearchParamGroupOperator.Or)
                        {
                            if (searchDateGroup.SearchAvatars)
                            {
                                if (searchDateGroup.AvatarSerachParams.CreatedDate)
                                {
                                    if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }
                                }

                                //TODO: Implement rest of properties.
                            }

                            if (searchDateGroup.SearchHolons)
                            {
                                if (searchDateGroup.HolonSearchParams.CreatedDate)
                                {
                                    if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }
                                }

                                //TODO: Implement rest of properties.
                            }
                        }
                    }

                    ISearchNumberGroup searchNumberGroup = searchGroup as ISearchNumberGroup;

                    if (searchNumberGroup != null) 
                    {
                        if (searchNumberGroup.PreviousSearchGroupOperator == Core.Enums.SearchParamGroupOperator.Or)
                        {
                            if (searchNumberGroup.SearchAvatars)
                            {
                                if (searchNumberGroup.AvatarSerachParams.Version)
                                {
                                    if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                    {
                                        avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                        avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                    }
                                }

                                //TODO: Implement rest of properties.
                            }

                            if (searchDateGroup.SearchHolons)
                            {
                                if (searchDateGroup.HolonSearchParams.Version)
                                {
                                    if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }

                                    else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                    {
                                        holonFilter = Builders<Holon>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                        holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                    }
                                }

                                //TODO: Implement rest of properties.
                            }
                        }
                    }
                }

                result.Result.SearchResultHolons = (List<IHolon>)DataHelper.ConvertMongoEntitysToOASISHolons(holons);
                result.Result.SearchResultAvatars = (List<IAvatar>)DataHelper.ConvertMongoEntitysToOASISAvatars(avatars);
            }
            catch
            {
                throw;
            }

            return result;
        }

        public OASISResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return SearchAsync(searchParams).Result; //TODO: Temp, implement properly as below once async version is finished properly above...


            //OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();

            //try
            //{
            //    foreach (ISearchParamsBase searchParam in searchParams.SearchQuery)
            //    {
            //        ISearchTextGroup searchTextGroup = searchParam as ISearchTextGroup;

            //        if (searchTextGroup != null)
            //        {
            //            if (searchTextGroup.SearchAvatars)
            //            {
            //                FilterDefinition<Avatar> avatarFilter = Builders<Avatar>.Filter.Regex("FirstName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
            //                //IEnumerable<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
            //                //IAsyncCursor<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
            //                List<Avatar> avatars = _dbContext.Avatar.Find(avatarFilter).ToList();

            //                avatarFilter = Builders<Avatar>.Filter.Regex("LastName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
            //                avatars.AddRange(_dbContext.Avatar.Find(avatarFilter).ToList());

            //                avatarFilter = Builders<Avatar>.Filter.Regex("Username", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
            //                avatars.AddRange(_dbContext.Avatar.Find(avatarFilter).ToList());

            //                avatarFilter = Builders<Avatar>.Filter.Regex("Address", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
            //                avatars.AddRange(_dbContext.Avatar.Find(avatarFilter).ToList());


            //                result.Result.SearchResultAvatars = (List<IAvatar>)DataHelper.ConvertMongoEntitysToOASISAvatars(avatars);
            //            }

            //            if (searchTextGroup.SearchHolons)
            //            {
            //                FilterDefinition<Holon> holonFilter = Builders<Holon>.Filter.Regex("holon", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
            //                List<Holon> holons = _dbContext.Holon.Find(holonFilter).ToList();
            //                result.Result.SearchResultHolons = (List<IHolon>)DataHelper.ConvertMongoEntitysToOASISHolons(holons);
            //            }
            //        }
            //    }
            //}
            //catch
            //{
            //    throw;
            //}

            //return result;
        }
    }
}