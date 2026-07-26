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
    /// <summary>
    /// The WEB6 AI abstraction layer. Normalises completion requests/responses across every AI provider
    /// (OpenAI, Anthropic, Gemini, Ollama, Groq, Mistral, OpenServ, etc.) behind a single unified interface,
    /// with automatic provider selection, fail-over and cost/latency/quality-based routing.
    /// </summary>
    public class AIProviderManager : OASISManager
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
        public async Task<OASISResult<CompletionResponse>> CompleteAsync(CompletionRequest request)
        {
            OASISResult<CompletionResponse> result = new OASISResult<CompletionResponse>();

            if (request == null || request.Messages == null || request.Messages.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "CompletionRequest.Messages is required and cannot be empty.");
                return result;
            }

            List<AIProviderType> candidates = ResolveProviderCandidates(request);
            Exception lastException = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                AIProviderType provider = candidates[i];
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    CompletionResponse response = await CallProviderAsync(provider, request);
                    sw.Stop();
                    response.LatencyMs = sw.ElapsedMilliseconds;
                    response.FailedOver = i > 0;
                    result.Result = response;
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!request.Routing.Fallback)
                        break;

                    // Try the next candidate provider.
                    continue;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Error calling AI provider(s). Reason: {lastException?.Message}", lastException);
            return result;
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

        private async Task<CompletionResponse> CallGeminiAsync(CompletionRequest request)
        {
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? "gemini-2.5-flash" : request.Model;
            string apiKey = ApiKeys.TryGetValue(AIProviderType.Gemini, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for Gemini.");

            string systemPrompt = string.Join("\n", request.Messages.Where(m => m.Role == "system").Select(m => m.Content));

            var payloadObj = new System.Collections.Generic.Dictionary<string, object>
            {
                ["contents"] = BuildGeminiContents(request.Messages),
                ["generationConfig"] = new { temperature = request.Temperature, maxOutputTokens = request.MaxTokens }
            };
            if (!string.IsNullOrEmpty(systemPrompt))
                payloadObj["systemInstruction"] = new { parts = new[] { new { text = systemPrompt } } };
            if (request.Tools?.Count > 0)
                payloadObj["tools"] = new object[] { new { functionDeclarations = request.Tools.Select(t => (object)new
                {
                    name = t.Function.Name,
                    description = t.Function.Description,
                    parameters = t.Function.Parameters
                }).ToList() } };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            JsonElement candidate = root.GetProperty("candidates")[0];
            string finishReason = candidate.TryGetProperty("finishReason", out JsonElement fr) ? fr.GetString() : null;
            string content = null;
            List<ToolCall> toolCalls = null;

            foreach (JsonElement part in candidate.GetProperty("content").GetProperty("parts").EnumerateArray())
            {
                if (part.TryGetProperty("text", out JsonElement textEl))
                    content = textEl.GetString();
                else if (part.TryGetProperty("functionCall", out JsonElement fc))
                {
                    toolCalls ??= new List<ToolCall>();
                    toolCalls.Add(new ToolCall
                    {
                        Id = $"call_{Guid.NewGuid():N}",
                        Function = new ToolCallFunction
                        {
                            Name = fc.GetProperty("name").GetString(),
                            Arguments = fc.GetProperty("args").GetRawText()
                        }
                    });
                }
            }

            if (content == null && toolCalls == null)
                throw new InvalidOperationException($"Gemini returned null content. Raw response: {body}");

            int promptTokens = root.TryGetProperty("usageMetadata", out JsonElement usage) && usage.TryGetProperty("promptTokenCount", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usageMetadata", out JsonElement usage2) && usage2.TryGetProperty("candidatesTokenCount", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = AIProviderType.Gemini.ToString(),
                Model = model,
                Content = content,
                FinishReason = toolCalls != null ? "tool_calls" : finishReason,
                ToolCalls = toolCalls,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        private async Task<CompletionResponse> CallCohereAsync(CompletionRequest request)
        {
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? "command-r-plus" : request.Model;
            string apiKey = ApiKeys.TryGetValue(AIProviderType.Cohere, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for Cohere.");

            if (request.Tools?.Count > 0)
                request = InjectToolShimIntoSystemPrompt(request);

            var payload = new
            {
                model,
                messages = request.Messages.Select(m => new { role = m.Role == "assistant" ? "assistant" : m.Role, content = m.Content }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 4096
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Cohere returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string content = root.GetProperty("message").GetProperty("content")[0].GetProperty("text").GetString();
            int promptTokens = root.TryGetProperty("usage", out JsonElement usage) && usage.TryGetProperty("tokens", out JsonElement tokens) && tokens.TryGetProperty("input_tokens", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usage", out JsonElement usage2) && usage2.TryGetProperty("tokens", out JsonElement tokens2) && tokens2.TryGetProperty("output_tokens", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = AIProviderType.Cohere.ToString(),
                Model = model,
                Content = content,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        private async Task<CompletionResponse> CallAzureOpenAIAsync(CompletionRequest request)
        {
            string deployment = string.IsNullOrEmpty(request.Model) || request.Model == "auto"
                ? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o"
                : request.Model;
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string apiKey = ApiKeys.TryGetValue(AIProviderType.AzureOpenAI, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
                throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT and an Azure OpenAI API key must both be configured.");

            if (request.Tools?.Count > 0)
                request = InjectToolShimIntoSystemPrompt(request);

            string url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-06-01";

            var payload = new
            {
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 4096
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("api-key", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Azure OpenAI returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return new CompletionResponse
            {
                Provider = AIProviderType.AzureOpenAI.ToString(),
                Model = deployment,
                Content = content
            };
        }

        private async Task<CompletionResponse> CallHuggingFaceAsync(CompletionRequest request)
        {
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? "meta-llama/Llama-3.3-70B-Instruct" : request.Model;
            string apiKey = ApiKeys.TryGetValue(AIProviderType.HuggingFace, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for HuggingFace.");

            if (request.Tools?.Count > 0)
                request = InjectToolShimIntoSystemPrompt(request);

            var payload = new
            {
                model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 4096
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://router.huggingface.co/v1/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"HuggingFace returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return new CompletionResponse
            {
                Provider = AIProviderType.HuggingFace.ToString(),
                Model = model,
                Content = content
            };
        }

        /// <summary>
        /// Calls AWS Bedrock's unified Converse API - the same call shape works across every model Bedrock hosts
        /// (Anthropic Claude, Amazon Titan/Nova, Meta Llama, Mistral, etc.). Credentials are resolved via the
        /// standard AWS SDK credential chain (environment variables, shared config/profile, or an IAM role) -
        /// set AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY/AWS_REGION (or AWS_DEFAULT_REGION) to use this provider.
        /// </summary>
        private async Task<CompletionResponse> CallAWSBedrockAsync(CompletionRequest request)
        {
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto"
                ? Environment.GetEnvironmentVariable("AWS_BEDROCK_DEFAULT_MODEL_ID") ?? "amazon.nova-pro-v1:0"
                : request.Model;

            string regionName = Environment.GetEnvironmentVariable("AWS_REGION") ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

            if (string.IsNullOrEmpty(regionName))
                throw new InvalidOperationException("AWS_REGION (or AWS_DEFAULT_REGION) must be configured to use the AWSBedrock provider.");

            using var client = new Amazon.BedrockRuntime.AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.GetBySystemName(regionName));

            string systemPrompt = string.Join("\n", request.Messages.Where(m => m.Role == "system").Select(m => m.Content));

            var converseRequest = new Amazon.BedrockRuntime.Model.ConverseRequest
            {
                ModelId = model,
                Messages = request.Messages.Where(m => m.Role != "system").Select(m => new Amazon.BedrockRuntime.Model.Message
                {
                    Role = m.Role == "assistant" ? Amazon.BedrockRuntime.ConversationRole.Assistant : Amazon.BedrockRuntime.ConversationRole.User,
                    Content = new List<Amazon.BedrockRuntime.Model.ContentBlock> { new Amazon.BedrockRuntime.Model.ContentBlock { Text = m.Content } }
                }).ToList(),
                InferenceConfig = new Amazon.BedrockRuntime.Model.InferenceConfiguration
                {
                    Temperature = (float)(request.Temperature ?? 1.0),
                    MaxTokens = request.MaxTokens ?? 4096
                }
            };

            if (!string.IsNullOrEmpty(systemPrompt))
                converseRequest.System = new List<Amazon.BedrockRuntime.Model.SystemContentBlock> { new Amazon.BedrockRuntime.Model.SystemContentBlock { Text = systemPrompt } };

            Amazon.BedrockRuntime.Model.ConverseResponse converseResponse = await client.ConverseAsync(converseRequest);
            string content = string.Concat(converseResponse.Output.Message.Content.Select(c => c.Text));

            return new CompletionResponse
            {
                Provider = AIProviderType.AWSBedrock.ToString(),
                Model = model,
                Content = content,
                PromptTokens = converseResponse.Usage?.InputTokens ?? 0,
                CompletionTokens = converseResponse.Usage?.OutputTokens ?? 0
            };
        }

        /// <summary>
        /// Generates an image via the configured image-generation provider. Stability AI is fully wired (real REST
        /// call to the Stable Image Generate v2beta API). OpenAI's gpt-image-1 is wired as a second option.
        /// </summary>
        public async Task<OASISResult<ImageGenerationResponse>> GenerateImageAsync(ImageGenerationRequest request)
        {
            OASISResult<ImageGenerationResponse> result = new OASISResult<ImageGenerationResponse>();

            if (string.IsNullOrEmpty(request?.Prompt))
            {
                OASISErrorHandling.HandleError(ref result, "ImageGenerationRequest.Prompt is required.");
                return result;
            }

            try
            {
                result.Result = request.Provider switch
                {
                    AIProviderType.OpenAI => await GenerateImageOpenAIAsync(request),
                    _ => await GenerateImageStabilityAIAsync(request),
                };
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating image via {request.Provider}. Reason: {ex.Message}", ex);
                return result;
            }
        }

        private async Task<ImageGenerationResponse> GenerateImageStabilityAIAsync(ImageGenerationRequest request)
        {
            string apiKey = ApiKeys.TryGetValue(AIProviderType.StabilityAI, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for Stability AI (set STABILITY_API_KEY).");

            using var content = new MultipartFormDataContent
            {
                { new StringContent(request.Prompt), "prompt" },
                { new StringContent(request.OutputFormat ?? "png"), "output_format" }
            };

            if (!string.IsNullOrEmpty(request.AspectRatio))
                content.Add(new StringContent(request.AspectRatio), "aspect_ratio");

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.stability.ai/v2beta/stable-image/generate/core");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            httpRequest.Content = content;

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string error = await httpResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Stability AI returned {(int)httpResponse.StatusCode}: {error}");
            }

            byte[] imageBytes = await httpResponse.Content.ReadAsByteArrayAsync();

            return new ImageGenerationResponse
            {
                Provider = AIProviderType.StabilityAI.ToString(),
                Model = "stable-image-core",
                ImageBase64 = Convert.ToBase64String(imageBytes),
                OutputFormat = request.OutputFormat ?? "png"
            };
        }

        /// <summary>
        /// Streams completion chunks as an async sequence via SSE (OpenAI-compatible providers only).
        /// Each item is a CompletionChunk; the final item has Done=true and token counts.
        /// </summary>
        public async IAsyncEnumerable<CompletionChunk> CompleteStreamAsync(CompletionRequest request)
        {
            List<AIProviderType> candidates = ResolveProviderCandidates(request);
            AIProviderType provider = candidates.Count > 0 ? candidates[0] : AIProviderType.OpenAI;

            // Anthropic and Gemini have different streaming wire formats; fall back to non-streaming for them.
            if (provider == AIProviderType.Anthropic || provider == AIProviderType.Gemini ||
                provider == AIProviderType.Cohere || provider == AIProviderType.AWSBedrock)
            {
                CompletionResponse full = await CallProviderAsync(provider, request);
                yield return new CompletionChunk { Delta = full.Content, Provider = full.Provider, Model = full.Model };
                yield return new CompletionChunk { Done = true, Provider = full.Provider, Model = full.Model, PromptTokens = full.PromptTokens, CompletionTokens = full.CompletionTokens };
                yield break;
            }

            (string baseUrl, string defaultModel) = GetOpenAICompatibleEndpoint(provider);
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? defaultModel : request.Model;
            string apiKey = ApiKeys.TryGetValue(provider, out string k) ? k : null;

            var payload = new
            {
                model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 4096,
                stream = true
            };

            using HttpRequestMessage httpReq = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            if (!string.IsNullOrEmpty(apiKey))
                httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResp = await _httpClient.SendAsync(httpReq, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            if (!httpResp.IsSuccessStatusCode)
            {
                string err = await httpResp.Content.ReadAsStringAsync();
                yield return new CompletionChunk { Delta = $"[ERROR] {err}", Done = true, Provider = provider.ToString(), Model = model };
                yield break;
            }

            using System.IO.Stream stream = await httpResp.Content.ReadAsStreamAsync();
            using System.IO.StreamReader reader = new System.IO.StreamReader(stream);
            int promptTokens = 0, completionTokens = 0;

            while (!reader.EndOfStream)
            {
                string line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                string data = line[6..];
                if (data == "[DONE]")
                {
                    yield return new CompletionChunk { Done = true, Provider = provider.ToString(), Model = model, PromptTokens = promptTokens, CompletionTokens = completionTokens };
                    yield break;
                }
                using JsonDocument doc = JsonDocument.Parse(data);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                {
                    JsonElement choice = choices[0];
                    if (choice.TryGetProperty("delta", out JsonElement delta) && delta.TryGetProperty("content", out JsonElement content))
                    {
                        string text = content.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            completionTokens++;
                            yield return new CompletionChunk { Delta = text, Provider = provider.ToString(), Model = model };
                        }
                    }
                }
                if (root.TryGetProperty("usage", out JsonElement usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out JsonElement pt)) promptTokens = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out JsonElement ct)) completionTokens = ct.GetInt32();
                }
            }
            yield return new CompletionChunk { Done = true, Provider = provider.ToString(), Model = model, PromptTokens = promptTokens, CompletionTokens = completionTokens };
        }

        // ── Tool calling helpers ─────────────────────────────────────────────────────────────────

        /// <summary>Serialises messages into the OpenAI wire format, handling tool-role and assistant-with-tool_calls messages.</summary>
        private static List<object> BuildOpenAIMessages(List<ChatMessage> messages)
        {
            var result = new List<object>();
            foreach (ChatMessage m in messages)
            {
                if (m.Role == "tool")
                    result.Add(new { role = "tool", tool_call_id = m.ToolCallId, content = m.Content });
                else if (m.Role == "assistant" && m.ToolCalls?.Count > 0)
                    result.Add(new
                    {
                        role = "assistant",
                        content = (string)null,
                        tool_calls = m.ToolCalls.Select(tc => (object)new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                        }).ToList()
                    });
                else
                    result.Add(new { role = m.Role, content = m.Content });
            }
            return result;
        }

        /// <summary>Serialises messages into the Anthropic wire format, handling tool_use and tool_result blocks.</summary>
        private static List<object> BuildAnthropicMessages(List<ChatMessage> messages)
        {
            var result = new List<object>();
            foreach (ChatMessage m in messages.Where(x => x.Role != "system"))
            {
                if (m.Role == "tool")
                    result.Add(new
                    {
                        role = "user",
                        content = new object[] { new { type = "tool_result", tool_use_id = m.ToolCallId, content = m.Content } }
                    });
                else if (m.Role == "assistant" && m.ToolCalls?.Count > 0)
                    result.Add(new
                    {
                        role = "assistant",
                        content = m.ToolCalls.Select(tc => (object)new
                        {
                            type = "tool_use",
                            id = tc.Id,
                            name = tc.Function.Name,
                            input = JsonSerializer.Deserialize<object>(tc.Function.Arguments ?? "{}")
                        }).ToList()
                    });
                else
                    result.Add(new { role = m.Role, content = m.Content });
            }
            return result;
        }

        /// <summary>Serialises messages into the Gemini contents format, handling functionCall and functionResponse parts.</summary>
        private static List<object> BuildGeminiContents(List<ChatMessage> messages)
        {
            var result = new List<object>();
            foreach (ChatMessage m in messages.Where(x => x.Role != "system"))
            {
                if (m.Role == "tool")
                {
                    object responseVal;
                    try { responseVal = JsonSerializer.Deserialize<object>(m.Content ?? "null"); }
                    catch { responseVal = m.Content; }
                    result.Add(new
                    {
                        role = "user",
                        parts = new object[] { new { functionResponse = new { name = m.Name ?? "tool", response = new { result = responseVal } } } }
                    });
                }
                else if (m.Role == "assistant" && m.ToolCalls?.Count > 0)
                    result.Add(new
                    {
                        role = "model",
                        parts = m.ToolCalls.Select(tc => (object)new
                        {
                            functionCall = new
                            {
                                name = tc.Function.Name,
                                args = JsonSerializer.Deserialize<object>(tc.Function.Arguments ?? "{}")
                            }
                        }).ToList()
                    });
                else
                    result.Add(new
                    {
                        role = m.Role == "assistant" ? "model" : "user",
                        parts = new[] { new { text = m.Content } }
                    });
            }
            return result;
        }

        /// <summary>
        /// Returns a shallow copy of the request with tool descriptions injected into the system prompt.
        /// Used for providers that lack native tool calling (Cohere, HuggingFace, AzureOpenAI).
        /// </summary>
        private static CompletionRequest InjectToolShimIntoSystemPrompt(CompletionRequest request)
        {
            if (request.Tools == null || request.Tools.Count == 0)
                return request;

            var sb = new StringBuilder("\n\nYou have access to the following tools. When you want to call a tool, respond ONLY with a JSON object in this format:\n{\"tool_call\":{\"name\":\"<tool_name>\",\"arguments\":{<args>}}}\n\nAvailable tools:");
            foreach (ToolDefinition t in request.Tools)
            {
                sb.Append($"\n- {t.Function.Name}: {t.Function.Description}");
                if (t.Function.Parameters != null)
                    sb.Append($"\n  Parameters: {t.Function.Parameters}");
            }

            var cloned = new CompletionRequest
            {
                AvatarId = request.AvatarId,
                Provider = request.Provider,
                Model = request.Model,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Routing = request.Routing,
                Tools = null, // shim injected; don't pass tools through
                ToolChoice = request.ToolChoice,
                Messages = new List<ChatMessage>(request.Messages)
            };

            ChatMessage systemMsg = cloned.Messages.FirstOrDefault(m => m.Role == "system");
            if (systemMsg != null)
                systemMsg.Content = (systemMsg.Content ?? "") + sb.ToString();
            else
                cloned.Messages.Insert(0, new ChatMessage { Role = "system", Content = sb.ToString().TrimStart() });

            return cloned;
        }

        private async Task<ImageGenerationResponse> GenerateImageOpenAIAsync(ImageGenerationRequest request)
        {
            string apiKey = ApiKeys.TryGetValue(AIProviderType.OpenAI, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for OpenAI.");

            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? "gpt-image-1" : request.Model;

            var payload = new
            {
                model,
                prompt = request.Prompt,
                size = request.Size ?? "1024x1024",
                n = 1
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"OpenAI returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            string base64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();

            return new ImageGenerationResponse
            {
                Provider = AIProviderType.OpenAI.ToString(),
                Model = model,
                ImageBase64 = base64,
                OutputFormat = "png"
            };
        }
    }
}
