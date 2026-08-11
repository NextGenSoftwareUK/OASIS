using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class AvatarProfileController
    {
        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByIdAsync(id));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonById(id);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByUsernameAsync(username));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByUsername(username);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByEmailAsync(email));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByEmail(email);
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar()
        {
            /* JwtMiddleware sets HttpContext.Items["Avatar"] from the JWT "id" claim. AvatarManager.LoggedInAvatar is process-global and can be another user (last full login) — never use it alone for JWT-authenticated requests. */
            var avatar = Avatar ?? AvatarManager.LoggedInAvatar;
            if (avatar == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { Result = avatar });
        }

         /// <summary>
        /// Gets the logged-in avatar with XP (AvatarDetail). Used by STAR API GET /api/avatar/current so clients can refresh XP after beam-in.
        /// </summary>
        [Authorize]
        [HttpGet("get-logged-in-avatar-with-xp")]
        public async Task<OASISHttpResponseMessage<LoggedInAvatarResponse>> GetLoggedInAvatarWithXp()
        {
            var avatar = Avatar ?? AvatarManager.LoggedInAvatar;
            if (avatar == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            var detailResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatar.Id);
            if (detailResult.IsError || detailResult.Result == null)
            {
                _logger.LogWarning(
                    "[Quest] GetLoggedInAvatarWithXp: LoadAvatarDetailAsync failed for avatar {AvatarId}: {Message}. Returning error (do not return HTTP 200 with xp=0 — that makes clients think profile refresh succeeded with empty XP/quest).",
                    avatar.Id,
                    detailResult.Message ?? "(no message)");
                return HttpResponseHelper.FormatResponse(
                    new OASISResult<LoggedInAvatarResponse>
                    {
                        IsError = true,
                        Message = detailResult.Message ?? "Failed to load avatar detail (XP and active quest are stored on AvatarDetail).",
                        DetailedMessage = detailResult.DetailedMessage
                    },
                    HttpStatusCode.BadRequest);
            }

            var detail = detailResult.Result;
            var xp = detail.XP;
            var activeQuestId = detail.ActiveQuestId;
            var activeObjectiveId = detail.ActiveObjectiveId;
            _logger.LogInformation("[Quest] GetLoggedInAvatarWithXp loaded detail for avatar {AvatarId}: XP={Xp}, ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId} (from storage)", avatar.Id, xp, activeQuestId, activeObjectiveId);
            _logger.LogInformation("[Quest] GetLoggedInAvatarWithXp returning for avatar {AvatarId}: XP={Xp}, ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", avatar.Id, xp, activeQuestId, activeObjectiveId);
            StarLog($"GetLoggedInAvatarWithXp returning: ActiveQuestId={activeQuestId}, ActiveObjectiveId={activeObjectiveId}");
            var response = new LoggedInAvatarResponse
            {
                Id = avatar.Id,
                Username = avatar.Username ?? string.Empty,
                Email = avatar.Email ?? string.Empty,
                FirstName = avatar.FirstName ?? string.Empty,
                LastName = avatar.LastName ?? string.Empty,
                XP = xp,
                ActiveQuestId = activeQuestId,
                ActiveObjectiveId = activeObjectiveId
            };
            return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { Result = response });
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetLoggedInAvatar();
        }

        /// <summary>
        /// Add experience points to the logged-in avatar (e.g. from game actions like killing monsters). Only works for logged-in users.
        /// Amount 0 is allowed: returns current XP without changing it (used by clients to refresh XP cache after beam-in).
        /// </summary>
        /// <param name="request">Body with amount (non-negative integer).</param>
        /// <returns>New total XP after adding (or current XP if amount is 0).</returns>
        [Authorize]
        [HttpPost("add-xp")]
        public async Task<OASISHttpResponseMessage<AddXpResponse>> AddXp([FromBody] AddXpRequest request)
        {
            if (request == null || request.Amount < 0)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = "Amount must be a non-negative integer." }, HttpStatusCode.BadRequest);

            var avatarId = Avatar?.Id ?? Guid.Empty;
            if (avatarId == Guid.Empty)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);

            var loadResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." }, HttpStatusCode.BadRequest);

            var detail = loadResult.Result;
            if (request.Amount > 0)
            {
                detail.XP = detail.XP + request.Amount;
                if (detail.XP < 0)
                    detail.XP = 0;
                var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
                if (updateResult.IsError)
                    return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = updateResult.Message ?? "Failed to update avatar XP." }, HttpStatusCode.BadRequest);
            }

            var newTotal = detail.XP;
            return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { Result = new AddXpResponse { NewTotal = newTotal }, IsError = false });
        }

        /// <summary>
        /// Sets the active quest and objective for the logged-in avatar (tracker state). Persisted on AvatarDetail so they are restored after beam-in.
        /// </summary>
        [Authorize]
        [HttpPost("set-active-quest")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<bool>> SetActiveQuest([FromBody] SetActiveQuestRequest request)
        {
            _logger.LogInformation("[Quest] SetActiveQuest called: ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", request?.ActiveQuestId, request?.ActiveObjectiveId);
            StarLog($"SetActiveQuest called: ActiveQuestId={request?.ActiveQuestId}, ActiveObjectiveId={request?.ActiveObjectiveId}");

            var avatarId = Avatar?.Id ?? Guid.Empty;
            if (avatarId == Guid.Empty)
            {
                _logger.LogWarning("[Quest] SetActiveQuest rejected: not authenticated.");
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            }

            var loadResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
            {
                _logger.LogWarning("[Quest] SetActiveQuest failed to load detail: {Message}", loadResult.Message);
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." }, HttpStatusCode.BadRequest);
            }

            var detail = loadResult.Result;
            detail.ActiveQuestId = request?.ActiveQuestId;
            detail.ActiveObjectiveId = request?.ActiveObjectiveId;

            var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
            if (updateResult.IsError)
            {
                _logger.LogWarning("[Quest] SetActiveQuest update failed: {Message}", updateResult.Message);
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = updateResult.Message ?? "Failed to update active quest." }, HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("[Quest] SetActiveQuest saved for avatar {AvatarId}: ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", avatarId, detail.ActiveQuestId, detail.ActiveObjectiveId);
            StarLog($"SetActiveQuest saved OK for avatar {avatarId}: ActiveQuestId={detail.ActiveQuestId}, ActiveObjectiveId={detail.ActiveObjectiveId}");
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { Result = true, IsError = false });
        }
    }
}
