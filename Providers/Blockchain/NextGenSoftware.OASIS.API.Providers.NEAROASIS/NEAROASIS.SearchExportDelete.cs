using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

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
            StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
            searchParams=Core.Serialization.StoredRecordJson.Project<SearchParams>(searchParams);
            StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);

            var matches = new SearchResults();
            var avatarsResult = await LoadAllAvatarRecordsAsync(version);
            if (avatarsResult.IsError) throw avatarsResult.Exception ?? new InvalidOperationException(avatarsResult.Message);
            var holonsResult = await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,
                continueOnError:continueOnError,loadChildrenFromProvider:true,version:version);
            if (holonsResult.IsError) throw holonsResult.Exception ?? new InvalidOperationException(holonsResult.Message);
            var avatars = avatarsResult.Result.ToList();
            var holons = holonsResult.Result.ToList();

            matches.SearchResultAvatars.AddRange(avatars.Where(avatar=>StoredRecordSearch.Matches(avatar,true,searchParams,holons)));
            matches.SearchResultHolons.AddRange(holons.Where(holon=>StoredRecordSearch.Matches(holon,false,searchParams,holons)));

            matches.NumberOfResults = matches.SearchResultAvatars.Count + matches.SearchResultHolons.Count;
            result.Result = matches;
            result.IsLoaded = true;
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
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => LoadAllHolonRecordsAsync(version);
    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (avatarId == Guid.Empty) throw new ArgumentException("A nonempty export avatar identity is required.", nameof(avatarId));
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            var all = await LoadAllHolonRecordsAsync(version);
            if (!all.IsError)
            {
                all.Result = all.Result.Where(holon => holon.CreatedByAvatarId == avatarId).ToList();
                all.IsLoaded = true; all.LoadedCount = all.Result.Count();
            }
            return all;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
        ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        => ExportAvatarSelectorAsync(email, true, version);

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0) =>
        ExportAllDataForAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        => ExportAvatarSelectorAsync(username, false, version);

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportAvatarSelectorAsync(string selector, bool email, int version)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            // Avatar identity and exported holons have independent version counters.
            var avatar = email ? await LoadAvatarByEmailAsync(selector, 0) : await LoadAvatarByUsernameAsync(selector, 0);
            if (avatar.IsError || avatar.Result == null)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatar);
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0) =>
        ExportAllDataForAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string username, bool softDelete = true)
    {
        var avatar = await LoadAvatarByUsernameAsync(username);
        if (avatar.IsError || avatar.Result == null)
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(avatar);
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
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(avatar);
        return await DeleteAvatarRecordAsync(avatar.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) =>
        DeleteAvatarByEmailAsync(email, softDelete).GetAwaiter().GetResult();
}
