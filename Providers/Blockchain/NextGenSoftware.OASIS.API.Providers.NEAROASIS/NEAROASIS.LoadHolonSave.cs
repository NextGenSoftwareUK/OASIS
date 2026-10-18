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
    private async Task<OASISResult<IHolon>> SaveHolonTreeAsync(IHolon holon, bool saveChildren,
        bool recursive, int maxChildDepth, int depth, bool continueOnError)
    {
        var saved = await SaveHolonForestAsync(new[] { holon }, saveChildren, recursive, maxChildDepth, depth, continueOnError);
        return new OASISResult<IHolon>(saved.Result?.FirstOrDefault())
        {
            IsError = saved.IsError, IsSaved = saved.IsSaved, Message = saved.Message, Exception = saved.Exception
        };
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonForestAsync(IEnumerable<IHolon> holons,
        bool saveChildren, bool recursive, int maxChildDepth, int depth, bool continueOnError)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        (List<IHolon> Roots, List<PlannedHolonWrite> Nodes) plan;
        try
        {
            if (maxChildDepth < 0 || depth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
            var children = saveChildren && (depth == 0 || recursive) && (maxChildDepth == 0 || depth + 1 < maxChildDepth);
            // OASIS save depth includes the root; the shared plan counts edges.
            plan = HolonGraphWritePlan.Create(holons, children, recursive, maxChildDepth == 0 ? 0 : maxChildDepth - 1, depth);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Invalid NEAR save graph: {ex.Message}", ex);
            return result;
        }
        var blocked = new HashSet<Guid>();
        var acknowledged = new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        foreach (var node in plan.Nodes)
        {
            if (blocked.Contains(node.Snapshot.ParentHolonId))
            {
                blocked.Add(node.Snapshot.Id);
                continue;
            }
            var saved = await SaveHolonRecordAsync(node.Snapshot);
            if (saved.IsError)
            {
                blocked.Add(node.Snapshot.Id);
                OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
                if (!continueOnError) break;
                continue;
            }
            // Publish only identities/stamps whose current-record write was acknowledged.
            node.Source.Id = node.Snapshot.Id;
            node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
            node.Source.ProviderUniqueStorageKey = node.Snapshot.ProviderUniqueStorageKey;
            node.Source.Version = node.Snapshot.Version;
            node.Source.VersionId = node.Snapshot.VersionId;
            node.Source.PreviousVersionId = node.Snapshot.PreviousVersionId;
            acknowledged.Add(node.Source);
        }
        result.Result = plan.Roots.Where(acknowledged.Contains).ToList();
        result.IsSaved = !result.IsError;
        result.Message = $"Acknowledged {acknowledged.Count} of {plan.Nodes.Count} selected NEAR graph writes. " + result.Message;
        return result;
    }

    private async Task<List<IHolon>> LoadChildTreeAsync(Guid parentId, HolonType type, bool recursive,
        int maxChildDepth, int depth, int version = 0)
    {
        var all = await LoadAllHolonRecordsAsync();
        if (all.IsError) throw all.Exception ?? new InvalidOperationException(all.Message);
        var records = all.Result.ToList();
        var roots = all;
        if (version != 0)
        {
            roots = await LoadAllHolonRecordsAsync(version);
            if (roots.IsError) throw roots.Exception ?? new InvalidOperationException(roots.Message);
        }
        var direct = roots.Result.Where(item => item.ParentHolonId == parentId &&
            (type == HolonType.All || item.HolonType == type)).ToList();
        var project = HolonGraphProjection.Create(records, recursive, true, maxChildDepth, checked(depth + 1), true,
            item => OasisJson.Deserialize<Holon>(OasisJson.Serialize(item)));
        return direct.Select(project).ToList();
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
                maxChildDepth, loadChildrenRecursiveDepthInt, version);
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
        var parent = await LoadHolonByProviderKeyAsync(parentProviderKey);
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
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true)
    {
        return await SaveHolonForestAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true) =>
        SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError,
            loadChildrenRecursiveDepth).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) =>
        SaveAvatarDetailRecordAsync(avatarDetail);

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
        SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
    {
        var all = await LoadAllAvatarDetailRecordsAsync(version);
        if (all.IsError) return new OASISResult<IAvatarDetail>(null) { IsError=true, Message=all.Message, Exception=all.Exception };
        return new OASISResult<IAvatarDetail>(all.Result.FirstOrDefault(item => string.Equals(item.Email,email,StringComparison.OrdinalIgnoreCase)));
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) =>
        LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
    {
        var all = await LoadAllAvatarDetailRecordsAsync(version);
        if (all.IsError) return new OASISResult<IAvatarDetail>(null) { IsError=true, Message=all.Message, Exception=all.Exception };
        return new OASISResult<IAvatarDetail>(all.Result.FirstOrDefault(item => string.Equals(item.Username,username,StringComparison.OrdinalIgnoreCase)));
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) =>
        LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) =>
        LoadAllAvatarDetailRecordsAsync(version);

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
        LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();
}
