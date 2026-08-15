using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// OpenAI-compatible shim — exposes POST /v1/chat/completions so that any framework that
    /// targets the OpenAI API (LangChain, AutoGen, CrewAI, Semantic Kernel, etc.) can use
    /// WEB6 as a drop-in by changing only the base URL and API key.
    ///
    /// Usage:
    ///   Base URL : https://api.web6.oasisomniverse.one
    ///   API key  : your OASIS avatar JWT
    ///   Model    : any WEB6 model id ("claude-sonnet-4-6", "gpt-4o", "auto", etc.)
    ///
    /// The model field controls provider routing:
    ///   "auto"             → WEB6 picks the best available provider
    ///   "claude-*"         → Anthropic
    ///   "gpt-*" / "o1-*"  → OpenAI
    ///   "gemini-*"         → Google Gemini
    ///   anything else      → WEB6 best-match or "auto" fallback
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class OpenAICompatController : Web6ControllerBase
    {
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ── Request / Response DTOs ──────────────────────────────────────────────────

        public class OAIRequest
        {
            public string Model { get; set; } = "auto";
            public List<ChatMessage> Messages { get; set; } = new();
            public double? Temperature { get; set; }
            public int? MaxTokens { get; set; }
            [JsonPropertyName("max_completion_tokens")]
            public int? MaxCompletionTokens { get; set; }
            public bool Stream { get; set; } = false;
            public List<ToolDefinition> Tools { get; set; }
            public string ToolChoice { get; set; } = "auto";
        }

        public class OAIResponse
        {
            public string Id { get; set; }
            public string Object { get; set; } = "chat.completion";
            public long Created { get; set; }
            public string Model { get; set; }
            public List<OAIChoice> Choices { get; set; }
            public OAIUsage Usage { get; set; }
        }

        public class OAIChoice
        {
            public int Index { get; set; } = 0;
            public ChatMessage Message { get; set; }
            public string FinishReason { get; set; }
        }

        public class OAIUsage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
        }

        public class OAIStreamChunk
        {
            public string Id { get; set; }
            public string Object { get; set; } = "chat.completion.chunk";
            public long Created { get; set; }
            public string Model { get; set; }
            public List<OAIStreamChoice> Choices { get; set; }
        }

        public class OAIStreamChoice
        {
            public int Index { get; set; } = 0;
            public OAIDelta Delta { get; set; }
            public string FinishReason { get; set; }
        }

        public class OAIDelta
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        // ── Endpoints ────────────────────────────────────────────────────────────────

        /// <summary>
        /// OpenAI-compatible chat completions. Accepts the standard OpenAI request body and
        /// returns a standard OpenAI response, backed by WEB6's full provider-routing pipeline.
        /// POST https://api.web6.oasisomniverse.one/v1/chat/completions
        /// </summary>
        [HttpPost("chat/completions")]
        [ProducesResponseType(typeof(OAIResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChatCompletions([FromBody] OAIRequest request)
        {
            var completion = MapToCompletionRequest(request);

            if (request.Stream)
            {
                await StreamResponse(completion, request.Model);
                return new EmptyResult();
            }

            string bearer = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var manager = new AIProviderManager(AvatarId, OASISDNA);
            var result = await manager.CompleteAsync(completion, bearer);

            if (result.IsError || result.Result == null)
                return StatusCode(500, new { error = new { message = "WEB6 completion failed", type = "server_error" } });

            return Ok(MapToOAIResponse(result.Result, request.Model));
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private CompletionRequest MapToCompletionRequest(OAIRequest req)
        {
            // Infer provider from model name so routing works without explicit provider field.
            string provider = InferProvider(req.Model);
            string model    = provider == "auto" ? "auto" : req.Model;

            return new CompletionRequest
            {
                Provider    = provider,
                Model       = model,
                Messages    = req.Messages,
                Temperature = req.Temperature,
                MaxTokens   = req.MaxTokens ?? req.MaxCompletionTokens,
                Tools       = req.Tools,
                ToolChoice  = req.ToolChoice,
                AvatarId    = AvatarId,
            };
        }

        private static string InferProvider(string model)
        {
            if (string.IsNullOrEmpty(model) || model == "auto") return "auto";
            if (model.StartsWith("claude",  StringComparison.OrdinalIgnoreCase)) return "anthropic";
            if (model.StartsWith("gpt-",    StringComparison.OrdinalIgnoreCase)) return "openai";
            if (model.StartsWith("o1-",     StringComparison.OrdinalIgnoreCase)) return "openai";
            if (model.StartsWith("o3-",     StringComparison.OrdinalIgnoreCase)) return "openai";
            if (model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase)) return "gemini";
            if (model.StartsWith("mistral", StringComparison.OrdinalIgnoreCase)) return "mistral";
            if (model.StartsWith("llama",   StringComparison.OrdinalIgnoreCase)) return "llama";
            if (model.StartsWith("deepseek",StringComparison.OrdinalIgnoreCase)) return "deepseek";
            return "auto";
        }

        private static OAIResponse MapToOAIResponse(CompletionResponse r, string requestedModel)
        {
            return new OAIResponse
            {
                Id      = $"chatcmpl-{r.Id}",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model   = r.Model ?? requestedModel,
                Choices = new List<OAIChoice>
                {
                    new OAIChoice
                    {
                        Message      = new ChatMessage { Role = "assistant", Content = r.Content },
                        FinishReason = r.FinishReason ?? "stop"
                    }
                },
                Usage = new OAIUsage
                {
                    PromptTokens     = r.PromptTokens,
                    CompletionTokens = r.CompletionTokens,
                    TotalTokens      = r.PromptTokens + r.CompletionTokens
                }
            };
        }

        private async Task StreamResponse(CompletionRequest completion, string requestedModel)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"]    = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            string id      = $"chatcmpl-{Guid.NewGuid():N}";
            long   created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // First chunk carries the role
            await WriteStreamChunk(new OAIStreamChunk
            {
                Id      = id,
                Created = created,
                Model   = requestedModel,
                Choices = new List<OAIStreamChoice>
                {
                    new OAIStreamChoice { Delta = new OAIDelta { Role = "assistant" } }
                }
            });

            var manager = new AIProviderManager(AvatarId, OASISDNA);
            await foreach (var chunk in manager.CompleteStreamAsync(completion))
            {
                if (chunk.Done)
                {
                    await WriteStreamChunk(new OAIStreamChunk
                    {
                        Id      = id,
                        Created = created,
                        Model   = chunk.Model ?? requestedModel,
                        Choices = new List<OAIStreamChoice>
                        {
                            new OAIStreamChoice { Delta = new OAIDelta(), FinishReason = "stop" }
                        }
                    });
                    break;
                }

                await WriteStreamChunk(new OAIStreamChunk
                {
                    Id      = id,
                    Created = created,
                    Model   = chunk.Model ?? requestedModel,
                    Choices = new List<OAIStreamChoice>
                    {
                        new OAIStreamChoice { Delta = new OAIDelta { Content = chunk.Delta } }
                    }
                });
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }

        private async Task WriteStreamChunk(OAIStreamChunk chunk)
        {
            string json = JsonSerializer.Serialize(chunk, _json);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
