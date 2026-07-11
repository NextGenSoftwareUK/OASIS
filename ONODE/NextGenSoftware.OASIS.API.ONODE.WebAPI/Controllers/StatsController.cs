using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : OASISControllerBase
    {
        private readonly OASISDNA _OASISDNA;

        public StatsController()
        {
            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
        }

        /// <summary>
        /// Get comprehensive stats for the currently logged in avatar
        /// </summary>
        /// <returns>Avatar statistics including karma, achievements, gifts, etc.</returns>
        [Authorize]
        [HttpGet("get-stats-for-current-logged-in-avatar")]
        public async Task<OASISResult<Dictionary<string, object>>> GetStatsForCurrentLoggedInAvatar()
        {
            try
            {
                if (Avatar == null)
                {
                    return new OASISResult<Dictionary<string, object>> 
                    { 
                        IsError = true, 
                        Message = "Avatar not found. Please ensure you are logged in." 
                    };
                }

                OASISResult<Dictionary<string, object>> result = null;
                try
                {
                    result = await Program.StatsManager.GetAvatarStatsAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Stats retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Stats retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving stats: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get karma statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Karma statistics</returns>
        [Authorize]
        [HttpGet("karma-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKarmaStats(Guid avatarId)
        {
            try
            {
                OASISResult<Dictionary<string, object>> result = null;
                try
                {
                    result = await Program.StatsManager.GetKarmaStatsAsync(avatarId);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Karma stats retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Karma stats retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving karma stats: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get karma history for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="limit">Number of records to return (default: 50)</param>
        /// <returns>Karma history</returns>
        [Authorize]
        [HttpGet("karma-history/{avatarId}")]
        public async Task<OASISResult<List<Dictionary<string, object>>>> GetKarmaHistory(Guid avatarId, int limit = 50)
        {
            return await Program.StatsManager.GetKarmaHistoryAsync(avatarId, limit);
        }

        /// <summary>
        /// Get gift statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Gift statistics</returns>
        [Authorize]
        [HttpGet("gift-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetGiftStats(Guid avatarId)
        {
            return await Program.StatsManager.GetGiftStatsAsync(avatarId);
        }

        /// <summary>
        /// Get chat statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Chat statistics</returns>
        [Authorize]
        [HttpGet("chat-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetChatStats(Guid avatarId)
        {
            return await Program.StatsManager.GetChatStatsAsync(avatarId);
        }

        /// <summary>
        /// Get key statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Key statistics</returns>
        [Authorize]
        [HttpGet("key-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStats(Guid avatarId)
        {
            return await Program.StatsManager.GetKeyStatsAsync(avatarId);
        }

        /// <summary>
        /// Get leaderboard statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Leaderboard statistics</returns>
        [Authorize]
        [HttpGet("leaderboard-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetLeaderboardStats(Guid avatarId)
        {
            return await Program.StatsManager.GetLeaderboardStatsAsync(avatarId);
        }

        /// <summary>
        /// Get system-wide statistics
        /// </summary>
        /// <returns>System statistics</returns>
        [Authorize]
        [HttpGet("system-stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetSystemStats()
        {
            return await Program.StatsManager.GetSystemStatsAsync();
        }
    }
}