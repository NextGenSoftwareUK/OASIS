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
        /// What each avatar may do with a holon, set by anyone with Manage on it (the creator always has it) or a Wizard.
        /// Body: <c>{"grants":{"&lt;avatarId&gt;":["read","write","delete","addChildren","manage"]}}</c>.
        /// An empty list removes that avatar; avatars not listed are unchanged; an empty "grants" object marks the
        /// holon as restricted (creating children under it then needs addChildren).
        /// </summary>
        [Authorize]
        [HttpPost("permissions/{holonId:guid}")]
        public async Task<OASISResult<bool>> SetPermissions(Guid holonId, [FromBody] SetPermissionsRequest request)
        {
            try
            {
                var changes = new Dictionary<Guid, HolonPermission>();
                foreach (var grant in request?.Grants ?? new Dictionary<string, string[]>())
                {
                    if (!Guid.TryParse(grant.Key, out var avatarId) || !HolonPermissions.TryParse(grant.Value, out var flags))
                        return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_PERMISSIONS_INPUT_INVALID",
                            Message = $"Invalid grant for '{grant.Key}'. Use an avatar id and any of read, write, delete, addChildren, manage." };
                    changes[avatarId] = flags;
                }
                return await HolonAccess.SetPermissionsAsync(HolonManager, holonId, changes, Avatar);
            }
            catch (Exception exception)
            {
                return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_PERMISSIONS_FAILED",
                    Message = "Permissions could not be changed.", Exception = exception };
            }
        }

        /// <summary>The stored permission grants for a holon. Visible to the creator, anyone with Manage, or a Wizard.</summary>
        [Authorize]
        [HttpGet("permissions/{holonId:guid}")]
        public async Task<OASISResult<Dictionary<string, string[]>>> GetPermissions(Guid holonId)
        {
            var result = new OASISResult<Dictionary<string, string[]>>();
            try
            {
                var loaded = await HolonManager.LoadHolonAsync(holonId, loadChildren: false);
                if (loaded == null || loaded.IsError || loaded.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unable to load holon {holonId}. Reason: {loaded?.Message}");
                    return result;
                }
                if (Avatar?.AvatarType?.Value != AvatarType.Wizard
                    && !HolonPermissions.Allows(loaded.Result, AvatarId, HolonPermission.Manage, includePublic: false))
                {
                    OASISErrorHandling.HandleError(ref result, "Unauthorized. Only the creator, or an avatar granted Manage, can see who has access.");
                    return result;
                }
                result.Result = HolonAccess.GetPermissions(loaded.Result)
                    .ToDictionary(g => g.Key.ToString("D"), g => HolonPermissions.NamesOf(g.Value).ToArray());
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unable to read permissions. Reason: {ex.Message}", ex);
                return result;
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
    /// <summary>Body of <c>POST api/share/permissions/{holonId}</c>: avatar id to permission names.</summary>
    public class SetPermissionsRequest
    {
        public Dictionary<string, string[]> Grants { get; set; } = new Dictionary<string, string[]>();
    }
}
