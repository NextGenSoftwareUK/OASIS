using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        bool continueOnError = true, int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
        try
        {
            var groups = searchParams?.SearchGroups?.OfType<ISearchTextGroup>()
                .Where(group => !string.IsNullOrWhiteSpace(group.SearchQuery)).ToList();
            if (groups == null || groups.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "Search requires at least one text search group with a query.");
                return result;
            }

            var matches = new SearchResults();
            var avatarsResult = await LoadAllAvatarRecordsAsync();
            if (avatarsResult.IsError) throw avatarsResult.Exception ?? new InvalidOperationException(avatarsResult.Message);
            var holonsResult = await LoadAllHolonRecordsAsync();
            if (holonsResult.IsError) throw holonsResult.Exception ?? new InvalidOperationException(holonsResult.Message);
            var avatars = avatarsResult.Result.ToList();
            var holons = holonsResult.Result.ToList();

            foreach (var group in groups)
            {
                var query = group.SearchQuery.Trim();
                bool Contains(string value) => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
                if (group.SearchAvatars || !group.SearchHolons)
                    matches.SearchResultAvatars.AddRange(avatars.Where(avatar =>
                        Contains(avatar.Username) || Contains(avatar.Email) || Contains(avatar.FirstName) ||
                        Contains(avatar.LastName) || (group.SearchIds && Contains(avatar.Id.ToString())))
                        .Where(avatar => matches.SearchResultAvatars.All(existing => existing.Id != avatar.Id)));
                if (group.SearchHolons || !group.SearchAvatars)
                    matches.SearchResultHolons.AddRange(holons.Where(holon =>
                        Contains(holon.Name) || Contains(holon.Description) ||
                        (group.SearchIds && Contains(holon.Id.ToString())))
                        .Where(holon => matches.SearchResultHolons.All(existing => existing.Id != holon.Id)));
            }

            matches.NumberOfResults = matches.SearchResultAvatars.Count + matches.SearchResultHolons.Count;
            result.Result = matches;
            result.Message = $"Found {matches.NumberOfResults} NEAR records.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error searching NEAR storage: {ex.Message}", ex); }
        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) =>
        SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var saved = await SaveHolonsAsync(holons, true, true, 0, 0, false);
        return new OASISResult<bool>(!saved.IsError)
        {
            IsError = saved.IsError,
            Message = saved.IsError ? saved.Message : $"Imported {saved.Result.Count()} holons into NEAR contract storage.",
            Exception = saved.Exception
        };
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).GetAwaiter().GetResult();
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => LoadAllHolonRecordsAsync();
    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var all = await LoadAllHolonRecordsAsync();
        if (!all.IsError) all.Result = all.Result.Where(holon => holon.CreatedByAvatarId == avatarId).ToList();
        return all;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
        ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
    {
        var avatar = await LoadAvatarByEmailAsync(email, version);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0) =>
        ExportAllDataForAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
    {
        var avatar = await LoadAvatarByUsernameAsync(username, version);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0) =>
        ExportAllDataForAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string username, bool softDelete = true)
    {
        var avatar = await LoadAvatarByUsernameAsync(username);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<bool>(false) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await DeleteAvatarRecordAsync(avatar.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatar(string username, bool softDelete = true) =>
        DeleteAvatarAsync(username, softDelete).GetAwaiter().GetResult();
    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true) =>
        DeleteAvatarAsync(username, softDelete);
    public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) =>
        DeleteAvatarByUsernameAsync(username, softDelete).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
    {
        var avatar = await LoadAvatarByEmailAsync(email);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<bool>(false) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await DeleteAvatarRecordAsync(avatar.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) =>
        DeleteAvatarByEmailAsync(email, softDelete).GetAwaiter().GetResult();
}
