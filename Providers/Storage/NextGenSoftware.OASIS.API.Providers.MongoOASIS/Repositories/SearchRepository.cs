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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class SearchRepository : ISearchRepository
    {
        private MongoDbContext _dbContext;

        private static bool ExtendedTextMask(object mask,params string[] nativeFields)
            => mask!=null && mask.GetType().GetProperties().Any(property=>property.PropertyType==typeof(bool)
                && (bool)property.GetValue(mask) && !nativeFields.Contains(property.Name));

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


        public Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
            => SearchCoreAsync(searchParams, false);

        private async Task<OASISResult<ISearchResults>> SearchCoreAsync(ISearchParams searchParams, bool synchronous)
        {
            var currentGroupType=HolonType.All;
            async Task<List<T>> Read<T>(IMongoCollection<T> collection, FilterDefinition<T> filter)
            {
                if(currentGroupType!=HolonType.All)
                    filter &= Builders<T>.Filter.Eq("HolonType",currentGroupType);
                return synchronous ? collection.Find(filter).ToList()
                    : await collection.Find(filter).ToListAsync().ConfigureAwait(false);
            }
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            List<Avatar> avatars = new List<Avatar>();
            List<Holon> holons = new List<Holon>();
            FilterDefinition<Avatar> avatarFilter = null;
            FilterDefinition<Holon> holonFilter = null;

            try
            {
                bool isFirstGroup = true;

                foreach (ISearchGroupBase searchGroup in searchParams.SearchGroups ?? new List<ISearchGroupBase>())
                {
                    currentGroupType=searchGroup.HolonType;
                    // Collect this group's results into temporary lists, then apply AND/OR below.
                    List<Avatar> groupAvatars = new List<Avatar>();
                    List<Holon> groupHolons = new List<Holon>();

                    ISearchTextGroup searchTextGroup = searchGroup as ISearchTextGroup;

                    if (searchTextGroup != null)
                    {
                        if (searchTextGroup.SearchAvatars)
                        {
                            var mask=searchTextGroup.AvatarSearchParams;
                            if(ExtendedTextMask(mask,"FirstName","LastName","Username","Email"))
                            {
                                var candidates=await Read(_dbContext.Avatar,Builders<Avatar>.Filter.Eq(row=>row.DeletedDate,DateTime.MinValue)).ConfigureAwait(false);
                                groupAvatars.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesTextFields(
                                    DataHelper.ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(row),false).Result,true,searchTextGroup)));
                            }
                            else
                            {
                            var text=new BsonRegularExpression(Regex.Escape(searchTextGroup.SearchQuery),"i");
                            var fields=new List<FilterDefinition<Avatar>>();
                            if(mask==null || mask.FirstName || mask.SearchAllFields) fields.Add(Builders<Avatar>.Filter.Regex(row=>row.FirstName,text));
                            if(mask==null || mask.LastName || mask.SearchAllFields) fields.Add(Builders<Avatar>.Filter.Regex(row=>row.LastName,text));
                            if(mask==null || mask.Username || mask.SearchAllFields) fields.Add(Builders<Avatar>.Filter.Regex(row=>row.Username,text));
                            if(mask==null || mask.Email || mask.SearchAllFields) fields.Add(Builders<Avatar>.Filter.Regex(row=>row.Email,text));
                            if(fields.Count!=0)
                            {
                                var filter=Builders<Avatar>.Filter.Eq(row=>row.DeletedDate,DateTime.MinValue)
                                    & Builders<Avatar>.Filter.Or(fields);
                                groupAvatars.AddRange(await Read(_dbContext.Avatar,filter).ConfigureAwait(false));
                            }
                            if(searchTextGroup.SearchIds || searchTextGroup.SearchProviderKeys)
                            {
                                var filter=Builders<Avatar>.Filter.Eq(row=>row.DeletedDate,DateTime.MinValue);
                                var candidates=await Read(_dbContext.Avatar,filter).ConfigureAwait(false);
                                groupAvatars.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesTextIdentifiers(
                                    DataHelper.ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(row),false).Result,searchTextGroup.SearchQuery,searchTextGroup.SearchIds,searchTextGroup.SearchProviderKeys)));
                            }
                            }
                        }

                        if (searchTextGroup.SearchHolons)
                        {
                            var filter = HolonRepository.BuildHolonTypeFilter(HolonType.All);
                            if (searchParams.ParentId != Guid.Empty)
                                filter &= Builders<Holon>.Filter.Eq(row => row.ParentHolonId, searchParams.ParentId);
                            if(ExtendedTextMask(searchTextGroup.HolonSearchParams,"Name","Description"))
                            {
                                var candidates=await Read(_dbContext.Holon,filter).ConfigureAwait(false);
                                groupHolons.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesTextFields(
                                    DataHelper.ConvertMongoEntityToOASISHolon(row,false),false,searchTextGroup)));
                            }
                            else
                            {
                            // Text search is literal substring matching, not caller-supplied regex.
                            var text = new BsonRegularExpression(Regex.Escape(searchTextGroup.SearchQuery), "i");
                            var fields = new List<FilterDefinition<Holon>>();
                            if (searchTextGroup.HolonSearchParams==null || searchTextGroup.HolonSearchParams.Name || searchTextGroup.HolonSearchParams.SearchAllFields)
                                fields.Add(Builders<Holon>.Filter.Regex(row => row.Name, text));
                            if (searchTextGroup.HolonSearchParams==null || searchTextGroup.HolonSearchParams.Description || searchTextGroup.HolonSearchParams.SearchAllFields)
                                fields.Add(Builders<Holon>.Filter.Regex(row => row.Description, text));
                            if (fields.Count != 0)
                                groupHolons.AddRange(await Read(_dbContext.Holon, filter & Builders<Holon>.Filter.Or(fields)).ConfigureAwait(false));
                            if(searchTextGroup.SearchIds || searchTextGroup.SearchProviderKeys)
                            {
                                var candidates=await Read(_dbContext.Holon,filter).ConfigureAwait(false);
                                groupHolons.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesTextIdentifiers(
                                    DataHelper.ConvertMongoEntityToOASISHolon(row,false),searchTextGroup.SearchQuery,searchTextGroup.SearchIds,searchTextGroup.SearchProviderKeys)));
                            }
                            }
                        }
                    }

                    ISearchDateGroup searchDateGroup = searchGroup as ISearchDateGroup;
                    var comparison=searchGroup is ISearchDateGroup || searchGroup is ISearchNumberGroup;
                    var nativeComparisonFields=searchGroup is ISearchDateGroup ? new[] {"CreatedDate","ModifiedDate","DeletedDate"}:new[] {"Version"};
                    var extendedAvatar=comparison && ExtendedTextMask(searchGroup.AvatarSearchParams,nativeComparisonFields);
                    var extendedHolon=comparison && ExtendedTextMask(searchGroup.HolonSearchParams,nativeComparisonFields);
                    if(extendedAvatar && searchGroup.SearchAvatars)
                    {
                        var candidates=await Read(_dbContext.Avatar,Builders<Avatar>.Filter.Eq(row=>row.DeletedDate,DateTime.MinValue)).ConfigureAwait(false);
                        groupAvatars.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesSearchFields(
                            DataHelper.ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(row),false).Result,true,searchGroup)));
                    }
                    if(extendedHolon && searchGroup.SearchHolons)
                    {
                        var candidates=await Read(_dbContext.Holon,HolonRepository.BuildHolonTypeFilter(HolonType.All)).ConfigureAwait(false);
                        groupHolons.AddRange(candidates.Where(row=>StoredRecordSearch.MatchesSearchFields(
                            DataHelper.ConvertMongoEntityToOASISHolon(row,false),false,searchGroup)));
                    }

                    if (searchDateGroup != null)
                    {
                        if (searchDateGroup.SearchAvatars && !extendedAvatar)
                        {
                            if (searchDateGroup.AvatarSearchParams?.CreatedDate ?? true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                            }

                            if (searchDateGroup.AvatarSearchParams?.ModifiedDate ?? true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.ModifiedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                            }

                            if (searchDateGroup.AvatarSearchParams?.DeletedDate == true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate == searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate != searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate < searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate <= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate > searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.DeletedDate >= searchDateGroup.Date);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                            }
                        }

                        if (searchDateGroup.SearchHolons && !extendedHolon)
                        {
                            if (searchDateGroup.HolonSearchParams?.CreatedDate ?? true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.CreatedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                            }

                            if (searchDateGroup.HolonSearchParams?.ModifiedDate ?? true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.ModifiedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                            }

                            if (searchDateGroup.HolonSearchParams?.DeletedDate == true)
                            {
                                if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate == searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate != searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate < searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate <= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate > searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchDateGroup.DateOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.DeletedDate >= searchDateGroup.Date);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                            }
                        }
                    }

                    ISearchNumberGroup searchNumberGroup = searchGroup as ISearchNumberGroup;

                    if (searchNumberGroup != null)
                    {
                        if (searchNumberGroup.SearchAvatars && !extendedAvatar)
                        {
                            if (searchNumberGroup.AvatarSearchParams?.Version ?? true)
                            {
                                if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    avatarFilter = Builders<Avatar>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                    groupAvatars.AddRange(await Read(_dbContext.Avatar, avatarFilter).ConfigureAwait(false));
                                }
                            }
                        }

                        if (searchNumberGroup.SearchHolons && !extendedHolon)
                        {
                            if (searchNumberGroup.HolonSearchParams?.Version ?? true)
                            {
                                if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.EqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version == searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.NotEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version != searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version < searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.LessThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version <= searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThan)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version > searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
                                }
                                else if (searchNumberGroup.NumberOperator == Core.Enums.SearchOperatorType.GreaterThanOrEqualTo)
                                {
                                    holonFilter = Builders<Holon>.Filter.Where(x => x.Version >= searchNumberGroup.Number);
                                    groupHolons.AddRange(await Read(_dbContext.Holon, holonFilter).ConfigureAwait(false));
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
                                {
                                    matchedHolons.Add(holon);
                                    // ANY accepts this candidate once, not once per matching key.
                                    break;
                                }
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
            return SearchCoreAsync(searchParams, true).GetAwaiter().GetResult();


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
