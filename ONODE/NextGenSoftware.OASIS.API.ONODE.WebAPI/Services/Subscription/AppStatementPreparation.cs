using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository
{
    private sealed record FrozenAppPayer(string AvatarId, string CustomerId, string SubscriptionId);

    private static FrozenAppPayer ReadOriginalAppPayer(BsonDocument document, AppRequestChargeTerms terms, string appId)
    {
        if (terms == null || terms.AppId != appId || terms.Currency != "USD" ||
            !Guid.TryParseExact(terms.ConsentId, "D", out var consentId) || consentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(terms.PricingVersion) || terms.PricingSha256?.Length != 64 ||
            !terms.PricingSha256.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            terms.RatePerRequestUsd <= 0 || terms.RatePerRequestUsd > 1 || terms.MonthlyCapUsd < 0.5m ||
            terms.MonthlyCapUsd > 1000000m || decimal.Round(terms.MonthlyCapUsd, 2) != terms.MonthlyCapUsd ||
            document == null || document.GetValue("Kind", "").AsString != "app-payg-settings-changed" ||
            document.GetValue("UserId", "").AsString != PoolSubject(appId))
            throw new SubscriptionUsageConflictException("APP_STATEMENT_CONSENT_REQUIRED: Original app terms and protected consent evidence are required.");
        var envelope = JsonSerializer.Deserialize<AppBillingConsentAudit>(document["PayloadJson"].AsString);
        var original = envelope?.Settings;
        if (envelope?.AppId != appId || original == null || !original.Enabled || original.ConsentSchemaVersion != 1 ||
            original.ConsentId != terms.ConsentId || original.PricingVersion != terms.PricingVersion ||
            original.PricingSha256 != terms.PricingSha256 || original.Currency != terms.Currency ||
            original.RatePerRequestUsd != terms.RatePerRequestUsd || original.MonthlyCapUsd != terms.MonthlyCapUsd ||
            !Guid.TryParseExact(original.BillingAvatarId, "D", out var payerId) || payerId == Guid.Empty ||
            envelope.Actor != original.BillingAvatarId || original.ConsentBy != original.BillingAvatarId ||
            !original.ConsentAtUtc.HasValue || !original.PaymentVerifiedAtUtc.HasValue ||
            original.PaymentVerifiedAtUtc > original.ConsentAtUtc ||
            original.ConsentAtUtc.Value - original.PaymentVerifiedAtUtc.Value > TimeSpan.FromMinutes(5) ||
            original.BillingAuthorizationVersion <= 0 || string.IsNullOrWhiteSpace(original.VerifiedStripeCustomerId) ||
            string.IsNullOrWhiteSpace(original.VerifiedStripeSubscriptionId))
            throw new SubscriptionUsageConflictException("APP_STATEMENT_CONSENT_CONFLICT: Original consent does not authorize these frozen terms and payer.");
        return new(original.BillingAvatarId, original.VerifiedStripeCustomerId, original.VerifiedStripeSubscriptionId);
    }

    private Task<PaidOverageStatement> PrepareAppStatementAsync(string appId, string month, string requestKey, bool creditsOnly, CancellationToken token)
    {
        AppAccountRules.ValidateAppId(appId);
        if (string.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 200)
            throw new ArgumentException("An app billing request identity is required.");
        var start = MonthStart(month); var end = start.AddMonths(1);
        if (start > _time.GetUtcNow().UtcDateTime) throw new ArgumentException("Future usage cannot be invoiced.");
        var subject = PoolSubject(appId);
        var id = "ovga_" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            Scope = OverageScope, Subject = subject, Month = month, RequestKey = requestKey
        })));
        return TransactAsync(async (session, ct) => {
            await EnsureOpeningBalanceAsync(session, ct);
            var existing = await OverageStatements.Find(session, x => x.Id == id).FirstOrDefaultAsync(ct);
            if (existing != null) {
                if (existing.AppId != appId || existing.Scope != OverageScope || existing.CreditsOnly != creditsOnly)
                    throw new SubscriptionUsageConflictException("APP_STATEMENT_REQUEST_CONFLICT: Billing request identity was reused with different preparation intent.");
                return existing;
            }
            var app = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("The app billing account was not found.");
            var operations = creditsOnly ? new List<UsageEventDocument>() : await _events.Find(session,
                x => x.PooledAppId == appId && x.AuthorizedAtUtc >= start && x.AuthorizedAtUtc < end &&
                    x.Status == "settled" && x.AppBillingStatementId == null && x.SettledAppRequestOverageUsd > 0)
                .SortBy(x => x.AuthorizedAtUtc).ThenBy(x => x.OperationId).Limit(500).ToListAsync(ct);
            var adjustmentFilter = Builders<PaidOverageAdjustment>.Filter.Where(x => x.UserId == subject && x.Month == month && x.BillingStatementId == null);
            if (creditsOnly) adjustmentFilter &= Builders<PaidOverageAdjustment>.Filter.Lt(x => x.AmountUsd, 0);
            var adjustments = await OverageAdjustments.Find(session, adjustmentFilter).SortBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Limit(500).ToListAsync(ct);
            if (operations.Count == 0 && adjustments.Count == 0) return null;
            var terms = operations.Select(x => x.AppRequestChargeTerms).Concat(adjustments.Select(x => x.AppRequestChargeTerms)).ToArray();
            if (terms.Any(x => x == null)) throw new SubscriptionUsageConflictException("APP_STATEMENT_CONSENT_REQUIRED: App entries lack original terms.");
            var consentIds = terms.Select(x => x.ConsentId).Distinct(StringComparer.Ordinal).ToArray();
            var evidenceIds = consentIds.Select(AppConsentAuditId).ToArray();
            var evidence = (await BillingAudit.Find(session, Builders<BsonDocument>.Filter.In("_id", evidenceIds)).ToListAsync(ct))
                .ToDictionary(x => x["_id"].AsString, StringComparer.Ordinal);
            var payers = new Dictionary<string, FrozenAppPayer>(StringComparer.Ordinal);
            foreach (var item in terms) {
                evidence.TryGetValue(AppConsentAuditId(item.ConsentId), out var original);
                var payer = ReadOriginalAppPayer(original, item, appId);
                if (payers.TryGetValue(item.ConsentId, out var prior) && prior != payer)
                    throw new SubscriptionUsageConflictException("APP_STATEMENT_CONSENT_CONFLICT: One consent has inconsistent payer evidence.");
                payers[item.ConsentId] = payer;
            }
            var anchor = payers[terms[0].ConsentId];
            // Billing currency rounds per original customer account, including subscription changes.
            bool SameAccount(AppRequestChargeTerms item) => payers[item.ConsentId].AvatarId == anchor.AvatarId && payers[item.ConsentId].CustomerId == anchor.CustomerId;
            operations = operations.Where(x => SameAccount(x.AppRequestChargeTerms)).ToList();
            adjustments = adjustments.Where(x => SameAccount(x.AppRequestChargeTerms)).ToList();
            foreach (var operation in operations) {
                BoundPoolSubject(operation); ValidateAppRequestChargeBinding(operation);
                if (operation.AppRequestChargeTerms.AppPlanId != operation.PooledAppPlanId ||
                    operation.SettledAppRequestOverageUsd > operation.ReservedAppRequestOverageUsd)
                    throw new SubscriptionUsageConflictException("APP_STATEMENT_CHARGE_INVALID: Original settled app charge exceeds its binding.");
            }
            // This fence serializes concurrent app preparations, without writing on empty polls.
            await AppAccounts.UpdateOneAsync(session, x => x.AppId == appId,
                Builders<AppAccountRecord>.Update.Set(x => x.AuthorizationVersion, checked(app.AuthorizationVersion + 1)), cancellationToken: ct);
            // Detect unresolved legacy billing identity before calculating any new liability.
            var legacyFilter = new BsonDocument { { "UserId", subject }, { "Month", month }, { "$or", new BsonArray {
                new BsonDocument("AppId", new BsonDocument("$ne", appId)),
                new BsonDocument("Scope", new BsonDocument("$ne", OverageScope)),
                new BsonDocument("PayerAvatarId", new BsonDocument("$not", new BsonRegularExpression("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"))),
                new BsonDocument("StripeCustomerId", new BsonDocument("$in", new BsonArray { BsonNull.Value, "" })),
                new BsonDocument("StripeSubscriptionId", new BsonDocument("$in", new BsonArray { BsonNull.Value, "" })),
                new BsonDocument("AppConsentIds", new BsonDocument("$not", new BsonDocument("$type", "array"))),
                new BsonDocument("AppConsentIds", new BsonDocument("$size", 0)),
                new BsonDocument("AiChargesUsd", new BsonDocument { { "$exists", true }, { "$ne", 0 } })
            } } };
            if (await OverageStatements.Find(session, legacyFilter).AnyAsync(ct))
                throw new SubscriptionUsageConflictException("APP_STATEMENT_LEGACY_REVIEW_REQUIRED: Existing app statements lack immutable billing identity or terms.");
            var sourceIds = adjustments.Select(x => x.SourceStatementId).Distinct(StringComparer.Ordinal).ToArray();
            var sources = sourceIds.Length == 0 ? new List<PaidOverageStatement>() : await OverageStatements.Find(session,
                Builders<PaidOverageStatement>.Filter.In(x => x.Id, sourceIds)).ToListAsync(ct);
            foreach (var adjustment in adjustments) {
                var source = sources.SingleOrDefault(x => x.Id == adjustment.SourceStatementId);
                if (adjustment.Scope != OverageScope || source == null || source.Scope != OverageScope || source.AppId != appId ||
                    source.UserId != subject || source.Month != month || source.PayerAvatarId != anchor.AvatarId ||
                    source.StripeCustomerId != anchor.CustomerId || source.AppConsentIds == null || source.OperationIds == null ||
                    !source.AppConsentIds.Contains(adjustment.AppRequestChargeTerms.ConsentId, StringComparer.Ordinal) ||
                    !source.OperationIds.Contains(adjustment.SourceOperationId, StringComparer.Ordinal))
                    throw new SubscriptionUsageConflictException("APP_STATEMENT_ADJUSTMENT_CONFLICT: Credit/debit does not belong to the original app/payer/operation statement.");
            }
            var operationIds = operations.Select(x => x.OperationId).ToArray();
            var corrections = new Dictionary<string, decimal>(StringComparer.Ordinal);
            if (operationIds.Length > 0) {
                var correctionPipeline = new[] {
                    new BsonDocument("$match", new BsonDocument { { "OperationId", new BsonDocument("$in", new BsonArray(operationIds)) }, { "Kind", "correction" } }),
                    new BsonDocument("$group", new BsonDocument { { "_id", "$OperationId" }, { "Amount", new BsonDocument("$sum", "$SettledAppRequestOverageDeltaUsd") } })
                };
                using var corrected = await _audit.AggregateAsync<BsonDocument>(session, correctionPipeline, cancellationToken: ct);
                foreach (var row in await corrected.ToListAsync(ct)) corrections[row["_id"].AsString] = row["Amount"].ToDecimal();
            }
            decimal requests = 0;
            foreach (var operation in operations) {
                var net = checked(operation.SettledAppRequestOverageUsd + corrections.GetValueOrDefault(operation.OperationId));
                if (net < 0 || net > operation.SettledAppRequestOverageUsd)
                    throw new SubscriptionUsageConflictException("APP_STATEMENT_CHARGE_INVALID: Audited app corrections exceed the original charge.");
                requests = checked(requests + net);
            }
            requests = checked(requests + adjustments.Sum(x => x.AmountUsd));
            // Aggregate immutable history on the server; do not load every prior pay-now invoice.
            var priorPipeline = new[] {
                new BsonDocument("$match", new BsonDocument { { "Scope", OverageScope }, { "AppId", appId }, { "Month", month },
                    { "PayerAvatarId", anchor.AvatarId }, { "StripeCustomerId", anchor.CustomerId } }),
                new BsonDocument("$group", new BsonDocument { { "_id", BsonNull.Value },
                    { "Charges", new BsonDocument("$sum", "$RequestChargesUsd") }, { "Cents", new BsonDocument("$sum", "$AmountMinorUsd") } })
            };
            using var priorRows = await OverageStatements.AggregateAsync<BsonDocument>(session, priorPipeline, cancellationToken: ct);
            var total = (await priorRows.ToListAsync(ct)).SingleOrDefault();
            var cumulative = checked((total?.GetValue("Charges", 0).ToDecimal() ?? 0) + requests);
            if (cumulative < 0) throw new SubscriptionUsageConflictException("APP_STATEMENT_CHARGE_INVALID: Cumulative customer liability is negative.");
            // Round cumulatively across consent/price/subscription changes for this original customer.
            var cents = checked(PaidOveragePricing.MinorUnits(cumulative) - (total?.GetValue("Cents", 0).ToInt64() ?? 0));
            var selectedTerms = operations.Select(x => x.AppRequestChargeTerms).Concat(adjustments.Select(x => x.AppRequestChargeTerms)).ToArray();
            var now = _time.GetUtcNow().UtcDateTime;
            var statement = new PaidOverageStatement {
                Id = id, UserId = subject, AppId = appId, PayerAvatarId = anchor.AvatarId, Month = month, Scope = OverageScope, CreditsOnly = creditsOnly,
                StripeCustomerId = anchor.CustomerId, StripeSubscriptionId = anchor.SubscriptionId,
                AppConsentIds = selectedTerms.Select(x => x.ConsentId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                PricingVersions = selectedTerms.Select(x => x.PricingVersion).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                RequestChargesUsd = requests, AiChargesUsd = 0, AmountMinorUsd = cents, OperationIds = operationIds,
                AdjustmentIds = adjustments.Select(x => x.Id).ToArray(), CreatedAtUtc = now, UpdatedAtUtc = now, NextAttemptAtUtc = now
            };
            await OverageStatements.InsertOneAsync(session, statement, cancellationToken: ct);
            if (operationIds.Length > 0) {
                var allocated = await _events.UpdateManyAsync(session, Builders<UsageEventDocument>.Filter.In(x => x.OperationId, operationIds) &
                    Builders<UsageEventDocument>.Filter.Eq(x => x.AppBillingStatementId, null),
                    Builders<UsageEventDocument>.Update.Set(x => x.AppBillingStatementId, id).Inc(x => x.AppChargeVersion, 1), cancellationToken: ct);
                if (allocated.MatchedCount != operationIds.Length) throw new SubscriptionUsageConflictException("APP_STATEMENT_ALLOCATION_CONFLICT: Operation allocation changed.");
            }
            if (adjustments.Count > 0) {
                var allocated = await OverageAdjustments.UpdateManyAsync(session,
                    Builders<PaidOverageAdjustment>.Filter.In(x => x.Id, statement.AdjustmentIds) & Builders<PaidOverageAdjustment>.Filter.Eq(x => x.BillingStatementId, null),
                    Builders<PaidOverageAdjustment>.Update.Set(x => x.BillingStatementId, id), cancellationToken: ct);
                if (allocated.MatchedCount != adjustments.Count) throw new SubscriptionUsageConflictException("APP_STATEMENT_ALLOCATION_CONFLICT: Adjustment allocation changed.");
            }
            await BillingAudit.InsertOneAsync(session, new BsonDocument {
                { "_id", "app-overage-statement:" + id }, { "Kind", "app-overage-statement" }, { "UserId", subject },
                { "OccurredAtUtc", now }, { "PayloadJson", JsonSerializer.Serialize(statement) }
            }, cancellationToken: ct);
            return statement;
        }, token);
    }
}
