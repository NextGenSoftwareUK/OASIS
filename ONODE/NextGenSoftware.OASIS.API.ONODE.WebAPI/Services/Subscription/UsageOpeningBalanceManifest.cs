using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class UsageOpeningBalance
    {
        public string UserId { get; set; }
        public string Day { get; set; }
        public long Requests { get; set; }
        public long ReservedTokens { get; set; }
        public long SettledTokens { get; set; }
        public decimal ReservedCostUsd { get; set; }
        public decimal SettledCostUsd { get; set; }
    }
    public sealed class UsageMigrationInventory
    {
        public string SourceDigest { get; set; }
        public string LegacyAggregatesJson { get; set; }
        public string LegacyOperationsJson { get; set; }
        public bool AlreadyImported { get; set; }
    }
    public sealed class UsageOpeningBalanceManifest
    {
        public string MigrationId { get; set; }
        public int BatchIndex { get; set; }
        public int BatchCount { get; set; } = 1;
        public string ApprovedBy { get; set; }
        public string SourceDigest { get; set; }
        public string EvidenceSha256 { get; set; }
        public string Signature { get; set; }
        public List<UsageOpeningBalance> Balances { get; set; } = new();
        public List<SubscriptionRecord> Subscriptions { get; set; } = new();
        public List<OrderRecord> Orders { get; set; } = new();
    }
    public static class UsageMigrationSignature
    {
        public static string CanonicalJson(UsageOpeningBalanceManifest manifest) => JsonSerializer.Serialize(new {
            manifest.MigrationId, manifest.BatchIndex, manifest.BatchCount, manifest.ApprovedBy, manifest.SourceDigest, manifest.EvidenceSha256,
            Balances = manifest.Balances.OrderBy(x => x.UserId, StringComparer.Ordinal).ThenBy(x => x.Day, StringComparer.Ordinal),
            Subscriptions = manifest.Subscriptions.OrderBy(x => x.UserId, StringComparer.Ordinal),
            Orders = manifest.Orders.OrderBy(x => x.StripeInvoiceId, StringComparer.Ordinal)
        });
        public static void RequireApproval(UsageOpeningBalanceManifest manifest, string actor, IConfiguration configuration)
        {
            if (manifest == null || manifest.ApprovedBy != actor || !Guid.TryParseExact(manifest.MigrationId, "D", out _) ||
                !IsHash(manifest.SourceDigest) || !IsHash(manifest.EvidenceSha256) || !IsHash(manifest.Signature))
                throw new ArgumentException("A reviewed opening-balance manifest, source digest, evidence hash and detached approval signature are required.");
            var key = configuration["SUBSCRIPTION_MIGRATION_APPROVAL_KEY"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                throw new InvalidOperationException("SUBSCRIPTION_MIGRATION_APPROVAL_KEY must have at least 32 bytes.");
            var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(CanonicalJson(manifest)));
            if (!CryptographicOperations.FixedTimeEquals(signature, Convert.FromHexString(manifest.Signature)))
                throw new UnauthorizedAccessException("Opening-balance approval signature is invalid.");
        }
        private static bool IsHash(string value) => value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-f0-9]{64}$");
    }
}
