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

/// <summary>Authenticated app ownership and atomic personal app-setting choices.</summary>
[ApiController]
[Authorize]
[Route("api/subscription/app-owner")]
public sealed class SubscriptionAppOwnerController(MongoSubscriptionUsageRepository repository, IConfiguration configuration) : ControllerBase
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

    private static UsageVisibilitySettings Defaults => OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility ?? new();

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
        catch (KeyNotFoundException error)
        { return NotFound(new OASISResult<bool> { IsError = true, Message = error.Message }); }
    }
}
