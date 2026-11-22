using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository
{
    private sealed record PersonalCorrectionCandidate(UsageEventDocument Operation, decimal Before, decimal After,
        decimal OriginalActual, decimal TargetActual, decimal TargetWriteOff);

    private async Task ApplyOverageCorrectionAsync(IClientSessionHandle session, SubscriptionUsageEvent operation,
        SubscriptionUsageAuditEntry correction, CancellationToken token)
    {
        // Request fees are immutable. Unit-only/app-request adjustments cannot change personal AI charges.
        if (correction.SettledCostDeltaUsd == 0) return;
        var start = MonthStart(correction.Month); var end = start.AddMonths(1);
        var filter = Builders<UsageEventDocument>.Filter.Where(x => x.UserId == operation.UserId && x.Status == "settled" &&
            x.AuthorizedAtUtc >= start && x.AuthorizedAtUtc < end);
        if (!await _events.Find(session, filter & Builders<UsageEventDocument>.Filter.Ne(x => x.PaidOveragePricingVersion, null)).AnyAsync(token)) return;
        var history = await _audit.Find(session, x => x.UserId == operation.UserId && x.Month == correction.Month && x.Kind == "correction").ToListAsync(token);
        if (history.Any(x => (x.SettledOverageDeltaUsd != 0 || x.OverageWriteOffDeltaUsd != 0) && x.PersonalChargeMovements == null))
            throw new SubscriptionUsageConflictException("PERSONAL_CORRECTION_LEGACY_REVIEW_REQUIRED: Historical personal corrections lack operation-level liability and writeoff attribution.");
        var offsets = history.GroupBy(x => x.OperationId).ToDictionary(x => x.Key, x => x.Sum(e => e.SettledCostDeltaUsd));
        var feePipeline = new[] {
            new BsonDocument("$match", new BsonDocument { { "UserId", operation.UserId }, { "Status", "settled" },
                { "AuthorizedAtUtc", new BsonDocument { { "$gte", start }, { "$lt", end } } } }),
            new BsonDocument("$group", new BsonDocument { { "_id", BsonNull.Value }, { "Fees", new BsonDocument("$sum", "$SettledRequestOverageUsd") } })
        };
        using var feeCursor = await _events.AggregateAsync<BsonDocument>(session, feePipeline, cancellationToken: token);
        var fees = (await feeCursor.ToListAsync(token)).SingleOrDefault()?.GetValue("Fees", 0).ToDecimal() ?? 0;
        decimal beforeCost = 0, afterCost = 0, actualCharges = fees, writeoffs = 0;
        var candidates = new List<PersonalCorrectionCandidate>();
        var revisedIds = offsets.Keys.Append(operation.OperationId).ToArray();
        var costFilter = filter & Builders<UsageEventDocument>.Filter.Or(Builders<UsageEventDocument>.Filter.Gt(x => x.SettledCostUsd, 0),
            Builders<UsageEventDocument>.Filter.In(x => x.OperationId, revisedIds));
        using var cursor = await _events.Find(session, costFilter).SortBy(x => x.SettledSequence).ThenBy(x => x.SettledAtUtc).ThenBy(x => x.OperationId).ToCursorAsync(token);
        while (await cursor.MoveNextAsync(token)) foreach (var current in cursor.Current) {
            var cost = checked(current.SettledCostUsd + offsets.GetValueOrDefault(current.OperationId));
            var revised = checked(cost + (current.OperationId == operation.OperationId ? correction.SettledCostDeltaUsd : 0));
            if (current.PaidOveragePricingVersion != null) {
                var bound = Math.Max(0, current.ReservedOverageUsd - current.SettledRequestOverageUsd);
                var before = Math.Min(bound, PaidOveragePricing.AiSettlement(current.AuthorizationPolicy, new() { SettledCostUsd = beforeCost }, cost));
                var after = Math.Min(bound, PaidOveragePricing.AiSettlement(current.AuthorizationPolicy, new() { SettledCostUsd = afterCost }, revised));
                var actual = checked(current.SettledAiOverageUsd + current.PersonalAiCorrectionUsd);
                if (actual < 0 || current.PersonalAiWriteOffUsd < 0 || before - current.PersonalAiWriteOffUsd != actual)
                    throw new SubscriptionUsageConflictException("PERSONAL_CORRECTION_HISTORY_CONFLICT: Original operation charges and attributed writeoffs do not balance.");
                actualCharges = checked(actualCharges + actual); writeoffs = checked(writeoffs + current.PersonalAiWriteOffUsd);
                var target = Math.Max(0, after - current.PersonalAiWriteOffUsd);
                var targetWriteoff = after - target;
                if (target != actual || targetWriteoff != current.PersonalAiWriteOffUsd)
                    candidates.Add(new(current, before, after, actual, target, targetWriteoff));
            }
            beforeCost = checked(beforeCost + cost); afterCost = checked(afterCost + revised);
        }
        var aggregate = await LoadAggregateAsync(session, operation.UserId, operation.AuthorizedAtUtc, token);
        if (actualCharges != aggregate.SettledOverageUsd || writeoffs != history.Sum(x => x.OverageWriteOffDeltaUsd))
            throw new SubscriptionUsageConflictException("PERSONAL_CORRECTION_HISTORY_CONFLICT: Personal monthly charge/writeoff history does not balance.");
        var payers = await ReadPersonalPayersAsync(session, operation.UserId, candidates.Select(x => x.Operation.PersonalOverageChargeTerms), token);
        var sourceIds = candidates.Where(x => x.Operation.BillingStatementId != null).Select(x => x.Operation.BillingStatementId).Distinct(StringComparer.Ordinal).ToArray();
        var sources = sourceIds.Length == 0 ? new List<PaidOverageStatement>() : await OverageStatements.Find(session,
            Builders<PaidOverageStatement>.Filter.In(x => x.Id, sourceIds)).ToListAsync(token);
        // Refunds release capacity before positive redistribution, in deterministic settlement order.
        var running = checked(aggregate.SettledOverageUsd + candidates.Sum(x => Math.Min(0, x.TargetActual - x.OriginalActual)));
        var movements = new List<PersonalChargeMovement>();
        foreach (var candidate in candidates) {
            var current = candidate.Operation;
            ValidatePersonalChargeBinding(current);
            var terms = current.PersonalOverageChargeTerms;
            var target = candidate.TargetActual;
            if (target > candidate.OriginalActual) {
                var available = Math.Max(0, terms.MonthlyCapUsd - running - aggregate.ReservedOverageUsd);
                var accepted = Math.Min(target - candidate.OriginalActual, available);
                target = checked(candidate.OriginalActual + accepted); running = checked(running + accepted);
            }
            var writeoff = candidate.After - target;
            var chargeDelta = target - candidate.OriginalActual;
            var writeoffDelta = writeoff - current.PersonalAiWriteOffUsd;
            if (current.BillingStatementId != null) {
                var source = sources.SingleOrDefault(x => x.Id == current.BillingStatementId);
                if (source == null || source.Scope != OverageScope || source.UserId != operation.UserId || source.Month != correction.Month ||
                    source.AppId != null || source.PayerAvatarId != null || source.StripeCustomerId != payers[terms.ConsentId].CustomerId ||
                    source.PersonalConsentIds == null || !source.PersonalConsentIds.Contains(terms.ConsentId, StringComparer.Ordinal) ||
                    source.OperationIds == null || !source.OperationIds.Contains(current.OperationId, StringComparer.Ordinal))
                    throw new SubscriptionUsageConflictException("PERSONAL_CORRECTION_SOURCE_CONFLICT: Original operation statement binding is unavailable or inconsistent.");
            }
            // Allocation writes the same event/version, so either commit order observes the original statement identity.
            await _events.UpdateOneAsync(session, x => x.OperationId == current.OperationId,
                Builders<UsageEventDocument>.Update.Set(x => x.PersonalAiCorrectionUsd, target - current.SettledAiOverageUsd)
                    .Set(x => x.PersonalAiWriteOffUsd, writeoff).Inc(x => x.PersonalChargeVersion, 1), cancellationToken: token);
            movements.Add(new() { OperationId = current.OperationId, ConsentId = terms.ConsentId, SourceStatementId = current.BillingStatementId,
                ChargeDeltaUsd = chargeDelta, WriteOffDeltaUsd = writeoffDelta });
            if (chargeDelta != 0 && current.BillingStatementId != null)
                await OverageAdjustments.InsertOneAsync(session, new PaidOverageAdjustment {
                    Id = "personal-ai:" + correction.Id + ":" + current.OperationId, UserId = operation.UserId, Month = correction.Month,
                    Scope = OverageScope, SourceStatementId = current.BillingStatementId, SourceOperationId = current.OperationId,
                    PersonalOverageChargeTerms = terms, AmountUsd = chargeDelta, Reason = correction.Reason,
                    EvidenceReference = correction.ExternalReference, CreatedAtUtc = correction.OccurredAtUtc
                }, cancellationToken: token);
        }
        correction.PersonalChargeMovements = movements.ToArray();
        correction.SettledOverageDeltaUsd = movements.Sum(x => x.ChargeDeltaUsd);
        correction.OverageWriteOffDeltaUsd = movements.Sum(x => x.WriteOffDeltaUsd);
        if (checked(aggregate.SettledOverageUsd + correction.SettledOverageDeltaUsd) != running)
            throw new SubscriptionUsageConflictException("PERSONAL_CORRECTION_CONSERVATION_CONFLICT: Attributed personal movements do not conserve monthly liability.");
    }
}
