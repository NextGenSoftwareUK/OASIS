using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Classifies an incoming problem string into a FAHRN task category by calling the fastest
    /// available model with a single-word classification prompt. Falls back to "general" on error.
    /// </summary>
    public class FahrnTaskClassifier
    {
        private static readonly HashSet<string> _validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "code", "reasoning", "writing", "mathematics", "legal", "architecture", "real-time", "general"
        };

        private readonly AIProviderManager _providerManager;

        public FahrnTaskClassifier(Guid avatarId, OASISDNA OASISDNA = null)
        {
            _providerManager = new AIProviderManager(avatarId, OASISDNA);
        }

        public async Task<string> ClassifyAsync(string problem)
        {
            if (string.IsNullOrWhiteSpace(problem))
                return "general";

            // Priority 25: Try ML.NET in-process classifier first (zero latency, no API call)
            try
            {
                var mlNet = new MLNetManager(Guid.Empty);
                string mlResult = mlNet.ClassifyTaskType(problem);
                if (!string.Equals(mlResult, "general", StringComparison.OrdinalIgnoreCase))
                    return mlResult;
                // "general" from ML.NET means unclassified — fall through to LLM for confirmation
            }
            catch { }

            try
            {
                var request = new CompletionRequest
                {
                    Provider = "auto",
                    Routing = new RoutingOptions { Priority = "latency" },
                    MaxTokens = 10,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage
                        {
                            Role = "system",
                            Content = "Classify the following problem into exactly one category. Reply with only the category word and nothing else. " +
                                      "Categories: code, reasoning, writing, mathematics, legal, architecture, real-time, general"
                        },
                        new ChatMessage { Role = "user", Content = problem.Length > 500 ? problem[..500] : problem }
                    }
                };

                OASISResult<CompletionResponse> result = await _providerManager.CompleteAsync(request);

                if (!result.IsError && !string.IsNullOrWhiteSpace(result.Result?.Content))
                {
                    string raw = result.Result.Content.Trim().ToLowerInvariant();
                    if (_validCategories.Contains(raw))
                        return raw;
                }
            }
            catch { }

            return "general";
        }
    }
}
