using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/mission")]
    public class MissionController : OASISControllerBase
    {
        public MissionController()
        {
        }

        /// <summary>
        /// Get available missions for the current avatar
        /// </summary>
        /// <param name="type">Optional mission type filter</param>
        /// <param name="difficulty">Optional difficulty filter</param>
        /// <returns>List of available missions</returns>
        [Authorize]
        [HttpGet("available")]
        public async Task<OASISResult<List<Mission>>> GetAvailableMissions([FromQuery] MissionType? type = null, [FromQuery] MissionDifficulty? difficulty = null)
        {
            return await MissionManager.Instance.GetAvailableMissionsAsync(Avatar.Id, type, difficulty);
        }

        /// <summary>
        /// Start a mission
        /// </summary>
        /// <param name="missionId">Mission ID</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("start/{missionId}")]
        public async Task<OASISResult<bool>> StartMission(Guid missionId)
        {
            return await MissionManager.Instance.StartMissionAsync(Avatar.Id, missionId);
        }

        /// <summary>
        /// Update mission progress
        /// </summary>
        /// <param name="missionId">Mission ID</param>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="note">Optional progress note</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("update-progress/{missionId}")]
        public async Task<OASISResult<bool>> UpdateMissionProgress(Guid missionId, [FromBody] int progress, [FromBody] string note = null)
        {
            return await MissionManager.Instance.UpdateMissionProgressAsync(Avatar.Id, missionId, progress, note);
        }

        /// <summary>
        /// Get mission progress for the current avatar
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of mission progress</returns>
        [Authorize]
        [HttpGet("progress")]
        public async Task<OASISResult<List<MissionProgress>>> GetMissionProgress([FromQuery] MissionStatus? status = null)
        {
            return await MissionManager.Instance.GetMissionProgressAsync(Avatar.Id, status);
        }

        /// <summary>
        /// Get mission statistics for the current avatar
        /// </summary>
        /// <returns>Mission statistics</returns>
        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetMissionStats()
        {
            return await MissionManager.Instance.GetMissionStatsAsync(Avatar.Id);
        }
    }
}