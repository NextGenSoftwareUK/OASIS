using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
        LoadAvatarDetailRecordAsync(id, version);

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
        LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int version = 0)
    {
        var result = await LoadHolonRecordAsync(id, version);
        return await ProjectLoadedHolonAsync(result, loadChildren, recursive, maxChildDepth, continueOnError,
            loadChildrenRecursiveDepth);
    }

    private async Task<OASISResult<IHolon>> ProjectLoadedHolonAsync(OASISResult<IHolon> result,
        bool loadChildren, bool recursive, int maxChildDepth, bool continueOnError, bool childrenFromProvider)
    {
        if (!result.IsError && result.Result != null)
        {
            try
            {
                IEnumerable<IHolon> records = Array.Empty<IHolon>();
                if (loadChildren && childrenFromProvider)
                {
                    var all = await LoadAllHolonRecordsAsync();
                    if (all.IsError) throw all.Exception ?? new InvalidOperationException(all.Message);
                    records = all.Result;
                }
                result.Result = HolonGraphProjection.Create(records, loadChildren, recursive, maxChildDepth, 0,
                    childrenFromProvider, item => OasisJson.Deserialize<Holon>(OasisJson.Serialize(item)))(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons from NEAR: {ex.Message}", ex);
                if (!continueOnError) return result;
            }
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
        LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0) =>
        await ProjectLoadedHolonAsync(await LoadHolonByProviderKeyAsync(providerKey, version), loadChildren,
            recursive, maxChildDepth, continueOnError, loadChildrenFromProvider);

    public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, int version = 0)
    {
        if (Guid.TryParse(providerKey, out var id))
            return await LoadHolonRecordAsync(id, version);

        var all = await LoadAllHolonRecordsAsync(version);
        if (all.IsError) return new OASISResult<IHolon>(null) { IsError = true, Message = all.Message, Exception = all.Exception };
        var holon = all.Result?.FirstOrDefault(item =>
            item.ProviderUniqueStorageKey != null &&
            item.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.NEAROASIS, out var key) &&
            string.Equals(key, providerKey, StringComparison.Ordinal));
        return new OASISResult<IHolon>(holon)
        {
            Message = holon == null ? $"Holon with NEAR provider key '{providerKey}' was not found." : "Holon loaded from NEAR contract storage."
        };
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
        LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

    public OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, int version = 0) =>
        LoadHolonByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenRecursiveDepth = true) =>
        SaveHolonTreeAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

    public Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon) => SaveHolonRecordAsync(holon);

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true) =>
        SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, loadChildrenRecursiveDepth).GetAwaiter().GetResult();

    public OASISResult<IHolon> SaveHolon(IHolon holon) => SaveHolonRecordAsync(holon).GetAwaiter().GetResult();

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonRecordAsync(id);
    public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).GetAwaiter().GetResult();

    public async Task<OASISResult<bool>> DeleteHolonByIdAsync(Guid id, bool softDelete = true)
    {
        if (softDelete)
        {
            var deleted = await DeleteHolonRecordAsync(id);
            return new OASISResult<bool>(!deleted.IsError && deleted.Result != null)
            {
                IsError = deleted.IsError,
                Message = deleted.Message,
                Exception = deleted.Exception
            };
        }

        var result = new OASISResult<bool>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            await DeleteStoredVersionsAsync($"holon:{id:D}");
            await _sdk.DeleteAsync($"holon:{id:D}");
            result.Result = true;
            result.Message = "Holon removed from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon from NEAR: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) =>
        DeleteHolonByIdAsync(id, softDelete).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        var loaded = await LoadHolonByProviderKeyAsync(providerKey);
        if (loaded.IsError || loaded.Result == null) return loaded;
        return await DeleteHolonRecordAsync(loaded.Result.Id);
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey) =>
        DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

    public async Task<OASISResult<bool>> DeleteHolonByProviderKeyAsync(string providerKey, bool softDelete = true)
    {
        var loaded = await LoadHolonByProviderKeyAsync(providerKey);
        if (loaded.IsError || loaded.Result == null)
            return new OASISResult<bool>(false) { IsError = loaded.IsError, Message = loaded.Message, Exception = loaded.Exception };
        return await DeleteHolonByIdAsync(loaded.Result.Id, softDelete);
    }

    public OASISResult<bool> DeleteHolonByProviderKey(string providerKey, bool softDelete = true) =>
        DeleteHolonByProviderKeyAsync(providerKey, softDelete).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
    {
        var all = await LoadAllHolonRecordsAsync(version);
        var records = all.Result?.ToList() ?? new List<IHolon>();
        if (!all.IsError && version > 0 && loadChildren && loadChildrenRecursiveDepth)
        {
            var current = await LoadAllHolonRecordsAsync();
            if (current.IsError) return current;
            records = current.Result.ToList();
        }
        if (!all.IsError && holonType != HolonType.All)
            all.Result = all.Result?.Where(holon => holon.HolonType == holonType).ToList();
        if (!all.IsError)
        {
            try
            {
                // Keep the full record set for descendants: filtering selects roots, not child types.
                var project = HolonGraphProjection.Create(records, loadChildren, recursive, maxChildDepth,
                    loadChildrenRecursiveDepthInt, loadChildrenRecursiveDepth,
                    item => OasisJson.Deserialize<Holon>(OasisJson.Serialize(item)));
                all.Result = all.Result.Select(project).ToList();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref all, $"Error projecting NEAR holon graph: {ex.Message}", ex); }
        }
        return all;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true,
        bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0) =>
        LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError,
            loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();
}
