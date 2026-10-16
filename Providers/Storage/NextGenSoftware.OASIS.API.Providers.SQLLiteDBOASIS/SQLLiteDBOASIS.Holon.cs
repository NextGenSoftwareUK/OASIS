using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // SQLite provider does not generate native code from STAR metadata.
            return false;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatar(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatarAsync(Avatar);
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetail(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetailAsync(Avatar);
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                // SQLite accepts an explicit stored-Version filter; this does not imply archived revisions.
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(
                    searchParams,version,maxChildDepth,supportsHistory:true);
            }
            catch(Exception cause) when(cause is ArgumentException or NotSupportedException)
            {
                OASISErrorHandling.HandleError(ref result,"Invalid SQLite search request: "+cause.Message,cause);
                result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
                return result;
            }
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var groups = searchParams.SearchGroups;
                    HashSet<Guid> avatarParentScope=null;
                    bool MatchesGroups(Func<ISearchGroupBase, bool> matches)
                    {
                        var matched = matches(groups[0]);
                        foreach (var group in groups.Skip(1))
                        {
                            var next = matches(group);
                            matched = group.PreviousSearchGroupOperator switch {
                                SearchParamGroupOperator.And => matched && next,
                                SearchParamGroupOperator.Or => matched || next,
                                _ => throw new ArgumentException("Invalid search group operator.")
                            };
                        }
                        return matched;
                    }
                    bool Contains(string value, ISearchTextGroup group) =>
                        (value ?? string.Empty).Contains(group.SearchQuery ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                    bool MatchesIdentityAndMetadata(IHolonBase value, ISearchTextGroup group, bool providerKeys, bool metadata, string metadataKey)
                    {
                        if (group.SearchIds && Contains(value.Id.ToString(), group)) return true;
                        if ((group.SearchProviderKeys || providerKeys) && value.ProviderUniqueStorageKey != null &&
                            value.ProviderUniqueStorageKey.Values.Any(key => Contains(key, group))) return true;
                        if (!metadata || value.MetaData == null) return false;
                        if (!string.IsNullOrEmpty(metadataKey))
                            return value.MetaData.TryGetValue(metadataKey, out var selected) && selected != null && Contains(selected.ToString(), group);
                        return value.MetaData.Values.Any(item => item != null && Contains(item.ToString(), group));
                    }
                    bool Compare(int value, ISearchNumberGroup group) => group.NumberOperator switch {
                        SearchOperatorType.LessThan => value < group.Number,
                        SearchOperatorType.LessThanOrEqualTo => value <= group.Number,
                        SearchOperatorType.EqualTo => value == group.Number,
                        SearchOperatorType.NotEqualTo => value != group.Number,
                        SearchOperatorType.GreaterThan => value > group.Number,
                        SearchOperatorType.GreaterThanOrEqualTo => value >= group.Number,
                        _ => throw new ArgumentException("Invalid numeric search operator.")
                    };
                    bool CompareDate(DateTime value, ISearchDateGroup group) => group.DateOperator switch {
                        SearchOperatorType.LessThan => value < group.Date,
                        SearchOperatorType.LessThanOrEqualTo => value <= group.Date,
                        SearchOperatorType.EqualTo => value == group.Date,
                        SearchOperatorType.NotEqualTo => value != group.Date,
                        SearchOperatorType.GreaterThan => value > group.Date,
                        SearchOperatorType.GreaterThanOrEqualTo => value >= group.Date,
                        _ => throw new ArgumentException("Invalid date search operator.")
                    };
                    // Basic in-memory search using available repo APIs
                    var holonLoad = await _holonRepository.LoadAllHolonsAsync(loadChildren: loadChildren,
                        recursive: recursive, maxChildDepth: maxChildDepth, continueOnError: continueOnError, version: version);
                    if(holonLoad.IsError){
                        OASISErrorHandling.HandleError(ref result,"SQLite holon search failed: "+holonLoad.Message,holonLoad.Exception);
                        return result;
                    }
                    if (!holonLoad.IsError && holonLoad.Result != null)
                    {
                        var candidates = holonLoad.Result.Where(h => h.DeletedDate == DateTime.MinValue).ToList();
                        if (searchParams.ParentId != Guid.Empty)
                        {
                            // Version filters returned roots, not the independently versioned
                            // ancestors needed to reach those roots under the requested parent.
                            var scopeRows=candidates;
                            if(version!=0)
                            {
                                var scopeLoad=await _holonRepository.LoadAllHolonsAsync(loadChildren:false,version:0);
                                if(scopeLoad.IsError)
                                {
                                    OASISErrorHandling.HandleError(ref result,"SQLite search parent links failed: "+scopeLoad.Message,scopeLoad.Exception);
                                    return result;
                                }
                                if(scopeLoad.Result==null) throw new System.IO.InvalidDataException("SQLite search parent-link collection is missing.");
                                scopeRows=scopeLoad.Result.Where(row=>row.DeletedDate==DateTime.MinValue).ToList();
                            }
                            var descendants = new HashSet<Guid>();
                            var visited = new HashSet<Guid> { searchParams.ParentId };
                            var pending = new Queue<Guid>();
                            pending.Enqueue(searchParams.ParentId);
                            var byParent = scopeRows.ToLookup(h => h.ParentHolonId);
                            while (pending.Count > 0)
                            {
                                foreach (var child in byParent[pending.Dequeue()])
                                {
                                    if (!visited.Add(child.Id)) continue;
                                    descendants.Add(child.Id);
                                    if (searchParams.Recursive) pending.Enqueue(child.Id);
                                }
                            }
                            candidates = candidates.Where(h => descendants.Contains(h.Id)).ToList();
                            avatarParentScope=new HashSet<Guid> {searchParams.ParentId};
                            if(searchParams.Recursive) avatarParentScope.UnionWith(descendants);
                        }
                        holons.AddRange(candidates.Where(h => MatchesGroups(group =>
                            (group.HolonType == HolonType.All || group.HolonType == h.HolonType) && (group switch {
                            ISearchNumberGroup number => number.SearchHolons && number.HolonSearchParams?.Version == true && Compare(h.Version, number),
                            ISearchDateGroup date => date.SearchHolons && (
                                date.HolonSearchParams?.CreatedDate == true && CompareDate(h.CreatedDate, date) ||
                                date.HolonSearchParams?.ModifiedDate == true && CompareDate(h.ModifiedDate, date) ||
                                date.HolonSearchParams?.DeletedDate == true && CompareDate(h.DeletedDate, date)),
                            ISearchTextGroup text => (!text.SearchAvatars || text.SearchHolons) &&
                                (MatchesIdentityAndMetadata(h, text,
                                text.HolonSearchParams?.ProviderUniqueStorageKey == true || text.HolonSearchParams?.SearchAllFields == true,
                                text.HolonSearchParams?.MetaData == true || text.HolonSearchParams?.SearchAllFields == true,
                                text.HolonSearchParams?.MetaDataKey) || (text.HolonSearchParams == null
                                ? Contains(h.Name, text) || Contains(h.Description, text)
                                : (text.HolonSearchParams.Name || text.HolonSearchParams.SearchAllFields) && Contains(h.Name, text) ||
                                  (text.HolonSearchParams.Description || text.HolonSearchParams.SearchAllFields) && Contains(h.Description, text))),
                            _ => false
                        }))));
                    }

                    var avatarLoad = await _avatarRepository.LoadAllAvatarsAsync(version);
                    if(avatarLoad.IsError){
                        OASISErrorHandling.HandleError(ref result,"SQLite avatar search failed: "+avatarLoad.Message,avatarLoad.Exception);
                        return result;
                    }
                    if (!avatarLoad.IsError && avatarLoad.Result != null)
                    {
                        avatars.AddRange(avatarLoad.Result.Where(a => (avatarParentScope==null || avatarParentScope.Contains(a.ParentHolonId)) && MatchesGroups(group => group switch {
                            ISearchNumberGroup number => number.SearchAvatars && number.AvatarSearchParams?.Version == true && Compare(a.Version, number),
                            ISearchDateGroup date => date.SearchAvatars && (
                                date.AvatarSearchParams?.CreatedDate == true && CompareDate(a.CreatedDate, date) ||
                                date.AvatarSearchParams?.ModifiedDate == true && CompareDate(a.ModifiedDate, date) ||
                                date.AvatarSearchParams?.DeletedDate == true && CompareDate(a.DeletedDate, date)),
                            ISearchTextGroup text => (!text.SearchHolons || text.SearchAvatars) && (MatchesIdentityAndMetadata(a, text,
                                text.AvatarSearchParams?.ProviderUniqueStorageKey == true || text.AvatarSearchParams?.SearchAllFields == true,
                                text.AvatarSearchParams?.MetaData == true || text.AvatarSearchParams?.SearchAllFields == true, null) || (text.AvatarSearchParams == null
                                ? Contains(a.Name, text) || Contains(a.Username, text) || Contains(a.Email, text)
                                : (text.AvatarSearchParams.Username || text.AvatarSearchParams.SearchAllFields) && Contains(a.Username, text) ||
                                  (text.AvatarSearchParams.Email || text.AvatarSearchParams.SearchAllFields) && Contains(a.Email, text) ||
                                  (text.AvatarSearchParams.FirstName || text.AvatarSearchParams.SearchAllFields) && Contains(a.FirstName, text) ||
                                  (text.AvatarSearchParams.LastName || text.AvatarSearchParams.SearchAllFields) && Contains(a.LastName, text) ||
                                  (text.AvatarSearchParams.Title || text.AvatarSearchParams.SearchAllFields) && Contains(a.Title, text))),
                            _ => false
                        })));
                    }
                }
                
                // Deleted, ownership and metadata constraints apply to both result sets.
                holons = holons.Where(h => h.DeletedDate == DateTime.MinValue).ToList();
                avatars = avatars.Where(a => a.DeletedDate == DateTime.MinValue).ToList();
                if (searchParams.SearchOnlyForCurrentAvatar)
                {
                    holons = holons.Where(h => h.CreatedByAvatarId == searchParams.AvatarId).ToList();
                    avatars = avatars.Where(a => a.CreatedByAvatarId == searchParams.AvatarId).ToList();
                }
                if (searchParams.FilterByMetaData?.Count > 0)
                {
                    holons=holons.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(
                        row.MetaData,searchParams.FilterByMetaData,searchParams.MetaKeyValuePairMatchMode)).ToList();
                    avatars=avatars.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(
                        row.MetaData,searchParams.FilterByMetaData,searchParams.MetaKeyValuePairMatchMode)).ToList();
                }

                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                searchResults.NumberOfResults = holons.Count + avatars.Count;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"SQLite text search returned {holons.Count} holons and {avatars.Count} avatars";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        {
            return LoadProviderWalletsAsync().Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load all avatars and aggregate their provider wallets
                var allAvatarsResult = await _avatarRepository.LoadAllAvatarsAsync();
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}", allAvatarsResult.Exception);
                    result.ErrorCode = allAvatarsResult.ErrorCode;
                    return result;
                }

                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Aggregate wallets from all avatars
                foreach (var avatar in allAvatarsResult.Result ?? Enumerable.Empty<IAvatar>())
                {
                    var localWallets = await ReadLocalWalletsAsync(avatar.Id);
                    if (localWallets != null)
                    {
                        foreach (var kvp in localWallets)
                        {
                            if (!wallets.ContainsKey(kvp.Key))
                            {
                                wallets[kvp.Key] = new List<IProviderWallet>();
                            }
                            wallets[kvp.Key].AddRange(kvp.Value);
                        }
                    }
                }
                
                result.Result = wallets;
                result.IsError = false;
                result.Message = $"Successfully loaded provider wallets from {allAvatarsResult.Result?.Count() ?? 0} avatars in SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsAsync(providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                if (providerWallets == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Provider wallets cannot be null");
                    return result;
                }

                return await StoreOwnedWalletBatchAsync(providerWallets);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
                    return result;
                }

                var providerWallets = avatarResult.Result == null
                    ? new Dictionary<ProviderType, List<IProviderWallet>>()
                    : await ReadLocalWalletsAsync(avatarResult.Result.Id);

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        {
            return LoadProviderWalletsForAvatarByUsernameAsync(username).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar by username to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
                    return result;
                }

                var providerWallets = avatarResult.Result == null
                    ? new Dictionary<ProviderType, List<IProviderWallet>>()
                    : await ReadLocalWalletsAsync(avatarResult.Result.Id);

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {username} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by username from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        {
            return LoadProviderWalletsForAvatarByEmailAsync(email).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar by email to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
                    return result;
                }

                var providerWallets = avatarResult.Result == null
                    ? new Dictionary<ProviderType, List<IProviderWallet>>()
                    : await ReadLocalWalletsAsync(avatarResult.Result.Id);

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {email} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by email from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await StoreLocalWalletsAsync(avatar.Id, providerWallets);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving local wallets: {saveResult.Message}", saveResult.Exception);
                        result.ErrorCode = saveResult.ErrorCode;
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to SQLite: {ex.Message}", ex);
            }
            return result;
        }

    }
}
