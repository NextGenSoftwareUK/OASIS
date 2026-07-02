using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
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
            if (request.AvatarId == System.Guid.Empty)
                request.AvatarId = AvatarId;

            var web6 = OASISDNA?.OASIS?.Web6;

            // Resolve effective FAHRN / Holonic BRAID flags: per-request override → DNA default → false.
            bool useFAHRN         = request.UseFAHRN         ?? web6?.EnableFAHRN         ?? false;
            bool useHolonicBraid  = request.UseHolonicBraid  ?? web6?.EnableHolonicBraid  ?? false;

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

            // --- Provider call ---
            var manager = new AIProviderManager(request.AvatarId, OASISDNA);
            var result  = await manager.CompleteAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
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
