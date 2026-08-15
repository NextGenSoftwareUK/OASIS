using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Enforces karma-gated AI model access tiers.
    /// Tier thresholds and model allow-lists are intentionally flat and easy to extend.
    ///
    /// Tiers:
    ///   Bronze  (0–999)      — fast/cheap models only; good for most tasks
    ///   Silver  (1000–4999)  — mid-tier models; adds GPT-4o, Sonnet, Gemini Pro
    ///   Gold    (5000–9999)  — all models, all providers
    ///   Diamond (10000+)     — all models + priority routing label
    /// </summary>
    public static class KarmaGateManager
    {
        // ── Tier thresholds ──────────────────────────────────────────────────────────

        public const int SilverThreshold  =  1_000;
        public const int GoldThreshold    =  5_000;
        public const int DiamondThreshold = 10_000;

        // ── Model tier requirements ──────────────────────────────────────────────────

        /// <summary>
        /// Models that require at least Silver tier. Any model name containing one of these
        /// substrings (case-insensitive) is restricted to Silver+.
        /// </summary>
        private static readonly string[] SilverRequiredModelSubstrings = new[]
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

        /// <summary>
        /// Models that require Gold tier. Any model name containing one of these substrings
        /// (case-insensitive) requires Gold+.
        /// </summary>
        private static readonly string[] GoldRequiredModelSubstrings = new[]
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

        // ── Provider tier requirements ───────────────────────────────────────────────

        /// <summary>Providers that require at least Silver tier.</summary>
        private static readonly HashSet<AIProviderType> SilverRequiredProviders = new HashSet<AIProviderType>
        {
            AIProviderType.AWSBedrock,
            AIProviderType.AzureOpenAI,
        };

        /// <summary>Providers that require Gold tier (advanced/enterprise).</summary>
        private static readonly HashSet<AIProviderType> GoldRequiredProviders = new HashSet<AIProviderType>();

        // ── Fallback models per tier ─────────────────────────────────────────────────

        private static readonly string BronzeFallbackModel = "llama-3.1-8b-instant";
        private static readonly string SilverFallbackModel = "gpt-4o-mini";

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>Returns the karma tier name for a given karma score.</summary>
        public static string GetTierName(int karma) => karma switch
        {
            >= DiamondThreshold => "Diamond",
            >= GoldThreshold    => "Gold",
            >= SilverThreshold  => "Silver",
            _                   => "Bronze",
        };

        /// <summary>
        /// Validates and optionally adjusts a completion request based on the avatar's karma tier.
        /// Returns a <see cref="KarmaGateResult"/> describing whether access is allowed, was downgraded,
        /// or is denied (which should result in a 403 response).
        /// </summary>
        public static KarmaGateResult Evaluate(int karma, AIProviderType provider, string model)
        {
            string tier = GetTierName(karma);

            // Gold and Diamond always pass through.
            if (karma >= GoldThreshold)
                return KarmaGateResult.Allowed(tier);

            // ── Provider checks ────────────────────────────────────────────────────

            if (GoldRequiredProviders.Contains(provider) && karma < GoldThreshold)
                return KarmaGateResult.Downgraded(tier,
                    $"Provider '{provider}' requires Gold tier ({GoldThreshold}+ karma). Routing to Silver-tier provider.",
                    AIProviderType.OpenAI, SilverFallbackModel);

            if (SilverRequiredProviders.Contains(provider) && karma < SilverThreshold)
                return KarmaGateResult.Downgraded(tier,
                    $"Provider '{provider}' requires Silver tier ({SilverThreshold}+ karma). Routing to Bronze-tier provider.",
                    AIProviderType.Groq, BronzeFallbackModel);

            // ── Model checks ──────────────────────────────────────────────────────

            if (!string.IsNullOrEmpty(model))
            {
                string modelLower = model.ToLowerInvariant();

                // Gold-required models — downgrade if below Gold
                foreach (string sub in GoldRequiredModelSubstrings)
                {
                    if (modelLower.Contains(sub.ToLowerInvariant()) && karma < GoldThreshold)
                        return KarmaGateResult.Downgraded(tier,
                            $"Model '{model}' requires Gold tier ({GoldThreshold}+ karma). Falling back to Silver-tier model.",
                            provider, SilverFallbackModel);
                }

                // Silver-required models — downgrade if below Silver
                foreach (string sub in SilverRequiredModelSubstrings)
                {
                    if (modelLower.Contains(sub.ToLowerInvariant()) && karma < SilverThreshold)
                        return KarmaGateResult.Downgraded(tier,
                            $"Model '{model}' requires Silver tier ({SilverThreshold}+ karma). Falling back to Bronze-tier model.",
                            provider, BronzeFallbackModel);
                }
            }

            return KarmaGateResult.Allowed(tier);
        }
    }

    /// <summary>Result of a karma gate evaluation.</summary>
    public class KarmaGateResult
    {
        public bool IsAllowed    { get; private set; }
        public bool IsDowngraded { get; private set; }
        public string TierName   { get; private set; }
        public string Reason     { get; private set; }

        /// <summary>Provider to use after downgrade (only meaningful when IsDowngraded = true).</summary>
        public AIProviderType DowngradedProvider { get; private set; }

        /// <summary>Model to use after downgrade (only meaningful when IsDowngraded = true).</summary>
        public string DowngradedModel { get; private set; }

        private KarmaGateResult() { }

        public static KarmaGateResult Allowed(string tier) =>
            new KarmaGateResult { IsAllowed = true, TierName = tier };

        public static KarmaGateResult Downgraded(string tier, string reason, AIProviderType fallbackProvider, string fallbackModel) =>
            new KarmaGateResult
            {
                IsAllowed        = true,
                IsDowngraded     = true,
                TierName         = tier,
                Reason           = reason,
                DowngradedProvider = fallbackProvider,
                DowngradedModel  = fallbackModel,
            };
    }
}
