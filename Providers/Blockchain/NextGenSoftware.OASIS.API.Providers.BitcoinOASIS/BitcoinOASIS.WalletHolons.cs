using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ExportWalletHolonsForAvatarAsync(
        Task<OASISResult<IAvatar>> resolve, int version)
    {
        var avatar = await resolve;
        if (!avatar.IsError)
            return await ExportWalletHolonsForOwnerAsync(avatar.Result.Id, version);

        var result = new OASISResult<IEnumerable<IHolon>> { ErrorCode = avatar.ErrorCode };
        OASISErrorHandling.HandleError(ref result, avatar.Message, avatar.Exception);
        return result;
    }

    private Task<OASISResult<IEnumerable<IHolon>>> ExportWalletHolonsForOwnerAsync(Guid owner, int version)
    {
        if (owner != Guid.Empty)
            return LoadWalletHolonsAsync(holon => holon.CreatedByAvatarId == owner || holon.ParentHolonId == owner,
                loadChildren: false, recursive: false, maxDepth: 0, currentDepth: 0, version: version);

        var result = new OASISResult<IEnumerable<IHolon>>();
        OASISErrorHandling.HandleError(ref result, "A Bitcoin avatar identity is required for a scoped export.");
        return Task.FromResult(result);
    }

    private async Task<OASISResult<ISearchResults>> SearchWalletRecordsAsync(
        ISearchParams search, bool loadChildren, bool recursive, int maxDepth, int version)
    {
        var result = new OASISResult<ISearchResults>();
        try
        {
            StoredRecordSearch.Validate(search, version, maxDepth, supportsHistory: true);
            var groups = search.SearchGroups ?? new List<ISearchGroupBase>();
            var wantsAvatars = groups.Count == 0 || groups.Any(group => group.SearchAvatars || !group.SearchHolons);
            var wantsHolons = groups.Count == 0 || groups.Any(group => group.SearchHolons || !group.SearchAvatars);
            var holons = wantsHolons || search.ParentId != Guid.Empty
                ? await ReadWalletRecordsAsync<IHolon>(DecodeHolonTransactionAsync, version)
                : Array.Empty<IHolon>();
            var found = new SearchResults();
            if (wantsAvatars)
            {
                var avatars = await ReadWalletRecordsAsync<IAvatar>(DecodeAvatarTransactionAsync, version);
                found.SearchResultAvatars.AddRange(avatars.Where(avatar => StoredRecordSearch.Matches(avatar, true, search, holons)));
            }

            if (wantsHolons)
            {
                found.SearchResultHolons.AddRange(holons.Where(holon => StoredRecordSearch.Matches(holon, false, search, holons)));
                if (loadChildren)
                {
                    var children = holons.Where(holon => holon.ParentHolonId != Guid.Empty)
                        .ToLookup(holon => holon.ParentHolonId);
                    foreach (var holon in found.SearchResultHolons)
                        AttachChildren(holon, children, new HashSet<Guid>(), recursive, maxDepth, 0);
                }

                if (search.SearchOnlyForCurrentAvatar)
                {
                    var visible = IOASISStorageProvider.ProjectVisibleHolons(
                        new OASISResult<IEnumerable<IHolon>> { Result = found.SearchResultHolons, IsLoaded = true },
                        search.AvatarId, false);
                    if (visible.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, visible.Message, visible.Exception);
                        return result;
                    }
                    found.SearchResultHolons = visible.Result.ToList();
                }
            }

            found.NumberOfResults = found.SearchResultAvatars.Count + found.SearchResultHolons.Count;
            result.Result = found;
            result.LoadedCount = found.NumberOfResults;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin wallet search failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<OASISResult<IHolon>> ResolveWalletHolonForDeleteAsync(string providerKey)
    {
        var referenced = await LoadHolonAsync(providerKey, loadChildren: false);
        return await ResolveCurrentWalletRecordAsync(referenced, providerKey,
            id => LoadHolonAsync(id, loadChildren: false));
    }

    private async Task<OASISResult<IHolon>> DeleteWalletHolonAsync(Func<Task<OASISResult<IHolon>>> resolve)
    {
        var current = await resolve();
        if (current.IsError)
            return current;

        if (current.Result.Version == int.MaxValue)
        {
            var error = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref error, "The Bitcoin holon version has reached its limit.");
            return error;
        }

        var tombstone = new Holon
        {
            Id = current.Result.Id,
            HolonType = current.Result.HolonType,
            Version = current.Result.Version + 1,
            IsActive = false,
            DeletedDate = DateTime.UtcNow
        };
        return await SaveHolonAsync(tombstone, saveChildren: false);
    }

    private async Task<OASISResult<IHolon>> LoadWalletHolonAsync(
        Guid id, bool loadChildren, bool recursive, int maxDepth, int version)
    {
        var matches = await LoadWalletHolonsAsync(holon => holon.Id == id, loadChildren, recursive, maxDepth, 0, version);
        var result = new OASISResult<IHolon>();
        if (matches.IsError)
        {
            OASISErrorHandling.HandleError(ref result, matches.Message, matches.Exception);
            return result;
        }

        result.Result = matches.Result.SingleOrDefault();
        if (result.Result == null)
        {
            result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
            OASISErrorHandling.HandleError(ref result, "The Bitcoin wallet has no matching holon.");
        }

        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> LoadWalletHolonsAsync(
        Func<IHolon, bool> select, bool loadChildren, bool recursive, int maxDepth, int currentDepth, int version)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            var records = await ReadWalletRecordsAsync<IHolon>(DecodeHolonTransactionAsync, version);
            var selected = records.Where(select).ToArray();
            if (loadChildren)
            {
                var children = records.Where(holon => holon.ParentHolonId != Guid.Empty)
                    .ToLookup(holon => holon.ParentHolonId);
                foreach (var root in selected)
                    AttachChildren(root, children, new HashSet<Guid>(), recursive, maxDepth, currentDepth);
            }

            result.Result = selected;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin wallet holon query failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private static void AttachChildren(IHolon parent, ILookup<Guid, IHolon> children,
        HashSet<Guid> ancestors, bool recursive, int maxDepth, int currentDepth)
    {
        if (!ancestors.Add(parent.Id))
            throw new InvalidOperationException("Bitcoin holon history contains a parent cycle.");

        parent.Children = maxDepth > 0 && currentDepth >= maxDepth
            ? new List<IHolon>()
            : children[parent.Id].ToList();
        foreach (var child in parent.Children)
        {
            if (ancestors.Contains(child.Id))
                throw new InvalidOperationException("Bitcoin holon history contains a parent cycle.");
            if (recursive)
                AttachChildren(child, children, ancestors, recursive, maxDepth, currentDepth + 1);
        }
        ancestors.Remove(parent.Id);
    }
}
