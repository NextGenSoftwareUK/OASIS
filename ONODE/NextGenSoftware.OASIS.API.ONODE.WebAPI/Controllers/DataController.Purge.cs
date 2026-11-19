using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PurgeHolonsRequest
    {
        /// <summary>Holon ids to purge (at most 100; extras are ignored and reported).</summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();
        /// <summary>Also purge holons that are not deleted. Default false: a live holon is refused with NotDeleted.</summary>
        public bool AllowLive { get; set; }
    }

    public class PurgeHolonsResponse
    {
        public List<HolonPurgeResult> Results { get; set; } = new List<HolonPurgeResult>();
        public int Purged { get; set; }
        public int PurgedPartially { get; set; }
        public int Refused { get; set; }
        public int IgnoredBeyondLimit { get; set; }
    }

    public class HolonPurgeHoldRequest { public string Reason { get; set; } }

    public class HolonPurgeStatusResponse
    {
        public Guid HolonId { get; set; }
        /// <summary>none, pending, purged or cancelled.</summary>
        public string Status { get; set; }
        public bool Hold { get; set; }
        public string HoldReason { get; set; }
        public DateTime? PurgeAfterUtc { get; set; }
        public DateTime? PurgedAtUtc { get; set; }
        public int VersionsRemoved { get; set; }
    }

    public partial class DataController
    {
        private HolonPurgeService PurgeService => HttpContext.RequestServices.GetRequiredService<HolonPurgeService>();

        private HolonPurgeCaller PurgeCaller() => new HolonPurgeCaller
        {
            AvatarId = AvatarId,
            AppId = Request.Headers.TryGetValue("X-OASIS-App-Id", out var app) ? app.ToString() : null
        };

        private static HttpStatusCode PurgeHttpStatus(HolonPurgeStatus status)
        {
            switch (status)
            {
                case HolonPurgeStatus.Purged: case HolonPurgeStatus.PurgedPartially: case HolonPurgeStatus.AlreadyPurged:
                case HolonPurgeStatus.HoldSet: case HolonPurgeStatus.HoldReleased: return HttpStatusCode.OK;
                case HolonPurgeStatus.NotFound: return HttpStatusCode.NotFound;
                case HolonPurgeStatus.Forbidden: case HolonPurgeStatus.Disabled: return HttpStatusCode.Forbidden;
                case HolonPurgeStatus.OnHold: case HolonPurgeStatus.NotDeleted: return HttpStatusCode.Conflict;
                default: return HttpStatusCode.InternalServerError;
            }
        }

        private OASISHttpResponseMessage<HolonPurgeResult> PurgeResponse(HolonPurgeResult result)
        {
            var status = PurgeHttpStatus(result.Status);
            Response.StatusCode = (int)status;
            return HttpResponseHelper.FormatResponse(new OASISResult<HolonPurgeResult> { Result = result, IsLoaded = true, IsError = status != HttpStatusCode.OK, Message = result.Message }, status, false);
        }

        /// <summary>
        /// Permanently removes EVERY saved version of a holon (the current record and all earlier versions). Opt-in: needs OASIS.VersionPurge.Enabled on the node.
        /// Only the holon's creator, or an avatar with a Delete grant on it, may call it. Refused while a legal hold is set. By default the holon must already be
        /// deleted (pass allowLive=true to purge a live one). The answer lists every store that was purged and every store that could not be (replicas, backups).
        /// Cannot be undone.
        /// </summary>
        [Authorize]
        [HttpDelete("purge-holon/{id}")]
        public async Task<OASISHttpResponseMessage<HolonPurgeResult>> PurgeHolon(Guid id, [FromQuery] bool allowLive = false)
            => PurgeResponse(await PurgeService.PurgeAsync(id, PurgeCaller(), allowLive, HttpContext.RequestAborted));

        /// <summary>Batch form of purge-holon: up to 100 ids, one result per id. The HTTP status is 200 whenever the batch was processed; read each result's Status.</summary>
        [Authorize]
        [HttpPost("purge-holons")]
        public async Task<OASISHttpResponseMessage<PurgeHolonsResponse>> PurgeHolons([FromBody] PurgeHolonsRequest request)
        {
            var ids = request?.Ids ?? new List<Guid>();
            var distinct = ids.Where(i => i != Guid.Empty).Distinct().ToList();
            var results = await PurgeService.PurgeBatchAsync(distinct, PurgeCaller(), request?.AllowLive == true, HttpContext.RequestAborted);
            var body = new PurgeHolonsResponse
            {
                Results = results.ToList(),
                Purged = results.Count(r => r.Status == HolonPurgeStatus.Purged || r.Status == HolonPurgeStatus.AlreadyPurged),
                PurgedPartially = results.Count(r => r.Status == HolonPurgeStatus.PurgedPartially),
                IgnoredBeyondLimit = Math.Max(0, distinct.Count - HolonPurgeService.MaxBatch)
            };
            body.Refused = body.Results.Count - body.Purged - body.PurgedPartially;
            return HttpResponseHelper.FormatResponse(new OASISResult<PurgeHolonsResponse> { Result = body, IsLoaded = true, Message = $"{body.Purged} purged, {body.PurgedPartially} purged with some stores not purged, {body.Refused} refused." }, HttpStatusCode.OK, false);
        }

        /// <summary>Sets a legal hold on a holon: neither the scheduled worker nor purge-holon will purge it until the hold is released. Same permission as purge-holon.</summary>
        [Authorize]
        [HttpPost("holon-purge-hold/{id}")]
        public async Task<OASISHttpResponseMessage<HolonPurgeResult>> SetHolonPurgeHold(Guid id, [FromBody] HolonPurgeHoldRequest request)
            => PurgeResponse(await PurgeService.SetHoldAsync(id, PurgeCaller(), true, request?.Reason, HttpContext.RequestAborted));

        /// <summary>Releases the legal hold; the holon is purged on schedule again if its policy still applies.</summary>
        [Authorize]
        [HttpDelete("holon-purge-hold/{id}")]
        public async Task<OASISHttpResponseMessage<HolonPurgeResult>> ReleaseHolonPurgeHold(Guid id)
            => PurgeResponse(await PurgeService.SetHoldAsync(id, PurgeCaller(), false, null, HttpContext.RequestAborted));

        /// <summary>Purge schedule, legal hold and purged state of one holon, for its creator.</summary>
        [Authorize]
        [HttpGet("holon-purge/status/{id}")]
        public async Task<OASISHttpResponseMessage<HolonPurgeStatusResponse>> GetHolonPurgeStatus(Guid id)
        {
            try
            {
                var store = HttpContext.RequestServices.GetRequiredService<IHolonPurgeStore>();
                var e = await store.GetAsync(id, HttpContext.RequestAborted);
                if (e != null && e.OwnerAvatarId != AvatarId)
                    return TestDataHelper.CreateErrorResponse<HolonPurgeStatusResponse>("Forbidden. You do not have permission to see this holon's purge status.", null, HttpStatusCode.Forbidden);
                var body = new HolonPurgeStatusResponse
                {
                    HolonId = id, Status = e?.Status ?? "none", Hold = e?.Hold == true, HoldReason = e?.HoldReason,
                    PurgeAfterUtc = e?.PurgeAfterUtc, PurgedAtUtc = e?.PurgedAtUtc, VersionsRemoved = e?.VersionsRemoved ?? 0
                };
                return HttpResponseHelper.FormatResponse(new OASISResult<HolonPurgeStatusResponse> { Result = body, IsLoaded = true, Message = "Purge status." }, HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<HolonPurgeStatusResponse>("Could not read the purge status: " + ex.Message, ex, HttpStatusCode.BadRequest);
            }
        }
    }
}
