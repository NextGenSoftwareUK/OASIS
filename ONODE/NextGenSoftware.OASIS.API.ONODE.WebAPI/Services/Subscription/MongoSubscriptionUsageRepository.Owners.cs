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
                foreach (var owner in owners.Except(account.OwnerAvatarIds ?? new List<string>(), StringComparer.Ordinal))
                    await FenceOpenAccountOwnershipAsync(session, owner, ct);
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

        public async Task<AppAccountRecord> GetAppForOwnerAsync(string appId, string actor, bool administrator, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            var account = await AppAccounts.Find(x => x.AppId == appId).FirstOrDefaultAsync(cancellationToken);
            if (!administrator && !(account?.OwnerAvatarIds?.Contains(actor, StringComparer.OrdinalIgnoreCase) ?? false))
                throw new UnauthorizedAccessException("Only an app owner or approved administrator may read these settings.");
            return account?.Redacted() ?? throw new KeyNotFoundException("The app account was not found.");
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
            return ReadNotificationSettings(subject, d);
        }

        private static UsageNotificationSettings ReadNotificationSettings(string subject, BsonDocument d)
        {
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
            var clean = thresholds == null || thresholds.Count == 0
                ? new List<int>() : UsageAlerts.NormaliseThresholds(thresholds, null);
            return await TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var before = ReadNotificationSettings(subject, await NotificationSettingsCollection
                    .Find(session, new BsonDocument("_id", subject)).FirstOrDefaultAsync(ct));
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

        /// <summary>Audited app consent. Disabling preserves the terms and payer for previously incurred liability.</summary>
        public Task<AppAccountRecord> SetAppPaygAsync(string appId, AppPaygConsentRequest request, string actor,
            bool administrator, AppBillingPayerSnapshot verifiedPayer, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            if (!Guid.TryParseExact(actor, "D", out var actorId) || actorId == Guid.Empty)
                throw new ArgumentException("A canonical actor avatar id is required.");
            if (request == null) throw new ArgumentException("App billing preferences are required.");
            var prices = request.Enabled ? AppOveragePrices.Load() : null;
            if (request.Enabled) request.ValidateOptIn(prices);
            return TransactAsync(async (session, ct) =>
            {
                var now = AppBillingConsent.CanonicalUtc(_time.GetUtcNow().UtcDateTime);
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The app account was not found.");
                var owner = account.OwnerAvatarIds?.Contains(actor, StringComparer.OrdinalIgnoreCase) ?? false;
                if (!owner && (!administrator || request.Enabled))
                    throw new UnauthorizedAccessException("An app owner must consent to charges against their own billing account.");
                var before = account.Payg?.Copy();
                var next = account.Payg?.Copy() ?? new AppPaygSettings();
                if (request.Enabled)
                {
                    AppBillingConsent.RequireEligible(account);
                    if (!prices.HasPrice(account.PlanId)) throw new SubscriptionUsageConflictException("No reviewed app price is available for this plan.");
                    if (verifiedPayer == null || verifiedPayer.AvatarId != actor || verifiedPayer.VerifiedAtUtc.Kind != DateTimeKind.Utc ||
                        verifiedPayer.VerifiedAtUtc.Ticks % TimeSpan.TicksPerMillisecond != 0 || verifiedPayer.VerifiedAtUtc > now || now - verifiedPayer.VerifiedAtUtc > TimeSpan.FromMinutes(5))
                        throw new SubscriptionUsageConflictException("Fresh server-side payment verification is required.");
                    var payer = await Subscriptions.Find(session, x => x.UserId == actor).FirstOrDefaultAsync(ct);
                    AppBillingConsent.RequirePaidPayer(payer, now);
                    if (payer.PlanId != verifiedPayer.PlanId || payer.AuthorizationVersion != verifiedPayer.AuthorizationVersion ||
                        payer.StripeCustomerId != verifiedPayer.CustomerId || payer.StripeSubscriptionId != verifiedPayer.SubscriptionId)
                        throw new SubscriptionUsageConflictException("The payer account changed during payment verification; review it again.");
                    if (await OverageStatements.Find(session, x => x.UserId == "app:" + appId && x.PaymentFailed && x.State != "paid").AnyAsync(ct))
                        throw new SubscriptionUsageConflictException("Resolve this app's failed payment before enabling additional usage.");
                    // This shared fence also participates in subscription/webhook/authorization mutations.
                    payer.AuthorizationVersion = checked(payer.AuthorizationVersion + 1);
                    await Subscriptions.ReplaceOneAsync(session, x => x.UserId == actor, payer, cancellationToken: ct);
                    next = new AppPaygSettings
                    {
                        Enabled = true, MonthlyCapUsd = request.MonthlyCapUsd, PricingVersion = prices.Version,
                        PricingSha256 = prices.PricingSha256, Currency = prices.Currency, RatePerRequestUsd = prices.Rate(account.PlanId),
                        ConsentId = Guid.NewGuid().ToString("D"), ConsentSchemaVersion = 1, ConsentAtUtc = now, ConsentBy = actor, BillingAvatarId = actor,
                        VerifiedStripeCustomerId = payer.StripeCustomerId, VerifiedStripeSubscriptionId = payer.StripeSubscriptionId,
                        BillingAuthorizationVersion = payer.AuthorizationVersion, PaymentVerifiedAtUtc = verifiedPayer.VerifiedAtUtc
                    };
                }
                else next.Enabled = false;
                account.Payg = next;
                account.AuthorizationVersion = checked(account.AuthorizationVersion + 1);
                account.UpdatedAtUtc = now; account.UpdatedBy = actor;
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", request.Enabled ? AppConsentAuditId(next.ConsentId) : Guid.NewGuid().ToString() }, { "Kind", "app-payg-settings-changed" }, { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Previous = before, Settings = next, Actor = actor }) }
                }, cancellationToken: ct);
                return account.Redacted();
            }, cancellationToken);
        }

        /// <summary>Owner or administrator: set this app's own on/off choices that override the node default (null = follow the default). Audited.</summary>
        public Task<AppAccountRecord> SetAppOverridesAsync(string appId, AppUsageSettingsPatch patch, string actor, bool administrator, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("An actor is required.");
            if (patch == null) throw new ArgumentException("App settings are required.");
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("The app account was not found.");
                if (!administrator && !(account.OwnerAvatarIds?.Contains(actor, StringComparer.OrdinalIgnoreCase) ?? false))
                    throw new UnauthorizedAccessException("Only an app owner or approved administrator may change these settings.");
                var before = account.Overrides ?? new AppUsageOverrides();
                account.Overrides = patch.Apply(account.Overrides);
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
