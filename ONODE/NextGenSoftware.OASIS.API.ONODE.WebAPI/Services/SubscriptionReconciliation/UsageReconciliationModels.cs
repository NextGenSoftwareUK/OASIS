using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;

/// <summary>Independent evidence, never a replacement for the WEB4 ledger.</summary>
public sealed class UsageExternalReceipt
{
    [BsonId] public string Id { get; set; }
    public string Source { get; set; } // provider or stripe
    public string UserId { get; set; }
    public string Month { get; set; }
    public string OperationId { get; set; }
    public string ConsumingService { get; set; }
    public string ProviderRequestId { get; set; }
    public string Provider { get; set; }
    public string ExternalId { get; set; }
    [BsonRepresentation(BsonType.Decimal128)] public decimal AmountUsd { get; set; }
    public string EvidenceSha256 { get; set; }
    public string Actor { get; set; }
    public DateTime RecordedAtUtc { get; set; }

    public void Validate()
    {
        if (!Guid.TryParseExact(UserId, "D", out _) || !UsageReconciliationMath.IsMonth(Month))
            throw new ArgumentException("A canonical avatar UUID and UTC yyyy-MM month are required.");
        if (Source is not ("provider" or "stripe") || AmountUsd < 0 || string.IsNullOrWhiteSpace(ExternalId) || ExternalId.Length > 200)
            throw new ArgumentException("Receipt source, non-negative USD amount and external identifier are required.");
        if (Source == "provider" && (!Guid.TryParseExact(OperationId, "D", out _) ||
            ConsumingService is not ("WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10") ||
            string.IsNullOrWhiteSpace(Provider) || string.IsNullOrWhiteSpace(ProviderRequestId)))
            throw new ArgumentException("Provider evidence must identify the operation, service, provider and provider request.");
        if (string.IsNullOrEmpty(EvidenceSha256) || EvidenceSha256.Length != 64 || !EvidenceSha256.All(Uri.IsHexDigit))
            throw new ArgumentException("EvidenceSha256 must identify the retained source evidence.");
        if (string.IsNullOrWhiteSpace(Actor)) throw new ArgumentException("Authenticated evidence actor is required.");
        Id = Source + ":" + (Source == "provider" ? ConsumingService + ":" + Provider + ":" : "") + ExternalId;
    }
}

public sealed class UsageReconciliationFinding
{
    public string Code { get; set; }
    public string Reference { get; set; }
    public string Detail { get; set; }
}

public sealed class UsageReconciliationReport
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string UserId { get; set; }
    public string Month { get; set; }
    public DateTime CheckedAtUtc { get; set; }
    public bool ProviderEvidenceComplete { get; set; }
    public bool StripeEvidenceComplete { get; set; }
    public bool AwaitingPeriodClose { get; set; }
    public int UnsettledOperations { get; set; }
    public List<UsageReconciliationFinding> Findings { get; set; } = new();
    public bool ActionRequired => Findings.Count > 0;
    public bool Balanced => !ActionRequired && !AwaitingPeriodClose && ProviderEvidenceComplete && StripeEvidenceComplete && UnsettledOperations == 0;
}

public readonly record struct UsageLedgerTotals(long Requests, long ReservedUnits, long SettledUnits, decimal ReservedCostUsd, decimal SettledCostUsd)
{
    public static UsageLedgerTotals operator +(UsageLedgerTotals a, UsageLedgerTotals b) => new(
        checked(a.Requests + b.Requests), checked(a.ReservedUnits + b.ReservedUnits), checked(a.SettledUnits + b.SettledUnits),
        checked(a.ReservedCostUsd + b.ReservedCostUsd), checked(a.SettledCostUsd + b.SettledCostUsd));
}

public static class UsageReconciliationMath
{
    public static bool IsMonth(string month) => DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out _);

    // Internal receipts have no vendor invoice. Match their entire accounting shape,
    // not a caller-supplied label that could conceal actual external provider work.
    public static bool IsInternalReceipt(JsonElement receipt, string service, string operationId, string outcome)
    {
        if (service is not ("WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10")) return false;
        if (outcome is not ("succeeded" or "failed" or "cancelled")) return false;
        string provider = String(receipt, "Provider");
        string source = String(receipt, "CostSource");
        if (provider != service && !(service == "WEB6" && provider == "WEB6-cache")) return false;
        if (!Zero(receipt, "PromptTokens") || !Zero(receipt, "CompletionTokens")) return false;
        if (service == "WEB6" && provider == "WEB6-cache")
            return source == "semantic-cache" && String(receipt, "Model") == "semantic-cache" &&
                String(receipt, "PricingCatalogueVersion") == "web6-cache-v1" &&
                String(receipt, "ClientRequestId") == operationId + ":cache" &&
                String(receipt, "ProviderRequestId") == operationId + ":cache" &&
                Zero(receipt, "Units") && Zero(receipt, "ActualCostUsd");
        if (String(receipt, "ProviderRequestId") != operationId || !string.IsNullOrEmpty(String(receipt, "Model")) ||
            !string.IsNullOrEmpty(String(receipt, "ClientRequestId"))) return false;
        if (source == "not-executed")
            return outcome is "failed" or "cancelled" && String(receipt, "PricingCatalogueVersion") == "no-provider-execution-v1" &&
                Zero(receipt, "Units") && Zero(receipt, "ActualCostUsd");
        return source == "service-catalogue" && !string.IsNullOrWhiteSpace(String(receipt, "PricingCatalogueVersion")) &&
            receipt.TryGetProperty("Units", out var units) && units.TryGetInt64(out var count) && count == (outcome == "succeeded" ? 1 : 0) &&
            receipt.TryGetProperty("ActualCostUsd", out var cost) && cost.TryGetDecimal(out var amount) && amount >= 0 &&
            (outcome == "succeeded" || amount == 0);
    }

    private static string String(JsonElement receipt, string name) =>
        receipt.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Zero(JsonElement receipt, string name) =>
        receipt.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) && number == 0;

    public static UsageLedgerTotals ReadAudit(BsonDocument document) => new(
        document["RequestsDelta"].ToInt64(), document["ReservedUnitsDelta"].ToInt64(), document["SettledUnitsDelta"].ToInt64(),
        document["ReservedCostDeltaUsd"].ToDecimal(), document["SettledCostDeltaUsd"].ToDecimal());

    public static UsageLedgerTotals ReadBucket(BsonDocument document) => new(
        document["Requests"].ToInt64(), document["ReservedUnits"].ToInt64(), document["SettledUnits"].ToInt64(),
        document["ReservedCostUsd"].ToDecimal(), document["SettledCostUsd"].ToDecimal());

    // Receipt retries compare every accounting field. Server-assigned time/actor are excluded.
    public static bool SameReceipt(UsageExternalReceipt a, UsageExternalReceipt b) =>
        a.Id == b.Id && a.UserId == b.UserId && a.Month == b.Month && a.OperationId == b.OperationId &&
        a.ConsumingService == b.ConsumingService && a.ProviderRequestId == b.ProviderRequestId &&
        a.Provider == b.Provider && a.ExternalId == b.ExternalId && a.Source == b.Source &&
        a.AmountUsd == b.AmountUsd && a.EvidenceSha256 == b.EvidenceSha256;
}
