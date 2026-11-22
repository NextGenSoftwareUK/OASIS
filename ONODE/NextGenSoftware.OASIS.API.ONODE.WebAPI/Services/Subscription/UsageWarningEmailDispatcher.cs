using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Explicit bounded delivery of one persisted warning; no polling or background scan.</summary>
public sealed class UsageWarningEmailDispatcher(MongoSubscriptionUsageRepository repository, UsageWarningEmailGateway gateway)
{
    public async Task<OASISResult<Guid>> DispatchAsync(Guid eventId, string authenticatedActor,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(authenticatedActor, "D", out var actor) || actor == Guid.Empty)
            return Error("A canonical authenticated subscriber is required.");
        var delivery = await repository.GetWarningEmailAsync(eventId, cancellationToken);
        if (delivery == null) return Error("The persisted warning event was not found.");
        var defaults = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();
        bool enabled = defaults.WarningsEnabled;
        if (delivery.Subject.StartsWith("app:", StringComparison.Ordinal))
        {
            var suffix = ":owner:" + authenticatedActor;
            if (!delivery.Subject.EndsWith(suffix, StringComparison.Ordinal))
                return Error("The warning does not belong to this subscriber.");
            var appId = delivery.Subject.Substring(4, delivery.Subject.Length - 4 - suffix.Length);
            AppAccountRecord app;
            try { app = await repository.GetAppForOwnerAsync(appId, authenticatedActor, false, cancellationToken); }
            catch (UnauthorizedAccessException) { return Error("The subscriber no longer owns this app warning."); }
            if (!(app.Overrides?.OwnerViewEnabled ?? defaults.OwnerViewEnabled))
                return Error("The app owner warning view is disabled.");
            enabled = app.Overrides?.WarningsEnabled ?? enabled;
        }
        else if (delivery.Subject != "user:" + authenticatedActor)
            return Error("The warning does not belong to this subscriber.");
        var preferences = await repository.GetNotificationSettingsAsync(delivery.Subject, cancellationToken);
        if (!(preferences?.WarningsOverride ?? enabled) || !(preferences?.EmailRequested ?? true) || !defaults.EmailEnabled)
            return Error("The subscriber's current preferences do not permit warning email delivery.");
        if (delivery.ProviderAcknowledgement is Guid acknowledgement)
            return new OASISResult<Guid> { Result = acknowledgement, IsLoaded = true,
                Message = "The email provider previously accepted this warning." };
        cancellationToken.ThrowIfCancellationRequested();
        var avatar = await AvatarManager.Instance.LoadAvatarAsync(actor, false, true);
        if (avatar.IsError || avatar.Result == null ||
            !string.Equals(avatar.Result.Email, delivery.Email.To, StringComparison.OrdinalIgnoreCase))
            return Error("The warning recipient must match the subscriber's current email address.");
        var submitted = await gateway.SendAsync(delivery.Email, cancellationToken);
        if (submitted.IsError) return submitted;
        if (!submitted.IsSaved || submitted.Result == Guid.Empty)
            return Error("The warning submission has no provider acknowledgement.");
        await repository.AcknowledgeWarningEmailAsync(eventId, submitted.Result, cancellationToken);
        return submitted;
    }

    private static OASISResult<Guid> Error(string message) => new() { IsError = true, Message = message };
}
