using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// An application (for example PIPER) that holds a plan of its own. A request that presents the app's id and key
    /// is evaluated against the more generous of the app's plan and the signed-in avatar's personal plan, so a product
    /// with many users does not need a grant per user. Usage is still recorded per avatar, and the settlement protocol
    /// is unchanged; requests without app credentials behave exactly as before.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class AppAccountRecord
    {
        [BsonId] public string AppId { get; set; }
        public string Name { get; set; }
        public string PlanId { get; set; }
        /// <summary>active or disabled.</summary>
        public string Status { get; set; }
        /// <summary>SHA-256 of the key. The key itself is shown once, when created or rotated, and never stored.</summary>
        public string KeyHashSha256 { get; set; }
        public string KeyPrefix { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string UpdatedBy { get; set; }
        public string Reason { get; set; }
        /// <summary>When true (and the pooled-quota feature is on) the app's plan is ONE pool shared by all its users.</summary>
        public bool PooledQuota { get; set; }
        /// <summary>Personal plan floor for users while they use a pooled app. Defaults to silver.</summary>
        public string PerUserPlanId { get; set; }

        /// <summary>A copy that is safe to return from an API: no key hash.</summary>
        public AppAccountRecord Redacted()
        {
            var copy = (AppAccountRecord)MemberwiseClone();
            copy.KeyHashSha256 = null;
            return copy;
        }
    }

    public interface IAppAccountRepository
    {
        Task<(AppAccountRecord Account, string ApiKey)> CreateAppAccountAsync(string appId, string name, string planId, string actor, string reason, bool pooledQuota = false, string perUserPlanId = null, CancellationToken cancellationToken = default);
        Task<(AppAccountRecord Account, string ApiKey)> RotateAppAccountKeyAsync(string appId, string actor, string reason, CancellationToken cancellationToken = default);
        Task<AppAccountRecord> UpdateAppAccountAsync(string appId, string planId, string status, string actor, string reason, bool? pooledQuota = null, string perUserPlanId = null, CancellationToken cancellationToken = default);
        /// <summary>Returns the active account when the id and key match; null otherwise (never says which part was wrong).</summary>
        Task<AppAccountRecord> VerifyAppCredentialAsync(string appId, string apiKey, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AppAccountRecord>> ListAppAccountsAsync(CancellationToken cancellationToken = default);
    }

    public static class AppAccountRules
    {
        public const string AppIdHeader = "X-OASIS-App-Id";
        public const string AppKeyHeader = "X-OASIS-App-Key";
        public const string KeyPrefixText = "oak_";

        private static readonly string[] PlanOrder = { "free", "bronze", "silver", "gold", "enterprise" };
        private static readonly Regex AppIdPattern = new("^[a-z0-9][a-z0-9-]{2,39}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsValidAppId(string appId) => appId != null && AppIdPattern.IsMatch(appId);

        public static void ValidateAppId(string appId)
        {
            if (!IsValidAppId(appId)) throw new ArgumentException("The app id must be 3 to 40 characters: lowercase letters, digits and hyphens, starting with a letter or digit.");
        }

        public static void ValidatePlan(string planId)
        {
            if (Array.IndexOf(PlanOrder, planId) < 0) throw new ArgumentException("Plan must be free, bronze, silver, gold or enterprise.");
        }

        /// <summary>
        /// Until app-wide pooled quotas exist, an app plan is a per-user entitlement, which only makes sense for an
        /// unlimited plan. A finite app plan would multiply its limits by the number of users, so it is refused.
        /// </summary>
        public static void ValidateAppPlan(string planId, bool pooled = false)
        {
            ValidatePlan(planId);
            if (planId == "enterprise") return;
            if (!pooled)
                throw new ArgumentException("A finite app plan must be a pooled quota (PooledQuota=true); only the enterprise plan can be used without a pool.");
            if (!PooledQuotasEnabled)
                throw new ArgumentException("Pooled app quotas are not enabled on this server (set OASIS_POOLED_APP_QUOTAS=true).");
        }

        public const string PooledFlagName = "OASIS_POOLED_APP_QUOTAS";
        public const string DefaultPerUserPlanId = "silver";

        /// <summary>Feature flag, off by default so every existing flow is unchanged until it is switched on.</summary>
        public static bool PooledQuotasEnabled =>
            string.Equals(Environment.GetEnvironmentVariable(PooledFlagName), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>True when this app's requests are also counted against, and limited by, a shared app pool.</summary>
        public static bool UsesPool(AppAccountRecord app) =>
            app != null && app.PooledQuota && app.PlanId != "enterprise" && PooledQuotasEnabled;

        public static string PerUserPlan(AppAccountRecord app) =>
            Rank(app?.PerUserPlanId) > 0 ? app.PerUserPlanId : DefaultPerUserPlanId;

        public static void ValidatePerUserPlan(string planId)
        {
            if (planId == null) return;
            ValidatePlan(planId);
            if (planId == "enterprise") throw new ArgumentException("The per-user plan cannot be enterprise; that would defeat the pool.");
        }

        public static void ValidateActorAndReason(string actor, string reason)
        {
            if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("The administrator is required.");
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new ArgumentException("A reason of at least 5 characters is required.");
            if (reason.Trim().Length > 500) throw new ArgumentException("The reason must be 500 characters or fewer.");
        }

        public static string GenerateKey() => KeyPrefixText + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key ?? ""))).ToLowerInvariant();

        /// <summary>Fixed-time comparison of the presented key against the stored hash.</summary>
        public static bool KeyMatches(string presentedKey, string storedHash)
        {
            if (string.IsNullOrEmpty(presentedKey) || string.IsNullOrEmpty(storedHash)) return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(presentedKey)), Encoding.UTF8.GetBytes(storedHash));
        }

        public static int Rank(string planId) => Array.IndexOf(PlanOrder, planId);

        /// <summary>The app's plan applies only when it is better than the avatar's own plan; an app can never make a user worse off.</summary>
        public static string EffectivePlanId(string avatarPlanId, string appPlanId) =>
            Rank(appPlanId) > Rank(avatarPlanId) ? appPlanId : avatarPlanId;
    }
}
