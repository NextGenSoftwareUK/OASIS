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
    /// (OpenAI, Anthropic, Gemini, Ollama, Groq, Mistral, etc.) behind a single unified interface, with
    /// automatic provider selection, fail-over and cost/latency/quality-based routing.
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
            ApiKeys[AIProviderType.OpenAI] = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            ApiKeys[AIProviderType.Anthropic] = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            ApiKeys[AIProviderType.Gemini] = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            ApiKeys[AIProviderType.Groq] = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            ApiKeys[AIProviderType.Mistral] = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
            ApiKeys[AIProviderType.Cohere] = Environment.GetEnvironmentVariable("COHERE_API_KEY");
            ApiKeys[AIProviderType.XAI] = Environment.GetEnvironmentVariable("XAI_API_KEY");
            ApiKeys[AIProviderType.DeepSeek] = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            ApiKeys[AIProviderType.HuggingFace] = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY");
            ApiKeys[AIProviderType.AzureOpenAI] = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            ApiKeys[AIProviderType.StabilityAI] = Environment.GetEnvironmentVariable("STABILITY_API_KEY");
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
            if (Enum.TryParse(request.Provider, true, out AIProviderType requested) && requested != AIProviderType.Auto)
                return new List<AIProviderType> { requested };

            // "auto" - prefer whichever providers we actually have an API key configured for,
            // ordered by the requested routing priority.
            List<AIProviderType> configured = ApiKeys.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();

            if (configured.Count == 0)
                return new List<AIProviderType> { AIProviderType.OpenAI };

            return request.Routing?.Priority?.ToLowerInvariant() switch
            {
                "quality" => OrderByPreference(configured, AIProviderType.Anthropic, AIProviderType.OpenAI, AIProviderType.Gemini),
                "latency" => OrderByPreference(configured, AIProviderType.Groq, AIProviderType.Gemini, AIProviderType.OpenAI),
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
                max_tokens = request.MaxTokens
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
            string content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
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
                max_tokens = request.MaxTokens
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
                max_tokens = request.MaxTokens
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
                max_tokens = request.MaxTokens
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
