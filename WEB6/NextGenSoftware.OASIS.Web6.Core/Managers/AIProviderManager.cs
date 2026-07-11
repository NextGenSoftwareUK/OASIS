using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
                _ => ("https://api.openai.com/v1/chat/completions", "gpt-4o"),
            };
        }

        private async Task<CompletionResponse> CallOpenAICompatibleAsync(AIProviderType provider, CompletionRequest request)
        {
            (string baseUrl, string defaultModel) = GetOpenAICompatibleEndpoint(provider);
            string model = string.IsNullOrEmpty(request.Model) || request.Model == "auto" ? defaultModel : request.Model;
            string apiKey = ApiKeys.TryGetValue(provider, out string key) ? key : null;

            if (string.IsNullOrEmpty(apiKey) && provider != AIProviderType.Ollama)
                throw new InvalidOperationException($"No API key configured for {provider}.");

            var payload = new
            {
                model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 4096
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl);

            if (!string.IsNullOrEmpty(apiKey))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"{provider} returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            JsonElement choice = root.GetProperty("choices")[0];
            JsonElement message = choice.GetProperty("message");
            string content = message.GetProperty("content").GetString();

            if (content == null)
            {
                // content is null when the model refused, used tool calls, or hit a content filter.
                // Surface the finish_reason and any refusal text so the caller knows exactly why.
                string finishReason = choice.TryGetProperty("finish_reason", out JsonElement fr) ? fr.GetString() : "unknown";
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
            var userMessages = request.Messages.Where(m => m.Role != "system").Select(m => new { role = m.Role, content = m.Content });

            var payload = new
            {
                model,
                max_tokens = request.MaxTokens ?? 4096,
                system = string.IsNullOrEmpty(systemPrompt) ? null : systemPrompt,
                messages = userMessages,
                temperature = request.Temperature
            };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Anthropic returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string content = root.GetProperty("content")[0].GetProperty("text").GetString();

            if (content == null)
                throw new InvalidOperationException($"Anthropic returned null content. Raw response: {body}");

            int promptTokens = root.TryGetProperty("usage", out JsonElement usage) && usage.TryGetProperty("input_tokens", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usage", out JsonElement usage2) && usage2.TryGetProperty("output_tokens", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = AIProviderType.Anthropic.ToString(),
                Model = model,
                Content = content,
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
            var contents = request.Messages.Where(m => m.Role != "system").Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            });

            var payload = new
            {
                contents,
                systemInstruction = string.IsNullOrEmpty(systemPrompt) ? null : new { parts = new[] { new { text = systemPrompt } } },
                generationConfig = new { temperature = request.Temperature, maxOutputTokens = request.MaxTokens }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini returned {(int)httpResponse.StatusCode}: {body}");

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string content = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (content == null)
                throw new InvalidOperationException($"Gemini returned null content. Raw response: {body}");

            int promptTokens = root.TryGetProperty("usageMetadata", out JsonElement usage) && usage.TryGetProperty("promptTokenCount", out JsonElement pt) ? pt.GetInt32() : 0;
            int completionTokens = root.TryGetProperty("usageMetadata", out JsonElement usage2) && usage2.TryGetProperty("candidatesTokenCount", out JsonElement ct) ? ct.GetInt32() : 0;

            return new CompletionResponse
            {
                Provider = AIProviderType.Gemini.ToString(),
                Model = model,
                Content = content,
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
