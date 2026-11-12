using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        private const int AuditTrailMaxLimit = 200;

        /// <summary>
        /// Readable audit trail of a holon: one entry per saved version (newest first) with who changed it,
        /// when, what kind of change (created/updated/deleted/restored) and the field-level changes
        /// (core fields and metadata keys). Only callers who may read the holon get its trail, and the
        /// trail only contains the fields that caller may read.
        /// </summary>
        [Authorize]
        [HttpGet("holon-audit-trail/{id}")]
        public async Task<OASISHttpResponseMessage<HolonAuditTrailResponse>> GetHolonAuditTrail(Guid id, [FromQuery] int fromVersion = 0, [FromQuery] int toVersion = 0, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
            => await BuildHolonAuditTrail(id, new HolonAuditTrailRequest { FromVersion = fromVersion, ToVersion = toVersion, Limit = limit, Offset = offset });

        /// <summary>Same as the GET, with optional field labels and keys to skip in the request body.</summary>
        [Authorize]
        [HttpPost("holon-audit-trail/{id}")]
        public async Task<OASISHttpResponseMessage<HolonAuditTrailResponse>> PostHolonAuditTrail(Guid id, [FromBody] HolonAuditTrailRequest request)
            => await BuildHolonAuditTrail(id, request ?? new HolonAuditTrailRequest());

        private async Task<OASISHttpResponseMessage<HolonAuditTrailResponse>> BuildHolonAuditTrail(Guid id, HolonAuditTrailRequest request)
        {
            try
            {
                if (id == Guid.Empty)
                    return TestDataHelper.CreateErrorResponse<HolonAuditTrailResponse>("A holon id is required.", null, System.Net.HttpStatusCode.BadRequest);
                var limit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, AuditTrailMaxLimit);
                var offset = Math.Max(0, request.Offset);
                var wizard = Avatar?.AvatarType?.Value == AvatarType.Wizard;

                // Same read rule as load-holon: whole holon or only the fields this avatar may read.
                var latest = await HolonManager.LoadHolonAsync(id, false, false, 0, true, false, HolonType.All, 0);
                if (latest == null || latest.IsError || latest.Result == null)
                    return TestDataHelper.CreateErrorResponse<HolonAuditTrailResponse>("Holon not found.", null, System.Net.HttpStatusCode.NotFound);
                if (!wizard && await HolonManager.ProjectForAvatarAsync(latest.Result, AvatarId) == null)
                    return TestDataHelper.CreateErrorResponse<HolonAuditTrailResponse>("Forbidden. You do not have permission to access this holon.", null, System.Net.HttpStatusCode.Forbidden);

                var latestVersion = Math.Max(1, latest.Result.Version);
                var from = Math.Max(1, request.FromVersion);
                var to = request.ToVersion <= 0 ? latestVersion : Math.Min(request.ToVersion, latestVersion);
                var versions = new List<IHolon>();
                // The first version in the window is compared with the one before it, so load one extra.
                for (var v = Math.Max(1, from - 1); v <= to; v++)
                {
                    var loaded = v == latestVersion ? latest : await HolonManager.LoadHolonAsync(id, false, false, 0, true, false, HolonType.All, v);
                    var holon = loaded?.Result;
                    if (holon == null || loaded.IsError) continue; // a missing version is skipped, never invented
                    if (!wizard)
                    {
                        holon = await HolonManager.ProjectForAvatarAsync(holon, AvatarId);
                        if (holon == null) continue;
                    }
                    versions.Add(holon);
                }

                var names = new Dictionary<Guid, string>();
                foreach (var actor in versions.SelectMany(h => new[] { h.ModifiedByAvatarId, h.CreatedByAvatarId, h.DeletedByAvatarId }).Where(a => a != Guid.Empty).Distinct())
                {
                    try
                    {
                        var avatar = await AvatarManager.Instance.LoadAvatarAsync(actor, false, true);
                        names[actor] = avatar?.Result?.Username ?? "";
                    }
                    catch (Exception) { names[actor] = ""; }
                }

                var options = new HolonAuditTrailOptions
                {
                    FieldLabels = request.FieldLabels,
                    SkipKeys = request.SkipKeys,
                    AvatarNameResolver = a => names.TryGetValue(a, out var n) ? n : ""
                };
                var trail = HolonAuditTrail.Build(versions, options).Where(e => e.Version >= from).ToList();
                var response = new HolonAuditTrailResponse
                {
                    HolonId = id,
                    LatestVersion = latestVersion,
                    Total = trail.Count,
                    Limit = limit,
                    Offset = offset,
                    Entries = trail.Skip(offset).Take(limit).ToList()
                };
                var result = new OASISResult<HolonAuditTrailResponse> { Result = response, IsLoaded = true, Message = "Audit trail loaded." };
                return HttpResponseHelper.FormatResponse(result, System.Net.HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<HolonAuditTrailResponse>($"Error loading audit trail: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}
