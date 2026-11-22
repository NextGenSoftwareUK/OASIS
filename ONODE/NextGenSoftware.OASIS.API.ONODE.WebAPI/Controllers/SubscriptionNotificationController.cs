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
            View(id, await repository.GetNotificationSettingsAsync("user:" + id, HttpContext.RequestAborted)));
    }

    [HttpPost]
    public Task<IActionResult> Save([FromBody] UsageNotificationPreferencesRequest request)
    {
        return ForSubscriber(async id =>
        {
            if (request == null) throw new ArgumentException("Notification preferences are required.");
            var saved = await repository.SaveNotificationSettingsAsync("user:" + id, request.Thresholds,
                request.EmailRequested, id, request.WarningsEnabled, HttpContext.RequestAborted);
            return View(id, saved);
        }, isSave: true);
    }

    private static object View(string id, UsageNotificationSettings settings)
    {
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        var defaults = dna?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();
        bool inherited = settings?.Thresholds == null || settings.Thresholds.Count == 0;
        return new
        {
            subject = "user:" + id,
            thresholds = inherited ? defaults.DefaultWarningPercents : settings.Thresholds,
            isDefault = inherited,
            warningsEnabled = settings?.WarningsOverride ?? defaults.WarningsEnabled,
            warningsOverride = settings?.WarningsOverride,
            nodeDefault = defaults.WarningsEnabled,
            emailEnabled = defaults.EmailEnabled && NextGenSoftware.OASIS.API.Core.Managers.EmailManager.IsEmailConfigured(dna),
            emailRequested = settings?.EmailRequested ?? true
        };
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
