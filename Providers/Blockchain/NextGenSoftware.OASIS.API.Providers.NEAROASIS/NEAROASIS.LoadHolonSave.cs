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
    private async Task<OASISResult<IHolon>> SaveHolonTreeAsync(IHolon holon, bool saveChildren,
        bool recursive, int maxChildDepth, int depth, bool continueOnError)
    {
        var saved = await SaveHolonRecordAsync(holon);
        if (saved.IsError || !saveChildren || holon.Children == null)
            return saved;

        var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
        if (reachedDepth) return saved;

        foreach (var child in holon.Children)
        {
            child.ParentHolonId = holon.Id;
            var childSaved = recursive
                ? await SaveHolonTreeAsync(child, true, true, maxChildDepth, depth + 1, continueOnError)
                : await SaveHolonRecordAsync(child);
            if (!childSaved.IsError) continue;
            OASISErrorHandling.HandleError(ref saved, $"Error saving child holon {child.Id}: {childSaved.Message}", childSaved.Exception);
            if (!continueOnError) break;
        }
        return saved;
    }

    private async Task<List<IHolon>> LoadChildTreeAsync(Guid parentId, HolonType type, bool recursive,
        int maxChildDepth, int depth)
    {
        var all = await LoadAllHolonRecordsAsync();
        if (all.IsError) throw all.Exception ?? new InvalidOperationException(all.Message);
        var direct = all.Result.Where(item => item.ParentHolonId == parentId &&
            (type == HolonType.All || item.HolonType == type)).ToList();
        var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
        if (recursive && !reachedDepth)
        {
            foreach (var child in direct)
                child.Children = await LoadChildTreeAsync(child.Id, HolonType.All, true, maxChildDepth, depth + 1);
        }
        return direct;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true,
        int loadChildrenRecursiveDepthInt = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            result.Result = await LoadChildTreeAsync(parentId, holonType, loadChildren && recursive,
                maxChildDepth, loadChildrenRecursiveDepthInt);
            result.Message = "Child holons loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading NEAR child holons: {ex.Message}", ex); }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType holonType,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0) =>
        LoadHolonsForParentAsync(parentId, holonType, loadChildren, recursive, maxChildDepth, version,
            continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string parentProviderKey,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true,
        int loadChildrenRecursiveDepthInt = 0)
    {
        var parent = await LoadHolonByProviderKeyAsync(parentProviderKey, version);
        if (parent.IsError || parent.Result == null)
            return new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()) { IsError = parent.IsError, Message = parent.Message, Exception = parent.Exception };
        return await LoadHolonsForParentAsync(parent.Result.Id, holonType, loadChildren, recursive, maxChildDepth,
            version, continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt);
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string parentProviderKey,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true,
        int loadChildrenRecursiveDepthInt = 0) =>
        LoadHolonsForParentAsync(parentProviderKey, holonType, loadChildren, recursive, maxChildDepth,
            version, continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        if (holons == null)
        {
            OASISErrorHandling.HandleError(ref result, "Holons are required.");
            return result;
        }

        var saved = new List<IHolon>();
        foreach (var holon in holons)
        {
            var item = await SaveHolonTreeAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);
            if (item.IsError)
            {
                OASISErrorHandling.HandleError(ref result, item.Message, item.Exception);
                if (!continueOnError) break;
            }
            else if (item.Result != null) saved.Add(item.Result);
        }
        result.Result = saved;
        result.IsSaved = !result.IsError;
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true) =>
        SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, version, continueOnError,
            loadChildrenRecursiveDepth).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) =>
        SaveAvatarDetailRecordAsync(avatarDetail);

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
        SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
    {
        var avatar = await LoadAvatarByEmailAsync(email, version);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<IAvatarDetail>(null) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await LoadAvatarDetailRecordAsync(avatar.Result.Id);
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) =>
        LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
    {
        var avatar = await LoadAvatarByUsernameAsync(username, version);
        if (avatar.IsError || avatar.Result == null)
            return new OASISResult<IAvatarDetail>(null) { IsError = avatar.IsError, Message = avatar.Message, Exception = avatar.Exception };
        return await LoadAvatarDetailRecordAsync(avatar.Result.Id);
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) =>
        LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) =>
        LoadAllAvatarDetailRecordsAsync();

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
        LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();
}
