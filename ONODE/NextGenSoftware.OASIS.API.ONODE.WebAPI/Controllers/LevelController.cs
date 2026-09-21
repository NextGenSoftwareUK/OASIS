using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Level management endpoints — exposes the karma-to-level lookup table and calculation.
    /// </summary>
    [Route("api/level")]
    [ApiController]
    public class LevelController : OASISControllerBase
    {
        /// <summary>
        /// Returns the full karma threshold table: { level → minimumKarmaRequired }.
        /// </summary>
        [Authorize]
        [HttpGet("lookup")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<int, long>>), StatusCodes.Status200OK)]
        public OASISResult<Dictionary<int, long>> GetLevelLookup()
        {
            return new OASISResult<Dictionary<int, long>>
            {
                Result = LevelManager.LevelLookup,
                Message = "Level lookup table retrieved successfully."
            };
        }

        /// <summary>
        /// Calculates the level for the given karma value.
        /// </summary>
        [Authorize]
        [HttpGet("calculate/{karma}")]
        [ProducesResponseType(typeof(OASISResult<int>), StatusCodes.Status200OK)]
        public OASISResult<int> CalculateLevelFromKarma(long karma)
        {
            return new OASISResult<int>
            {
                Result = LevelManager.GetLevelFromKarma(karma),
                Message = "Level calculated successfully."
            };
        }
    }
}
