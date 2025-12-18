using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

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
                            if (searchTextGroup.AvatarSearchParams.FirstName || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("FirstName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                //IEnumerable<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
                                //IAsyncCursor<IAvatar> avatars = await _dbContext.Avatar.Find(avatarFilter).ToEnumerable<IAvatar>();
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSearchParams.LastName || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("LastName", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSearchParams.Username || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                //avatarFilter = Builders<Avatar>.Filter.Regex("Username", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                //avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());

                                var collection = _dbContext.MongoDB.GetCollection<Avatar>("Avatar");
                                //var query = null;

                                // Perform a case-insensitive search using LINQ
                                if (searchTextGroup.HolonType == HolonType.All)
                                {
                                    var query = from doc in collection.AsQueryable<Avatar>()
                                                where doc.Username.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                select doc;

                                    avatars.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Avatar>()
                                                where doc.Username.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;

                                    avatars.AddRange(query.ToList());
                                }

                                //avatars.AddRange(query.ToList());
                            }

                            if (searchTextGroup.AvatarSearchParams.Email || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("Email", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                avatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            //TODO: Add remaining properties...
                        }

                        if (searchTextGroup.SearchHolons)
                        {
                            if (searchTextGroup.HolonSearchParams.Name || searchTextGroup.HolonSearchParams.SearchAllFields)
                            {
                                //var collection = _dbContext.MongoDB.GetCollection<BsonDocument>("Holon");
                                var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");
                                // public IMongoCollection<Holon> Holon => MongoDB.GetCollection<Holon>("Holon");

                                if (searchParams.ParentId != Guid.Empty)
                                {
                                    // Perform a case-insensitive search using LINQ
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Name.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                where doc.ParentHolonId == searchParams.ParentId
                                                select doc;

                                    holons.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Name.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;

                                    holons.AddRange(query.ToList());
                                }

                                //holonFilter = Builders<Holon>.Filter.Regex("Name", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                //holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());

                                //if (searchTextGroup.HolonType != Core.Enums.HolonType.All)
                                //    holons = holons.Where(x => x.HolonType == searchTextGroup.HolonType).ToList();
                            }

                            if (searchTextGroup.HolonSearchParams.Description || searchTextGroup.HolonSearchParams.SearchAllFields)
                            {
                                var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                                // Perform a case-insensitive search using LINQ
                                if (searchParams.ParentId != Guid.Empty)
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Description.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                where doc.ParentHolonId == searchParams.ParentId
                                                select doc;

                                    holons.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Description.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;

                                    holons.AddRange(query.ToList());
                                }

                                //holonFilter = Builders<Holon>.Filter.Regex("Name", new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
                                //holons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());

                                //if (searchTextGroup.HolonType != Core.Enums.HolonType.All)
                                //    holons = holons.Where(x => x.HolonType == searchTextGroup.HolonType).ToList();
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
                                if (searchDateGroup.AvatarSearchParams.CreatedDate)
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
                                if (searchNumberGroup.AvatarSearchParams.Version)
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

                //Make sure results are unique.
                holons = holons
                .GroupBy(p => new { p.Id })
                .Select(g => g.First())
                .ToList();

                avatars = avatars
               .GroupBy(p => new { p.Id })
               .Select(g => g.First())
               .ToList();

                avatars = avatars.Where(x => x.DeletedDate == DateTime.MinValue).ToList();
                holons = holons.Where(x => x.DeletedDate == DateTime.MinValue).ToList();

                if (searchParams.SearchOnlyForCurrentAvatar)
                {
                    avatars = avatars.Where(x => x.CreatedByAvatarId == searchParams.AvatarId.ToString()).ToList();
                    holons = holons.Where(x => x.CreatedByAvatarId == searchParams.AvatarId.ToString()).ToList();
                }

                result.Result = new SearchResults();
                //result.Result.SearchResultHolons = (List<IHolon>)DataHelper.ConvertMongoEntitysToOASISHolons(holons.Distinct());
                //result.Result.SearchResultAvatars = (List<IAvatar>)DataHelper.ConvertMongoEntitysToOASISAvatars(avatars.Distinct());
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