using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eggs")]
    public class EggsController : OASISControllerBase
    {
        public EggsController()
        {
        }

        /// <summary>
        /// Get's all eggs currently hidden in the OASIS
        /// </summary>
        /// <returns>List of all eggs in the OASIS</returns>
        [Authorize]
        [HttpGet("get-all-eggs")]
        public async Task<OASISResult<List<Egg>>> GetAllEggs()
        {
            try
            {
                // Use EggsManager for business logic
                var result = await EggsManager.Instance.GetAllEggsAsync(AvatarId);


                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<List<Egg>>
                {
                    IsError = true,
                    Message = $"Error retrieving eggs: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get's the current egg quests
        /// </summary>
        /// <returns>List of current egg quests</returns>
        [Authorize]
        [HttpGet("get-current-egg-quests")]
        public async Task<OASISResult<List<EggQuest>>> GetCurrentEggQuests()
        {
            try
            {
                // Use EggsManager for business logic
                var result = await EggsManager.Instance.GetCurrentEggQuestsAsync(AvatarId);


                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<List<EggQuest>>
                {
                    IsError = true,
                    Message = $"Error retrieving egg quests: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get's the current egg quest leaderboard
        /// </summary>
        /// <returns>Leaderboard for current egg quests</returns>
        [Authorize]
        [HttpGet("get-current-egg-quest-leader-board")]
        public async Task<OASISResult<List<EggQuestLeaderboard>>> GetCurrentEggQuestLeaderBoard()
        {
            try
            {
                // Use EggsManager for business logic
                var result = await EggsManager.Instance.GetCurrentEggQuestLeaderboardAsync(AvatarId);


                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<List<EggQuestLeaderboard>>
                {
                    IsError = true,
                    Message = $"Error retrieving egg quest leaderboard: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get eggs for the current avatar
        /// </summary>
        /// <returns>List of eggs owned by the current avatar</returns>
        [Authorize]
        [HttpGet("my-eggs")]
        public async Task<OASISResult<List<Egg>>> GetMyEggs()
        {
            try
            {
                // Use EggsManager for business logic
                var result = await EggsManager.Instance.GetAllEggsAsync(Avatar.Id);


                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<List<Egg>>
                {
                    IsError = true,
                    Message = $"Error retrieving my eggs: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Discover a new egg
        /// </summary>
        /// <param name="locationId">Location where the egg was discovered</param>
        /// <param name="discoveryMethod">Method used to discover the egg</param>
        /// <returns>Newly discovered egg</returns>
        [Authorize]
        [HttpPost("discover")]
        public async Task<OASISResult<Egg>> DiscoverEgg([FromBody] EggType eggType, string name, Guid locationId, string locationName, [FromQuery] EggDiscoveryMethod discoveryMethod = EggDiscoveryMethod.Exploration)
        {
            // Use EggsManager for business logic
            return await EggsManager.Instance.DiscoverEggAsync(Avatar.Id, eggType, name, locationId, locationName, discoveryMethod);
        }

        /// <summary>
        /// Hatch an egg
        /// </summary>
        /// <param name="eggId">ID of the egg to hatch</param>
        /// <returns>Result of hatching</returns>
        [Authorize]
        [HttpPost("hatch/{eggId}")]
        public async Task<OASISResult<Egg>> HatchEgg(Guid eggId)
        {
            // Use EggsManager for business logic
            return await EggsManager.Instance.HatchEggAsync(Avatar.Id, eggId);
        }

        ///// <summary>
        ///// Update egg gallery position
        ///// </summary>
        ///// <param name="eggId">ID of the egg</param>
        ///// <param name="position">New gallery position</param>
        ///// <returns>Success status</returns>
        //[Authorize]
        //[HttpPut("gallery-position/{eggId}")]
        //public async Task<OASISResult<bool>> UpdateGalleryPosition(Guid eggId, [FromBody] GalleryPosition position)
        //{
        //    // Use EggsManager for business logic
        //    return await EggsManager.Instance.(Avatar.Id, eggId, position);
        //}
    }
}
