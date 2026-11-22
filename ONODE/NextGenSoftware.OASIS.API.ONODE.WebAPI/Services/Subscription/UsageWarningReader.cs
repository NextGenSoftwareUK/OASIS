using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed record UsageWarning(string Subject, UsageAlert Alert, bool EmailRequested);

/// <summary>One warning calculation for portal reads and consent-checked email preparation.</summary>
public sealed class UsageWarningReader(MongoSubscriptionUsageRepository repository, ISubscriptionService subscriptions)
{
    public async Task<IReadOnlyList<UsageWarning>> ReadAsync(string actor, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(actor, "D", out var avatarId) || avatarId == Guid.Empty)
            throw new ArgumentException("A canonical authenticated subscriber is required.");
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        var defaults = dna?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();
        var alerts = new List<UsageWarning>();
        var mine = await repository.GetNotificationSettingsAsync("user:" + actor, cancellationToken);
        if (mine?.WarningsOverride ?? defaults.WarningsEnabled)
        {
            var karma = await SubscriptionController.GetCurrentKarmaAsync(actor, cancellationToken);
            var summary = await subscriptions.GetUsageSummaryAsync(actor, karma, cancellationToken);
            var policy = SubscriptionService.GetUsagePolicy(summary.PlanId ?? "free", karma);
            var alert = UsageAlerts.Evaluate("user", null, summary.MonthlyRequests, policy.MonthlyRequestLimit,
                mine?.Thresholds is { Count: > 0 } ? mine.Thresholds : defaults.DefaultWarningPercents);
            if (alert != null) alerts.Add(new UsageWarning("user:" + actor, alert, mine?.EmailRequested ?? true));
        }
        foreach (var app in await repository.GetOwnedAppsAsync(actor, cancellationToken))
        {
            if (!(app.Overrides?.OwnerViewEnabled ?? defaults.OwnerViewEnabled)) continue;
            var preferences = await repository.GetNotificationSettingsAsync("app:" + app.AppId + ":owner:" + actor,
                cancellationToken);
            if (!(preferences?.WarningsOverride ?? app.Overrides?.WarningsEnabled ?? defaults.WarningsEnabled)) continue;
            var pool = await repository.GetAppPoolUsageAsync(app.AppId, cancellationToken);
            var alert = UsageAlerts.Evaluate("app", app.AppId, pool.Usage?.MonthlyRequests ?? 0,
                pool.Limits?.MonthlyRequestLimit ?? 0,
                preferences?.Thresholds is { Count: > 0 } ? preferences.Thresholds : defaults.DefaultWarningPercents);
            if (alert != null) alerts.Add(new UsageWarning("app:" + app.AppId + ":owner:" + actor, alert, preferences?.EmailRequested ?? true));
        }
        return alerts;
    }
}
