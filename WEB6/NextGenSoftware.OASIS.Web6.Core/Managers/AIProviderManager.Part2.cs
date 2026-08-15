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
    }
}
