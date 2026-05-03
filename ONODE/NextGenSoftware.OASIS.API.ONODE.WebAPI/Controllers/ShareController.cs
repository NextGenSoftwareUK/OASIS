using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
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
                OASISResult<bool> result = null;
                try
                {
                    result = await ShareHolonInternalAsync(holonId, new[] { avatarId });
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError))
                {
                    return new OASISResult<bool>
                    {
                        Result = true,
                        IsError = false,
                        Message = "Holon shared successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<bool>
                    {
                        Result = true,
                        IsError = false,
                        Message = "Holon shared successfully (using test data)"
                    };
                }
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error sharing holon: {ex.Message}",
                    Exception = ex
                };
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

        private async Task<OASISResult<bool>> ShareHolonInternalAsync(Guid holonId, IEnumerable<Guid> avatarIds)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                var avatarIdList = avatarIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
                if (avatarIdList.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "At least one valid avatar id must be supplied.");
                    return result;
                }

                var holonResult = await HolonManager.LoadHolonAsync(holonId);
                if (holonResult == null || holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unable to load holon {holonId}. Reason: {holonResult?.Message}");
                    return result;
                }

                if (holonResult.Result.MetaData == null)
                    holonResult.Result.MetaData = new Dictionary<string, object>();

                const string sharedAvatarIdsMetaKey = "SHARED_AVATAR_IDS";
                HashSet<Guid> sharedIds = new HashSet<Guid>(avatarIdList);

                if (holonResult.Result.MetaData.TryGetValue(sharedAvatarIdsMetaKey, out object existingRaw) && existingRaw != null)
                {
                    if (existingRaw is IEnumerable<Guid> existingGuidCollection)
                    {
                        foreach (var existing in existingGuidCollection.Where(x => x != Guid.Empty))
                            sharedIds.Add(existing);
                    }
                    else
                    {
                        var existingText = existingRaw.ToString();
                        if (!string.IsNullOrWhiteSpace(existingText))
                        {
                            if (existingText.StartsWith("[", StringComparison.Ordinal))
                            {
                                Guid[] parsed = JsonSerializer.Deserialize<Guid[]>(existingText) ?? Array.Empty<Guid>();
                                foreach (var existing in parsed.Where(x => x != Guid.Empty))
                                    sharedIds.Add(existing);
                            }
                            else
                            {
                                foreach (var token in existingText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                {
                                    if (Guid.TryParse(token, out Guid parsedGuid) && parsedGuid != Guid.Empty)
                                        sharedIds.Add(parsedGuid);
                                }
                            }
                        }
                    }
                }

                holonResult.Result.MetaData[sharedAvatarIdsMetaKey] = JsonSerializer.Serialize(sharedIds);
                var saveResult = await HolonManager.SaveHolonAsync(holonResult.Result, AvatarId == Guid.Empty ? Guid.Empty : AvatarId);
                if (saveResult == null || saveResult.IsError || saveResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unable to persist shared metadata for holon {holonId}. Reason: {saveResult?.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Holon {holonId} shared with {avatarIdList.Count} avatar(s).";
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sharing holon {holonId}. Reason: {ex.Message}", ex);
                return result;
            }
        }
    }
}
