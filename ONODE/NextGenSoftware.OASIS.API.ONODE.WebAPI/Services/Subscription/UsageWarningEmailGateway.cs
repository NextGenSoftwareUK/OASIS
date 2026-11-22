using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using Resend;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>An immutable persisted delivery payload. EventId identifies one warning, not one attempt.</summary>
public sealed record UsageWarningEmail(Guid EventId, DateTime PreparedAtUtc, string From,
    string To, string Subject, string HtmlBody);

/// <summary>Submits the exact persisted warning using Resend's provider-side deduplication contract.</summary>
public sealed class UsageWarningEmailGateway(HttpClient http, TimeProvider clock)
{
    public async Task<OASISResult<Guid>> SendAsync(UsageWarningEmail warning, CancellationToken cancellationToken)
    {
        if (warning == null || warning.EventId == Guid.Empty || warning.PreparedAtUtc.Kind != DateTimeKind.Utc ||
            string.IsNullOrWhiteSpace(warning.From) || string.IsNullOrWhiteSpace(warning.To) ||
            string.IsNullOrWhiteSpace(warning.Subject) || string.IsNullOrWhiteSpace(warning.HtmlBody))
            return Error("A complete persisted warning event is required.");
        var now = clock.GetUtcNow().UtcDateTime;
        // An ambiguous old attempt must be reconciled, never resubmitted after provider deduplication expires.
        if (warning.PreparedAtUtc > now || now - warning.PreparedAtUtc >= TimeSpan.FromHours(24) - TimeSpan.FromMinutes(1))
            return Error("The warning delivery requires reconciliation outside the provider idempotency window.");
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        if (dna?.OASIS?.UsageVisibility?.EmailEnabled != true || !EmailManager.IsEmailConfigured(dna))
            return Error("Usage warning email delivery is disabled or not configured.");
        var client = new ResendClient(new OptionsSnapshot<ResendClientOptions>(new ResendClientOptions {
            ApiToken = EmailManager.ResolveResendKey(dna)
        }), http);
        var message = new EmailMessage { From = warning.From, Subject = warning.Subject, HtmlBody = warning.HtmlBody };
        message.To.Add(warning.To);
        using var submissionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        submissionTimeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var response = await client.EmailSendAsync("usage-warning/" + warning.EventId.ToString("D"),
                message, submissionTimeout.Token).ConfigureAwait(false);
            if (!response.Success || response.Content == Guid.Empty)
                return Error("The email provider did not acknowledge the warning submission.");
            return new OASISResult<Guid> { Result = response.Content, IsSaved = true, SavedCount = 1,
                Message = "The email provider accepted the warning submission." };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Error("The warning submission timed out without confirmation; retain the persisted event for reconciliation."); }
        catch (HttpRequestException)
        { return Error("The warning submission could not be confirmed; reconcile or reuse the same persisted event within its idempotency window."); }
        catch (ResendException)
        { return Error("The email provider refused the warning submission; it has not been acknowledged."); }
    }

    private static OASISResult<Guid> Error(string message) => new() { IsError = true, Message = message };
}
