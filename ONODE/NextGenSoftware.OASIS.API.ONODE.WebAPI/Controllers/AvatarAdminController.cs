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
    /// <summary>AvatarAdminController endpoints — part of the Avatar API surface at api/avatar.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarAdminController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarAdminController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarAdminController(ILogger<AvatarAdminController> logger, IConfiguration configuration, IWebHostEnvironment env)
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

        #region Session Management - OASIS SSO System 🚀

        /// <summary>
        /// Get all active sessions for a specific avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>List of active sessions</returns>
        [HttpGet("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>> GetAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from specific sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionIds">List of session IDs to logout</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAvatarSessions(Guid avatarId, [FromBody] List<string> sessionIds)
        {
            try
            {
                var result = await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from all sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout-all")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAllAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out all avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Create a new session for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionData">Session information</param>
        /// <returns>Created session</returns>
        [HttpPost("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> CreateAvatarSession(Guid avatarId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.CreateAvatarSessionAsync(avatarId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error creating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Update an existing session (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="sessionData">Updated session information</param>
        /// <returns>Updated session</returns>
        [HttpPut("{avatarId}/sessions/{sessionId}")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> UpdateAvatarSession(Guid avatarId, string sessionId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error updating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get session statistics for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session statistics</returns>
        [HttpGet("{avatarId}/sessions/stats")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>> GetAvatarSessionStats(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionStatsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar session stats: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}