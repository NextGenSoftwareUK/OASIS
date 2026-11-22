using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class HolonSealSettingResponse
    {
        /// <summary>This avatar's own switch (default false).</summary>
        public bool Enabled { get; set; }
        /// <summary>True when the node has HolonSeal.Enabled and a key; when false the switch is stored but nothing is sealed.</summary>
        public bool NodeAvailable { get; set; }
    }
    public class HolonSealSettingRequest { public bool Enabled { get; set; } }

    /// <summary>
    /// Sealed (tamper-evident) history for the signed-in avatar's own data. Off by default; the avatar can turn it on or off whenever they like.
    /// Turning it off never rewrites anything; versions saved while it was on keep their seals.
    /// </summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarSealController : OASISControllerBase
    {
        private HolonSealPolicy Policy => HttpContext.RequestServices.GetRequiredService<HolonSealPolicy>();
        private IHolonSealStore Store => HttpContext.RequestServices.GetRequiredService<IHolonSealStore>();

        private OASISHttpResponseMessage<HolonSealSettingResponse> Ok(bool enabled)
            => HttpResponseHelper.FormatResponse(new OASISResult<HolonSealSettingResponse>
            { Result = new HolonSealSettingResponse { Enabled = enabled, NodeAvailable = Policy.IsAvailable }, IsLoaded = true, Message = "Sealed history setting." }, HttpStatusCode.OK, false);

        [Authorize]
        [HttpGet("holon-seal")]
        public async Task<OASISHttpResponseMessage<HolonSealSettingResponse>> Get()
        {
            try { return Ok(await Store.GetAsync(AvatarId, HttpContext.RequestAborted)); }
            catch (System.Exception ex) { return TestDataHelper.CreateErrorResponse<HolonSealSettingResponse>("Could not read the setting: " + ex.Message, ex, HttpStatusCode.BadRequest); }
        }

        [Authorize]
        [HttpPost("holon-seal")]
        public async Task<OASISHttpResponseMessage<HolonSealSettingResponse>> Set([FromBody] HolonSealSettingRequest request)
        {
            try
            {
                var enabled = request?.Enabled == true;
                await Store.SetAsync(AvatarId, enabled, HttpContext.RequestAborted);
                Policy.SetOwner(AvatarId, enabled);
                return Ok(enabled);
            }
            catch (System.Exception ex) { return TestDataHelper.CreateErrorResponse<HolonSealSettingResponse>("Could not save the setting: " + ex.Message, ex, HttpStatusCode.BadRequest); }
        }
    }
}
