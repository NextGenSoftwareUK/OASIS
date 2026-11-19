using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Immutable public charge terms. Payer identity/proof stays in the referenced protected consent audit.</summary>
public sealed class AppRequestChargeTerms
{
    public const string LedgerScope = "app-request-overages-usd-v1";
    public string ConsentId { get; init; }
    public string AppId { get; init; }
    public string AppPlanId { get; init; }
    public string PricingVersion { get; init; }
    public string PricingSha256 { get; init; }
    public string Currency { get; init; }
    [BsonRepresentation(BsonType.Decimal128)] public decimal RatePerRequestUsd { get; init; }
    [BsonRepresentation(BsonType.Decimal128)] public decimal MonthlyCapUsd { get; init; }
}

internal sealed class AppBillingConsentAudit
{
    public string AppId { get; set; }
    public string Actor { get; set; }
    public AppPaygSettings Settings { get; set; }
}

public sealed partial class MongoSubscriptionUsageRepository
{
    internal static string AppConsentAuditId(string consentId)
    {
        if (!Guid.TryParseExact(consentId, "D", out _))
            throw new SubscriptionUsageConflictException("APP_BILLING_CONSENT_REQUIRED: A recorded consent identity is required.");
        return "app-payg-consent:" + consentId;
    }

    private async Task<AppRequestChargeTerms> BindAppChargeAsync(IClientSessionHandle session, AppAccountRecord app,
        SubscriptionRecord consumingSubscriber, CancellationToken ct)
    {
        if (app.AppOveragePaymentBlocked)
            throw new SubscriptionUsageLimitException("APP_OVERAGE_PAYMENT_BLOCKED", "Resolve this app's failed overage payment before additional usage.", 402);
        var settings = app.Payg;
        var prices = AppOveragePrices.Load();
        if (!AppBillingConsent.HasCurrentTerms(settings, app.PlanId, prices))
            throw new SubscriptionUsageLimitException("APP_OVERAGE_TERMS_REQUIRED", "The app owner must accept the current published terms before additional usage.", 402);
        if (!(app.OwnerAvatarIds?.Contains(settings.BillingAvatarId, StringComparer.OrdinalIgnoreCase) ?? false))
            throw new SubscriptionUsageLimitException("APP_BILLING_PAYER_CHANGED", "The app's current owner must authorize additional usage.", 402);
        var evidence = await BillingAudit.Find(session, new BsonDocument("_id", AppConsentAuditId(settings.ConsentId))).FirstOrDefaultAsync(ct);
        if (evidence == null || evidence.GetValue("Kind", "").AsString != "app-payg-settings-changed" ||
            evidence.GetValue("UserId", "").AsString != PoolSubject(app.AppId))
            throw new SubscriptionUsageConflictException("APP_BILLING_CONSENT_REQUIRED: The original protected consent audit is unavailable; new charges are forbidden.");
        var original = JsonSerializer.Deserialize<AppBillingConsentAudit>(evidence["PayloadJson"].AsString);
        if (original?.AppId != app.AppId || original.Actor != settings.BillingAvatarId ||
            !SameAppConsent(original.Settings, settings))
            throw new SubscriptionUsageConflictException("APP_BILLING_CONSENT_CONFLICT: Current preferences differ from their original immutable consent.");
        var payer = consumingSubscriber.UserId == settings.BillingAvatarId ? consumingSubscriber :
            await Subscriptions.Find(session, x => x.UserId == settings.BillingAvatarId).FirstOrDefaultAsync(ct);
        AppBillingConsent.RequirePaidPayer(payer, _time.GetUtcNow().UtcDateTime);
        if (payer.StripeCustomerId != settings.VerifiedStripeCustomerId || payer.StripeSubscriptionId != settings.VerifiedStripeSubscriptionId)
            throw new SubscriptionUsageLimitException("APP_BILLING_PAYER_CHANGED", "The app payer's billing identity changed; review and authorize it again.", 402);
        if (await OverageStatements.Find(session, x => x.UserId == PoolSubject(app.AppId) && x.PaymentFailed && x.State != "paid").AnyAsync(ct))
            throw new SubscriptionUsageLimitException("APP_OVERAGE_PAYMENT_BLOCKED", "Resolve this app's failed overage payment before additional usage.", 402);
        // The consuming avatar's document is already fenced by AuthorizeAsync. An external payer
        // must share a write with subscription/webhook changes before their account can incur liability.
        if (payer.UserId != consumingSubscriber.UserId)
            await Subscriptions.UpdateOneAsync(session, x => x.UserId == payer.UserId,
                Builders<SubscriptionRecord>.Update.Set(x => x.AuthorizationVersion, checked(payer.AuthorizationVersion + 1)), cancellationToken: ct);
        return new AppRequestChargeTerms {
            ConsentId = settings.ConsentId, AppId = app.AppId, AppPlanId = app.PlanId,
            PricingVersion = settings.PricingVersion, PricingSha256 = settings.PricingSha256, Currency = settings.Currency,
            RatePerRequestUsd = settings.RatePerRequestUsd, MonthlyCapUsd = settings.MonthlyCapUsd
        };
    }

