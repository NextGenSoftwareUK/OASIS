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
    [Route("api/avatar")]
    [ApiController]
    [Produces("application/json")]
    public class AvatarProfileController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarProfileController(ILogger<AvatarProfileController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        private void StarLog(string message, LogLevel level = LogLevel.Information)
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] {message}";
            _logger.Log(level, "[STAR] {Message}", message);
            try
            {
                var dir = string.IsNullOrEmpty(_env?.ContentRootPath) ? AppContext.BaseDirectory : _env.ContentRootPath;
                if (string.IsNullOrEmpty(dir)) dir = System.IO.Directory.GetCurrentDirectory() ?? ".";
                var path = System.IO.Path.Combine(dir, "star_api.log");
                lock (StarLogLock)
                    System.IO.File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }

        // ── GET ──────────────────────────────────────────────────────────────

        /// <returns></returns>
        [HttpGet("get-terms")]
        public async Task<OASISHttpResponseMessage<string>> GetTerms()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(new OASISResult<string> { Result = OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Terms });

                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                    return TestDataHelper.CreateSuccessResponse<string>("Test Terms and Conditions", "Terms retrieved successfully (using test data)");

                return response;
            }
            catch (Exception ex)
            {
                if (UseTestDataWhenLiveDataNotAvailable)
                    return TestDataHelper.CreateSuccessResponse<string>("Test Terms and Conditions", "Terms retrieved successfully (using test data)");
                return TestDataHelper.CreateErrorResponse<string>($"Error retrieving terms: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        [Authorize]
        [HttpGet("get-avatar-portrait/{id}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitById(Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByIdAsync(id));
        }

        [Authorize]
        [HttpGet("get-avatar-portrait/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitById(id);
        }

        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByUsernameAsync(username));
        }

        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitByUsername(username);
        }

        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByEmailAsync(email));
        }

        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitByEmail(email);
        }

        [Authorize]
        [HttpPost("upload-avatar-portrait")]
        public async Task<OASISHttpResponseMessage<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait)
        {
            if (avatarPortrait.AvatarId != Avatar.Id && avatarPortrait.Username != Avatar.Username && avatarPortrait.Email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await AvatarManager.UploadAvatarPortraitAsync(avatarPortrait.AvatarId, avatarPortrait.Username, avatarPortrait.Email, avatarPortrait.ImageBase64));
        }

        [Authorize]
        [HttpPost("upload-avatar-portrait/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UploadAvatarPortrait(avatarPortrait);
        }

        /// <summary>
        /// Get's the avatar's details for a given id.
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar.
        /// </summary>
        [Authorize]
        [HttpGet("get-avatar-detail-by-id/{id:guid}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailAsync(id));
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                    return TestDataHelper.CreateSuccessResponse<IAvatarDetail>(null, "Avatar detail retrieved successfully (using test data)");
                return response;
            }
            catch (Exception ex)
            {
                if (UseTestDataWhenLiveDataNotAvailable)
                    return TestDataHelper.CreateSuccessResponse<IAvatarDetail>(null, "Avatar detail retrieved successfully (using test data)");
                return TestDataHelper.CreateErrorResponse<IAvatarDetail>($"Error retrieving avatar detail: {ex.Message}", ex);
            }
        }

        [Authorize]
        [HttpGet("get-avatar-detail-by-id/{id:guid}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetail(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetail(id);
        }

        [Authorize]
        [HttpGet("get-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailByEmailAsync(email));
        }

        [Authorize]
        [HttpGet("get-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetailByEmail(email);
        }

        [Authorize]
        [HttpGet("get-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailByUsernameAsync(username));
        }

        [Authorize]
        [HttpGet("get-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetailByUsername(username);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarDetailsAsync());
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>(), "Avatar details retrieved successfully (using test data)");
                return response;
            }
            catch (Exception ex)
            {
                if (UseTestDataWhenLiveDataNotAvailable)
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>(), "Avatar details retrieved successfully (using test data)");
                return TestDataHelper.CreateErrorResponse<IEnumerable<IAvatarDetail>>($"Error retrieving avatar details: {ex.Message}", ex);
            }
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarDetails();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatar>>> GetAll()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarsAsync());
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatar>>(new List<IAvatar>(), "Avatars retrieved successfully (using test data)");
                return response;
            }
            catch (Exception ex)
            {
                if (UseTestDataWhenLiveDataNotAvailable)
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatar>>(new List<IAvatar>(), "Avatars retrieved successfully (using test data)");
                return TestDataHelper.CreateErrorResponse<IEnumerable<IAvatar>>($"Error retrieving avatars: {ex.Message}", ex);
            }
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatar>>> GetAll(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAll();
        }

        [Authorize]
        [HttpGet("get-all-avatar-names/{includeUsernames}/{includeIds}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool includeUsernames = true, bool includeIds = true)
        {
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarNamesAsync(includeUsernames, includeIds));
        }

        [Authorize]
        [HttpGet("get-all-avatar-names/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool includeUsernames, bool includeIds, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarNames(includeUsernames, includeIds);
        }

        [Authorize]
        [HttpGet("get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}")]
        public async Task<OASISHttpResponseMessage<Dictionary<string, List<string>>>> GetAllAvatarNamesGroupedByName(bool includeUsernames = true, bool includeIds = true)
        {
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarNamesGroupedByNameAsync(includeUsernames, includeIds));
        }

        [Authorize]
        [HttpGet("get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<Dictionary<string, List<string>>>> GetAllAvatarNamesGroupedByName(bool includeUsernames, bool includeIds, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarNamesGroupedByName(includeUsernames, includeIds);
        }

        [Authorize]
        [HttpGet("get-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetById(Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarAsync(id));
        }

        [Authorize]
        [HttpGet("get-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetById(id);
        }

        [Authorize]
        [HttpGet("get-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            try
            {
                return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarAsync(username));
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = ex.Message }, HttpStatusCode.InternalServerError);
            }
        }

        [Authorize]
        [HttpGet("get-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetByUsername(username);
        }

        [Authorize]
        [HttpGet("get-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarByEmailAsync(email));
        }

        [Authorize]
        [HttpGet("get-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetByUsername(email);
        }

        // ── UMA ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByIdAsync(id));
        }

        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonById(id);
        }

        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByUsernameAsync(username));
        }

        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByUsername(username);
        }

        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByEmailAsync(email));
        }

        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByEmail(email);
        }

        [Authorize]
        [HttpGet("get-logged-in-avatar")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar()
        {
            var avatar = Avatar ?? AvatarManager.LoggedInAvatar;
            if (avatar == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { Result = avatar });
        }

        /// <summary>
        /// Gets the logged-in avatar with XP (AvatarDetail).
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
                _logger.LogWarning("[Quest] GetLoggedInAvatarWithXp: LoadAvatarDetailAsync failed for avatar {AvatarId}: {Message}.", avatar.Id, detailResult.Message ?? "(no message)");
                return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { IsError = true, Message = detailResult.Message ?? "Failed to load avatar detail.", DetailedMessage = detailResult.DetailedMessage }, HttpStatusCode.BadRequest);
            }
            var detail = detailResult.Result;
            StarLog($"GetLoggedInAvatarWithXp returning: ActiveQuestId={detail.ActiveQuestId}, ActiveObjectiveId={detail.ActiveObjectiveId}");
            var response = new LoggedInAvatarResponse
            {
                Id = avatar.Id,
                Username = avatar.Username ?? string.Empty,
                Email = avatar.Email ?? string.Empty,
                FirstName = avatar.FirstName ?? string.Empty,
                LastName = avatar.LastName ?? string.Empty,
                XP = detail.XP,
                ActiveQuestId = detail.ActiveQuestId,
                ActiveObjectiveId = detail.ActiveObjectiveId
            };
            return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { Result = response });
        }

        [Authorize]
        [HttpGet("get-logged-in-avatar/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetLoggedInAvatar();
        }

        /// <summary>
        /// Add experience points to the logged-in avatar. Amount 0 returns current XP without changing it.
        /// </summary>
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
                if (detail.XP < 0) detail.XP = 0;
                var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
                if (updateResult.IsError)
                    return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = updateResult.Message ?? "Failed to update avatar XP." }, HttpStatusCode.BadRequest);
            }
            return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { Result = new AddXpResponse { NewTotal = detail.XP }, IsError = false });
        }

        /// <summary>
        /// Sets the active quest and objective for the logged-in avatar.
        /// </summary>
        [Authorize]
        [HttpPost("set-active-quest")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<bool>> SetActiveQuest([FromBody] SetActiveQuestRequest request)
        {
            StarLog($"SetActiveQuest called: ActiveQuestId={request?.ActiveQuestId}, ActiveObjectiveId={request?.ActiveObjectiveId}");
            var avatarId = Avatar?.Id ?? Guid.Empty;
            if (avatarId == Guid.Empty)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            var loadResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." }, HttpStatusCode.BadRequest);
            var detail = loadResult.Result;
            detail.ActiveQuestId = request?.ActiveQuestId;
            detail.ActiveObjectiveId = request?.ActiveObjectiveId;
            var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
            if (updateResult.IsError)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = updateResult.Message ?? "Failed to update active quest." }, HttpStatusCode.BadRequest);
            StarLog($"SetActiveQuest saved OK for avatar {avatarId}: ActiveQuestId={detail.ActiveQuestId}, ActiveObjectiveId={detail.ActiveObjectiveId}");
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { Result = true, IsError = false });
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        [Authorize]
        [HttpPost("search")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams)
        {
            return HttpResponseHelper.FormatResponse(await SearchManager.Instance.SearchAsync(searchParams));
        }

        [Authorize]
        [HttpPost("search/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await SearchAvatar(searchParams);
        }

        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.AddKarmaToAvatarAsync(avatarId, (KarmaTypePositive)Enum.Parse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType), (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc, null);
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.RemoveKarmaFromAvatarAsync(avatarId, (KarmaTypeNegative)Enum.Parse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType), (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc, null);
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        [Authorize]
        [HttpPost("update-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(id);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);
            var existingAvatar = existingAvatarResult.Result;
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.Description)) existingAvatar.Description = avatar.Description;
            if (!string.IsNullOrEmpty(avatar.DID)) existingAvatar.DID = avatar.DID;
            if (!string.IsNullOrEmpty(avatar.DIDPublicKey)) existingAvatar.DIDPublicKey = avatar.DIDPublicKey;
            if (avatar.AcceptTerms.HasValue) existingAvatar.AcceptTerms = avatar.AcceptTerms.Value;
            if (avatar.IsActive.HasValue) existingAvatar.IsActive = avatar.IsActive.Value;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }
            if (avatar.MetaData != null && avatar.MetaData.Count > 0)
            {
                if (existingAvatar.MetaData == null)
                    existingAvatar.MetaData = new Dictionary<string, object>();
                foreach (var kvp in avatar.MetaData)
                    existingAvatar.MetaData[kvp.Key] = kvp.Value;
            }
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        [Authorize]
        [HttpPost("update-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Update(avatar, id);
        }

        [Authorize]
        [HttpPost("update-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarByEmailAsync(email);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);
            var existingAvatar = existingAvatarResult.Result;
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.Description)) existingAvatar.Description = avatar.Description;
            if (!string.IsNullOrEmpty(avatar.DID)) existingAvatar.DID = avatar.DID;
            if (!string.IsNullOrEmpty(avatar.DIDPublicKey)) existingAvatar.DIDPublicKey = avatar.DIDPublicKey;
            if (avatar.AcceptTerms.HasValue) existingAvatar.AcceptTerms = avatar.AcceptTerms.Value;
            if (avatar.IsActive.HasValue) existingAvatar.IsActive = avatar.IsActive.Value;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }
            if (avatar.MetaData != null && avatar.MetaData.Count > 0)
            {
                if (existingAvatar.MetaData == null)
                    existingAvatar.MetaData = new Dictionary<string, object>();
                foreach (var kvp in avatar.MetaData)
                    existingAvatar.MetaData[kvp.Key] = kvp.Value;
            }
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        [Authorize]
        [HttpPost("update-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByEmail(avatar, email);
        }

        [Authorize]
        [HttpPost("update-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(username);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);
            var existingAvatar = existingAvatarResult.Result;
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.Description)) existingAvatar.Description = avatar.Description;
            if (!string.IsNullOrEmpty(avatar.DID)) existingAvatar.DID = avatar.DID;
            if (!string.IsNullOrEmpty(avatar.DIDPublicKey)) existingAvatar.DIDPublicKey = avatar.DIDPublicKey;
            if (avatar.AcceptTerms.HasValue) existingAvatar.AcceptTerms = avatar.AcceptTerms.Value;
            if (avatar.IsActive.HasValue) existingAvatar.IsActive = avatar.IsActive.Value;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }
            if (avatar.MetaData != null && avatar.MetaData.Count > 0)
            {
                if (existingAvatar.MetaData == null)
                    existingAvatar.MetaData = new Dictionary<string, object>();
                foreach (var kvp in avatar.MetaData)
                    existingAvatar.MetaData[kvp.Key] = kvp.Value;
            }
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        [Authorize]
        [HttpPost("update-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByUsername(avatar, username);
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(AvatarDetail avatarDetail, Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailAsync(id, avatarDetail));
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(Guid id, AvatarDetail avatarDetail, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetail(avatarDetail, id);
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByEmailAsync(email, avatarDetail));
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByEmail(avatarDetail, email);
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByUsernameAsync(username, avatarDetail));
        }

        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByUsername(avatarDetail, username);
        }

        [Authorize]
        [HttpDelete("{id:Guid}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarAsync(id));
        }

        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Delete(id);
        }

        [Authorize]
        [HttpDelete("delete-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByUsernameAsync(username));
        }

        [Authorize]
        [HttpDelete("delete-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(username);
        }

        [Authorize]
        [HttpDelete("delete-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByEmailAsync(email));
        }

        [Authorize]
        [HttpDelete("delete-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(email);
        }

        // ── INVENTORY ─────────────────────────────────────────────────────────

        [HttpGet("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> GetAvatarInventory()
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.GetAvatarInventoryAsync(AvatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>> { IsError = true, Message = $"Error loading avatar inventory: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> AddItemToAvatarInventory([FromBody] InventoryItem inventoryItem)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                if (inventoryItem == null)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem> { IsError = true, Message = "The request body is required. Please provide a valid Inventory Item object with Name, Description, and optional HolonSubType." }, HttpStatusCode.BadRequest);
                if (inventoryItem.HolonType == HolonType.None)
                    inventoryItem.HolonType = HolonType.InventoryItem;
                var result = await AvatarManager.AddItemToAvatarInventoryAsync(AvatarId, inventoryItem);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem> { IsError = true, Message = $"Error adding item to inventory: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> RemoveItemFromAvatarInventory(Guid itemId, [FromQuery] int quantity = 1)
        {
            var starLogEnabled = _configuration?.GetSection("Star")?["LoggingEnabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (starLogEnabled) StarLog($"RemoveItemFromAvatarInventory called: itemId={itemId} quantity={quantity} avatarId={AvatarId}");
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                if (quantity < 1)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Quantity must be 1 or greater." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.RemoveItemFromAvatarInventoryAsync(AvatarId, itemId, quantity);
                if (starLogEnabled) StarLog($"RemoveItemFromAvatarInventory result: itemId={itemId} success={!result.IsError}");
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                if (starLogEnabled) _logger.LogError(ex, "[STAR] RemoveItemFromAvatarInventory exception: itemId={ItemId}", itemId);
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = $"Error removing item from inventory: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("inventory/{itemId}/has")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.AvatarHasItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = $"Error checking if avatar has item: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("inventory/has-by-name")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItemByName([FromQuery] string itemName)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.AvatarHasItemByNameAsync(AvatarId, itemName);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = $"Error checking if avatar has item by name: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("inventory/search")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> SearchAvatarInventory([FromQuery] string searchTerm)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.SearchAvatarInventoryAsync(AvatarId, searchTerm);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>> { IsError = true, Message = $"Error searching inventory: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> GetAvatarInventoryItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem> { IsError = true, Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                var result = await AvatarManager.GetAvatarInventoryItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem> { IsError = true, Message = $"Error getting inventory item: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost("inventory/send-to-avatar")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToAvatar([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Target and ItemName are required." }, HttpStatusCode.BadRequest);
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToAvatarAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = $"Error sending item to avatar: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost("inventory/send-to-clan")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToClan([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header." }, HttpStatusCode.BadRequest);
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Target (clan name) and ItemName are required." }, HttpStatusCode.BadRequest);
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToClanAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = $"Error sending item to clan: {ex.Message}", Exception = ex }, HttpStatusCode.InternalServerError);
            }
        }
    }
}
