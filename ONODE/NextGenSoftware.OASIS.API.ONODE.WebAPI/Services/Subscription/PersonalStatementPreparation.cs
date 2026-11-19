using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository
{
    private Task<PaidOverageStatement> PreparePersonalStatementAsync(string userId, string month, string requestKey,
        CancellationToken token)
    {
        if (!Guid.TryParseExact(userId, "D", out var user) || user == Guid.Empty ||
            string.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 200)
            throw new ArgumentException("A canonical subscriber, month and billing request identity are required.");
        var start = MonthStart(month); var end = start.AddMonths(1);
        if (start > _time.GetUtcNow().UtcDateTime) throw new ArgumentException("Future usage cannot be invoiced.");
        return TransactAsync(async (session, ct) => {
            await EnsureOpeningBalanceAsync(session, ct);
            var id = StatementId(userId, month, requestKey);
            var existing = await OverageStatements.Find(session, x => x.Id == id).FirstOrDefaultAsync(ct);
            if (existing != null) {
                if (existing.Scope != OverageScope || existing.UserId != userId || existing.AppId != null || existing.PayerAvatarId != null)
                    throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_BINDING_REQUIRED: Statement does not belong to this subscriber and billing scope.");
                return existing;
            }
            var subscriber = await Subscriptions.Find(session, x => x.UserId == userId).FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Subscriber billing account was not found.");
            var operations = await _events.Find(session, x => x.UserId == userId && x.AuthorizedAtUtc >= start && x.AuthorizedAtUtc < end &&
                x.Status == "settled" && x.PaidOveragePricingVersion != null && x.BillingStatementId == null &&
                (x.SettledRequestOverageUsd > 0 || x.SettledAiOverageUsd > 0 || x.PersonalAiCorrectionUsd > 0))
                .SortBy(x => x.AuthorizedAtUtc).ThenBy(x => x.OperationId).Limit(500).ToListAsync(ct);
            var adjustments = await OverageAdjustments.Find(session, x => x.UserId == userId && x.Month == month && x.BillingStatementId == null)
                .SortBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Limit(500).ToListAsync(ct);
            if (operations.Count == 0 && adjustments.Count == 0) return null;
            var terms = operations.Select(x => x.PersonalOverageChargeTerms).Concat(adjustments.Select(x => x.PersonalOverageChargeTerms)).ToArray();
            var payers = await ReadPersonalPayersAsync(session, userId, terms, ct);
            var anchor = payers[terms[0].ConsentId];
            bool SameAccount(PersonalOverageChargeTerms item) => payers[item.ConsentId].CustomerId == anchor.CustomerId;
            operations = operations.Where(x => SameAccount(x.PersonalOverageChargeTerms)).ToList();
            adjustments = adjustments.Where(x => SameAccount(x.PersonalOverageChargeTerms)).ToList();
            foreach (var operation in operations) ValidatePersonalChargeBinding(operation);
            // Legacy totals must not be silently assigned to a newly proved original account.
            var legacy = new BsonDocument { { "UserId", userId }, { "Month", month }, { "$or", new BsonArray {
                new BsonDocument("Scope", new BsonDocument("$ne", OverageScope)),
                new BsonDocument("AppId", new BsonDocument("$ne", BsonNull.Value)),
                new BsonDocument("PayerAvatarId", new BsonDocument("$ne", BsonNull.Value)),
                new BsonDocument("StripeCustomerId", new BsonDocument("$in", new BsonArray { BsonNull.Value, "" })),
                new BsonDocument("StripeSubscriptionId", new BsonDocument("$in", new BsonArray { BsonNull.Value, "" })),
                new BsonDocument("PersonalConsentIds", new BsonDocument("$not", new BsonDocument("$type", "array"))),
                new BsonDocument("PersonalConsentIds", new BsonDocument("$size", 0))
            } } };
            if (await OverageStatements.Find(session, legacy).AnyAsync(ct))
                throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_LEGACY_REVIEW_REQUIRED: Earlier statements lack original personal consent binding.");
            var sourceIds = adjustments.Select(x => x.SourceStatementId).Distinct(StringComparer.Ordinal).ToArray();
            var sources = sourceIds.Length == 0 ? new List<PaidOverageStatement>() : await OverageStatements.Find(session,
                Builders<PaidOverageStatement>.Filter.In(x => x.Id, sourceIds)).ToListAsync(ct);
            var correctedIds = adjustments.Select(x => x.SourceOperationId).Distinct(StringComparer.Ordinal).ToArray();
            var corrected = correctedIds.Length == 0 ? new List<UsageEventDocument>() : await _events.Find(session,
                Builders<UsageEventDocument>.Filter.In(x => x.OperationId, correctedIds)).ToListAsync(ct);
            foreach (var adjustment in adjustments) {
                var source = sources.SingleOrDefault(x => x.Id == adjustment.SourceStatementId);
                var operation = corrected.SingleOrDefault(x => x.OperationId == adjustment.SourceOperationId);
                if (adjustment.Scope != OverageScope || source == null || source.Scope != OverageScope || source.UserId != userId ||
                    source.AppId != null || source.PayerAvatarId != null || source.Month != month || source.StripeCustomerId != anchor.CustomerId ||
                    source.PersonalConsentIds == null || !source.PersonalConsentIds.Contains(adjustment.PersonalOverageChargeTerms.ConsentId, StringComparer.Ordinal) ||
                    source.OperationIds == null || !source.OperationIds.Contains(adjustment.SourceOperationId, StringComparer.Ordinal) ||
                    operation == null || operation.UserId != userId || operation.BillingStatementId != source.Id ||
                    operation.PersonalOverageChargeTerms?.ConsentId != adjustment.PersonalOverageChargeTerms.ConsentId)
                    throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_ADJUSTMENT_CONFLICT: Adjustment does not belong to its original account, operation and statement.");
            }
            // Nonempty preparations serialize with settings/reservations. Corrections also write each allocated event below.
            subscriber.AuthorizationVersion = checked(subscriber.AuthorizationVersion + 1);
            await Subscriptions.ReplaceOneAsync(session, x => x.UserId == userId, subscriber, cancellationToken: ct);
            decimal requests = operations.Sum(x => x.SettledRequestOverageUsd);
            decimal ai = checked(operations.Sum(x => x.SettledAiOverageUsd + x.PersonalAiCorrectionUsd) + adjustments.Sum(x => x.AmountUsd));
            var pipeline = new[] {
                new BsonDocument("$match", new BsonDocument { { "UserId", userId }, { "Scope", OverageScope }, { "Month", month },
                    { "AppId", BsonNull.Value }, { "PayerAvatarId", BsonNull.Value }, { "StripeCustomerId", anchor.CustomerId } }),
                new BsonDocument("$group", new BsonDocument { { "_id", BsonNull.Value },
                    { "Requests", new BsonDocument("$sum", "$RequestChargesUsd") }, { "Ai", new BsonDocument("$sum", "$AiChargesUsd") },
                    { "Cents", new BsonDocument("$sum", "$AmountMinorUsd") } })
            };
            using var priorRows = await OverageStatements.AggregateAsync<BsonDocument>(session, pipeline, cancellationToken: ct);
            var prior = (await priorRows.ToListAsync(ct)).SingleOrDefault();
            var cumulative = checked((prior?.GetValue("Requests", 0).ToDecimal() ?? 0) +
                (prior?.GetValue("Ai", 0).ToDecimal() ?? 0) + requests + ai);
            if (cumulative < 0) throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_CHARGE_INVALID: Original account liability cannot be negative.");
            var cents = checked(PaidOveragePricing.MinorUnits(cumulative) - (prior?.GetValue("Cents", 0).ToInt64() ?? 0));
            var selectedTerms = operations.Select(x => x.PersonalOverageChargeTerms).Concat(adjustments.Select(x => x.PersonalOverageChargeTerms)).ToArray();
            var now = _time.GetUtcNow().UtcDateTime;
            var statement = new PaidOverageStatement {
                Id = id, UserId = userId, Scope = OverageScope, Month = month,
                StripeCustomerId = anchor.CustomerId, StripeSubscriptionId = anchor.SubscriptionId,
                PersonalConsentIds = selectedTerms.Select(x => x.ConsentId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                PricingVersions = selectedTerms.Select(x => x.PricingVersion).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                OperationIds = operations.Select(x => x.OperationId).ToArray(), AdjustmentIds = adjustments.Select(x => x.Id).ToArray(),
                RequestChargesUsd = requests, AiChargesUsd = ai, AmountMinorUsd = cents,
                CreatedAtUtc = now, UpdatedAtUtc = now, NextAttemptAtUtc = now
            };
            await OverageStatements.InsertOneAsync(session, statement, cancellationToken: ct);
            if (operations.Count > 0) {
                var allocated = await _events.UpdateManyAsync(session, Builders<UsageEventDocument>.Filter.In(x => x.OperationId, statement.OperationIds) &
                    Builders<UsageEventDocument>.Filter.Eq(x => x.BillingStatementId, null),
                    Builders<UsageEventDocument>.Update.Set(x => x.BillingStatementId, id).Inc(x => x.PersonalChargeVersion, 1), cancellationToken: ct);
                if (allocated.MatchedCount != operations.Count) throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_ALLOCATION_CONFLICT: Operation allocation changed.");
            }
            if (adjustments.Count > 0) {
                var allocated = await OverageAdjustments.UpdateManyAsync(session, Builders<PaidOverageAdjustment>.Filter.In(x => x.Id, statement.AdjustmentIds) &
                    Builders<PaidOverageAdjustment>.Filter.Eq(x => x.BillingStatementId, null),
                    Builders<PaidOverageAdjustment>.Update.Set(x => x.BillingStatementId, id), cancellationToken: ct);
                if (allocated.MatchedCount != adjustments.Count) throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_ALLOCATION_CONFLICT: Adjustment allocation changed.");
            }
            await BillingAudit.InsertOneAsync(session, new BsonDocument { { "_id", "overage-statement:" + id }, { "Kind", "overage-statement" },
                { "UserId", userId }, { "OccurredAtUtc", now }, { "PayloadJson", JsonSerializer.Serialize(statement) } }, cancellationToken: ct);
            return statement;
        }, token);
    }
}
