using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public static class UsageLedgerValidation
    {
        public static void ValidateSettlement(string userId, UsageSettlementRequest value)
        {
            if (!Guid.TryParseExact(userId, "D", out _) || value == null || value.UserId != userId ||
                !Guid.TryParseExact(value.OperationId, "D", out _) || !SubscriptionServiceIdentity.IsService(value.ConsumingService))
                throw new ArgumentException("Canonical avatar and operation UUIDs and a consuming service are required.");
            if (value.Outcome is not ("succeeded" or "failed" or "cancelled"))
                throw new ArgumentException("Outcome must be succeeded, failed, or cancelled.");
            if (value.PromptTokens < 0 || value.CompletionTokens < 0 || value.Units < 0 || value.EstimatedCostUsd < 0 ||
                !value.ActualCostUsd.HasValue || value.ActualCostUsd.Value < 0)
                throw new ArgumentException("Actual measured nonnegative cost and usage are required; estimates cannot settle an operation.");
            _ = checked(value.PromptTokens + value.CompletionTokens);
            if (string.IsNullOrWhiteSpace(value.Provider) || value.Provider.Length > 128 ||
                string.IsNullOrWhiteSpace(value.ProviderRequestId) || value.ProviderRequestId.Length > 256 || value.Model?.Length > 256 ||
                string.IsNullOrWhiteSpace(value.CostSource) || value.CostSource.Length > 128 ||
                string.IsNullOrWhiteSpace(value.PricingCatalogueVersion) || value.PricingCatalogueVersion.Length > 128)
                throw new ArgumentException("Provider identity, immutable receipt reference, measurement source and pricing version are required.");
            if (value.ProviderReceipts == null || value.ProviderReceipts.Count == 0 || value.ProviderReceipts.Count > 1000)
                throw new ArgumentException("Between one and 1000 measured provider or service receipts are required.");
            foreach (var receipt in value.ProviderReceipts)
            {
                if (receipt == null) throw new ArgumentException("A provider receipt cannot be null.");
                try { receipt.Validate(); }
                catch (UsageProtocolException ex) { throw new ArgumentException(ex.Message, ex); }
            }
            if (value.ProviderReceipts.GroupBy(x => new { x.Provider, x.ProviderRequestId }).Any(x => x.Count() != 1))
                throw new SubscriptionUsageConflictException("Duplicate provider receipt identifiers are not permitted.");
            if (value.ProviderReceipts.Sum(x => x.PromptTokens) != value.PromptTokens ||
                value.ProviderReceipts.Sum(x => x.CompletionTokens) != value.CompletionTokens ||
                value.ProviderReceipts.Sum(x => x.Units) != value.Units || value.ProviderReceipts.Sum(x => x.ActualCostUsd) != value.ActualCostUsd.Value)
                throw new ArgumentException("Settlement totals must exactly equal their measured receipt totals.");
        }

        // Explicit fields exclude JSON property order. Receipt order is not significant, while every
        // supplied identity, quantity and pricing field participates in conflict detection.
        public static string SettlementFingerprint(UsageSettlementRequest value)
        {
            string json = JsonSerializer.Serialize(new {
                value.OperationId, value.UserId, value.ConsumingService, value.Outcome, value.Provider, value.Model,
                value.ProviderRequestId, value.PromptTokens, value.CompletionTokens, value.Units,
                EstimatedCostUsd = value.EstimatedCostUsd.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
                ActualCostUsd = value.ActualCostUsd?.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), value.CostSource, value.PricingCatalogueVersion,
                Receipts = value.ProviderReceipts.OrderBy(x => x.Provider, StringComparer.Ordinal).ThenBy(x => x.ProviderRequestId, StringComparer.Ordinal).Select(x => new {
                    x.Provider, x.Model, x.ProviderRequestId, x.ClientRequestId, x.PromptTokens, x.CompletionTokens, x.Units,
                    ActualCostUsd = x.ActualCostUsd.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), x.CostSource, x.PricingCatalogueVersion })
            });
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }
    }
}
