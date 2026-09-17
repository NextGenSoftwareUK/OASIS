using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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


        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            List<Avatar> avatars = new List<Avatar>();
            List<Holon> holons = new List<Holon>();
            FilterDefinition<Avatar> avatarFilter = null;
            FilterDefinition<Holon> holonFilter = null;

            try
            {
                bool isFirstGroup = true;

                foreach (ISearchGroupBase searchGroup in searchParams.SearchGroups)
                {
                    // Collect this group's results into temporary lists, then apply AND/OR below.
                    List<Avatar> groupAvatars = new List<Avatar>();
                    List<Holon> groupHolons = new List<Holon>();

                    ISearchTextGroup searchTextGroup = searchGroup as ISearchTextGroup;

                    if (searchTextGroup != null)
                    {
                        if (searchTextGroup.SearchAvatars)
                        {
                            if (searchTextGroup.AvatarSearchParams.FirstName || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("FirstName", new BsonRegularExpression("/" + Regex.Escape(searchTextGroup.SearchQuery.ToLower()) + "/i"));
                                groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSearchParams.LastName || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("LastName", new BsonRegularExpression("/" + Regex.Escape(searchTextGroup.SearchQuery.ToLower()) + "/i"));
                                groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }

                            if (searchTextGroup.AvatarSearchParams.Username || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                var collection = _dbContext.MongoDB.GetCollection<Avatar>("Avatar");

                                if (searchTextGroup.HolonType == HolonType.All)
                                {
                                    var query = from doc in collection.AsQueryable<Avatar>()
                                                where doc.Username.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                select doc;
                                    groupAvatars.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Avatar>()
                                                where doc.Username.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;
                                    groupAvatars.AddRange(query.ToList());
                                }
                            }

                            if (searchTextGroup.AvatarSearchParams.Email || searchTextGroup.AvatarSearchParams.SearchAllFields)
                            {
                                avatarFilter = Builders<Avatar>.Filter.Regex("Email", new BsonRegularExpression("/" + Regex.Escape(searchTextGroup.SearchQuery.ToLower()) + "/i"));
                                groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                            }
                        }

                        if (searchTextGroup.SearchHolons)
                        {
                            if (searchTextGroup.HolonSearchParams.Name || searchTextGroup.HolonSearchParams.SearchAllFields)
                            {
                                var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                                if (searchParams.ParentId != Guid.Empty)
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Name.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                where doc.ParentHolonId == searchParams.ParentId
                                                select doc;
                                    groupHolons.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Name.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;
                                    groupHolons.AddRange(query.ToList());
                                }
                            }

                            if (searchTextGroup.HolonSearchParams.Description || searchTextGroup.HolonSearchParams.SearchAllFields)
                            {
                                var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                                if (searchParams.ParentId != Guid.Empty)
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Description.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                where doc.ParentHolonId == searchParams.ParentId
                                                select doc;
                                    groupHolons.AddRange(query.ToList());
                                }
                                else
                                {
                                    var query = from doc in collection.AsQueryable<Holon>()
                                                where doc.Description.ToLower().Contains(searchTextGroup.SearchQuery.ToLower())
                                                where doc.HolonType == searchTextGroup.HolonType
                                                select doc;
                                    groupHolons.AddRange(query.ToList());
                                }
                            }
                        }
                    }

                    ISearchDateGroup searchDateGroup = searchGroup as ISearchDateGroup;

                    if (searchDateGroup != null)
                    {
                        if (searchDateGroup.SearchAvatars)
                        {
                            if (searchDateGroup.AvatarSearchParams.CreatedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                            }

                            if (searchDateGroup.AvatarSearchParams.ModifiedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                            }

                            if (searchDateGroup.AvatarSearchParams.DeletedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                            }
                        }

                        if (searchDateGroup.SearchHolons)
                        {
                            if (searchDateGroup.HolonSearchParams.CreatedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                            }

                            if (searchDateGroup.HolonSearchParams.ModifiedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                            }

                            if (searchDateGroup.HolonSearchParams.DeletedDate)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                            }
                        }
                    }

                    ISearchNumberGroup searchNumberGroup = searchGroup as ISearchNumberGroup;

                    if (searchNumberGroup != null)
                    {
                        if (searchNumberGroup.SearchAvatars)
                        {
                            if (searchNumberGroup.AvatarSearchParams.Version)
                            {
                                if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                    groupAvatars.AddRange(await _dbContext.Avatar.FindAsync(avatarFilter).Result.ToListAsync());
                                }
                            }
                        }

                        if (searchNumberGroup.SearchHolons)
                        {
                            if (searchNumberGroup.HolonSearchParams.Version)
                            {
                                if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                    groupHolons.AddRange(await _dbContext.Holon.FindAsync(holonFilter).Result.ToListAsync());
                                }
                            }
                        }
                    }

                    // Apply AND/OR operator to merge this group's results into the accumulated lists.
                    // The first group always unions; subsequent AND groups intersect.
                    if (isFirstGroup || searchGroup.PreviousSearchGroupOperator == Core.Enums.SearchParamGroupOperator.Or)
                    {
                        avatars.AddRange(groupAvatars);
                        holons.AddRange(groupHolons);
                    }
                    else
                    {
                        var groupAvatarIds = groupAvatars.Select(a => a.HolonId).ToHashSet();
                        var groupHolonIds = groupHolons.Select(h => h.HolonId).ToHashSet();
                        avatars = avatars.Where(a => groupAvatarIds.Contains(a.HolonId)).ToList();
                        holons = holons.Where(h => groupHolonIds.Contains(h.HolonId)).ToList();
                    }

                    isFirstGroup = false;
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

                if (searchParams.FilterByMetaData != null)
                {
                    List<Holon> matchedHolons = new List<Holon>();

                    foreach (Holon holon in holons)
                    {
                        if (holon.MetaData == null)
                            continue;
                        int matchedKeys = 0;
                        foreach (KeyValuePair<string, string> metaKeyValuePair in searchParams.FilterByMetaData)
                        {
                            if (holon.MetaData.ContainsKey(metaKeyValuePair.Key) && holon.MetaData[metaKeyValuePair.Key] != null && holon.MetaData[metaKeyValuePair.Key].ToString() == metaKeyValuePair.Value)
                            {
                                if (searchParams.MetaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any)
                                    matchedHolons.Add(holon);
                                else
                                    matchedKeys++;
                            }
                        }

                        if (searchParams.MetaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All && matchedKeys == searchParams.FilterByMetaData.Count)
                            matchedHolons.Add(holon);
                    }

                    holons = matchedHolons;
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
            return SearchAsync(searchParams).Result;


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