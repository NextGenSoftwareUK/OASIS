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
    [Route("api/quest")]
    public class QuestController : OASISControllerBase
    {
        public QuestController()
        {
        }

        /// <summary>
        /// Get available quests for the current avatar
        /// </summary>
        /// <param name="type">Optional quest type filter</param>
        /// <param name="difficulty">Optional difficulty filter</param>
        /// <returns>List of available quests</returns>
        [Authorize]
        [HttpGet("available")]
        public async Task<OASISResult<List<Quest>>> GetAvailableQuests([FromQuery] QuestType? type = null, [FromQuery] QuestDifficulty? difficulty = null)
        {
            return await QuestManager.Instance.GetAvailableQuestsAsync(Avatar.Id, type, difficulty);
        }

        /// <summary>
        /// Start a quest
        /// </summary>
        /// <param name="questId">Quest ID</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("start/{questId}")]
        public async Task<OASISResult<bool>> StartQuest(Guid questId)
        {
            return await QuestManager.Instance.StartQuestAsync(Avatar.Id, questId);
        }

        /// <summary>
        /// Update quest progress
        /// </summary>
        /// <param name="questId">Quest ID</param>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="note">Optional progress note</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("update-progress/{questId}")]
        public async Task<OASISResult<bool>> UpdateQuestProgress(Guid questId, [FromBody] int progress, [FromBody] string note = null)
        {
            return await QuestManager.Instance.UpdateQuestProgressAsync(Avatar.Id, questId, progress, note);
        }

        /// <summary>
        /// Get quest progress for the current avatar
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of quest progress</returns>
        [Authorize]
        [HttpGet("progress")]
        public async Task<OASISResult<List<QuestProgress>>> GetQuestProgress([FromQuery] QuestStatus? status = null)
        {
            return await QuestManager.Instance.GetQuestProgressAsync(Avatar.Id, status);
        }

        /// <summary>
        /// Get quest statistics for the current avatar
        /// </summary>
        /// <returns>Quest statistics</returns>
        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetQuestStats()
        {
            return await QuestManager.Instance.GetQuestStatsAsync(Avatar.Id);
        }
    }
}