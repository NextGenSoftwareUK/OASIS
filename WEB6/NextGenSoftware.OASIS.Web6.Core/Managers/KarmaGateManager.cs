using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Enforces subscription-plan-based AI model access with a karma boost.
    ///
    /// Primary gate: OPORTAL subscription plan determines the base access tier.
    /// Karma boost: sufficient karma raises the effective tier by one level above the plan baseline,
    /// rewarding engaged community members without replacing the subscription model.
    ///
    /// Effective tier matrix:
    ///
    ///   Plan        Base tier    Karma boost (if karma >= KarmaBoostThreshold)
    ///   ─────────   ─────────    ────────────────────────────────────────────
    ///   Free        Bronze       → Silver
    ///   Starter     Silver       → Gold
    ///   Pro         Gold         → Diamond
    ///   Enterprise  Diamond      (already max — karma gives priority label only)
    /// </summary>
    public static class KarmaGateManager
    {
        // ── Karma boost threshold ─────────────────────────────────────────────────

        /// <summary>
        /// Avatar karma score needed to receive a one-tier boost above the subscription plan baseline.
        /// A reasonable default is 1,000 — achievable through normal OurWorld participation.
        /// </summary>
        public const int KarmaBoostThreshold = 1_000;

        // ── Model tier requirements ──────────────────────────────────────────────

        /// <summary>Models (substring match, case-insensitive) that require Silver tier or above.</summary>
        private static readonly string[] SilverModels = new[]
        {
            "gpt-4o",
            "claude-sonnet",
            "claude-3-5",
            "claude-3-7",
            "gemini-pro",
            "gemini-1.5-pro",
            "gemini-2.0-pro",
            "mistral-large",
            "command-r-plus",
            "llama-3.1-70b",
            "llama-3.3-70b",
            "mixtral-8x22b",
        };

        /// <summary>Models (substring match, case-insensitive) that require Gold tier or above.</summary>
        private static readonly string[] GoldModels = new[]
        {
            "gpt-5",
            "o1",
            "o3",
            "claude-opus",
            "claude-4",
            "claude-5",
            "gemini-2.5-pro",
            "gemini-ultra",
            "grok-3",
        };

        // ── Provider tier requirements ───────────────────────────────────────────

        /// <summary>Providers that require at least Silver tier (e.g. enterprise-priced APIs).</summary>
        private static readonly HashSet<AIProviderType> SilverProviders = new HashSet<AIProviderType>
        {
            AIProviderType.AWSBedrock,
            AIProviderType.AzureOpenAI,
        };

        // ── Fallback models ──────────────────────────────────────────────────────

        private const string BronzeFallbackModel = "llama-3.1-8b-instant";
        private const string SilverFallbackModel = "gpt-4o-mini";

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the effective access tier from the avatar's subscription plan and optional karma boost,
        /// then validates the requested provider and model. Returns a <see cref="KarmaGateResult"/>
        /// describing whether access is allowed as-is, downgraded, or boosted.
        /// </summary>
        public static KarmaGateResult Evaluate(
            SubscriptionPlan plan,
            int karma,
            AIProviderType provider,
            string model)
        {
            // Resolve effective tier (plan baseline + optional karma boost).
            AccessTier baseTier    = PlanToBaseTier(plan);
            bool       karmaBoost  = karma >= KarmaBoostThreshold && baseTier < AccessTier.Diamond;
            AccessTier effectiveTier = karmaBoost ? baseTier + 1 : baseTier;

            string tierLabel = TierLabel(effectiveTier, karmaBoost);

            // Enterprise/Diamond: always pass through.
            if (effectiveTier >= AccessTier.Diamond)
                return KarmaGateResult.Allowed(tierLabel, karmaBoost);

            // Provider check.
            if (SilverProviders.Contains(provider) && effectiveTier < AccessTier.Silver)
                return KarmaGateResult.Downgraded(tierLabel, karmaBoost,
                    $"Provider '{provider}' requires a Bronze plan or higher (or 1,000+ karma on Free). Routing to an available provider.",
                    AIProviderType.Groq, BronzeFallbackModel);

            // Model check.
            if (!string.IsNullOrEmpty(model))
            {
                string ml = model.ToLowerInvariant();

                foreach (string sub in GoldModels)
                    if (ml.Contains(sub.ToLowerInvariant()) && effectiveTier < AccessTier.Gold)
                        return KarmaGateResult.Downgraded(tierLabel, karmaBoost,
                            $"Model '{model}' requires a Silver plan or higher (or 1,000+ karma on Bronze). Falling back to a Silver-tier model.",
                            provider, SilverFallbackModel);

                foreach (string sub in SilverModels)
                    if (ml.Contains(sub.ToLowerInvariant()) && effectiveTier < AccessTier.Silver)
                        return KarmaGateResult.Downgraded(tierLabel, karmaBoost,
                            $"Model '{model}' requires a Bronze plan or higher (or 1,000+ karma on Free). Falling back to a Bronze-tier model.",
                            provider, BronzeFallbackModel);
            }

            return KarmaGateResult.Allowed(tierLabel, karmaBoost);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static AccessTier PlanToBaseTier(SubscriptionPlan plan) => plan switch
        {
            SubscriptionPlan.Bronze     => AccessTier.Silver,   // Bronze ($9)  → Silver-tier models
            SubscriptionPlan.Silver     => AccessTier.Gold,     // Silver ($29) → Gold-tier models
            SubscriptionPlan.Gold       => AccessTier.Diamond,  // Gold ($99)   → all models
            SubscriptionPlan.Enterprise => AccessTier.Diamond,  // Enterprise   → all models + priority
            _                           => AccessTier.Bronze,   // Free         → base models only
        };

        private static string TierLabel(AccessTier tier, bool boosted) =>
            (tier, boosted) switch
            {
                (AccessTier.Diamond, true)  => "Diamond (karma boost)",
                (AccessTier.Gold,    true)  => "Gold (karma boost)",
                (AccessTier.Silver,  true)  => "Silver (karma boost)",
                (AccessTier.Diamond, false) => "Diamond",
                (AccessTier.Gold,    false) => "Gold",
                (AccessTier.Silver,  false) => "Silver",
                _                           => "Bronze",
            };

        /// <summary>Internal tier ordering — not exposed in the public API.</summary>
        private enum AccessTier { Bronze = 0, Silver = 1, Gold = 2, Diamond = 3 }
    }

    /// <summary>Result of a subscription/karma gate evaluation.</summary>
    public class KarmaGateResult
    {
        public bool IsAllowed     { get; private set; }
        public bool IsDowngraded  { get; private set; }
        public bool KarmaBoosted  { get; private set; }
        public string TierLabel   { get; private set; }
        public string Reason      { get; private set; }
        public AIProviderType DowngradedProvider { get; private set; }
        public string DowngradedModel            { get; private set; }

        private KarmaGateResult() { }

        public static KarmaGateResult Allowed(string tierLabel, bool boosted) =>
            new KarmaGateResult { IsAllowed = true, TierLabel = tierLabel, KarmaBoosted = boosted };

        public static KarmaGateResult Downgraded(string tierLabel, bool boosted, string reason, AIProviderType fallbackProvider, string fallbackModel) =>
            new KarmaGateResult
            {
                IsAllowed        = true,
                IsDowngraded     = true,
                KarmaBoosted     = boosted,
                TierLabel        = tierLabel,
                Reason           = reason,
                DowngradedProvider = fallbackProvider,
                DowngradedModel  = fallbackModel,
            };
    }
}
