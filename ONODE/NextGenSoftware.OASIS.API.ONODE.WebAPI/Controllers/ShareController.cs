using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/share")]
    public class ShareController : OASISControllerBase
    {
        private HolonManager _holonManager;

        public ShareController()
        {
        }

        private HolonManager HolonManager
        {
            get
            {
                if (_holonManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _holonManager = new HolonManager(result.Result);
                }

                return _holonManager;
            }
        }

        /// <summary>
        /// Share a given holon with a given avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="holonId"></param>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("share-holon/{holonId:guid}/{avatarId:guid}")]
        public async Task<OASISResult<bool>> ShareHolon(Guid holonId, Guid avatarId)
        {
            try
            {
                // Authorization grants must reflect a real persistence outcome.
                return await ShareHolonInternalAsync(holonId, new[] { avatarId });
            }
            catch (Exception exception)
            {
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_FAILED", Message = "Sharing could not be completed.",
                    Exception = exception };
            }
        }

        /// <summary>
        /// Share a given holon with a groups of avatars. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="holonId"></param>
        /// <param name="avatarIds"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("share-holon/{holonId:guid}/many/{avatarIds}")]
        public async Task<OASISResult<bool>> ShareHolon(Guid holonId, string avatarIds)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(avatarIds))
                {
                    OASISErrorHandling.HandleError(ref result, "avatarIds cannot be null or empty.");
                    return result;
                }

                Guid[] parsedAvatarIds = avatarIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Guid.Parse)
                    .ToArray();

                return await ShareHolonInternalAsync(holonId, parsedAvatarIds);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error parsing avatarIds. Expected comma separated GUIDs. Reason: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// One-off maintenance (Wizard only): writes the per-recipient index flags for holons shared before the
        /// index existed, so they appear in "shared with me". Safe to run more than once.
        /// </summary>
        [Authorize]
        [HttpPost("backfill-share-index")]
        public async Task<OASISResult<int>> BackfillShareIndex()
        {
            var result = new OASISResult<int>();
            if (Avatar?.AvatarType?.Value != AvatarType.Wizard)
            {
                OASISErrorHandling.HandleError(ref result, "Unauthorized. Only Wizards can run the share index backfill.");
                return result;
            }

            try
            {
                var all = await HolonManager.LoadAllHolonsAsync(HolonType.All, loadChildren: false, recursive: false);
                if (all.IsError && all.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unable to load holons. Reason: {all.Message}");
                    return result;
                }

                var updated = 0;
                var failed = new List<Guid>();
                foreach (var holon in (all.Result ?? Enumerable.Empty<IHolon>()).Where(HolonAccess.NeedsShareIndex))
                {
                    HolonAccess.RecordShares(holon, HolonAccess.GetSharedAvatarIds(holon));
                    var save = await HolonManager.SaveHolonAsync(holon, AvatarId);
                    if (save == null || save.IsError) failed.Add(holon.Id);
                    else updated++;
                }

                result.Result = updated;
                result.Message = failed.Count == 0
                    ? $"Indexed {updated} shared holon(s)."
                    : $"Indexed {updated} shared holon(s); {failed.Count} failed: {string.Join(", ", failed)}";
                result.IsError = failed.Count > 0;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Share index backfill failed. Reason: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Who may do what with a holon, as a whole and field by field. Needs Manage on the holon (the creator always has it)
        /// or a Wizard. A grantee is an avatar id, a group id or <c>"public"</c>.
        /// Body: <c>{"grants":{"&lt;grantee&gt;":["read","write"]},"inherit":{"&lt;grantee&gt;":["read"]},
        /// "fields":{"&lt;grantee&gt;":{"metadata.username":["read"],"name":["read","write"]}}}</c>.
        /// An empty list removes that grantee (or that field); anything not listed is unchanged. An empty body marks the
        /// holon restricted (creating children under it then needs addChildren). Permission names: read, write, delete,
        /// addChildren, manage; fields accept read, write, delete only. Field names: name, description, a property such as
        /// username, metadata.&lt;key&gt;, metadata.* or *.
        /// </summary>
        [Authorize]
        [HttpPost("permissions/{holonId:guid}")]
        public async Task<OASISResult<bool>> SetPermissions(Guid holonId, [FromBody] SetPermissionsRequest request)
        {
            try
            {
                var change = request?.ToChange(out var error);
                if (change == null || error != null)
                    return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_PERMISSIONS_INPUT_INVALID",
                        Message = error ?? "A request body is required." };
                return await HolonAccess.SetPermissionsAsync(HolonManager, holonId, change, Avatar);
            }
            catch (Exception exception)
            {
                return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_PERMISSIONS_FAILED",
                    Message = "Permissions could not be changed.", Exception = exception };
            }
        }

        /// <summary>The stored permissions of a holon (grants, inherit, fields). Visible to the creator, anyone with Manage, or a Wizard.</summary>
        [Authorize]
        [HttpGet("permissions/{holonId:guid}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetPermissions(Guid holonId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var loaded = await HolonManager.LoadHolonAsync(holonId, loadChildren: false);
                if (loaded == null || loaded.IsError || loaded.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unable to load holon {holonId}. Reason: {loaded?.Message}");
                    return result;
                }
                if (Avatar?.AvatarType?.Value != AvatarType.Wizard
                    && !await HolonManager.AllowsAsync(loaded.Result, AvatarId, HolonPermission.Manage))
                {
                    OASISErrorHandling.HandleError(ref result, "Unauthorized. Only the creator, or an avatar granted Manage, can see who has access.");
                    return result;
                }
                result.Result = SetPermissionsRequest.Describe(HolonAccess.GetPermissions(loaded.Result));
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unable to read permissions. Reason: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Creates a group (or role). Grant permissions to the group's id like an avatar's, and change who is in it to change
        /// everyone's access at once. The creator owns the group and its membership.
        /// Body: <c>{"name":"Speech therapists","members":["&lt;avatarId&gt;"]}</c>. Returns the group holon (use its id as a grantee).
        /// </summary>
        [Authorize]
        [HttpPost("groups")]
        public async Task<OASISResult<IHolon>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                var members = new List<Guid>();
                foreach (var m in request?.Members ?? new List<string>())
                {
                    if (!Guid.TryParse(m, out var id))
                        return new OASISResult<IHolon> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_INPUT_INVALID",
                            Message = $"'{m}' is not an avatar id." };
                    members.Add(id);
                }
                return await HolonAccess.CreateGroupAsync(HolonManager, request?.Name, members, Avatar);
            }
            catch (Exception exception)
            {
                return new OASISResult<IHolon> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_FAILED",
                    Message = "The group could not be created.", Exception = exception };
            }
        }

        /// <summary>Adds and removes members of a group. Body: <c>{"add":["&lt;avatarId&gt;"],"remove":["&lt;avatarId&gt;"]}</c>. Needs Manage on the group.</summary>
        [Authorize]
        [HttpPost("groups/{groupId:guid}/members")]
        public async Task<OASISResult<bool>> SetGroupMembers(Guid groupId, [FromBody] GroupMembersRequest request)
        {
            try
            {
                bool Parse(List<string> source, out List<Guid> ids)
                {
                    ids = new List<Guid>();
                    foreach (var m in source ?? new List<string>())
                    {
                        if (!Guid.TryParse(m, out var id)) return false;
                        ids.Add(id);
                    }
                    return true;
                }
                if (!Parse(request?.Add, out var add) || !Parse(request?.Remove, out var remove))
                    return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_INPUT_INVALID",
                        Message = "Every member must be an avatar id." };
                return await HolonAccess.SetGroupMembersAsync(HolonManager, groupId, add, remove, Avatar);
            }
            catch (Exception exception)
            {
                return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_FAILED",
                    Message = "Group membership could not be changed.", Exception = exception };
            }
        }

        private Task<OASISResult<bool>> ShareHolonInternalAsync(Guid holonId, IEnumerable<Guid> avatarIds) =>
            HolonAccess.ShareAsync(HolonManager, holonId, avatarIds, Avatar);

        /// <summary>
        /// Lists holons other avatars have shared with the signed-in avatar. Uses the per-recipient index
        /// written when sharing, then re-checks access against the share list itself.
        /// </summary>
        [Authorize]
        [HttpGet("shared-with-me")]
        public async Task<OASISResult<IEnumerable<Holon>>> SharedWithMe()
        {
            var result = new OASISResult<IEnumerable<Holon>>();
            if (AvatarId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Unauthorized. Sign in to see holons shared with you.");
                return result;
            }

            try
            {
                // avatarId: Guid.Empty skips the creator-only visibility filter; HolonAccess applies the real rule.
                var loaded = await HolonManager.LoadHolonsByMetaDataAsync(
                    HolonAccess.SharedWithIndexKey(AvatarId), HolonAccess.SharedWithIndexValue,
                    HolonType.All, loadChildren: false, recursive: false, avatarId: Guid.Empty);
                if (loaded.IsError && loaded.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons shared with you. Reason: {loaded.Message}");
                    return result;
                }

                var shared = (loaded.Result ?? Enumerable.Empty<IHolon>())
                    .Where(h => h.CreatedByAvatarId != AvatarId && HolonAccess.GetSharedAvatarIds(h).Contains(AvatarId));
                result.Result = Mapper.Convert<IHolon, Holon>(shared)?.ToList() ?? new List<Holon>();
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons shared with you. Reason: {ex.Message}", ex);
                return result;
            }
        }
    }
    /// <summary>
    /// Body of <c>POST api/share/permissions/{holonId}</c> and of the avatar sharing endpoints. A grantee (avatar id, group id
    /// or "public") maps to permission names; the field maps add a field name level.
    /// </summary>
    public class SetPermissionsRequest
    {
        public Dictionary<string, string[]> Grants { get; set; } = new Dictionary<string, string[]>();
        public Dictionary<string, string[]> Inherit { get; set; } = new Dictionary<string, string[]>();
        /// <summary>Grantee to field name to permission names (read, write, delete).</summary>
        public Dictionary<string, Dictionary<string, string[]>> Fields { get; set; } = new Dictionary<string, Dictionary<string, string[]>>();
        /// <summary>Like <see cref="Fields"/>, and also applying to every holon below this one.</summary>
        public Dictionary<string, Dictionary<string, string[]>> InheritFields { get; set; } = new Dictionary<string, Dictionary<string, string[]>>();

        /// <summary>The change this body asks for, or the reason it is invalid.</summary>
        public HolonPermissionChange ToChange(out string error)
        {
            string problem = null;
            Dictionary<string, HolonPermission> Map(Dictionary<string, string[]> source)
            {
                var map = new Dictionary<string, HolonPermission>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in source ?? new Dictionary<string, string[]>())
                {
                    if (!HolonPermissions.TryParse(entry.Value, out var flags))
                    { problem ??= $"Invalid permission names for '{entry.Key}'. Use read, write, delete, addChildren, manage."; continue; }
                    map[entry.Key] = flags;
                }
                return map;
            }
            Dictionary<string, IReadOnlyDictionary<string, HolonPermission>> FieldMap(Dictionary<string, Dictionary<string, string[]>> source)
            {
                var map = new Dictionary<string, IReadOnlyDictionary<string, HolonPermission>>(StringComparer.OrdinalIgnoreCase);
                foreach (var grantee in source ?? new Dictionary<string, Dictionary<string, string[]>>()) map[grantee.Key] = Map(grantee.Value);
                return map;
            }

            var change = new HolonPermissionChange { Grants = Map(Grants), Inherit = Map(Inherit), Fields = FieldMap(Fields), InheritFields = FieldMap(InheritFields) };
            error = problem ?? change.Validate();
            return change;
        }

        /// <summary>A stored permission set as the API shows it.</summary>
        public static Dictionary<string, object> Describe(HolonPermissionSet set)
        {
            string[] Names(HolonPermission p) => HolonPermissions.NamesOf(p).ToArray();
            Dictionary<string, object> Fields(IReadOnlyDictionary<string, IReadOnlyDictionary<string, HolonPermission>> source) =>
                source.ToDictionary(g => g.Key, g => (object)g.Value.ToDictionary(f => f.Key, f => Names(f.Value)));
            return new Dictionary<string, object>
            {
                ["grants"] = set.Grants.ToDictionary(g => g.Key, g => Names(g.Value)),
                ["inherit"] = set.Inherit.ToDictionary(g => g.Key, g => Names(g.Value)),
                ["fields"] = Fields(set.Fields),
                ["inheritFields"] = Fields(set.InheritFields),
            };
        }
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; }
        public List<string> Members { get; set; } = new List<string>();
    }

    public class GroupMembersRequest
    {
        public List<string> Add { get; set; } = new List<string>();
        public List<string> Remove { get; set; } = new List<string>();
    }
}
