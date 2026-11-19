using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Authenticated app ownership, atomic setting choices and native usage/billing views.</summary>
[ApiController]
[Authorize]
[Route("api/subscription/app-owner")]
public sealed class SubscriptionAppOwnerController(MongoSubscriptionUsageRepository repository, IConfiguration configuration, IUsageSeriesSource series) : ControllerBase
{
    [HttpGet("apps")]
    public Task<IActionResult> MyApps()
    {
        return Run(async actor => (await repository.GetOwnedAppsAsync(actor, HttpContext.RequestAborted))
            .Select(app => new { app.AppId, app.Name, app.PlanId, app.Status,
                ownerViewEnabled = app.Overrides?.OwnerViewEnabled ?? Defaults.OwnerViewEnabled, app.Overrides }).ToList());
    }

    [HttpGet("{appId}/settings")]
    public Task<IActionResult> Settings(string appId)
    {
        return Run(async actor =>
        {
            var app = await repository.GetAppForOwnerAsync(appId, actor, IsAdministrator(actor), HttpContext.RequestAborted);
            return new { app.AppId, overrides = app.Overrides ?? new AppUsageOverrides(),
                nodeDefaults = new { ownerViewEnabled = Defaults.OwnerViewEnabled, warningsEnabled = Defaults.WarningsEnabled,
                    accessLogEnabled = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.AccessLog?.Enabled ?? false } };
        });
    }

    [HttpPost("{appId}/settings")]
    public Task<IActionResult> SaveSettings(string appId, [FromBody] JsonElement request)
    {
        return Run(async actor => await repository.SetAppOverridesAsync(appId, new AppUsageSettingsPatch(request),
            actor, IsAdministrator(actor), HttpContext.RequestAborted), isSave: true);
    }

    [HttpGet("{appId}/usage")]
    public Task<IActionResult> Usage(string appId, [FromQuery] string from = null, [FromQuery] string to = null)
    {
        return Run(async actor =>
        {
            var app = await repository.GetAppForOwnerAsync(appId, actor, IsAdministrator(actor), HttpContext.RequestAborted);
            if (!(app.Overrides?.OwnerViewEnabled ?? Defaults.OwnerViewEnabled))
                throw new SubscriptionUsageConflictException("The owner usage view is turned off for this app.");
            var now = DateTime.UtcNow;
            var (start, end) = UsageSeries.NormaliseRange(from, to, now);
            var pool = await repository.GetAppPoolUsageAsync(appId, HttpContext.RequestAborted);
            var subjects = Enumerable.Range(0, MongoSubscriptionUsageRepository.MaxPoolShards)
                .Select(shard => MongoSubscriptionUsageRepository.PoolShardSubject(appId, shard)).ToList();
            var buckets = await series.GetDailySeriesAsync(subjects, start, end, HttpContext.RequestAborted);
            var daily = UsageSeries.Build("app:" + appId, buckets, start, end, pool.Limits?.DailyCallLimit ?? 0,
                pool.Limits?.MonthlyRequestLimit ?? 0, pool.Usage?.MonthlyRequests ?? 0, now);
            var top = await repository.GetAppTopUsersAsync(appId, 25, HttpContext.RequestAborted);
            // The ledger identifies avatars by ID. Do not add one directory/provider read per row to render usage.
            return new { app = new { app.AppId, app.Name, app.PlanId }, pool, series = daily,
                users = top.Select(user => new { avatarId = user.UserId, requests = user.Requests,
                    units = user.Units, costUsd = user.SettledCostUsd }).ToList(), avatarNamesIncluded = false };
        });
    }

    private static UsageVisibilitySettings Defaults => OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility ?? new();

    [HttpGet("{appId}/payg")]
    public Task<IActionResult> Billing(string appId)
    {
        return Run(async actor =>
        {
            var app = await repository.GetAppForOwnerAsync(appId, actor, IsAdministrator(actor), HttpContext.RequestAborted);
            var prices = AppOveragePrices.Load();
            var pool = await repository.GetAppPoolUsageAsync(appId, HttpContext.RequestAborted);
            var settings = app.Payg ?? new AppPaygSettings();
            var configured = prices.HasPrice(app.PlanId);
            var rate = configured ? prices.Rate(app.PlanId) : 0m;
            var statements = await repository.GetStatementsAsync("app:" + appId, 24, HttpContext.RequestAborted);
            return new { featureEnabled = Defaults.AppPayAsYouGoEnabled, pricesConfigured = configured,
                pricingVersion = prices.Version, pricingSha256 = prices.PricingSha256, currency = prices.Currency,
                ratePerRequestUsd = rate, settings,
                estimate = AppOverageEstimate.Compute(pool.Usage?.MonthlyRequests ?? 0,
                    pool.Limits?.MonthlyRequestLimit ?? 0, rate, settings.MonthlyCapUsd),
                statements = statements.Select(value => new { value.Id, value.Month,
                    amountUsd = value.AmountMinorUsd / 100m, value.State, value.PaymentFailed, value.HostedInvoiceUrl }).ToList() };
        });
    }

    private bool IsAdministrator(string actor)
    {
        try { return SubscriptionServiceIdentity.RequireAdministrator(HttpContext, configuration) == actor; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private async Task<IActionResult> Run<T>(Func<string, Task<T>> action, bool isSave = false)
    {
        string actor;
        try { actor = SubscriptionServiceIdentity.RequireSubscriber(HttpContext); }
        catch (UnauthorizedAccessException error)
        { return Unauthorized(new OASISResult<bool> { IsError = true, Message = error.Message }); }
        try
        {
            return Ok(new OASISResult<T> { Result = await action(actor), IsLoaded = true,
                IsSaved = isSave, SavedCount = isSave ? 1 : 0 });
        }
        catch (UnauthorizedAccessException error)
        { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = error.Message }); }
        catch (ArgumentException error)
        { return BadRequest(new OASISResult<bool> { IsError = true, Message = error.Message }); }
        catch (SubscriptionUsageConflictException error)
        { return Conflict(new OASISResult<bool> { IsError = true, Message = error.Message }); }
        catch (KeyNotFoundException error)
        { return NotFound(new OASISResult<bool> { IsError = true, Message = error.Message }); }
    }
}
