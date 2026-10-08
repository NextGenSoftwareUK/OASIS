using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
        LoadAvatarDetailRecordAsync(id);

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
        LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int version = 0)
    {
        var result = await LoadHolonRecordAsync(id);
        if (!result.IsError && result.Result != null && loadChildren)
        {
            try { result.Result.Children = await LoadChildTreeAsync(id, HolonType.All, recursive, maxChildDepth, 0); }
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

    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0) =>
        LoadHolonByProviderKeyAsync(providerKey, version);

    public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, int version = 0)
    {
        if (Guid.TryParse(providerKey, out var id))
            return await LoadHolonRecordAsync(id);

        var all = await LoadAllHolonRecordsAsync();
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
        var all = await LoadAllHolonRecordsAsync();
        if (!all.IsError && holonType != HolonType.All)
            all.Result = all.Result?.Where(holon => holon.HolonType == holonType).ToList();
        return all;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true,
        bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0) =>
        LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError,
            loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();
}
