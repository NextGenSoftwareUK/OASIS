//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
////using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.STAR.WebAPI.Attributes;

//namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
//{
//    /// <summary>
//    /// Eggs and collectibles endpoints for the OASIS gamification system.
//    /// Provides egg discovery, collection, hatching, and quest functionality within the metaverse.
//    /// </summary>
//    [ApiController]
//    [Route("api/eggs")]
//    public class EggsController : STARControllerBase
//    {
//        public EggsController()
//        {
//        }

//        /// <summary>
//        /// Get all eggs currently hidden in the OASIS
//        /// </summary>
//        /// <param name="limit">Maximum number of eggs to return</param>
//        /// <param name="offset">Number of eggs to skip</param>
//        /// <returns>List of available eggs</returns>
//        [Authorize]
//        [HttpGet("all")]
//        public async Task<OASISResult<List<Egg>>> GetAllEggs([FromQuery] int limit = 50, [FromQuery] int offset = 0)
//        {
//            return await EggsManager.Instance.GetAllEggsAsync(limit, offset);
//        }

//        /// <summary>
//        /// Get eggs discovered by the current avatar
//        /// </summary>
//        /// <param name="limit">Maximum number of eggs to return</param>
//        /// <param name="offset">Number of eggs to skip</param>
//        /// <returns>List of discovered eggs</returns>
//        [Authorize]
//        [HttpGet("discovered")]
//        public async Task<OASISResult<List<Egg>>> GetDiscoveredEggs([FromQuery] int limit = 50, [FromQuery] int offset = 0)
//        {
//            return await EggsManager.Instance.GetDiscoveredEggsAsync(Avatar.Id, limit, offset);
//        }

//        /// <summary>
//        /// Discover an egg through exploration
//        /// </summary>
//        /// <param name="locationId">Location where the egg was discovered</param>
//        /// <param name="discoveryMethod">Method used to discover the egg</param>
//        /// <returns>Discovered egg information</returns>
//        [Authorize]
//        [HttpPost("discover")]
//        public async Task<OASISResult<Egg>> DiscoverEgg([FromBody] EggType type, string name, Guid locationId, string location, [FromQuery] EggDiscoveryMethod discoveryMethod = EggDiscoveryMethod.Exploration, [FromQuery] EggRarity eggRarity = EggRarity.Common)
//        {
//            return await EggsManager.Instance.DiscoverEggAsync(Avatar.Id, type, name, locationId, location, discoveryMethod, eggRarity);
//        }

//        /// <summary>
//        /// Hatch an egg to reveal its contents
//        /// </summary>
//        /// <param name="eggId">ID of the egg to hatch</param>
//        /// <returns>Hatching result with rewards</returns>
//        [Authorize]
//        [HttpPost("hatch/{eggId}")]
//        public async Task<OASISResult<HatchingResult>> HatchEgg(Guid eggId)
//        {
//            return await EggsManager.Instance.HatchEggAsync(Avatar.Id, eggId);
//        }

//        /// <summary>
//        /// Get current egg quests
//        /// </summary>
//        /// <param name="limit">Maximum number of quests to return</param>
//        /// <param name="offset">Number of quests to skip</param>
//        /// <returns>List of active egg quests</returns>
//        [Authorize]
//        [HttpGet("quests")]
//        public async Task<OASISResult<List<EggQuest>>> GetCurrentEggQuests([FromQuery] int limit = 20, [FromQuery] int offset = 0)
//        {
//            return await EggsManager.Instance.GetCurrentEggQuestsAsync(Avatar.Id, limit, offset);
//        }

//        /// <summary>
//        /// Complete an egg quest
//        /// </summary>
//        /// <param name="questId">ID of the quest to complete</param>
//        /// <param name="completionNotes">Optional completion notes</param>
//        /// <returns>Quest completion result</returns>
//        [Authorize]
//        [HttpPost("quests/{questId}/complete")]
//        public async Task<OASISResult<EggQuest>> CompleteEggQuest(Guid questId, [FromBody] string completionNotes = null)
//        {
//            return await EggsManager.Instance.CompleteEggQuestAsync(Avatar.Id, questId, completionNotes);
//        }

//        /// <summary>
//        /// Get egg quest leaderboard
//        /// </summary>
//        /// <param name="limit">Maximum number of entries to return</param>
//        /// <param name="offset">Number of entries to skip</param>
//        /// <returns>Egg quest leaderboard</returns>
//        [Authorize]
//        [HttpGet("quests/leaderboard")]
//        public async Task<OASISResult<List<EggQuestLeaderboard>>> GetEggQuestLeaderboard([FromQuery] int limit = 50, [FromQuery] int offset = 0)
//        {
//            return await EggsManager.Instance.GetEggQuestLeaderboardAsync(limit, offset);
//        }

//        /// <summary>
//        /// Get egg collection statistics for the current avatar
//        /// </summary>
//        /// <returns>Egg collection statistics</returns>
//        [Authorize]
//        [HttpGet("stats")]
//        public async Task<OASISResult<Dictionary<string, object>>> GetEggCollectionStats()
//        {
//            return await EggsManager.Instance.GetEggCollectionStatsAsync(Avatar.Id);
//        }

//        /// <summary>
//        /// Get egg types and rarities
//        /// </summary>
//        /// <returns>Available egg types and their rarities</returns>
//        [HttpGet("types")]
//        public async Task<OASISResult<Dictionary<string, object>>> GetEggTypes()
//        {
//            return await EggsManager.Instance.GetEggTypesAsync();
//        }
//    }
//}
