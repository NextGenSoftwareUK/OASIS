using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/subscription/admin")]
    public sealed class SubscriptionAdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly MongoSubscriptionUsageRepository _repository;
        public SubscriptionAdminController(IConfiguration configuration, MongoSubscriptionUsageRepository repository)
        { _configuration = configuration; _repository = repository; }

        [HttpGet("subscriptions")]
        public Task<IActionResult> Subscriptions([FromQuery] string cursor = null, [FromQuery] int limit = 100) =>
            Execute(() => _repository.GetAdminSubscriptionsAsync(cursor, limit, HttpContext.RequestAborted));

        [HttpGet("invoices")]
        public Task<IActionResult> Invoices([FromQuery] DateTime? before = null, [FromQuery] int limit = 100) =>
            Execute(() => _repository.GetAdminInvoicesAsync(Utc(before), limit, HttpContext.RequestAborted));

        [HttpGet("usage/events")]
        public Task<IActionResult> Events([FromQuery] DateTime? before = null, [FromQuery] int limit = 100) =>
            Execute(() => _repository.GetAdminEventsAsync(Utc(before), limit, HttpContext.RequestAborted));

        [HttpGet("usage/audit")]
        public Task<IActionResult> Audit([FromQuery] DateTime? before = null, [FromQuery] int limit = 100) =>
            Execute(() => _repository.GetAdminAuditAsync(Utc(before), limit, HttpContext.RequestAborted));

        [HttpGet("analytics")]
        public Task<IActionResult> Analytics([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            DateTime end = Utc(to) ?? DateTime.UtcNow;
            DateTime start = Utc(from) ?? end.AddDays(-30);
            return Execute(() => _repository.GetAdminAnalyticsAsync(start, end, HttpContext.RequestAborted));
        }

        [HttpGet("outboxes")]
        public async Task<IActionResult> Outboxes([FromQuery] string state = null, [FromQuery] int limit = 100)
        {
            try
            {
                SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                var result = new Dictionary<string, IReadOnlyList<UsageOutboxOperation>>();
                foreach (string service in new[] { "WEB5", "WEB6", "WEB7", "WEB8", "WEB9", "WEB10" })
                    result[service] = await Outbox(service).GetAdministrativeSnapshotAsync(state, limit, HttpContext.RequestAborted);
                return Ok(new OASISResult<Dictionary<string, IReadOnlyList<UsageOutboxOperation>>> { Result = result });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
        }

        [HttpPost("outboxes/requeue")]
        public async Task<IActionResult> Requeue([FromBody] RequeueOutboxRequest request)
        {
            try
            {
                string actor = SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                if (request == null || !Guid.TryParseExact(request.ActionId, "D", out _) || string.IsNullOrWhiteSpace(request.Reason))
                    throw new ArgumentException("Service, action UUID and reason are required.");
                await Outbox(request.Service).RequeueDeadletterAsync(request.OperationId, request.ActionId, actor, request.Reason, HttpContext.RequestAborted);
                return Ok(new OASISResult<bool> { Result = true, Message = "The immutable settlement was requeued; provider execution was not repeated." });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (UsageProtocolException ex) { return StatusCode(ex.StatusCode, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
        }

        private MongoUsageOutboxStore Outbox(string service)
        {
            if (service is not ("WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10"))
                throw new ArgumentException("Service must be WEB5 through WEB10.");
            return new MongoUsageOutboxStore(UsageOutboxConfiguration.ConnectionString(_configuration),
                UsageOutboxConfiguration.DatabaseName(_configuration, service));
        }

        private async Task<IActionResult> Execute<T>(Func<Task<T>> query)
        {
            try
            {
                SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                return Ok(new OASISResult<T> { Result = await query() });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<T> { IsError = true, Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new OASISResult<T> { IsError = true, Message = ex.Message }); }
        }

        private static DateTime? Utc(DateTime? value) => value.HasValue ? value.Value.ToUniversalTime() : null;

        public sealed class RequeueOutboxRequest
        {
            public string Service { get; set; }
            public string OperationId { get; set; }
            public string ActionId { get; set; }
            public string Reason { get; set; }
        }
    }
}
