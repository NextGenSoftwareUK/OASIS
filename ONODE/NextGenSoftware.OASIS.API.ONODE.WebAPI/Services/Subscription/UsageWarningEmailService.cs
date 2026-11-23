using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed record UsageWarningEmailBatch(int Accepted, int PreviouslyAccepted, int Deferred);

/// <summary>Prepare current server-calculated warnings and submit at most eight new events per explicit action.</summary>
public sealed class UsageWarningEmailService(MongoSubscriptionUsageRepository repository, UsageWarningReader reader,
    UsageWarningEmailDispatcher dispatcher)
{
    public async Task<OASISResult<UsageWarningEmailBatch>> SendCurrentAsync(string authenticatedActor,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(authenticatedActor, "D", out var actor) || actor == Guid.Empty)
            return Error("A canonical authenticated subscriber is required.");
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        if (dna?.OASIS?.UsageVisibility?.EmailEnabled != true || !EmailManager.IsEmailConfigured(dna) ||
            string.IsNullOrWhiteSpace(dna.OASIS.Email.EmailFrom))
            return Error("Usage warning email delivery is disabled or not configured.");
        var avatar = await AvatarManager.Instance.LoadAvatarAsync(actor, false, true);
        if (avatar.IsError || avatar.Result == null || !avatar.Result.IsVerified || string.IsNullOrWhiteSpace(avatar.Result.Email))
            return Error("A current verified subscriber email address is required.");
        var warnings = (await reader.ReadAsync(authenticatedActor, cancellationToken))
            .Where(warning => warning.EmailRequested).OrderBy(warning => warning.Subject, StringComparer.Ordinal).ToList();
        int accepted = 0, previous = 0;
        var month = DateTime.UtcNow.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        for (int index = 0; index < warnings.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (accepted == 8)
                return Result(accepted, previous, warnings.Count - index);
            var warning = warnings[index];
            var html = "<h4>OASIS usage warning</h4><p>" + WebUtility.HtmlEncode(warning.Alert.Message) +
                "</p><p>You can change usage warning preferences in OPORTAL.</p>";
            var delivery = await repository.PrepareWarningEmailAsync(warning.Subject, month, warning.Alert.ThresholdPercent,
                dna.OASIS.Email.EmailFrom, avatar.Result.Email, "OASIS usage warning", html, cancellationToken);
            var submitted = await dispatcher.DispatchAsync(delivery.Email.EventId, authenticatedActor, cancellationToken);
            if (submitted.IsError)
                return new OASISResult<UsageWarningEmailBatch> { IsError = true, Message = submitted.Message,
                    Result = new(accepted, previous, warnings.Count - index), IsSaved = accepted > 0, SavedCount = accepted };
            if (submitted.IsSaved) accepted++; else previous++;
        }
        return Result(accepted, previous, 0);
    }

    private static OASISResult<UsageWarningEmailBatch> Result(int accepted, int previous, int deferred) => new() {
        Result = new(accepted, previous, deferred), IsLoaded = true, IsSaved = accepted > 0, SavedCount = accepted,
        Message = "Warning email submissions processed; provider acceptance does not confirm inbox delivery." };
    private static OASISResult<UsageWarningEmailBatch> Error(string message) => new() { IsError = true, Message = message };
}
