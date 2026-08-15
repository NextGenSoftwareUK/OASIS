using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    public partial class AIProviderManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>API keys per provider. Defaults to reading from environment variables (e.g. OPENAI_API_KEY) if not supplied.</summary>
        public Dictionary<AIProviderType, string> ApiKeys { get; set; } = new Dictionary<AIProviderType, string>();

        public AIProviderManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            LoadApiKeysFromEnvironment();
        }

        public AIProviderManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            LoadApiKeysFromEnvironment();
        }

        private void LoadApiKeysFromEnvironment()
        {
            // Environment variables always win — they override OASIS_DNA.json values.
            // Fallback order: env var → OASIS_DNA.json Web6.ApiKeys → null (provider skipped).
            var dna = OASISDNA?.OASIS?.Web6?.ApiKeys;

            ApiKeys[AIProviderType.OpenAI]      = Resolve("OPENAI_API_KEY",       dna?.OpenAI);
            ApiKeys[AIProviderType.Anthropic]   = Resolve("ANTHROPIC_API_KEY",    dna?.Anthropic);
            ApiKeys[AIProviderType.Gemini]      = Resolve("GEMINI_API_KEY",       dna?.Gemini);
            ApiKeys[AIProviderType.Groq]        = Resolve("GROQ_API_KEY",         dna?.Groq);
            ApiKeys[AIProviderType.Mistral]     = Resolve("MISTRAL_API_KEY",      dna?.Mistral);
            ApiKeys[AIProviderType.Cohere]      = Resolve("COHERE_API_KEY",       dna?.Cohere);
            ApiKeys[AIProviderType.XAI]         = Resolve("XAI_API_KEY",          dna?.XAI);
            ApiKeys[AIProviderType.DeepSeek]    = Resolve("DEEPSEEK_API_KEY",     dna?.DeepSeek);
            ApiKeys[AIProviderType.HuggingFace] = Resolve("HUGGINGFACE_API_KEY",  dna?.HuggingFace);
            ApiKeys[AIProviderType.AzureOpenAI] = Resolve("AZURE_OPENAI_API_KEY", dna?.AzureOpenAI);
            ApiKeys[AIProviderType.StabilityAI] = Resolve("STABILITY_API_KEY",    dna?.StabilityAI);
            ApiKeys[AIProviderType.OpenServ]    = Resolve("SERV_API_KEY",         dna?.OpenServ);
            // Priority 9 — additional providers (all OpenAI-compatible)
            ApiKeys[AIProviderType.Cerebras]    = Resolve("CEREBRAS_API_KEY",     null);
            ApiKeys[AIProviderType.TogetherAI]  = Resolve("TOGETHER_API_KEY",     null);
            ApiKeys[AIProviderType.FireworksAI] = Resolve("FIREWORKS_API_KEY",    null);
            ApiKeys[AIProviderType.MoonshotAI]  = Resolve("MOONSHOT_API_KEY",     null);
            ApiKeys[AIProviderType.Perplexity]  = Resolve("PERPLEXITY_API_KEY",   null);
            ApiKeys[AIProviderType.LMStudio]    = Resolve("LM_STUDIO_API_KEY",    null);
            ApiKeys[AIProviderType.Bittensor]   = Resolve("BITTENSOR_API_KEY",   null);
            ApiKeys[AIProviderType.GaiaNet]     = Resolve("GAIANET_API_KEY",     null);
            ApiKeys[AIProviderType.Custom]      = Resolve("CUSTOM_AI_API_KEY",   null);
            ApiKeys[AIProviderType.LeelaAI]     = Resolve("LEELA_API_KEY",        dna?.LeelaAI);
            ApiKeys[AIProviderType.Replicate]   = Resolve("REPLICATE_API_KEY",   null);
        }

        /// <summary>Returns the env var value if set and non-empty, otherwise the OASIS_DNA fallback.</summary>
        private static string Resolve(string envVar, string dnaFallback)
        {
            string env = Environment.GetEnvironmentVariable(envVar);
            return !string.IsNullOrEmpty(env) ? env : dnaFallback;
        }

        /// <summary>
        /// Routes a unified completion request to the best-fit provider (or the one explicitly requested),
        /// normalising the response shape. Automatically fails over to the next provider on error if
        /// request.Routing.Fallback is true.
        /// </summary>
        public async Task<OASISResult<CompletionResponse>> CompleteAsync(CompletionRequest request, string bearerToken = null)
        {
            OASISResult<CompletionResponse> result = new OASISResult<CompletionResponse>();

            if (request == null || request.Messages == null || request.Messages.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "CompletionRequest.Messages is required and cannot be empty.");
                return result;
            }

            List<AIProviderType> candidates = ResolveProviderCandidates(request);

            // ── Subscription + karma gate ──────────────────────────────────────────
            int avatarKarma = 0;
            SubscriptionPlan effectivePlan = request.SubscriptionPlan; // caller may supply it directly
            if (request.AvatarId != Guid.Empty)
            {
                // Look up karma and active plan in parallel; both are non-fatal if unavailable.
                var karmaTask = FetchAvatarKarmaAsync(request.AvatarId, bearerToken);
                var planTask  = effectivePlan == SubscriptionPlan.Free
                    ? FetchSubscriptionPlanAsync(request.AvatarId, bearerToken)
                    : Task.FromResult(effectivePlan);

                await Task.WhenAll(karmaTask, planTask);
                avatarKarma   = karmaTask.Result;
                effectivePlan = planTask.Result;
            }

            // Apply gate; replace provider/model if downgraded.
            AIProviderType gatedProvider = candidates.Count > 0 ? candidates[0] : AIProviderType.OpenAI;
            string gatedModel = request.Model;
            KarmaGateResult gate = KarmaGateManager.Evaluate(effectivePlan, avatarKarma, gatedProvider, request.Model);
            if (gate.IsDowngraded)
            {
                gatedProvider = gate.DowngradedProvider;
                gatedModel    = gate.DowngradedModel;
                // Replace the candidate list with the downgraded provider first, retaining others as fallbacks.
                candidates.Remove(gatedProvider);
                candidates.Insert(0, gatedProvider);
            }
            // Mutate the request model field so CallProviderAsync uses the gated model.
            string originalModel = request.Model;
            if (gate.IsDowngraded && !string.IsNullOrEmpty(gatedModel))
                request.Model = gatedModel;
            // ───────────────────────────────────────────────────────────────────────

            Exception lastException = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                AIProviderType provider = candidates[i];
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    CompletionResponse response = await CallProviderAsync(provider, request);
                    sw.Stop();
                    response.LatencyMs  = sw.ElapsedMilliseconds;
                    response.FailedOver = i > 0;
                    response.AccessTier   = gate.TierLabel;
                    response.KarmaBoosted = gate.KarmaBoosted;
                    if (gate.IsDowngraded)
                        response.AccessDowngradeNote = gate.Reason;
                    result.Result = response;
                    request.Model = originalModel; // restore
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!request.Routing.Fallback)
                        break;

                    continue;
                }
            }

            request.Model = originalModel; // restore on error path too
            OASISErrorHandling.HandleError(ref result, $"Error calling AI provider(s). Reason: {lastException?.Message}", lastException);
            return result;
        }

        private async Task<SubscriptionPlan> FetchSubscriptionPlanAsync(Guid avatarId, string bearerToken)
        {
            try
            {
                string web4Base = Environment.GetEnvironmentVariable("WEB4_API_BASE_URL")
                    ?? OASISDNA?.OASIS?.Web6?.Web4BaseUrl
                    ?? "https://api.oasisomniverse.one";
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{web4Base}/api/subscription/subscriptions/me");
                if (!string.IsNullOrEmpty(bearerToken))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                using HttpResponseMessage resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return SubscriptionPlan.Free;

                using JsonDocument doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                // Response shape: { "result": [{ "planId": "silver", "status": "active", ... }] }
                JsonElement root = doc.RootElement;
                JsonElement arr = root.TryGetProperty("result", out var r) ? r : root;
                if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                {
                    JsonElement first = arr[0];
                    string planId = first.TryGetProperty("planId", out var p) ? p.GetString()
                        : first.TryGetProperty("PlanId", out var P) ? P.GetString() : null;
                    string status = first.TryGetProperty("status", out var s) ? s.GetString()
                        : first.TryGetProperty("Status", out var S) ? S.GetString() : "active";

                    if (status == "active" || status == "trialing")
                        return planId?.ToLowerInvariant() switch
                        {
                            "bronze"     => SubscriptionPlan.Bronze,
                            "silver"     => SubscriptionPlan.Silver,
                            "gold"       => SubscriptionPlan.Gold,
                            "enterprise" => SubscriptionPlan.Enterprise,
                            _            => SubscriptionPlan.Free,
                        };
                }
                return SubscriptionPlan.Free;
            }
            catch
            {
                return SubscriptionPlan.Free; // non-fatal; treat as Free
            }
        }

        private async Task<int> FetchAvatarKarmaAsync(Guid avatarId, string bearerToken)
        {
            try
            {
                string web4Base = Environment.GetEnvironmentVariable("WEB4_API_BASE_URL")
                    ?? OASISDNA?.OASIS?.Web6?.Web4BaseUrl
                    ?? "https://api.oasisomniverse.one";
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{web4Base}/api/karma/{avatarId}");
                if (!string.IsNullOrEmpty(bearerToken))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                using HttpResponseMessage resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return 0;
                using JsonDocument doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                return doc.RootElement.TryGetProperty("karmaTotal", out JsonElement k) ? k.GetInt32() : 0;
            }
            catch
            {
                return 0; // karma lookup failure is non-fatal; treat as Bronze
            }
        }

        private List<AIProviderType> ResolveProviderCandidates(CompletionRequest request)
        {
            string requestedProvider = string.IsNullOrEmpty(request.Provider) || request.Provider == "auto"
                ? OASISDNA?.OASIS?.Web6?.DefaultProvider ?? "Auto"
                : request.Provider;

            if (Enum.TryParse(requestedProvider, true, out AIProviderType requested) && requested != AIProviderType.Auto)
                return new List<AIProviderType> { requested };

            // "auto" - check whether OpenServ should be preferred (per-request overrides DNA default).
            bool preferOpenServ = request.Routing?.UseOpenServ
                ?? OASISDNA?.OASIS?.Web6?.PreferOpenServ
                ?? false;

            if (preferOpenServ && !string.IsNullOrEmpty(ApiKeys.GetValueOrDefault(AIProviderType.OpenServ)))
                return new List<AIProviderType> { AIProviderType.OpenServ };

            // Otherwise prefer whichever direct providers we have keys for, ordered by routing priority.
            List<AIProviderType> configured = ApiKeys
                .Where(kv => !string.IsNullOrEmpty(kv.Value) && kv.Key != AIProviderType.OpenServ)
                .Select(kv => kv.Key)
                .ToList();

            // Fall back to OpenServ if nothing else is configured.
            if (configured.Count == 0)
            {
                if (!string.IsNullOrEmpty(ApiKeys.GetValueOrDefault(AIProviderType.OpenServ)))
                    return new List<AIProviderType> { AIProviderType.OpenServ };
                return new List<AIProviderType> { AIProviderType.OpenAI };
            }

            string priority = request.Routing?.Priority ?? OASISDNA?.OASIS?.Web6?.DefaultRoutingPriority ?? "cost";

            return priority.ToLowerInvariant() switch
            {
                "quality" => OrderByPreference(configured, AIProviderType.Anthropic, AIProviderType.OpenAI, AIProviderType.Gemini, AIProviderType.Groq),
                "latency" => OrderByPreference(configured, AIProviderType.Groq, AIProviderType.Gemini, AIProviderType.OpenAI, AIProviderType.Anthropic),
                _ => OrderByPreference(configured, AIProviderType.Groq, AIProviderType.DeepSeek, AIProviderType.OpenAI, AIProviderType.Anthropic),
            };
        }

        private static List<AIProviderType> OrderByPreference(List<AIProviderType> configured, params AIProviderType[] preferredOrder)
        {
            List<AIProviderType> ordered = preferredOrder.Where(configured.Contains).ToList();
            ordered.AddRange(configured.Except(ordered));
            return ordered;
        }

        private async Task<CompletionResponse> CallProviderAsync(AIProviderType provider, CompletionRequest request)
        {
            switch (provider)
            {
                case AIProviderType.OpenAI:
                case AIProviderType.Groq:
                case AIProviderType.DeepSeek:
                case AIProviderType.Mistral:
                case AIProviderType.XAI:
                case AIProviderType.Ollama:
                case AIProviderType.OpenServ:
                case AIProviderType.Cerebras:
                case AIProviderType.TogetherAI:
                case AIProviderType.FireworksAI:
                case AIProviderType.MoonshotAI:
                case AIProviderType.Perplexity:
                case AIProviderType.LMStudio:
                case AIProviderType.Bittensor:
                case AIProviderType.GaiaNet:
                case AIProviderType.Custom:
                case AIProviderType.LeelaAI:
                case AIProviderType.Replicate:
                    return await CallOpenAICompatibleAsync(provider, request);

                case AIProviderType.Anthropic:
                    return await CallAnthropicAsync(request);

                case AIProviderType.Gemini:
                    return await CallGeminiAsync(request);

                case AIProviderType.Cohere:
                    return await CallCohereAsync(request);

                case AIProviderType.AzureOpenAI:
                    return await CallAzureOpenAIAsync(request);

                case AIProviderType.HuggingFace:
                    return await CallHuggingFaceAsync(request);

                case AIProviderType.AWSBedrock:
                    return await CallAWSBedrockAsync(request);

                case AIProviderType.StabilityAI:
                    throw new NotSupportedException("Stability AI is an image-generation provider, not a chat-completion provider. Use AIProviderManager.GenerateImageAsync() / POST /v1/images/generate instead of /v1/complete.");

                default:
                    throw new NotSupportedException($"Provider '{provider}' is not supported for text completion.");
            }
        }

        private (string baseUrl, string defaultModel) GetOpenAICompatibleEndpoint(AIProviderType provider)
        {
            return provider switch
            {
                AIProviderType.Groq => ("https://api.groq.com/openai/v1/chat/completions", "llama-3.3-70b-versatile"),
                AIProviderType.DeepSeek => ("https://api.deepseek.com/chat/completions", "deepseek-chat"),
                AIProviderType.Mistral => ("https://api.mistral.ai/v1/chat/completions", "mistral-large-latest"),
                AIProviderType.XAI => ("https://api.x.ai/v1/chat/completions", "grok-3"),
                AIProviderType.Ollama => ($"{Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434"}/v1/chat/completions", "llama3.3"),
                AIProviderType.OpenServ => (
                    OASISDNA?.OASIS?.Web6?.OpenServ?.BaseUrl ?? "https://inference-api.openserv.ai/v1/chat/completions",
                    OASISDNA?.OASIS?.Web6?.OpenServ?.DefaultModel ?? OpenServCatalog.DefaultModel),
                AIProviderType.Cerebras   => ("https://api.cerebras.ai/v1/chat/completions",                             "llama-3.3-70b"),
                AIProviderType.TogetherAI => ("https://api.together.xyz/v1/chat/completions",                            "meta-llama/Llama-3.3-70B-Instruct-Turbo"),
                AIProviderType.FireworksAI=> ("https://api.fireworks.ai/inference/v1/chat/completions",                  "accounts/fireworks/models/llama-v3p3-70b-instruct"),
                AIProviderType.MoonshotAI => ("https://api.moonshot.cn/v1/chat/completions",                             "moonshot-v1-128k"),
                AIProviderType.Perplexity => ("https://api.perplexity.ai/chat/completions",                              "sonar-pro"),
                AIProviderType.LMStudio   => ($"{Environment.GetEnvironmentVariable("LM_STUDIO_BASE_URL") ?? "http://localhost:1234"}/v1/chat/completions", "local"),
                AIProviderType.Bittensor  => (Environment.GetEnvironmentVariable("BITTENSOR_API_URL") ?? "https://api.corcel.io/v1/chat/completions", "bittensor-mistral-7b"),
                AIProviderType.GaiaNet    => ($"{Environment.GetEnvironmentVariable("GAIANET_NODE_URL") ?? "https://llama.us.gaianet.network"}/v1/chat/completions", "llama"),
                AIProviderType.Custom     => (Environment.GetEnvironmentVariable("CUSTOM_AI_BASE_URL") ?? "", "custom"),
                AIProviderType.LeelaAI    => (
                    $"{(Environment.GetEnvironmentVariable("LEELA_BASE_URL") ?? OASISDNA?.OASIS?.Web6?.LeelaAI?.BaseUrl ?? "https://namozyqyvwf62hqxpzujt7e5hq0njhge.lambda-url.eu-west-1.on.aws").TrimEnd('/')}/v1/chat/completions",
                    "leela"),
                AIProviderType.Replicate  => ("https://api.replicate.com/v1/chat/completions", "meta/llama-4-scout-instruct"),
                _ => ("https://api.openai.com/v1/chat/completions", "gpt-4o"),
            };
        }

        private async Task<CompletionResponse> CallOpenAICompatibleAsync(AIProviderType provider, CompletionRequest request)
        {
            (string baseUrl, string defaultModel) = GetOpenAICompatibleEndpoint(provider);
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? defaultModel : request.Model;
            string apiKey = ApiKeys.TryGetValue(provider, out string key) ? key : null;

            bool keyOptional = provider == AIProviderType.Ollama || provider == AIProviderType.LMStudio || provider == AIProviderType.GaiaNet || provider == AIProviderType.Custom;
            if (string.IsNullOrEmpty(apiKey) && !keyOptional)
                throw new InvalidOperationException($"No API key configured for {provider}.");

            // LeelaAI rejects tool definitions with 400; suppress them for that provider.
            bool hasTools = request.Tools?.Count > 0 && provider != AIProviderType.LeelaAI;

            var payloadObj = new System.Collections.Generic.Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = BuildOpenAIMessages(request.Messages),
                ["temperature"] = request.Temperature,
                ["max_tokens"] = request.MaxTokens ?? 4096
            };
            if (hasTools)
            {
                payloadObj["tools"] = request.Tools.Select(t => (object)new
                {
                    type = "function",
                    function = new { name = t.Function.Name, description = t.Function.Description, parameters = t.Function.Parameters }
                }).ToList();
                payloadObj["tool_choice"] = request.ToolChoice ?? "auto";
            }

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            if (!string.IsNullOrEmpty(apiKey))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"{provider} returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            JsonElement choice = root.GetProperty("choices")[0];
            JsonElement message = choice.GetProperty("message");
            string finishReason = choice.TryGetProperty("finish_reason", out JsonElement fr) ? fr.GetString() : null;

            // Parse tool calls when finish_reason is "tool_calls"
            List<ToolCall> toolCalls = null;
            if (finishReason == "tool_calls" && message.TryGetProperty("tool_calls", out JsonElement tcs))
            {
                toolCalls = tcs.EnumerateArray().Select(tc => new ToolCall
                {
                    Id = tc.GetProperty("id").GetString(),
                    Function = new ToolCallFunction
                    {
                        Name = tc.GetProperty("function").GetProperty("name").GetString(),
                        Arguments = tc.GetProperty("function").GetProperty("arguments").GetString()
                    }
                }).ToList();
            }

            string content = message.TryGetProperty("content", out JsonElement contentEl) ? contentEl.GetString() : null;

            // If content is null and it's not a tool_calls finish, surface the reason
            if (content == null && toolCalls == null)
            {
                string refusal = message.TryGetProperty("refusal", out JsonElement ref_) ? ref_.GetString() : null;
                string detail = !string.IsNullOrEmpty(refusal) ? $" Refusal: {refusal}" : $" Raw response: {body}";
                throw new InvalidOperationException($"{provider} returned null content (finish_reason={finishReason}).{detail}");
            }

            int promptTokens = root.TryGetProperty("usage", out JsonElement usage) && usage.TryGetProperty("prompt_tokens", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usage", out JsonElement usage2) && usage2.TryGetProperty("completion_tokens", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = provider.ToString(),
                Model = model,
                Content = content,
                FinishReason = finishReason,
                ToolCalls = toolCalls,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        private async Task<CompletionResponse> CallAnthropicAsync(CompletionRequest request)
        {
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? "claude-sonnet-4-6" : request.Model;
            string apiKey = ApiKeys.TryGetValue(AIProviderType.Anthropic, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for Anthropic.");

            string systemPrompt = string.Join("\n", request.Messages.Where(m => m.Role == "system").Select(m => m.Content));

            var payloadObj = new System.Collections.Generic.Dictionary<string, object>
            {
                ["model"] = model,
                ["max_tokens"] = request.MaxTokens ?? 4096,
                ["messages"] = BuildAnthropicMessages(request.Messages),
                ["temperature"] = request.Temperature
            };
            if (!string.IsNullOrEmpty(systemPrompt))
                payloadObj["system"] = systemPrompt;
            if (request.Tools?.Count > 0)
                payloadObj["tools"] = request.Tools.Select(t => (object)new
                {
                    name = t.Function.Name,
                    description = t.Function.Description,
                    input_schema = t.Function.Parameters
                }).ToList();

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Anthropic returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            string stopReason = root.TryGetProperty("stop_reason", out JsonElement sr) ? sr.GetString() : null;
            string content = null;
            List<ToolCall> toolCalls = null;

            foreach (JsonElement block in root.GetProperty("content").EnumerateArray())
            {
                string blockType = block.GetProperty("type").GetString();
                if (blockType == "text")
                    content = block.GetProperty("text").GetString();
                else if (blockType == "tool_use")
                {
                    toolCalls ??= new List<ToolCall>();
                    toolCalls.Add(new ToolCall
                    {
                        Id = block.GetProperty("id").GetString(),
                        Function = new ToolCallFunction
                        {
                            Name = block.GetProperty("name").GetString(),
                            Arguments = block.GetProperty("input").GetRawText()
                        }
                    });
                }
            }

            if (content == null && toolCalls == null)
                throw new InvalidOperationException($"Anthropic returned null content. Raw response: {body}");

            int promptTokens = root.TryGetProperty("usage", out JsonElement usage) && usage.TryGetProperty("input_tokens", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usage", out JsonElement usage2) && usage2.TryGetProperty("output_tokens", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = AIProviderType.Anthropic.ToString(),
                Model = model,
                Content = content,
                FinishReason = stopReason == "tool_use" ? "tool_calls" : stopReason,
                ToolCalls = toolCalls,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }
    }
}
