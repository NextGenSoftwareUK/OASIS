using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Personal usage-warning choices, derived only from the authenticated subscriber.</summary>
[ApiController]
[Authorize]
[Route("api/subscription/usage/notifications")]
public sealed class SubscriptionNotificationController(MongoSubscriptionUsageRepository repository) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> Get()
    {
        return ForSubscriber(async id =>
            View("user:" + id, await repository.GetNotificationSettingsAsync("user:" + id, HttpContext.RequestAborted)));
    }

    [HttpPost]
    public Task<IActionResult> Save([FromBody] UsageNotificationPreferencesRequest request)
    {
        return ForSubscriber(async id =>
        {
            if (request == null) throw new ArgumentException("Notification preferences are required.");
            var saved = await repository.SaveNotificationSettingsAsync("user:" + id, request.Thresholds,
                request.EmailRequested, id, request.WarningsEnabled, HttpContext.RequestAborted);
            return View("user:" + id, saved);
        }, isSave: true);
    }

    internal static object View(string subject, UsageNotificationSettings settings, bool? warningDefault = null)
    {
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        var defaults = dna?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();
        bool inherited = settings?.Thresholds == null || settings.Thresholds.Count == 0;
        return new
        {
            subject,
            thresholds = inherited ? defaults.DefaultWarningPercents : settings.Thresholds,
            isDefault = inherited,
            warningsEnabled = settings?.WarningsOverride ?? warningDefault ?? defaults.WarningsEnabled,
            warningsOverride = settings?.WarningsOverride,
            nodeDefault = defaults.WarningsEnabled,
            inheritedWarningDefault = warningDefault ?? defaults.WarningsEnabled,
            emailEnabled = defaults.EmailEnabled && NextGenSoftware.OASIS.API.Core.Managers.EmailManager.IsEmailConfigured(dna),
            emailRequested = settings?.EmailRequested ?? true
        };
    }

    /// <summary>Read the authenticated subscriber's plan and owned-app usage warnings.</summary>
    [HttpGet("/api/subscription/usage/alerts")]
    public Task<IActionResult> Alerts([FromServices] ISubscriptionService subscriptions)
    {
        return ForSubscriber(async actor =>
        {
            var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
            var defaults = dna?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();
            var alerts = new List<UsageAlert>();
            var mine = await repository.GetNotificationSettingsAsync("user:" + actor, HttpContext.RequestAborted);
            if (mine?.WarningsOverride ?? defaults.WarningsEnabled)
            {
                var karma = await SubscriptionController.GetCurrentKarmaAsync(actor, HttpContext.RequestAborted);
                var summary = await subscriptions.GetUsageSummaryAsync(actor, karma, HttpContext.RequestAborted);
                var policy = SubscriptionService.GetUsagePolicy(summary.PlanId ?? "free", karma);
                var alert = UsageAlerts.Evaluate("user", null, summary.MonthlyRequests, policy.MonthlyRequestLimit,
                    mine?.Thresholds is { Count: > 0 } ? mine.Thresholds : defaults.DefaultWarningPercents);
                if (alert != null) alerts.Add(alert);
            }
            foreach (var app in await repository.GetOwnedAppsAsync(actor, HttpContext.RequestAborted))
            {
                if (!(app.Overrides?.OwnerViewEnabled ?? defaults.OwnerViewEnabled)) continue;
                var preferences = await repository.GetNotificationSettingsAsync("app:" + app.AppId + ":owner:" + actor,
                    HttpContext.RequestAborted);
                if (!(preferences?.WarningsOverride ?? app.Overrides?.WarningsEnabled ?? defaults.WarningsEnabled)) continue;
                var pool = await repository.GetAppPoolUsageAsync(app.AppId, HttpContext.RequestAborted);
                var alert = UsageAlerts.Evaluate("app", app.AppId, pool.Usage?.MonthlyRequests ?? 0,
                    pool.Limits?.MonthlyRequestLimit ?? 0,
                    preferences?.Thresholds is { Count: > 0 } ? preferences.Thresholds : defaults.DefaultWarningPercents);
                if (alert != null) alerts.Add(alert);
            }
            return new { emailEnabled = defaults.EmailEnabled &&
                NextGenSoftware.OASIS.API.Core.Managers.EmailManager.IsEmailConfigured(dna), alerts };
        });
    }

    private async Task<IActionResult> ForSubscriber(Func<string, Task<object>> call, bool isSave = false)
    {
        try
        {
            var id = SubscriptionServiceIdentity.RequireSubscriber(HttpContext);
            return Ok(new OASISResult<object> { Result = await call(id), IsLoaded = true,
                IsSaved = isSave, SavedCount = isSave ? 1 : 0 });
        }
        catch (UnauthorizedAccessException error)
        { return Unauthorized(new OASISResult<bool> { IsError = true, Message = error.Message }); }
        catch (ArgumentException error)
        { return BadRequest(new OASISResult<bool> { IsError = true, Message = error.Message }); }
    }
}

public sealed class UsageNotificationPreferencesRequest
{
    public List<int> Thresholds { get; set; }
    public bool EmailRequested { get; set; } = true;
    public bool? WarningsEnabled { get; set; }
}
