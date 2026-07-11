using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Per-avatar token and cost usage summary.
    /// GET /v1/usage — returns current month spend (USD) and today's token count against configured limits.
    /// </summary>
    [ApiController]
    [Route("v1/usage")]
    public class UsageController : Web6ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(AvatarUsageSummary), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsage()
        {
            if (AvatarId == Guid.Empty)
                return BadRequest(new { error = "AvatarId required (pass Authorization: Bearer <jwt>)." });

            var metering = new UsageMeteringManager(AvatarId, OASISDNA);
            var result = await metering.GetUsageSummaryAsync();
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }
    }
}
