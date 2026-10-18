using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS;

public partial class AptosOASIS
{
    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
    {
        var avatar = await LoadAvatarByEmailAsync(email);
        if (avatar.IsError || avatar.Result == null) return new() { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
    }
    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;
    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
    {
        var avatar = await LoadAvatarByUsernameAsync(username);
        if (avatar.IsError || avatar.Result == null) return new() { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
    }
    public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

    public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
        LoadHolonAsync(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!await EnsureActivatedAsync(result)) return result;
            if (!await HasRecordAsync(HolonRecordType, providerKey)) { result.Message = $"Aptos holon '{providerKey}' was not found."; return result; }
            var holon = await GetRecordAsync<Holon>(HolonRecordType, providerKey);
            if (holon != null) holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = providerKey;
            result.Result = holon;
            result.Message = holon == null ? $"Aptos holon '{providerKey}' was not found." : "Holon loaded from Aptos.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos holon: {ex.Message}", ex); }
        return result;
    }
    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!await EnsureActivatedAsync(result)) return result;
            var holons = new List<IHolon>();
            foreach (var key in await GetRecordKeysAsync(HolonRecordType))
            {
                var holon = await GetRecordAsync<Holon>(HolonRecordType, key);
                if (holon != null && !holon.IsDeleted && (type == HolonType.All || holon.HolonType == type))
                { holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = key; holons.Add(holon); }
            }
            result.Result = holons;
            result.Message = $"Loaded {holons.Count} holons from Aptos.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos holons: {ex.Message}", ex); }
        return result;
    }
    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    { var all = await LoadAllHolonsAsync(type, false, false, version: version); if (!all.IsError) all.Result = all.Result.Where(h => h.ParentHolonId == id).ToList(); return all; }
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    { var parent = await LoadHolonAsync(providerKey, false, false, version: version); return parent.Result == null ? new() { IsError = parent.IsError, Message = parent.Message, Exception = parent.Exception } : await LoadHolonsForParentAsync(parent.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); }
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

    private static bool AptosMetaMatches(IHolon h, string key, string value) => h.MetaData != null && h.MetaData.TryGetValue(key, out var actual) && string.Equals(actual?.ToString(), value, StringComparison.OrdinalIgnoreCase);
    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string key, string value, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    { var all = await LoadAllHolonsAsync(type, false, false, version: version); if (!all.IsError) all.Result = all.Result.Where(h => AptosMetaMatches(h, key, value)).ToList(); return all; }
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string key, string value, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(key, value, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> filters, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    { var all = await LoadAllHolonsAsync(type, false, false, version: version); if (!all.IsError) all.Result = all.Result.Where(h => mode == MetaKeyValuePairMatchMode.Any ? filters.Any(x => AptosMetaMatches(h, x.Key, x.Value)) : filters.All(x => AptosMetaMatches(h, x.Key, x.Value))).ToList(); return all; }
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> filters, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(filters, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var response = new OASISResult<ISearchResults>();
        try
        {
            var results = new SearchResults();
            var avatars = (await LoadAllAvatarsAsync(version)).Result?.ToList() ?? new();
            var holons = (await LoadAllHolonsAsync(version: version)).Result?.ToList() ?? new();
            foreach (var group in searchParams?.SearchGroups?.OfType<ISearchTextGroup>().Where(x => !string.IsNullOrWhiteSpace(x.SearchQuery)) ?? Enumerable.Empty<ISearchTextGroup>())
            {
                bool Has(string value) => value?.Contains(group.SearchQuery, StringComparison.OrdinalIgnoreCase) == true;
                if (group.SearchAvatars || !group.SearchHolons) results.SearchResultAvatars.AddRange(avatars.Where(a => Has(a.Username) || Has(a.Email) || Has(a.FirstName) || Has(a.LastName) || (group.SearchIds && Has(a.Id.ToString()))));
                if (group.SearchHolons || !group.SearchAvatars) results.SearchResultHolons.AddRange(holons.Where(h => (group.HolonType == HolonType.All || h.HolonType == group.HolonType) && (Has(h.Name) || Has(h.Description) || (group.SearchIds && Has(h.Id.ToString())) || (group.SearchProviderKeys && h.ProviderUniqueStorageKey.Values.Any(Has)))));
            }
            results.SearchResultAvatars = results.SearchResultAvatars.GroupBy(x => x.Id).Select(x => x.First()).ToList();
            results.SearchResultHolons = results.SearchResultHolons.GroupBy(x => x.Id).Select(x => x.First()).ToList();
            if (searchParams?.FilterByMetaData?.Count > 0) results.SearchResultHolons = results.SearchResultHolons.Where(h => searchParams.MetaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? searchParams.FilterByMetaData.Any(x => AptosMetaMatches(h, x.Key, x.Value)) : searchParams.FilterByMetaData.All(x => AptosMetaMatches(h, x.Key, x.Value))).ToList();
            if (searchParams?.ParentId != Guid.Empty) results.SearchResultHolons = results.SearchResultHolons.Where(h => h.ParentHolonId == searchParams.ParentId).ToList();
            if (searchParams?.SearchOnlyForCurrentAvatar == true && searchParams.AvatarId != Guid.Empty) { results.SearchResultAvatars = results.SearchResultAvatars.Where(a => a.Id == searchParams.AvatarId || a.CreatedByAvatarId == searchParams.AvatarId).ToList(); results.SearchResultHolons = results.SearchResultHolons.Where(h => h.CreatedByAvatarId == searchParams.AvatarId).ToList(); }
            results.NumberOfResults = results.SearchResultAvatars.Count + results.SearchResultHolons.Count;
            response.Result = results; response.Message = $"Found {results.NumberOfResults} Aptos records.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref response, $"Error searching Aptos records: {ex.Message}", ex); }
        return response;
    }
    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
}
