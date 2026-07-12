using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// The WEB6 unified AI completion endpoint — one request shape, routed/normalised across every AI provider.
    ///
    /// Provider routing:
    ///   CompletionRequest.Provider = "openai" | "anthropic" | "gemini" | "groq" | "mistral" | "cohere" |
    ///                                "xai" | "deepseek" | "huggingface" | "azureopenai" | "awsbedrock" |
    ///                                "openserv" | "ollama" | "auto"
    ///   "auto" honours OASIS_DNA.Web6.DefaultProvider and PreferOpenServ:
    ///     - PreferOpenServ = true  → routes through the OpenServ SERV gateway (one key, every model)
    ///     - PreferOpenServ = false → calls the highest-priority configured direct provider
    ///   Override per-request via CompletionRequest.Routing.UseOpenServ.
    ///
    /// FAHRN + Holonic BRAID (optional reasoning enhancement):
    ///   Enabled globally via OASIS_DNA.Web6.EnableFAHRN / EnableHolonicBraid.
    ///   Override per-request via CompletionRequest.UseFAHRN / UseHolonicBraid.
    ///   When enabled, the pipeline runs before the provider call:
    ///     1. (if UseHolonicBraid) GET shared reasoning graph for FahrnTaskType → inject into system context
    ///     2. (if UseFAHRN) FAHRN dispatch for the last user message → inject Mermaid plan into system context
    ///     3. Call the AI provider with the enriched context.
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class CompletionController : Web6ControllerBase
    {
        /// <summary>
        /// Routes a completion request to whichever AI provider/model best fits, normalising the response.
        /// Optionally enhances the request with FAHRN reasoning-network dispatch and/or Holonic BRAID graph
        /// injection before calling the provider — controlled by OASIS_DNA.Web6 settings and/or per-request flags.
        /// POST https://api.web6.oasisomniverse.one/v1/complete
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Complete([FromBody] CompletionRequest request)
        {
            if (request.AvatarId == Guid.Empty)
                request.AvatarId = AvatarId;

            var web6 = OASISDNA?.OASIS?.Web6;

            // --- Quota pre-flight (Priority 12) ---
            if (request.AvatarId != Guid.Empty)
            {
                var metering = new UsageMeteringManager(request.AvatarId, OASISDNA);
                string quotaViolation = await metering.CheckQuotaAsync();
                if (quotaViolation != null)
                {
                    Response.Headers["Retry-After"] = SecondsUntilNextReset().ToString();
                    return StatusCode(429, new { error = quotaViolation });
                }
            }

            // --- Semantic cache lookup (Priority 13) ---
            var cache = new SemanticCacheManager(request.AvatarId, OASISDNA);
            CompletionResponse cached = await cache.GetAsync(request);
            if (cached != null)
                return Ok(cached);

            // Resolve effective FAHRN / Holonic BRAID flags: per-request override → DNA default → false.
            bool useFAHRN         = request.UseFAHRN         ?? web6?.EnableFAHRN         ?? false;
            bool useHolonicBraid  = request.UseHolonicBraid  ?? web6?.EnableHolonicBraid  ?? false;

            // --- Avatar context injection (Priority 3a) ---
            bool injectCtx = request.InjectAvatarContext ?? web6?.InjectAvatarContext ?? false;
            if (injectCtx && request.AvatarId != System.Guid.Empty)
            {
                string bearer = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                StarnetContextManager ctxManager = new StarnetContextManager(request.AvatarId, OASISDNA);
                string ctxStr = await ctxManager.GetAvatarContextStringAsync(request.AvatarId, bearer);
                if (!string.IsNullOrEmpty(ctxStr))
                    InjectIntoSystemContext(request, ctxStr);
            }

            // --- Holonic BRAID: inject shared reasoning graph into system context ---
            if (useHolonicBraid)
            {
                var braidManager = new HolonicBraidManager(AvatarId, OASISDNA);
                var braidResult  = await braidManager.FindGraphForTaskTypeAsync(request.FahrnTaskType ?? "general");

                if (!braidResult.IsError && !string.IsNullOrEmpty(braidResult.Result?.MermaidDiagram))
                {
                    string injection = $"[Holonic BRAID shared reasoning graph for task type '{request.FahrnTaskType}']\n```mermaid\n{braidResult.Result.MermaidDiagram}\n```";
                    InjectIntoSystemContext(request, injection);
                }
            }

            // --- FAHRN: dispatch the latest user message through the reasoning network ---
            if (useFAHRN)
            {
                string lastUserMessage = request.Messages?
                    .LastOrDefault(m => string.Equals(m.Role, "user", System.StringComparison.OrdinalIgnoreCase))
                    ?.Content;

                if (!string.IsNullOrEmpty(lastUserMessage))
                {
                    var fahrnManager = new FAHRNManager(AvatarId, OASISDNA);
                    var dispatchResult = await fahrnManager.DispatchAsync(new DispatchRequest
                    {
                        Problem  = lastUserMessage,
                        TaskType = request.FahrnTaskType ?? "general",
                        AvatarId = request.AvatarId,
                    });

                    if (!dispatchResult.IsError && !string.IsNullOrEmpty(dispatchResult.Result?.FinalMermaidPlan))
                    {
                        string injection = $"[FAHRN execution plan]\n```mermaid\n{dispatchResult.Result.FinalMermaidPlan}\n```";
                        InjectIntoSystemContext(request, injection);
                    }
                }
            }

            // --- External memory injection (Priority 15) ---
            if (request.ExternalMemoryProviders?.Count > 0 && request.AvatarId != Guid.Empty)
            {
                try
                {
                    string lastQuery = request.Messages?.LastOrDefault(m => string.Equals(m.Role, "user", System.StringComparison.OrdinalIgnoreCase))?.Content ?? "";
                    if (!string.IsNullOrEmpty(lastQuery))
                    {
                        var memResults = await MemoryProviderManager.Instance.SearchAllAsync(request.AvatarId, lastQuery, request.ExternalMemoryProviders);
                        string memBlock = MemoryProviderManager.BuildContextBlock(memResults);
                        if (!string.IsNullOrEmpty(memBlock))
                            InjectIntoSystemContext(request, memBlock);
                    }
                }
                catch { /* external memory search is best-effort */ }
            }

            // --- Auto-inject built-in OASIS tools when avatar is known and caller hasn't specified tools ---
            if (request.AvatarId != Guid.Empty && (request.Tools == null || request.Tools.Count == 0))
                request.Tools = BuiltInTools.All;

            // --- Provider call ---
            var manager = new AIProviderManager(request.AvatarId, OASISDNA);
            var result  = await manager.CompleteAsync(request);

            if (!result.IsError && result.Result != null)
            {
                // Cost metering (Priority 12)
                if (request.AvatarId != Guid.Empty)
                {
                    var metering = new UsageMeteringManager(request.AvatarId, OASISDNA);
                    if (Enum.TryParse<AIProviderType>(result.Result.Provider, true, out var providerType))
                        result.Result.EstimatedCostUSD = await metering.RecordUsageAsync(providerType, result.Result.Model ?? "", result.Result.PromptTokens, result.Result.CompletionTokens);
                }

                // Semantic cache store (Priority 13)
                await cache.SetAsync(request, result.Result);

                // Priority 19a — Telemetry
                TelemetryController.Publish(new TelemetryEvent
                {
                    Provider = result.Result.Provider,
                    Model = result.Result.Model,
                    PromptTokens = result.Result.PromptTokens,
                    CompletionTokens = result.Result.CompletionTokens,
                    EstimatedCostUSD = result.Result.EstimatedCostUSD,
                    AvatarId = request.AvatarId,
                    AvatarContextInjected = request.InjectAvatarContext == true
                });
            }

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// Continues a tool-calling loop by re-entering the completion pipeline with the full message
        /// history already containing the assistant's tool_call message(s) and one or more tool result
        /// messages (Role="tool"). The pipeline is identical to POST /v1/complete — the separate endpoint
        /// name is a semantic marker so callers can distinguish agentic loop turns from initial requests.
        /// POST https://api.web6.oasisomniverse.one/v1/complete/tool-result
        /// </summary>
        [HttpPost("complete/tool-result")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        public Task<IActionResult> CompleteToolResult([FromBody] CompletionRequest request)
            => Complete(request);

        /// <summary>
        /// Streams a completion response as SSE (text/event-stream). Each event is a JSON CompletionChunk;
        /// the final event has Done=true and includes token counts.
        /// POST https://api.web6.oasisomniverse.one/v1/complete/stream
        /// </summary>
        [HttpPost("complete/stream")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task CompleteStream([FromBody] CompletionRequest request)
        {
            if (request.AvatarId == System.Guid.Empty)
                request.AvatarId = AvatarId;

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            var manager = new AIProviderManager(request.AvatarId, OASISDNA);
            await foreach (var chunk in manager.CompleteStreamAsync(request))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// Lists the models reachable through the OpenServ provider (provider: "openserv"), i.e. the full
        /// SERV catalog spanning OpenAI, Anthropic, Google, xAI, Qwen and DeepSeek behind one SERV_API_KEY.
        /// GET https://api.web6.oasisomniverse.one/v1/openserv/models
        /// </summary>
        [HttpGet("openserv/models")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<OpenServModel>), StatusCodes.Status200OK)]
        public IActionResult OpenServModels()
        {
            return Ok(OpenServCatalog.Models);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private static int SecondsUntilNextReset()
        {
            DateTime now = DateTime.UtcNow;
            // Daily reset: seconds until midnight UTC
            return (int)(now.Date.AddDays(1) - now).TotalSeconds;
        }

        /// <summary>
        /// Appends <paramref name="text"/> to the existing system message, or inserts a new system message at
        /// position 0 if none is present. Used to inject FAHRN plans and Holonic BRAID graphs.
        /// </summary>
        private static void InjectIntoSystemContext(CompletionRequest request, string text)
        {
            if (request.Messages == null)
                return;

            var systemMsg = request.Messages.FirstOrDefault(
                m => string.Equals(m.Role, "system", System.StringComparison.OrdinalIgnoreCase));

            if (systemMsg != null)
                systemMsg.Content = string.IsNullOrEmpty(systemMsg.Content)
                    ? text
                    : systemMsg.Content + "\n\n" + text;
            else
                request.Messages.Insert(0, new ChatMessage { Role = "system", Content = text });
        }
    }
}
