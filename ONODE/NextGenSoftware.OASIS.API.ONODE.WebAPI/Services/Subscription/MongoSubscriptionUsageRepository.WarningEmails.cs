using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed record UsageWarningDelivery(string Subject, string Month, int Threshold,
    UsageWarningEmail Email, Guid? ProviderAcknowledgement);

public sealed partial class MongoSubscriptionUsageRepository
{
    /// <summary>Persist one immutable warning payload per subject, UTC month and threshold.</summary>
    public async Task<UsageWarningDelivery> PrepareWarningEmailAsync(string subject, string month, int threshold,
        string from, string to, string title, string html, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 256 ||
            !DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            threshold < 1 || threshold > 100 || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) ||
            string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(html) || html.Length > 65536 ||
            from.Length > 512 || to.Length > 512 || title.Length > 512)
            throw new ArgumentException("A bounded warning subject, UTC month, threshold and complete email payload are required.");
        var identity = SHA256.HashData(Encoding.UTF8.GetBytes(_database.DatabaseNamespace.DatabaseName + "\n" + subject + "\n" + month + "\n" + threshold.ToString(CultureInfo.InvariantCulture)));
        var eventId = new Guid(identity.AsSpan(0, 16));
        var now = _time.GetUtcNow().UtcDateTime;
        now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        var update = Builders<BsonDocument>.Update.SetOnInsert("kind", "usage-warning-email-v1")
            .SetOnInsert("subject", subject).SetOnInsert("month", month).SetOnInsert("threshold", threshold)
            .SetOnInsert("eventId", eventId.ToString("D")).SetOnInsert("preparedAtUtc", now)
            .SetOnInsert("from", from).SetOnInsert("to", to).SetOnInsert("title", title).SetOnInsert("html", html)
            .SetOnInsert("providerAcknowledgement", BsonNull.Value);
        var stored = await NotificationSettingsCollection.FindOneAndUpdateAsync(
            new BsonDocument("_id", "warning:" + eventId.ToString("D")), update,
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
        var result = ReadWarningDelivery(stored);
        if (result.Subject != subject || result.Month != month || result.Threshold != threshold)
            throw new SubscriptionUsageConflictException("The warning event identity belongs to another warning.");
        return result;
    }

    /// <summary>Persist only a nonempty provider acceptance ID; repeated identical acknowledgement is idempotent.</summary>
    public async Task AcknowledgeWarningEmailAsync(Guid eventId, Guid providerAcknowledgement,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty || providerAcknowledgement == Guid.Empty)
            throw new ArgumentException("Warning event and provider acknowledgement IDs are required.");
        var acknowledgement = providerAcknowledgement.ToString("D");
        var filter = new BsonDocument { { "_id", "warning:" + eventId.ToString("D") },
            { "kind", "usage-warning-email-v1" }, { "$or", new BsonArray {
                new BsonDocument("providerAcknowledgement", BsonNull.Value),
                new BsonDocument("providerAcknowledgement", acknowledgement) } } };
        var saved = await NotificationSettingsCollection.UpdateOneAsync(filter,
            Builders<BsonDocument>.Update.Set("providerAcknowledgement", acknowledgement), cancellationToken: cancellationToken);
        if (saved.MatchedCount != 1)
            throw new SubscriptionUsageConflictException("The warning event is missing or already has a different provider acknowledgement.");
    }

    public async Task<UsageWarningDelivery> GetWarningEmailAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("A warning event ID is required.");
        var stored = await NotificationSettingsCollection.Find(new BsonDocument {
            { "_id", "warning:" + eventId.ToString("D") }, { "kind", "usage-warning-email-v1" } }).FirstOrDefaultAsync(cancellationToken);
        return stored == null ? null : ReadWarningDelivery(stored);
    }

    private static UsageWarningDelivery ReadWarningDelivery(BsonDocument stored)
    {
        if (stored == null || stored["kind"].AsString != "usage-warning-email-v1")
            throw new SubscriptionUsageConflictException("The persisted warning event has an invalid type.");
        return new UsageWarningDelivery(stored["subject"].AsString, stored["month"].AsString, stored["threshold"].AsInt32,
            new UsageWarningEmail(Guid.ParseExact(stored["eventId"].AsString, "D"), stored["preparedAtUtc"].ToUniversalTime(),
                stored["from"].AsString, stored["to"].AsString, stored["title"].AsString, stored["html"].AsString),
            stored["providerAcknowledgement"].IsBsonNull ? null : Guid.ParseExact(stored["providerAcknowledgement"].AsString, "D"));
    }
}
