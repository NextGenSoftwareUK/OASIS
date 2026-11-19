using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Created after remote payment verification; private routing is kept only in protected consent evidence.</summary>
public sealed record PersonalBillingPayerSnapshot(string UserId, string PlanId, string CustomerId,
    string SubscriptionId, long PreferenceVersion, DateTime VerifiedAtUtc)
{
    public static PersonalBillingPayerSnapshot AfterVerification(SubscriptionRecord payer, DateTime verifiedAtUtc)
    {
        if (payer == null || verifiedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A verified subscriber and UTC verification time are required.");
        return new(payer.UserId, payer.PlanId, payer.StripeCustomerId, payer.StripeSubscriptionId,
            payer.PaidOveragePreferenceVersion, PersonalChargeConsent.CanonicalUtc(verifiedAtUtc));
    }
}

/// <summary>Immutable public financial terms. Original private routing stays in the referenced protected audit.</summary>
public sealed class PersonalOverageChargeTerms
{
    public int ConsentSchemaVersion { get; init; }
    public string ConsentId { get; init; }
    public string UserId { get; init; }
    public string PlanId { get; init; }
    public long PreferenceVersion { get; init; }
    public string PricingVersion { get; init; }
    public string PricingSha256 { get; init; }
    public string Currency { get; init; }
    [BsonRepresentation(BsonType.Decimal128)] public decimal RatePerRequestUsd { get; init; }
    [BsonRepresentation(BsonType.Decimal128)] public decimal MonthlyCapUsd { get; init; }
    public DateTime AcceptedAtUtc { get; init; }
}

internal sealed class PersonalChargeConsentAudit
{
    public string UserId { get; init; }
    public string Actor { get; init; }
    public PersonalOverageChargeTerms Terms { get; init; }
    public PersonalBillingPayerSnapshot VerifiedPayer { get; init; }
}

public static class PersonalChargeConsent
{
    internal static DateTime CanonicalUtc(DateTime value) =>
        DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(value.ToUniversalTime()).ToUnixTimeMilliseconds()).UtcDateTime;

    public static bool HasBoundTerms(SubscriptionRecord record) =>
        record != null && record.PaidOverageConsentSchemaVersion == 1 &&
        Guid.TryParseExact(record.PaidOverageConsentId, "D", out var consent) && consent != Guid.Empty &&
        record.PaidOverageConsentPlanId == record.PlanId && record.PlanId is ("bronze" or "silver" or "gold" or "enterprise") &&
        record.PaidOverageConsentPreferenceVersion > 0 &&
        record.PaidOverageConsentPreferenceVersion == record.PaidOveragePreferenceVersion;

    public static bool HasCurrentTerms(SubscriptionRecord record, PaidOveragePricing prices) =>
        HasBoundTerms(record) && prices != null &&
        record.PaidOverageTermsVersion == prices.Version && record.PaidOverageTermsSha256 == prices.PricingSha256 &&
        record.PaidOverageTermsAcceptedAtUtc.HasValue && record.MonthlyOverageLimitUsd >= 0.5m &&
        record.MonthlyOverageLimitUsd <= 1000000m && decimal.Round(record.MonthlyOverageLimitUsd, 2) == record.MonthlyOverageLimitUsd;

    internal static string AuditId(string consentId)
    {
        if (!Guid.TryParseExact(consentId, "D", out var consent) || consent == Guid.Empty)
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_CONSENT_REQUIRED", "Review and accept personal overage terms before additional paid exposure.", 402);
        return "personal-overage-consent:" + consentId;
    }
}

