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