    private static bool SameAppConsent(AppPaygSettings original, AppPaygSettings current) =>
        original != null && original.Enabled && current.Enabled && original.ConsentSchemaVersion == current.ConsentSchemaVersion &&
        original.ConsentId == current.ConsentId && original.BillingAvatarId == current.BillingAvatarId && original.ConsentBy == current.ConsentBy &&
        original.ConsentAtUtc == current.ConsentAtUtc && original.PaymentVerifiedAtUtc == current.PaymentVerifiedAtUtc &&
        original.BillingAuthorizationVersion == current.BillingAuthorizationVersion && original.MonthlyCapUsd == current.MonthlyCapUsd &&
        original.PricingVersion == current.PricingVersion && original.PricingSha256 == current.PricingSha256 && original.Currency == current.Currency &&
        original.RatePerRequestUsd == current.RatePerRequestUsd && original.VerifiedStripeCustomerId == current.VerifiedStripeCustomerId &&
        original.VerifiedStripeSubscriptionId == current.VerifiedStripeSubscriptionId;

    /// <summary>Snapshot verification of app request money only; never repairs or certifies external payment evidence.</summary>
    public Task<AppChargeProjectionCheck> CheckAppChargeProjectionAsync(string appId, string month, CancellationToken ct = default)
    {
        AppAccountRules.ValidateAppId(appId);
        var start = MonthStart(month); var end = start.AddMonths(1);
        return TransactAsync(async (session, token) => {
            if (!await AppAccounts.Find(session, x => x.AppId == appId).AnyAsync(token))
                throw new System.Collections.Generic.KeyNotFoundException("The app account was not found.");
            var check = new AppChargeProjectionCheck { AppId = appId, Month = month };
            var expected = new System.Collections.Generic.Dictionary<string, (decimal Reserved, decimal Settled)>();
            void Finding(string message) { if (check.Findings.Count < 256) check.Findings.Add(message); }
            var pipeline = new[] {
                new BsonDocument("$match", new BsonDocument { { "PooledAppId", appId },
                    { "AuthorizedAtUtc", new BsonDocument { { "$gte", start }, { "$lt", end } } } }),
                new BsonDocument("$lookup", new BsonDocument { { "from", _audit.CollectionNamespace.CollectionName },
                    { "localField", "OperationId" }, { "foreignField", "OperationId" }, { "as", "deltas" } }),
                new BsonDocument("$unwind", "$deltas"),
                new BsonDocument("$match", new BsonDocument("$or", new BsonArray {
                    new BsonDocument("deltas.ReservedAppRequestOverageDeltaUsd", new BsonDocument { { "$exists", true }, { "$ne", 0 } }),
                    new BsonDocument("deltas.SettledAppRequestOverageDeltaUsd", new BsonDocument { { "$exists", true }, { "$ne", 0 } }) })),
                new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "OperationId", 1 }, { "AppId", 1 },
                    { "PooledAppId", 1 }, { "PoolShardCountAtAuthorization", 1 },
                    { "Day", "$deltas.Day" }, { "Reserved", "$deltas.ReservedAppRequestOverageDeltaUsd" },
                    { "Settled", "$deltas.SettledAppRequestOverageDeltaUsd" } })
            };
            using var cursor = await _events.Aggregate<BsonDocument>(session, pipeline).ToCursorAsync(token);
            while (await cursor.MoveNextAsync(token)) foreach (var row in cursor.Current)
            {
                var reserved = row.GetValue("Reserved", 0).ToDecimal(); var settled = row.GetValue("Settled", 0).ToDecimal();
                if (reserved == 0 && settled == 0) continue;
                var operation = new SubscriptionUsageEvent { OperationId = row["OperationId"].AsString,
                    AppId = row.GetValue("AppId", BsonNull.Value).IsBsonNull ? null : row["AppId"].AsString,
                    PooledAppId = row["PooledAppId"].AsString,
                    PoolShardCountAtAuthorization = row.GetValue("PoolShardCountAtAuthorization", BsonNull.Value).IsBsonNull ? null : row["PoolShardCountAtAuthorization"].ToInt32() };
                string subject;
                try { subject = BoundPoolSubject(operation); }
                catch (SubscriptionUsageConflictException) { Finding("Unresolved original routing for operation " + operation.OperationId); continue; }
                var day = row.GetValue("Day", "").AsString;
                if (!DateTime.TryParseExact(day, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _) || !day.StartsWith(month + "-", StringComparison.Ordinal))
                { Finding("Invalid accounting day for operation " + operation.OperationId); continue; }
                foreach (var key in new[] { subject + ":month:" + month, subject + ":day:" + day })
                {
                    var before = expected.GetValueOrDefault(key);
                    expected[key] = (checked(before.Reserved + reserved), checked(before.Settled + settled));
                }
            }
            var subjects = Enumerable.Range(0, MaxPoolShards).Select(x => PoolShardSubject(appId, x)).ToArray();
            var filter = Builders<SubscriptionUsageBucket>.Filter.In(x => x.UserId, subjects) &
                Builders<SubscriptionUsageBucket>.Filter.Or(
                    Builders<SubscriptionUsageBucket>.Filter.Where(x => x.PeriodType == "month" && x.Period == month),
                    Builders<SubscriptionUsageBucket>.Filter.Eq(x => x.PeriodType, "day") &
                    Builders<SubscriptionUsageBucket>.Filter.Regex(x => x.Period, new BsonRegularExpression("^" + month + "-")));
            var actual = await _buckets.Find(session, filter).ToListAsync(token);
            var actualIds = actual.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var bucket in actual)
            {
                var want = expected.GetValueOrDefault(bucket.Id);
                if (want.Reserved != bucket.ReservedAppRequestOverageUsd || want.Settled != bucket.SettledAppRequestOverageUsd)
                    Finding("App money differs from audited movements in " + bucket.Id);
                if (bucket.PeriodType == "month") {
                    check.ActualReservedUsd += bucket.ReservedAppRequestOverageUsd; check.ActualSettledUsd += bucket.SettledAppRequestOverageUsd;
                }
            }
            foreach (var pair in expected)
            {
                if (!actualIds.Contains(pair.Key) && (pair.Value.Reserved != 0 || pair.Value.Settled != 0)) Finding("Missing app money bucket " + pair.Key);
                if (pair.Key.Contains(":month:", StringComparison.Ordinal)) {
                    check.ExpectedReservedUsd += pair.Value.Reserved; check.ExpectedSettledUsd += pair.Value.Settled;
                }
            }
            return check;
        }, ct);
    }

    private async Task ApplyAppRequestChargeCorrectionAsync(IClientSessionHandle session, SubscriptionUsageEvent operation,
        SubscriptionUsageAuditEntry correction, System.Collections.Generic.IReadOnlyList<SubscriptionUsageAuditEntry> previous, CancellationToken ct)
    {
        var delta = correction.SettledAppRequestOverageDeltaUsd;
        if (delta == 0) return;
        if (correction.CorrectionKind != "billing-adjustment" || operation.AppRequestChargeTerms == null)
            throw new SubscriptionUsageConflictException("APP_CHARGE_CORRECTION_INVALID: A billing adjustment and original app charge terms are required.");
        ValidateAppRequestChargeBinding(operation);
        var net = checked(operation.SettledAppRequestOverageUsd + previous.Sum(x => x.SettledAppRequestOverageDeltaUsd) + delta);
        if (net < 0 || net > operation.SettledAppRequestOverageUsd)
            throw new SubscriptionUsageConflictException("APP_CHARGE_CORRECTION_INVALID: The net charge must stay between zero and the originally settled app request charge.");
        if (delta > 0)
        {
            var app = await AppAccounts.Find(session, x => x.AppId == operation.PooledAppId).FirstOrDefaultAsync(ct)
                ?? throw new SubscriptionUsageConflictException("APP_CHARGE_CORRECTION_INVALID: The app account is unavailable for a positive charge correction.");
            await AppAccounts.UpdateOneAsync(session, x => x.AppId == app.AppId,
                Builders<AppAccountRecord>.Update.Set(x => x.AuthorizationVersion, checked(app.AuthorizationVersion + 1)), cancellationToken: ct);
            var pool = await LoadPoolAggregateAsync(session, app.AppId, operation.AuthorizedAtUtc, ct);
            if (checked(pool.ReservedAppRequestOverageUsd + pool.SettledAppRequestOverageUsd + delta) > operation.AppRequestChargeTerms.MonthlyCapUsd)
                throw new SubscriptionUsageConflictException("APP_CHARGE_CAP_EXCEEDED: Restoring the charge would exceed its original agreed cap.");
        }
        if (operation.AppBillingStatementId != null)
            await OverageAdjustments.InsertOneAsync(session, new PaidOverageAdjustment {
                Id = "app-request:" + correction.Id, UserId = PoolSubject(operation.PooledAppId), Month = correction.Month,
                Scope = AppRequestChargeTerms.LedgerScope, SourceStatementId = operation.AppBillingStatementId,
                AppRequestChargeTerms = operation.AppRequestChargeTerms, AmountUsd = delta,
                Reason = correction.Reason, EvidenceReference = correction.ExternalReference, CreatedAtUtc = correction.OccurredAtUtc
            }, cancellationToken: ct);
    }

    private static void ValidateAppRequestChargeBinding(SubscriptionUsageEvent operation)
    {
        if (operation.AppRequestChargeTerms == null)
        {
            if (operation.ReservedAppRequestOverageUsd != 0 || operation.SettledAppRequestOverageUsd != 0)
                throw new SubscriptionUsageConflictException("APP_CHARGE_BINDING_REQUIRED: Nonzero app exposure has no original terms.");
            return;
        }
        var terms = operation.AppRequestChargeTerms;
        if (operation.PooledAppId != terms.AppId || terms.Currency != "USD" ||
            !Guid.TryParseExact(terms.ConsentId, "D", out _) || terms.RatePerRequestUsd <= 0 ||
            operation.ReservedAppRequestOverageUsd != terms.RatePerRequestUsd)
            throw new SubscriptionUsageConflictException("APP_CHARGE_BINDING_INVALID: The reserved app exposure differs from its immutable terms.");
    }

    private static void SettleAppRequestCharge(SubscriptionUsageEvent operation, SubscriptionUsageAuditEntry entry, string outcome)
    {
        ValidateAppRequestChargeBinding(operation);
        if (operation.AppRequestChargeTerms == null) return;
        entry.ReservedAppRequestOverageDeltaUsd = -operation.ReservedAppRequestOverageUsd;
        operation.SettledAppRequestOverageUsd = outcome == "succeeded" ? operation.ReservedAppRequestOverageUsd : 0;
        entry.SettledAppRequestOverageDeltaUsd = operation.SettledAppRequestOverageUsd;
    }
}

/// <summary>App monetary projection verification only, not a Stripe/provider-evidence reconciliation result.</summary>
public sealed class AppChargeProjectionCheck
{
    public string Scope => "app-request-money-projection-v1";
    public string AppId { get; init; }
    public string Month { get; init; }
    public decimal ExpectedReservedUsd { get; set; }
    public decimal ExpectedSettledUsd { get; set; }
    public decimal ActualReservedUsd { get; set; }
    public decimal ActualSettledUsd { get; set; }
    public System.Collections.Generic.List<string> Findings { get; } = new();
    public bool Balanced => Findings.Count == 0;
}
