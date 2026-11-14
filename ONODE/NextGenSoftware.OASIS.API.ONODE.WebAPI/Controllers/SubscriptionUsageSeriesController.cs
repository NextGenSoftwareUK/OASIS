using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Daily usage series (for charts) read from the existing day buckets of the usage ledger. People see only their own series;
    /// administrators (Wizard) see any user's and any app pool's. Read-only; never returns request content.
    /// </summary>
    [ApiController]
    [Route("api/subscription")]
    public sealed class SubscriptionUsageSeriesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionService _subscriptions;
        private readonly IUsageSeriesSource _series;
        private readonly MongoSubscriptionUsageRepository _repository;

        public SubscriptionUsageSeriesController(IConfiguration configuration, ISubscriptionService subscriptions, IUsageSeriesSource series, MongoSubscriptionUsageRepository repository)
        { _configuration = configuration; _subscriptions = subscriptions; _series = series; _repository = repository; }

        /// <summary>The signed-in avatar's own daily requests for a date range (default last 30 days, at most 92), with its daily and monthly limits.</summary>
        [Authorize]
        [HttpGet("usage/series")]
        public async Task<IActionResult> MySeries([FromQuery] string from = null, [FromQuery] string to = null)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new OASISResult<bool> { IsError = true, Message = "User not authenticated." });
            return await Run(() => UserSeries(userId, from, to));
        }

        /// <summary>The signed-in avatar's pay-as-you-go estimate: request charges so far this month and projected to month end at the current pace, against its cap.</summary>
        [Authorize]
        [HttpGet("usage/estimate")]
        public async Task<IActionResult> MyEstimate()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new OASISResult<bool> { IsError = true, Message = "User not authenticated." });
            return await Run(async () =>
            {
                var karma = await SubscriptionController.GetCurrentKarmaAsync(userId, HttpContext.RequestAborted);
                var summary = await _subscriptions.GetUsageSummaryAsync(userId, karma, HttpContext.RequestAborted);
                var policy = SubscriptionService.GetUsagePolicy(summary.PlanId ?? "free", karma);
                var record = await _repository.GetSubscriptionAsync(userId, HttpContext.RequestAborted);
                var pricing = PaidOveragePricing.Load();
                var estimate = UserOverageEstimate.Compute(summary.MonthlyRequests, Math.Max(0, policy.MonthlyRequestLimit), pricing.Rate(summary.PlanId ?? "free"), record?.MonthlyOverageLimitUsd ?? 0, DateTime.UtcNow);
                return new { planId = summary.PlanId, estimate, payAsYouGoEnabled = PaidOveragePricing.HasConsent(record), capUsd = record?.MonthlyOverageLimitUsd ?? 0, aiNote = "AI usage above your included AI budget is billed separately at cost and is not part of this estimate." };
            });
        }

        /// <summary>Administrator: any avatar's daily usage series.</summary>
        [HttpGet("admin/users/{userId}/usage/series")]
        public Task<IActionResult> UserSeriesAdmin(string userId, [FromQuery] string from = null, [FromQuery] string to = null) =>
            Admin(() => UserSeries(userId, from, to));

        /// <summary>Administrator: an app pool's daily usage series with the pool's plan limits and status.</summary>
        [HttpGet("admin/app-accounts/{appId}/usage/series")]
        public Task<IActionResult> AppSeriesAdmin(string appId, [FromQuery] string from = null, [FromQuery] string to = null) =>
            Admin(() => AppSeries(appId, from, to));

        internal async Task<UsageSeriesResult> UserSeries(string userId, string from, string to)
        {
            if (!Guid.TryParse(userId, out _)) throw new ArgumentException("A valid avatar id is required.");
            var now = DateTime.UtcNow;
            var (start, end) = UsageSeries.NormaliseRange(from, to, now);
            var karma = await SubscriptionController.GetCurrentKarmaAsync(userId, HttpContext.RequestAborted);
            var summary = await _subscriptions.GetUsageSummaryAsync(userId, karma, HttpContext.RequestAborted);
            var policy = SubscriptionService.GetUsagePolicy(summary.PlanId ?? "free", karma);
            var buckets = await _series.GetDailySeriesAsync(new[] { userId }, start, end, HttpContext.RequestAborted);
            return UsageSeries.Build(userId, buckets, start, end, summary.DailyCallLimit, policy.MonthlyRequestLimit, summary.MonthlyRequests, now);
        }

        internal async Task<object> AppSeries(string appId, string from, string to)
        {
            if (string.IsNullOrWhiteSpace(appId)) throw new ArgumentException("An app id is required.");
            var now = DateTime.UtcNow;
            var (start, end) = UsageSeries.NormaliseRange(from, to, now);
            var pool = await _repository.GetAppPoolUsageAsync(appId, HttpContext.RequestAborted);
            var subjects = Enumerable.Range(0, MongoSubscriptionUsageRepository.MaxPoolShards).Select(s => MongoSubscriptionUsageRepository.PoolShardSubject(appId, s)).ToList();
            var buckets = await _series.GetDailySeriesAsync(subjects, start, end, HttpContext.RequestAborted);
            var series = UsageSeries.Build("app:" + appId, buckets, start, end, pool.Limits?.DailyCallLimit ?? 0, pool.Limits?.MonthlyRequestLimit ?? 0, pool.Usage?.MonthlyRequests ?? 0, now);
            return new { series, pool };
        }

        private async Task<IActionResult> Admin<T>(Func<Task<T>> call)
        {
            try { SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            return await Run(call);
        }

        private async Task<IActionResult> Run<T>(Func<Task<T>> call)
        {
            try { return Ok(new OASISResult<T> { Result = await call(), IsLoaded = true }); }
            catch (ArgumentException ex) { return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
        }

        private string CurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParseExact(claim, "D", out var id)) return null;
            if (HttpContext.Items.TryGetValue("Avatar", out var value) && value is IAvatar avatar && avatar.Id != id) return null;
            return id.ToString("D");
        }
    }
}
