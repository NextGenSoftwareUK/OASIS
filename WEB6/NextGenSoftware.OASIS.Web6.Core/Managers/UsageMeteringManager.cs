using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Tracks per-avatar token usage and estimated cost, accumulated in COSMIC ORM holon metadata.
    /// Enforces monthly budget and daily token limits defined in OASISDNA.
    /// </summary>
    public class UsageMeteringManager : OASISManager
    {
        // Per-1K-token pricing: (inputPer1k, outputPer1k) in USD
        private static readonly Dictionary<(AIProviderType provider, string model), (double inputPer1k, double outputPer1k)> _pricing
            = new Dictionary<(AIProviderType, string), (double, double)>
        {
            [(AIProviderType.OpenAI,     "gpt-4o")]                     = (0.0025,  0.010),
            [(AIProviderType.OpenAI,     "gpt-4o-mini")]                = (0.00015, 0.00060),
            [(AIProviderType.OpenAI,     "gpt-4.1")]                    = (0.002,   0.008),
            [(AIProviderType.Anthropic,  "claude-opus-4-8")]            = (0.015,   0.075),
            [(AIProviderType.Anthropic,  "claude-sonnet-5")]            = (0.003,   0.015),
            [(AIProviderType.Anthropic,  "claude-haiku-4-5")]           = (0.00025, 0.00125),
            [(AIProviderType.Gemini,     "gemini-2.5-pro")]             = (0.00125, 0.010),
            [(AIProviderType.Gemini,     "gemini-2.0-flash")]           = (0.00010, 0.00040),
            [(AIProviderType.Groq,       "llama-3.3-70b-versatile")]    = (0.00059, 0.00079),
            [(AIProviderType.Groq,       "mixtral-8x7b-32768")]         = (0.00024, 0.00024),
            [(AIProviderType.Mistral,    "mistral-large-latest")]       = (0.002,   0.006),
            [(AIProviderType.Mistral,    "mistral-small-latest")]       = (0.00020, 0.00060),
            [(AIProviderType.Cohere,     "command-r-plus")]             = (0.0025,  0.010),
            [(AIProviderType.XAI,        "grok-3")]                     = (0.003,   0.015),
            [(AIProviderType.DeepSeek,   "deepseek-chat")]              = (0.00014, 0.00028),
            [(AIProviderType.Cerebras,   "llama-3.3-70b")]              = (0.00060, 0.00060),
            [(AIProviderType.TogetherAI, "meta-llama/Llama-3.3-70B-Instruct-Turbo")] = (0.00088, 0.00088),
            [(AIProviderType.Perplexity, "llama-3.1-sonar-large-128k-online")]       = (0.001,   0.001),
        };

        private const string SpendKeyPrefix  = "web6_spend_";
        private const string TokensKeyPrefix = "web6_tokens_";

        public UsageMeteringManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna) { }

        /// <summary>Estimates cost in USD for a given completion and records it against this avatar's monthly spend.</summary>
        public async Task<double> RecordUsageAsync(AIProviderType provider, string model, int promptTokens, int completionTokens)
        {
            double cost = EstimateCost(provider, model, promptTokens, completionTokens);
            int totalTokens = promptTokens + completionTokens;

            try
            {
                string monthKey = $"{SpendKeyPrefix}{DateTime.UtcNow:yyyy_MM}";
                string dayKey   = $"{TokensKeyPrefix}{DateTime.UtcNow:yyyy_MM_dd}";

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return cost;

                double currentSpend = avatar.Result.MetaData.TryGetValue(monthKey, out object s) && double.TryParse(s?.ToString(), out double d) ? d : 0.0;
                avatar.Result.MetaData[monthKey] = (currentSpend + cost).ToString("F6");

                int currentTokens = avatar.Result.MetaData.TryGetValue(dayKey, out object t) && int.TryParse(t?.ToString(), out int it) ? it : 0;
                avatar.Result.MetaData[dayKey] = (currentTokens + totalTokens).ToString();

                await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.SaveAvatarAsync(avatar.Result);
            }
            catch { /* metering is best-effort — never block the completion */ }

            return cost;
        }

        /// <summary>
        /// Checks whether this avatar has exceeded their monthly USD budget or daily token limit.
        /// Returns null if within limits; returns a reason string if the quota is exceeded.
        /// </summary>
        public async Task<string> CheckQuotaAsync()
        {
            try
            {
                double monthlyBudget   = OASISDNA?.OASIS?.Web6?.DefaultMonthlyBudgetUSD ?? 0;
                int    dailyTokenLimit = OASISDNA?.OASIS?.Web6?.DefaultDailyTokenLimit  ?? 0;

                if (monthlyBudget <= 0 && dailyTokenLimit <= 0)
                    return null;

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return null;

                if (monthlyBudget > 0)
                {
                    string monthKey = $"{SpendKeyPrefix}{DateTime.UtcNow:yyyy_MM}";
                    double spend = avatar.Result.MetaData.TryGetValue(monthKey, out object s) && double.TryParse(s?.ToString(), out double d) ? d : 0.0;
                    if (spend >= monthlyBudget)
                        return $"Monthly budget of ${monthlyBudget:F2} exceeded (${spend:F2} used this month)";
                }

                if (dailyTokenLimit > 0)
                {
                    string dayKey = $"{TokensKeyPrefix}{DateTime.UtcNow:yyyy_MM_dd}";
                    int tokens = avatar.Result.MetaData.TryGetValue(dayKey, out object t) && int.TryParse(t?.ToString(), out int it) ? it : 0;
                    if (tokens >= dailyTokenLimit)
                        return $"Daily token limit of {dailyTokenLimit:N0} exceeded ({tokens:N0} used today)";
                }
            }
            catch { /* quota check is best-effort */ }

            return null;
        }

        /// <summary>Returns this avatar's usage summary for the current month and today.</summary>
        public async Task<OASISResult<AvatarUsageSummary>> GetUsageSummaryAsync()
        {
            OASISResult<AvatarUsageSummary> result = new OASISResult<AvatarUsageSummary> { Result = new AvatarUsageSummary { AvatarId = AvatarId } };
            try
            {
                string monthKey = $"{SpendKeyPrefix}{DateTime.UtcNow:yyyy_MM}";
                string dayKey   = $"{TokensKeyPrefix}{DateTime.UtcNow:yyyy_MM_dd}";

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

                result.Result.MonthlySpendUSD  = avatar.Result.MetaData.TryGetValue(monthKey, out object s) && double.TryParse(s?.ToString(), out double d) ? d : 0.0;
                result.Result.DailyTokensUsed  = avatar.Result.MetaData.TryGetValue(dayKey,   out object t) && int.TryParse(t?.ToString(), out int it) ? it : 0;
                result.Result.MonthlyBudgetUSD = OASISDNA?.OASIS?.Web6?.DefaultMonthlyBudgetUSD ?? 0;
                result.Result.DailyTokenLimit  = OASISDNA?.OASIS?.Web6?.DefaultDailyTokenLimit  ?? 0;
                result.Result.PeriodMonth      = DateTime.UtcNow.ToString("yyyy-MM");
                result.Result.PeriodDay        = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"UsageMeteringManager.GetUsageSummaryAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public static double EstimateCost(AIProviderType provider, string model, int promptTokens, int completionTokens)
        {
            if (_pricing.TryGetValue((provider, model), out var p))
                return (promptTokens / 1000.0 * p.inputPer1k) + (completionTokens / 1000.0 * p.outputPer1k);

            double fallback = provider switch
            {
                AIProviderType.Anthropic => 0.010,
                AIProviderType.OpenAI    => 0.005,
                AIProviderType.Gemini    => 0.002,
                AIProviderType.Groq      => 0.001,
                _                        => 0.005,
            };
            return (promptTokens + completionTokens) / 1000.0 * fallback;
        }
    }
}
