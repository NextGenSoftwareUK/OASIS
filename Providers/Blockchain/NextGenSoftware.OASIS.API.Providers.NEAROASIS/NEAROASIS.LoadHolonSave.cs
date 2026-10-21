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
            IsError = saved.IsError, IsSaved = saved.IsSaved, ErrorCode=saved.ErrorCode, Message = saved.Message, Exception = saved.Exception
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
        var publications=plan.Nodes.ToDictionary(node=>node.Source,node=>StoredRecordPublicationSnapshot<IHolon>.Capture(node.Source),ReferenceEqualityComparer.Instance);
        foreach (var node in plan.Nodes)
        {
            if (blocked.Contains(node.Snapshot.ParentHolonId))
            {
                blocked.Add(node.Snapshot.Id);
                continue;
            }
            string receipt=null;
            var saved = await SaveHolonRecordAsync(node.Snapshot,id=>receipt=id,node);
            if (saved.IsError)
            {
                blocked.Add(node.Snapshot.Id);
                OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
                result.ErrorCode=saved.ErrorCode;
                if (!continueOnError||NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(saved.Exception)) break;
                continue;
            }
            // Publish only identities/stamps whose current-record write was acknowledged.
            try {PublishRecordStamp(node.Source,node.Snapshot,publications[node.Source].ValidateUnchanged,receipt);}
            catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Acknowledged NEAR graph write cannot publish caller identity.",cause);break;}
            node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
            if(node.Source is Holon source)source.ChildRoleManifestJson=node.Snapshot.ChildRoleManifestJson;
            acknowledged.Add(node.Source);
        }
        result.Result = plan.Roots.Where(acknowledged.Contains).ToList();
        result.IsSaved = !result.IsError;
        result.Message = $"Acknowledged {acknowledged.Count} of {plan.Nodes.Count} selected NEAR graph writes. " + result.Message;
        return result;
    }

    private static void ValidateCollectionOptions(int version,int maxChildDepth,int curentChildDepth)
    {
        if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
        if(maxChildDepth<0 || curentChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
    }

    private async Task<List<IHolon>> LoadChildTreeAsync(Guid parentId, HolonType type, bool loadChildren,bool recursive,
        int maxChildDepth, int depth,bool loadChildrenFromProvider, int version = 0)
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
        var project = HolonGraphProjection.Create(records, loadChildren, recursive, maxChildDepth, depth, loadChildrenFromProvider,
            item => OasisJson.Deserialize<Holon>(OasisJson.Serialize(item)),rootsAreChildren:true);
        return direct.Select(project).ToList();
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            ValidateCollectionOptions(version,maxChildDepth,curentChildDepth);
            result.Result = await LoadChildTreeAsync(parentId, holonType, loadChildren,recursive,
                maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);
            result.Message = "Child holons loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading NEAR child holons: {ex.Message}", ex); }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType holonType,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
        LoadHolonsForParentAsync(parentId, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth,
            continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string parentProviderKey,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        try { ValidateCollectionOptions(version,maxChildDepth,curentChildDepth); }
        catch(Exception cause) { var invalid=new OASISResult<IEnumerable<IHolon>>();OASISErrorHandling.HandleError(ref invalid,cause.Message,cause);return invalid; }
        var parent = await LoadHolonByProviderKeyAsync(parentProviderKey);
        if (parent.IsError || parent.Result == null)
            return new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()) { IsError = parent.IsError, Message = parent.Message, Exception = parent.Exception };
        return await LoadHolonsForParentAsync(parent.Result.Id, holonType, loadChildren, recursive, maxChildDepth,
            curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string parentProviderKey,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0) =>
        LoadHolonsForParentAsync(parentProviderKey, holonType, loadChildren, recursive, maxChildDepth,
            curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

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
        return await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordIdentityLookup.SelectAsync(email,version,LoadAllAvatarDetailsAsync,record=>record.Email);
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) =>
        LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
    {
        return await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordIdentityLookup.SelectAsync(username,version,LoadAllAvatarDetailsAsync,record=>record.Username);
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) =>
        LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) =>
        LoadAllAvatarDetailRecordsAsync(version);

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
        LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();
}