public sealed partial class MongoSubscriptionUsageRepository
{
    private async Task<Dictionary<string, PersonalBillingPayerSnapshot>> ReadPersonalPayersAsync(
        IClientSessionHandle session, string userId, IEnumerable<PersonalOverageChargeTerms> source, CancellationToken token)
    {
        var terms = source.ToArray();
        if (terms.Any(x => x == null))
            throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_CONSENT_REQUIRED: Original personal terms are required.");
        var ids = terms.Select(x => PersonalChargeConsent.AuditId(x.ConsentId)).Distinct(StringComparer.Ordinal).ToArray();
        var documents = ids.Length == 0 ? new List<BsonDocument>() : await BillingAudit.Find(session,
            Builders<BsonDocument>.Filter.In("_id", ids)).ToListAsync(token);
        var evidence = documents.ToDictionary(x => x["_id"].AsString, StringComparer.Ordinal);
        var originals = new Dictionary<string, PersonalChargeConsentAudit>(StringComparer.Ordinal);
        var result = new Dictionary<string, PersonalBillingPayerSnapshot>(StringComparer.Ordinal);
        foreach (var term in terms)
        {
            if (!originals.TryGetValue(term.ConsentId, out var original)) {
                evidence.TryGetValue(PersonalChargeConsent.AuditId(term.ConsentId), out var document);
                original = ReadOriginalPersonalConsent(document, userId, term.ConsentId);
                originals.Add(term.ConsentId, original);
            }
            var approved = original.Terms;
            if (term.ConsentSchemaVersion != approved.ConsentSchemaVersion || term.UserId != approved.UserId ||
                term.PlanId != approved.PlanId || term.PreferenceVersion != approved.PreferenceVersion ||
                term.PricingVersion != approved.PricingVersion || term.PricingSha256 != approved.PricingSha256 ||
                term.Currency != approved.Currency || term.RatePerRequestUsd != approved.RatePerRequestUsd ||
                term.MonthlyCapUsd != approved.MonthlyCapUsd || term.AcceptedAtUtc != approved.AcceptedAtUtc)
                throw new SubscriptionUsageConflictException("PERSONAL_STATEMENT_CONSENT_CONFLICT: Captured terms differ from original protected consent.");
            result[term.ConsentId] = original.VerifiedPayer;
        }
        return result;
    }

    private static void ValidatePersonalChargeBinding(SubscriptionUsageEvent operation)
    {
        var terms = operation.PersonalOverageChargeTerms;
        if (terms == null || terms.UserId != operation.UserId || terms.PricingVersion != operation.PaidOveragePricingVersion ||
            operation.ReservedRequestOverageUsd < 0 || operation.ReservedRequestOverageUsd > terms.RatePerRequestUsd ||
            operation.SettledRequestOverageUsd < 0 || operation.SettledRequestOverageUsd > operation.ReservedRequestOverageUsd ||
            operation.SettledAiOverageUsd + operation.PersonalAiCorrectionUsd < 0 ||
            operation.SettledAiOverageUsd + operation.PersonalAiCorrectionUsd > Math.Max(0, operation.ReservedOverageUsd - operation.SettledRequestOverageUsd) ||
            operation.PersonalAiWriteOffUsd < 0)
            throw new SubscriptionUsageConflictException("PERSONAL_CHARGE_BINDING_INVALID: Personal liability differs from its original subscriber, terms or reserved exposure.");
    }

