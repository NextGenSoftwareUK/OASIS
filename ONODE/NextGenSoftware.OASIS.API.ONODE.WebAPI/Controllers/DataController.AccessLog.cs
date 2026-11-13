using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class AccessLogEntryDto
    {
        public DateTime Timestamp { get; set; }
        public Guid AvatarId { get; set; }
        /// <summary>The reader's username, or blank when it cannot be read.</summary>
        public string AvatarName { get; set; }
        public string AppId { get; set; }
        public string Operation { get; set; }
        public string HolonType { get; set; }
        /// <summary>Holon ids this read returned (capped); for the per-holon query it is just the holon asked about.</summary>
        public List<Guid> HolonIds { get; set; } = new List<Guid>();
        /// <summary>How many holons the read returned in total.</summary>
        public int TotalCount { get; set; }
        public string IpAddress { get; set; }
    }

    public class AccessLogPageDto
    {
        public bool Enabled { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        /// <summary>Newest first.</summary>
        public List<AccessLogEntryDto> Entries { get; set; } = new List<AccessLogEntryDto>();
    }

    public partial class DataController
    {
        /// <summary>
        /// Who read this holon (newest first, paged). Only the holon's owner or a Wizard (app admin) may ask. Holds ids only, never
        /// holon content, and only exists when OASIS.AccessLog.Enabled is true in OASIS_DNA.json.
        /// </summary>
        [Authorize]
        [HttpGet("holon-access-log/{id}")]
        public async Task<OASISHttpResponseMessage<AccessLogPageDto>> GetHolonAccessLog(Guid id, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            var service = HttpContext.RequestServices.GetRequiredService<AccessLogService>();
            if (!service.IsEnabled)
                return TestDataHelper.CreateErrorResponse<AccessLogPageDto>("Access logging is not enabled on this node (OASIS.AccessLog.Enabled).", null, System.Net.HttpStatusCode.NotFound);
            try
            {
                var wizard = Avatar?.AvatarType?.Value == AvatarType.Wizard;
                var holon = await HolonManager.LoadHolonAsync(id, false, false, 0, true, false, HolonType.All, 0);
                if (holon == null || holon.IsError || holon.Result == null)
                    return TestDataHelper.CreateErrorResponse<AccessLogPageDto>("Holon not found.", null, System.Net.HttpStatusCode.NotFound);
                if (!AccessLogService.CanViewHolonLog(wizard, AvatarId, holon.Result.CreatedByAvatarId))
                    return TestDataHelper.CreateErrorResponse<AccessLogPageDto>("Forbidden. Only the holon's owner or an administrator can see who accessed it.", null, System.Net.HttpStatusCode.Forbidden);
                var events = await service.ForHolonAsync(id, limit, offset);
                return await AccessLogPage(events, limit, offset, onlyHolon: id);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<AccessLogPageDto>($"Error loading access log: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>An avatar's own reads (default), or any avatar's reads for a Wizard (app admin). Newest first, paged.</summary>
        [Authorize]
        [HttpGet("access-log")]
        public async Task<OASISHttpResponseMessage<AccessLogPageDto>> GetAccessLog([FromQuery] Guid? avatarId = null, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            var service = HttpContext.RequestServices.GetRequiredService<AccessLogService>();
            if (!service.IsEnabled)
                return TestDataHelper.CreateErrorResponse<AccessLogPageDto>("Access logging is not enabled on this node (OASIS.AccessLog.Enabled).", null, System.Net.HttpStatusCode.NotFound);
            try
            {
                var target = avatarId ?? AvatarId;
                var wizard = Avatar?.AvatarType?.Value == AvatarType.Wizard;
                if (!AccessLogService.CanViewAvatarLog(wizard, AvatarId, target))
                    return TestDataHelper.CreateErrorResponse<AccessLogPageDto>("Forbidden. You can only see your own access log.", null, System.Net.HttpStatusCode.Forbidden);
                var events = await service.ForAvatarAsync(target, limit, offset);
                return await AccessLogPage(events, limit, offset, onlyHolon: null);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<AccessLogPageDto>($"Error loading access log: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        private async Task<OASISHttpResponseMessage<AccessLogPageDto>> AccessLogPage(IReadOnlyList<AccessLogEvent> events, int limit, int offset, Guid? onlyHolon)
        {
            var names = new Dictionary<Guid, string>();
            foreach (var reader in events.Select(e => e.AvatarId).Distinct())
            {
                try { names[reader] = (await AvatarManager.Instance.LoadAvatarAsync(reader, false, true))?.Result?.Username ?? ""; }
                catch (Exception) { names[reader] = ""; }
            }
            var page = new AccessLogPageDto
            {
                Enabled = true,
                Limit = limit <= 0 ? 50 : Math.Min(limit, 200),
                Offset = Math.Max(0, offset),
                Entries = events.Select(e => new AccessLogEntryDto
                {
                    Timestamp = e.Timestamp, AvatarId = e.AvatarId, AvatarName = names.TryGetValue(e.AvatarId, out var n) ? n : "",
                    AppId = e.AppId, Operation = e.Operation, HolonType = e.HolonType,
                    HolonIds = onlyHolon.HasValue ? new List<Guid> { onlyHolon.Value } : e.HolonIds,
                    TotalCount = e.TotalCount, IpAddress = e.IpAddress
                }).ToList()
            };
            return HttpResponseHelper.FormatResponse(new OASISResult<AccessLogPageDto> { Result = page, IsLoaded = true, Message = "Access log loaded." }, System.Net.HttpStatusCode.OK, false);
        }
    }
}
