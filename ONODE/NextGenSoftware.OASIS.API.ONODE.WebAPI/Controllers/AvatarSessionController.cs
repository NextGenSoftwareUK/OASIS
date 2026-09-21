using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Avatar session management endpoints (OASIS SSO).
    /// All session routes hang under the same api/avatar prefix as AvatarAuthController.
    /// </summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarSessionController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;


        /// <summary>
        /// Get all active sessions for an avatar.
        /// </summary>
        [Authorize]
        [HttpGet("{avatarId}/sessions")]
        [ProducesResponseType(typeof(OASISResult<AvatarSessionManagement>), StatusCodes.Status200OK)]
        public async Task<OASISResult<AvatarSessionManagement>> GetAvatarSessions(Guid avatarId)
        {
            return await AvatarManager.GetAvatarSessionsAsync(avatarId);
        }


        /// <summary>
        /// Get session statistics for an avatar.
        /// </summary>
        [Authorize]
        [HttpGet("{avatarId}/sessions/stats")]
        [ProducesResponseType(typeof(OASISResult<AvatarSessionStats>), StatusCodes.Status200OK)]
        public async Task<OASISResult<AvatarSessionStats>> GetAvatarSessionStats(Guid avatarId)
        {
            return await AvatarManager.GetAvatarSessionStatsAsync(avatarId);
        }


        /// <summary>
        /// Create a new session for an avatar.
        /// </summary>
        [Authorize]
        [HttpPost("{avatarId}/sessions")]
        [ProducesResponseType(typeof(OASISResult<AvatarSession>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<AvatarSession>> CreateAvatarSession(Guid avatarId, [FromBody] CreateSessionRequest request)
        {
            return await AvatarManager.CreateAvatarSessionAsync(avatarId, request);
        }


        /// <summary>
        /// Update an existing session for an avatar.
        /// </summary>
        [Authorize]
        [HttpPut("{avatarId}/sessions/{sessionId}")]
        [ProducesResponseType(typeof(OASISResult<AvatarSession>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<AvatarSession>> UpdateAvatarSession(Guid avatarId, string sessionId, [FromBody] UpdateSessionRequest request)
        {
            return await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, request);
        }


        /// <summary>
        /// Logout an avatar from specific sessions.
        /// </summary>
        [Authorize]
        [HttpPost("{avatarId}/sessions/logout")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        public async Task<OASISResult<bool>> LogoutAvatarSessions(Guid avatarId, [FromBody] List<string> sessionIds)
        {
            return await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);
        }


        /// <summary>
        /// Logout an avatar from all sessions.
        /// </summary>
        [Authorize]
        [HttpPost("{avatarId}/sessions/logout-all")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        public async Task<OASISResult<bool>> LogoutAllAvatarSessions(Guid avatarId)
        {
            return await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);
        }


        /// <summary>
        /// Validate a JWT account token.
        /// </summary>
        [Authorize]
        [HttpPost("validate-account-token")]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status200OK)]
        public OASISResult<string> ValidateAccountToken([FromBody] string accountToken)
        {
            return AvatarManager.ValidateAccountToken(accountToken);
        }
    }
}
