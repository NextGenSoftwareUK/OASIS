using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class UsageNotificationSettings
    {
        public string Subject { get; set; }
        public List<int> Thresholds { get; set; } = new List<int>();
        /// <summary>Reserved for the email hook; stored but nothing is sent while OASIS.UsageVisibility.EmailEnabled is false.</summary>
        public bool EmailRequested { get; set; }
        /// <summary>This person's or owner's own opt-in (true) or opt-out (false) of warnings; null follows the node default.</summary>
        public bool? WarningsOverride { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string UpdatedBy { get; set; }
    }

    public sealed class AppPaygSettings
    {
        public bool Enabled { get; set; }
        public decimal MonthlyCapUsd { get; set; }
        public string PricingVersion { get; set; }
        public DateTime? ConsentAtUtc { get; set; }
        public string ConsentBy { get; set; }
    }

    public sealed partial class MongoSubscriptionUsageRepository
    {
        public const int MaxAppOwners = 10;
        private IMongoCollection<BsonDocument> NotificationSettingsCollection => _database.GetCollection<BsonDocument>("usage_notification_settings");

        /// <summary>Administrator: replace the owner avatars of an app account. Audited with the administrator and a reason.</summary>
        public Task<AppAccountRecord> SetAppOwnersAsync(string appId, IReadOnlyList<string> ownerAvatarIds, string actor, string reason, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            AppAccountRules.ValidateActorAndReason(actor, reason);
            var owners = (ownerAvatarIds ?? new List<string>()).Select(o => o?.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (owners.Count > MaxAppOwners) throw new ArgumentException($"An app can have at most {MaxAppOwners} owners.");
            if (owners.Any(o => !Guid.TryParse(o, out _))) throw new ArgumentException("Owners must be avatar ids (GUIDs).");
            owners = owners.Select(o => Guid.Parse(o).ToString("D")).ToList();
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("The app account was not found.");
                var previous = string.Join(",", account.OwnerAvatarIds ?? new List<string>());
                account.OwnerAvatarIds = owners; account.UpdatedAtUtc = now; account.UpdatedBy = actor; account.Reason = reason.Trim();
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() }, { "Kind", "app-account-owners-set" }, { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Owners = owners, PreviousOwners = previous, Actor = actor, Reason = reason.Trim() }) }
                }, cancellationToken: ct);
                return account.Redacted();
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<AppAccountRecord>> GetOwnedAppsAsync(string avatarId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(avatarId, out var id)) return new List<AppAccountRecord>();
            var d = id.ToString("D");
            var rows = await AppAccounts.Find(x => x.OwnerAvatarIds.Contains(d)).SortBy(x => x.AppId).Limit(100).ToListAsync(cancellationToken);
            return rows.ConvertAll(a => a.Redacted());
        }

        public async Task<bool> IsAppOwnerAsync(string appId, string avatarId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(avatarId, out var id) || string.IsNullOrWhiteSpace(appId)) return false;
            var d = id.ToString("D");
            return await AppAccounts.Find(x => x.AppId == appId && x.OwnerAvatarIds.Contains(d)).AnyAsync(cancellationToken);
        }

        /// <summary>Records that a warning for this key (month and threshold) was emailed. True only for the first caller, so one warning is sent once.</summary>
        public async Task<bool> TryMarkNotifiedAsync(string subject, string key, CancellationToken cancellationToken = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", subject) & Builders<BsonDocument>.Filter.Ne("notified", key);
            try
            {
                var r = await NotificationSettingsCollection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("notified", key), new UpdateOptions { IsUpsert = true }, cancellationToken);
                return r.ModifiedCount > 0 || r.UpsertedId != null;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
        }

        public async Task<UsageNotificationSettings> GetNotificationSettingsAsync(string subject, CancellationToken cancellationToken = default)
        {
            var d = await NotificationSettingsCollection.Find(new BsonDocument("_id", subject)).FirstOrDefaultAsync(cancellationToken);
            if (d == null) return null;
            return new UsageNotificationSettings
            {
                Subject = subject, Thresholds = d.GetValue("thresholds", new BsonArray()).AsBsonArray.Select(v => v.ToInt32()).ToList(), EmailRequested = d.GetValue("email", true).AsBoolean,
                WarningsOverride = d.GetValue("warn", BsonNull.Value).IsBsonNull ? (bool?)null : d["warn"].AsBoolean,
                UpdatedAtUtc = d.GetValue("updated", DateTime.MinValue).ToUniversalTime(), UpdatedBy = d.GetValue("by", "").AsString
            };
        }

        /// <summary>Saves a person's or an app owner's thresholds and records the change in the audit trail.</summary>
        public async Task<UsageNotificationSettings> SaveNotificationSettingsAsync(string subject, IReadOnlyList<int> thresholds, bool emailRequested, string actor, bool? warningsOverride = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("A subject and an actor are required.");
            var clean = UsageAlerts.NormaliseThresholds(thresholds, null);
            return await TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var before = await GetNotificationSettingsAsync(subject, ct);
                await NotificationSettingsCollection.UpdateOneAsync(session, new BsonDocument("_id", subject), Builders<BsonDocument>.Update
                    .Set("thresholds", new BsonArray(clean)).Set("email", emailRequested)
                    .Set("warn", warningsOverride.HasValue ? (BsonValue)warningsOverride.Value : BsonNull.Value).Set("updated", now).Set("by", actor),
                    new UpdateOptions { IsUpsert = true }, ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() }, { "Kind", "usage-notification-settings-changed" }, { "UserId", subject }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { Subject = subject, Thresholds = clean, EmailRequested = emailRequested, PreviousThresholds = before?.Thresholds, WarningsOverride = warningsOverride, PreviousWarningsOverride = before?.WarningsOverride, Actor = actor }) }
                }, cancellationToken: ct);
                return new UsageNotificationSettings { Subject = subject, Thresholds = clean, EmailRequested = emailRequested, WarningsOverride = warningsOverride, UpdatedAtUtc = now, UpdatedBy = actor };
            }, cancellationToken);
        }

        /// <summary>App owner (or administrator): change the app pool's pay-as-you-go preference. SETTINGS ONLY: nothing is charged here.</summary>
        public Task<AppAccountRecord> SetAppPaygAsync(string appId, bool enabled, decimal monthlyCapUsd, string pricingVersion, string actor, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("An actor is required.");
            if (enabled && (monthlyCapUsd < 0.5m || monthlyCapUsd > 1000000m || decimal.Round(monthlyCapUsd, 2) != monthlyCapUsd)) throw new ArgumentException("Choose a monthly USD cap from 0.50 in whole cents.");
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("The app account was not found.");
                account.Payg = new AppPaygSettings { Enabled = enabled, MonthlyCapUsd = enabled ? monthlyCapUsd : 0, PricingVersion = enabled ? pricingVersion : null, ConsentAtUtc = enabled ? now : (DateTime?)null, ConsentBy = enabled ? actor : null };
                account.UpdatedAtUtc = now; account.UpdatedBy = actor;
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() }, { "Kind", "app-payg-settings-changed" }, { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Enabled = enabled, MonthlyCapUsd = monthlyCapUsd, PricingVersion = pricingVersion, Actor = actor }) }
                }, cancellationToken: ct);
                return account.Redacted();
            }, cancellationToken);
        }

        /// <summary>Owner or administrator: set this app's own on/off choices that override the node default (null = follow the default). Audited.</summary>
        public Task<AppAccountRecord> SetAppOverridesAsync(string appId, bool? ownerView, bool? warnings, bool? accessLog, string actor, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("An actor is required.");
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("The app account was not found.");
                var before = account.Overrides ?? new AppUsageOverrides();
                account.Overrides = new AppUsageOverrides { OwnerViewEnabled = ownerView, WarningsEnabled = warnings, AccessLogEnabled = accessLog };
                account.UpdatedAtUtc = now; account.UpdatedBy = actor;
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() }, { "Kind", "app-usage-overrides-changed" }, { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Overrides = account.Overrides, Previous = before, Actor = actor }) }
                }, cancellationToken: ct);
                return account.Redacted();
            }, cancellationToken);
        }
    }
}
