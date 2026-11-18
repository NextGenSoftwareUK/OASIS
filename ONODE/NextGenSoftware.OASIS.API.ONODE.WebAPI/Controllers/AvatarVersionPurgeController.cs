using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class VersionPurgeSettingResponse
    {
        /// <summary>This avatar's own choice: true = purge earlier versions of my deleted data, false = never, null = follow the app / node default.</summary>
        public bool? Enabled { get; set; }
        /// <summary>This avatar's own number of days (0 to 3650; 0 = at deletion). null = follow the app / node default.</summary>
        public int? Days { get; set; }
        /// <summary>True when the node master switch OASIS.VersionPurge.Enabled is on; when false the choice is stored but nothing is purged.</summary>
        public bool NodeAvailable { get; set; }
        /// <summary>The days that apply without an app's own value: this avatar's days, else the node's PurgeAfterDays (default 30).</summary>
        public int EffectiveDays { get; set; }
        /// <summary>"avatar" when EffectiveDays is this avatar's own value, otherwise "app or global default" (an app may set its own days).</summary>
        public string EffectiveDaysSource { get; set; }
        public int GlobalDays { get; set; }
    }
    public class VersionPurgeSettingRequest { public bool? Enabled { get; set; } public int? Days { get; set; } }

    /// <summary>
    /// The signed-in avatar's choice about purging earlier saved versions of their own deleted data. No choice (the default) follows the app and node defaults.
    /// The avatar's choice beats the app's; a legal hold on a holon beats both; the node master switch beats everything.
    /// </summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarVersionPurgeController : OASISControllerBase
    {
        private HolonPurgeService Service => HttpContext.RequestServices.GetRequiredService<HolonPurgeService>();
        private IHolonPurgeStore Store => HttpContext.RequestServices.GetRequiredService<IHolonPurgeStore>();

        private OASISHttpResponseMessage<VersionPurgeSettingResponse> Reply(PurgeChoice choice)
        {
            choice = choice ?? new PurgeChoice();
            var global = Service.Policy.Settings.PurgeAfterDays;
            return HttpResponseHelper.FormatResponse(new OASISResult<VersionPurgeSettingResponse>
            {
                Result = new VersionPurgeSettingResponse
                {
                    Enabled = choice.Enabled, Days = choice.Days, NodeAvailable = Service.Policy.MasterEnabled, GlobalDays = global,
                    EffectiveDays = choice.Days ?? global, EffectiveDaysSource = choice.Days.HasValue ? "avatar" : "app or global default"
                },
                IsLoaded = true, Message = "Version purge setting."
            }, HttpStatusCode.OK, false);
        }

        [Authorize]
        [HttpGet("version-purge")]
        public async Task<OASISHttpResponseMessage<VersionPurgeSettingResponse>> Get()
        {
            try { return Reply(await Store.GetAvatarChoiceAsync(AvatarId, HttpContext.RequestAborted)); }
            catch (System.Exception ex) { return TestDataHelper.CreateErrorResponse<VersionPurgeSettingResponse>("Could not read the setting: " + ex.Message, ex, HttpStatusCode.BadRequest); }
        }

        [Authorize]
        [HttpPost("version-purge")]
        public async Task<OASISHttpResponseMessage<VersionPurgeSettingResponse>> Set([FromBody] VersionPurgeSettingRequest request)
        {
            try
            {
                var invalid = PurgeChoice.Validate(request?.Days);
                if (invalid != null) return TestDataHelper.CreateErrorResponse<VersionPurgeSettingResponse>(invalid, null, HttpStatusCode.BadRequest); // nothing is saved
                var choice = new PurgeChoice { Enabled = request?.Enabled, Days = request?.Days };
                await Store.SetAvatarChoiceAsync(AvatarId, choice, HttpContext.RequestAborted);
                Service.Policy.SetOwnerChoice(AvatarId, choice);
                return Reply(choice);
            }
            catch (System.Exception ex) { return TestDataHelper.CreateErrorResponse<VersionPurgeSettingResponse>("Could not save the setting: " + ex.Message, ex, HttpStatusCode.BadRequest); }
        }
    }
}