    private async Task<PersonalOverageChargeTerms> BindPersonalChargeAsync(IClientSessionHandle session,
        SubscriptionRecord subscriber, SubscriptionUsageEvent operation, CancellationToken token)
    {
        var prices = _overagePricing ?? PaidOveragePricing.Load();
        if (!PersonalChargeConsent.HasCurrentTerms(subscriber, prices))
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_CONSENT_REQUIRED", "Review and accept the current personal overage terms for this billing account.", 402);
        var document = await BillingAudit.Find(session, new BsonDocument("_id", PersonalChargeConsent.AuditId(subscriber.PaidOverageConsentId)))
            .FirstOrDefaultAsync(token);
        var original = ReadOriginalPersonalConsent(document, subscriber.UserId, subscriber.PaidOverageConsentId);
        var terms = original.Terms;
        if (terms.PlanId != subscriber.PlanId || terms.PreferenceVersion != subscriber.PaidOverageConsentPreferenceVersion ||
            terms.PricingVersion != subscriber.PaidOverageTermsVersion || terms.PricingSha256 != subscriber.PaidOverageTermsSha256 ||
            terms.AcceptedAtUtc != subscriber.PaidOverageTermsAcceptedAtUtc || terms.MonthlyCapUsd != subscriber.MonthlyOverageLimitUsd ||
            terms.RatePerRequestUsd != prices.Rate(subscriber.PlanId))
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_CONSENT_CONFLICT", "Personal billing settings differ from their original consent. Review and authorize them again.", 409);
        if (original.VerifiedPayer.CustomerId != subscriber.StripeCustomerId || original.VerifiedPayer.SubscriptionId != subscriber.StripeSubscriptionId)
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_PAYER_CHANGED", "The personal billing account changed. Verify and authorize it again.", 402);
        if (operation.UserId != subscriber.UserId || operation.PaidOveragePricingVersion != terms.PricingVersion ||
            operation.ReservedRequestOverageUsd < 0 || operation.ReservedRequestOverageUsd > terms.RatePerRequestUsd)
            throw new SubscriptionUsageConflictException("PERSONAL_OVERAGE_BINDING_INVALID: Reservation differs from its original subscriber and terms.");
        return terms;
    }

    private static PersonalChargeConsentAudit ReadOriginalPersonalConsent(BsonDocument document, string userId, string consentId)
    {
        if (document == null || document.GetValue("_id", "").AsString != PersonalChargeConsent.AuditId(consentId) ||
            document.GetValue("Kind", "").AsString != "overage-consent" || document.GetValue("UserId", "").AsString != userId)
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_CONSENT_REQUIRED", "Original personal consent evidence is unavailable. New paid exposure requires fresh consent.", 402);
        var original = JsonSerializer.Deserialize<PersonalChargeConsentAudit>(document["PayloadJson"].AsString);
        var terms = original?.Terms;
        var payer = original?.VerifiedPayer;
        if (!Guid.TryParseExact(userId, "D", out var user) || user == Guid.Empty || original?.UserId != userId ||
            original.Actor != userId || terms == null || payer == null || terms.ConsentSchemaVersion != 1 || terms.UserId != userId || terms.ConsentId != consentId ||
            payer.UserId != userId || payer.PlanId != terms.PlanId || terms.PlanId is not ("bronze" or "silver" or "gold" or "enterprise") ||
            terms.PreferenceVersion <= 0 || payer.PreferenceVersion != terms.PreferenceVersion - 1 || terms.Currency != "USD" ||
            string.IsNullOrWhiteSpace(terms.PricingVersion) || terms.PricingSha256?.Length != 64 ||
            !terms.PricingSha256.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            terms.RatePerRequestUsd <= 0 || terms.RatePerRequestUsd > 1 || terms.MonthlyCapUsd < 0.5m ||
            terms.MonthlyCapUsd > 1000000m || decimal.Round(terms.MonthlyCapUsd, 2) != terms.MonthlyCapUsd ||
            string.IsNullOrWhiteSpace(payer.CustomerId) || string.IsNullOrWhiteSpace(payer.SubscriptionId) ||
            terms.AcceptedAtUtc.Kind != DateTimeKind.Utc || payer.VerifiedAtUtc.Kind != DateTimeKind.Utc ||
            terms.AcceptedAtUtc == default || payer.VerifiedAtUtc == default || payer.VerifiedAtUtc > terms.AcceptedAtUtc || terms.AcceptedAtUtc - payer.VerifiedAtUtc > TimeSpan.FromMinutes(5))
            throw new SubscriptionUsageLimitException("PERSONAL_OVERAGE_CONSENT_CONFLICT", "Original personal consent evidence does not match its subscriber, terms and verified billing account.", 409);
        return original;
    }
}
