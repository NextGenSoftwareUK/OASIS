using HotChocolate;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Web6.WebAPI.Controllers;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for Web6 AI/ML/FAHRN/A2A operations.</summary>
    public class Mutation
    {
        private static OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        /// <summary>
        /// Generate text embeddings for one or more input strings.
        /// Returns a list of float vectors, one per input.
        /// </summary>
        public async Task<EmbeddingsResult> GenerateEmbeddingsAsync(
            [GraphQLDescription("One or more text strings to embed.")] List<string> inputs,
            [GraphQLDescription("AI provider to use for embedding (e.g. OpenAI).")] string provider = "",
            [GraphQLDescription("Specific model to use (leave empty for provider default).")] string model = "",
            [GraphQLDescription("Avatar GUID for karma-gated access (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var request = new EmbeddingRequest { Texts = inputs, Provider = provider, Model = model };
            var manager = new EmbeddingManager(aid, DNA);
            var result = await manager.EmbedAsync(request);
            if (result.IsError)
                return new EmbeddingsResult { IsError = true, Message = result.Message };

            return new EmbeddingsResult
            {
                IsError = false,
                Vectors = result.Result?.Embeddings?
                    .Select(vec => new EmbeddingVector { Values = vec?.Select(f => (double)f).ToList() ?? new List<double>() })
                    .ToList() ?? new List<EmbeddingVector>()
            };
        }

        /// <summary>
        /// Dispatch a problem to the FAHRN agent network.
        /// FAHRN classifies the task, scores available agents, and returns the winning agent's answer
        /// along with a Mermaid reasoning plan.
        /// </summary>
        public async Task<AgentDispatchResult> DispatchAgentAsync(
            [GraphQLDescription("The problem or question to dispatch to the FAHRN network.")] string problem,
            [GraphQLDescription("Optional task type hint (e.g. 'creative', 'analytical').")] string taskType = "",
            [GraphQLDescription("Avatar GUID for context injection and karma gating (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var request = new DispatchRequest { Problem = problem, TaskType = taskType, AvatarId = aid };
            var manager = new FAHRNManager(aid, DNA);
            var result = await manager.DispatchAsync(request);
            if (result.IsError)
                return new AgentDispatchResult { IsError = true, Message = result.Message };

            return new AgentDispatchResult
            {
                IsError          = false,
                Answer           = result.Result?.FinalAnswer ?? "",
                FinalMermaidPlan = result.Result?.FinalMermaidPlan ?? "",
                ModeUsed         = result.Result?.ModeUsed.ToString() ?? "",
                TotalLatencyMs   = result.Result?.TotalLatencyMs ?? 0
            };
        }

        /// <summary>
        /// Store a memory entry via an external memory provider (Mem0, Zep, Letta, etc.).
        /// </summary>
        public async Task<StoreMemoryResult> StoreMemoryAsync(
            [GraphQLDescription("Text content to store in memory.")] string content,
            [GraphQLDescription("External memory provider name (e.g. 'mem0', 'zep', 'letta').")] string provider = "",
            [GraphQLDescription("Avatar GUID for ownership tagging (optional).")] string avatarId = "")
        {
            if (string.IsNullOrWhiteSpace(content))
                return new StoreMemoryResult { IsError = true, Message = "content is required" };

            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;

            IExternalMemoryProvider memProvider = string.IsNullOrWhiteSpace(provider)
                ? MemoryProviderManager.Instance.ProviderNames.Count > 0
                    ? MemoryProviderManager.Instance.Get(MemoryProviderManager.Instance.ProviderNames[0])
                    : null
                : MemoryProviderManager.Instance.Get(provider);

            if (memProvider == null)
                return new StoreMemoryResult { IsError = true, Message = "No external memory provider is registered. Configure MEM0_API_KEY, ZEP_BASE_URL, or LETTA_BASE_URL." };

            await memProvider.AddAsync(aid, content, new Dictionary<string, string>());

            return new StoreMemoryResult
            {
                IsError = false,
                Id      = Guid.NewGuid().ToString(),
                Message = "Memory entry stored successfully."
            };
        }

        /// <summary>
        /// Send a completion request to any configured AI provider.
        /// Returns the generated text along with token usage and cost estimate.
        /// Provider options: openai, anthropic, gemini, groq, mistral, cohere, xai, deepseek, auto.
        /// </summary>
        public async Task<CompletionResult> CompleteAsync(
            [GraphQLDescription("The prompt or last user message.")] string prompt,
            [GraphQLDescription("System instruction prepended to the conversation.")] string systemPrompt = "",
            [GraphQLDescription("AI provider to use (auto selects the best available).")] string provider = "auto",
            [GraphQLDescription("Model override (leave empty for provider default).")] string model = "",
            [GraphQLDescription("Max tokens to generate.")] int maxTokens = 1024,
            [GraphQLDescription("Temperature (0–2). Lower = more deterministic.")] double temperature = 0.7,
            [GraphQLDescription("Avatar GUID for karma-gated access and usage tracking.")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;

            var request = new NextGenSoftware.OASIS.Web6.Core.Models.CompletionRequest
            {
                AvatarId    = aid,
                Provider    = provider,
                Model       = model,
                MaxTokens   = maxTokens,
                Temperature = temperature,
                Messages    = new System.Collections.Generic.List<NextGenSoftware.OASIS.Web6.Core.Models.ChatMessage>
                {
                    new NextGenSoftware.OASIS.Web6.Core.Models.ChatMessage { Role = "user", Content = prompt }
                }
            };
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                request.Messages.Insert(0, new NextGenSoftware.OASIS.Web6.Core.Models.ChatMessage { Role = "system", Content = systemPrompt });

            var manager = new NextGenSoftware.OASIS.Web6.Core.Managers.AIProviderManager(aid, DNA);
            var result = await manager.CompleteAsync(request);
            if (result.IsError)
                return new CompletionResult { IsError = true, Message = result.Message ?? "Completion failed." };

            var r = result.Result;
            return new CompletionResult
            {
                IsError          = false,
                Content          = r?.Content ?? "",
                Provider         = r?.Provider ?? "",
                Model            = r?.Model ?? "",
                PromptTokens     = r?.PromptTokens ?? 0,
                CompletionTokens = r?.CompletionTokens ?? 0,
                LatencyMs        = r?.LatencyMs ?? 0,
                EstimatedCostUsd = r?.EstimatedCostUSD ?? 0,
                FailedOver       = r?.FailedOver ?? false
            };
        }

        // ── A2A ──────────────────────────────────────────────────────────────

        public async Task<object> SendA2ATaskAsync(
            [GraphQLDescription("Problem text to send as an A2A task.")] string problem,
            [GraphQLDescription("Optional task ID (auto-generated if omitted).")] string taskId = "")
        {
            string id = string.IsNullOrWhiteSpace(taskId) ? Guid.NewGuid().ToString() : taskId;
            var manager = new FahrnSolveManager(Guid.Empty, DNA);
            var req = new FahrnSolveRequest { Problem = problem, AvatarId = Guid.Empty, ReturnReasoning = true };
            var result = await manager.SolveAsync(req);
            return new { id, state = result.IsError ? "failed" : "completed", output = result.IsError ? result.Message : result.Result?.Answer ?? "" };
        }

        public object CancelA2ATask([GraphQLDescription("A2A task ID to cancel.")] string id)
        {
            if (A2AController._tasks.TryGetValue(id, out var task))
                task.State = "cancelled";
            return new { cancelled = true, id };
        }

        // ── DID / VC ──────────────────────────────────────────────────────────

        public object? CreateDid([GraphQLDescription("Avatar GUID to create DID for.")] string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var aid) || aid == Guid.Empty)
                return null;
            var manager = new DidManager(aid, DNA);
            var result = manager.CreateDid(aid);
            return result.IsError ? null : result.Result;
        }

        public object? IssueVc(
            [GraphQLDescription("DID of the credential subject.")] string subjectDid,
            [GraphQLDescription("Avatar GUID of the issuer.")] string issuerAvatarId,
            [GraphQLDescription("JSON string of claim key-value pairs (e.g. '{\"role\":\"admin\"}').")] string claimsJson = "{}")
        {
            if (!Guid.TryParse(issuerAvatarId, out var aid))
                return null;
            Dictionary<string, object> claims;
            try { claims = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(claimsJson) ?? new(); }
            catch { claims = new(); }
            var manager = new DidManager(aid, DNA);
            var result = manager.IssueCredential(aid, subjectDid, claims);
            return result.IsError ? null : result.Result;
        }

        public bool VerifyVc(
            [GraphQLDescription("Avatar GUID of the issuer.")] string issuerAvatarId,
            [GraphQLDescription("JSON string of the Verifiable Credential to verify.")] string credentialJson)
        {
            if (!Guid.TryParse(issuerAvatarId, out var aid))
                return false;
            VerifiableCredential? vc;
            try { vc = System.Text.Json.JsonSerializer.Deserialize<VerifiableCredential>(credentialJson); }
            catch { return false; }
            if (vc == null) return false;
            var manager = new DidManager(Guid.Empty, DNA);
            var result = manager.VerifyCredential(vc, aid);
            return !result.IsError && result.Result;
        }

        // ── External Memory ───────────────────────────────────────────────────

        public async Task<bool> DeleteMemoryAsync(
            [GraphQLDescription("Provider name (mem0, zep, letta, etc.).")] string provider,
            [GraphQLDescription("Memory entry ID to delete.")] string id,
            [GraphQLDescription("Avatar GUID for scope.")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var memProvider = MemoryProviderManager.Instance.Get(provider);
            if (memProvider == null) return false;
            await memProvider.DeleteAsync(aid, id);
            return true;
        }

        // ── Holonic Braid ─────────────────────────────────────────────────────

        public async Task<object?> SaveHolonicBraidGraphAsync(
            [GraphQLDescription("Task type this graph applies to.")] string taskType,
            [GraphQLDescription("Mermaid diagram string.")] string mermaidDiagram,
            [GraphQLDescription("Model that generated this graph.")] string generatedByModel = "")
        {
            var manager = new HolonicBraidManager(Guid.Empty);
            var result = await manager.SaveGraphAsync(taskType, mermaidDiagram, generatedByModel);
            return result.IsError ? null : result.Result;
        }

        // ── Holonic Memory ────────────────────────────────────────────────────

        public async Task<object?> GetOrCreateHolonAsync(
            [GraphQLDescription("Holonic memory level (Session, Agent, User, Group, etc.).")] string level,
            [GraphQLDescription("Holon name.")] string name,
            [GraphQLDescription("Parent holon GUID.")] string parentHolonId)
        {
            if (!Enum.TryParse<HolonicMemoryLevel>(level, true, out var lvl)) return null;
            if (!Guid.TryParse(parentHolonId, out var phid)) return null;
            var manager = new HolonicMemoryManager(Guid.Empty);
            var result = await manager.GetOrCreateHolonAsync(lvl, name, phid);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> SetMembraneRuleAsync(
            [GraphQLDescription("Holon GUID to set membrane rule on.")] string holonId,
            [GraphQLDescription("JSON of MembraneRule (propagation filter).")] string ruleJson)
        {
            if (!Guid.TryParse(holonId, out var hid)) return false;
            MembraneRule? rule;
            try { rule = System.Text.Json.JsonSerializer.Deserialize<MembraneRule>(ruleJson); }
            catch { return false; }
            if (rule == null) return false;
            var manager = new HolonicMemoryManager(Guid.Empty);
            var result = await manager.SetMembraneRuleAsync(hid, rule);
            return !result.IsError;
        }

        public async Task<bool> RecordHolonMemoryAsync(
            [GraphQLDescription("Holon GUID to record memory into.")] string holonId,
            [GraphQLDescription("JSON of HolonicMemoryItem to record.")] string itemJson)
        {
            if (!Guid.TryParse(holonId, out var hid)) return false;
            HolonicMemoryItem? item;
            try { item = System.Text.Json.JsonSerializer.Deserialize<HolonicMemoryItem>(itemJson); }
            catch { return false; }
            if (item == null) return false;
            var manager = new HolonicMemoryManager(Guid.Empty);
            var result = await manager.RecordMemoryAsync(hid, item);
            return !result.IsError;
        }

        public async Task<int> PropagateHolonMemoryUpAsync(
            [GraphQLDescription("Child holon GUID to propagate from.")] string childHolonId,
            [GraphQLDescription("Number of hops to propagate (use int.MaxValue for all the way to Earth).")] int levels = 1)
        {
            if (!Guid.TryParse(childHolonId, out var chid)) return 0;
            var manager = new HolonicMemoryManager(Guid.Empty, DNA);
            var result = await manager.PropagateUpAsync(chid, levels);
            return result.IsError ? 0 : result.Result;
        }

        public async Task<int> PropagateHolonMemoryAsync(
            [GraphQLDescription("Child holon GUID to propagate from (one hop up).")] string childHolonId)
        {
            if (!Guid.TryParse(childHolonId, out var chid)) return 0;
            var manager = new HolonicMemoryManager(Guid.Empty);
            var result = await manager.PropagateAsync(chid);
            return result.IsError ? 0 : result.Result;
        }

        // ── Key Vault ─────────────────────────────────────────────────────────

        public async Task<bool> UpsertKeyAsync(
            [GraphQLDescription("Avatar GUID.")] string avatarId,
            [GraphQLDescription("Provider name (openai, anthropic, etc.).")] string provider,
            [GraphQLDescription("API key to store (encrypted).")] string apiKey)
        {
            if (!Guid.TryParse(avatarId, out var aid)) return false;
            var vault = new KeyVaultManager(aid, DNA);
            var result = await vault.SaveProviderKeyAsync(provider.ToLowerInvariant(), apiKey);
            return !result.IsError;
        }

        public async Task<bool> DeleteKeyAsync(
            [GraphQLDescription("Avatar GUID.")] string avatarId,
            [GraphQLDescription("Provider whose key to delete.")] string provider)
        {
            if (!Guid.TryParse(avatarId, out var aid)) return false;
            var vault = new KeyVaultManager(aid, DNA);
            var result = await vault.DeleteProviderKeyAsync(provider.ToLowerInvariant());
            return !result.IsError;
        }

        // ── ML.NET ────────────────────────────────────────────────────────────

        public object TrainTaskClassifierAsync(
            [GraphQLDescription("List of problem texts (must be ≥10 items).")] List<string> problems,
            [GraphQLDescription("Corresponding task type labels.")] List<string> labels)
        {
            if (problems == null || labels == null || problems.Count < 10 || problems.Count != labels.Count)
                return new { ok = false, message = "At least 10 matched problem/label pairs required." };
            var manager = new MLNetManager(Guid.Empty, DNA);
            var samples = problems.Zip(labels, (p, l) => (p, l)).ToList();
            var result = manager.TrainTaskClassifier(samples);
            return new { ok = !result.IsError, message = result.IsError ? result.Message : "Model trained." };
        }

        // ── Orchestrators ─────────────────────────────────────────────────────

        public async Task<object?> RegisterOrchestratorAdapterAsync(
            [GraphQLDescription("JSON of OrchestratorAdapterConfig.")] string configJson)
        {
            OrchestratorAdapterConfig? config;
            try { config = System.Text.Json.JsonSerializer.Deserialize<OrchestratorAdapterConfig>(configJson); }
            catch { return null; }
            if (config == null) return null;
            var manager = new OrchestratorManager(Guid.Empty);
            var result = await manager.RegisterAdapterAsync(config);
            return result.IsError ? null : result.Result;
        }

        public async Task<object?> InvokeOrchestratorAsync(
            [GraphQLDescription("JSON of OrchestratorInvokeRequest.")] string requestJson)
        {
            OrchestratorInvokeRequest? request;
            try { request = System.Text.Json.JsonSerializer.Deserialize<OrchestratorInvokeRequest>(requestJson); }
            catch { return null; }
            if (request == null) return null;
            var manager = new OrchestratorManager(Guid.Empty);
            var result = await manager.InvokeAsync(request);
            return result.IsError ? null : result.Result;
        }

        // ── Reasoning Network ─────────────────────────────────────────────────

        public async Task<object?> RegisterReasoningAgentAsync(
            [GraphQLDescription("JSON of ReasoningAgentMetadata.")] string agentJson)
        {
            ReasoningAgentMetadata? agent;
            try { agent = System.Text.Json.JsonSerializer.Deserialize<ReasoningAgentMetadata>(agentJson); }
            catch { return null; }
            if (agent == null) return null;
            var manager = new FAHRNManager(Guid.Empty);
            var result = await manager.RegisterAgentAsync(agent);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<object>> SeedOpenServAgentsAsync()
        {
            var manager = new FAHRNManager(Guid.Empty);
            var result = await manager.SeedDefaultOpenServAgentsAsync();
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<object?> EvolveAgentSkillAsync(
            [GraphQLDescription("Agent GUID.")] string agentId,
            [GraphQLDescription("Skill category to evolve.")] string category)
        {
            if (!Guid.TryParse(agentId, out var aid)) return null;
            var manager = new SkillOptManager(Guid.Empty, DNA);
            var result = await manager.RunEpochAsync(aid, category);
            return result.IsError ? null : result.Result;
        }

        // ── FAHRN Solve ───────────────────────────────────────────────────────

        public async Task<object?> SolveFahrnAsync(
            [GraphQLDescription("Problem to solve via the full FAHRN pipeline.")] string problem,
            [GraphQLDescription("Avatar GUID for context injection.")] string avatarId = "",
            [GraphQLDescription("Return full reasoning trace?")] bool returnReasoning = false)
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var manager = new FahrnSolveManager(aid, DNA);
            var result = await manager.SolveAsync(new FahrnSolveRequest { Problem = problem, AvatarId = aid, ReturnReasoning = returnReasoning });
            return result.IsError ? null : result.Result;
        }

        /// <summary>
        /// Generate an image from a text prompt using the configured image-generation provider.
        /// Returns the image as a base64-encoded string.
        /// </summary>
        public async Task<ImageGenerationResult> GenerateImageAsync(
            [GraphQLDescription("Text prompt describing the image to generate.")] string prompt,
            [GraphQLDescription("AI provider to use (e.g. StabilityAI, OpenAI).")] string provider = "StabilityAI",
            [GraphQLDescription("Model name override (leave empty for provider default).")] string model = "",
            [GraphQLDescription("Image width in pixels.")] int width = 1024,
            [GraphQLDescription("Image height in pixels.")] int height = 1024,
            [GraphQLDescription("Avatar GUID for karma-gated access (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;

            if (!Enum.TryParse<AIProviderType>(provider, true, out var providerType))
                providerType = AIProviderType.StabilityAI;

            var request = new ImageGenerationRequest
            {
                Prompt   = prompt,
                Provider = providerType,
                Model    = model,
                Size     = $"{width}x{height}"
            };

            var manager = new AIProviderManager(aid, DNA);
            var result = await manager.GenerateImageAsync(request);
            if (result.IsError)
                return new ImageGenerationResult { IsError = true, Message = result.Message };

            return new ImageGenerationResult
            {
                IsError     = false,
                Base64Image = result.Result?.ImageBase64 ?? "",
                Message     = "Image generated successfully."
            };
        }
    }

    // ──────────────────────────── Result types ────────────────────────────────

    public class CompletionResult
    {
        public bool   IsError          { get; set; }
        public string Message          { get; set; } = "";
        public string Content          { get; set; } = "";
        public string Provider         { get; set; } = "";
        public string Model            { get; set; } = "";
        public int    PromptTokens     { get; set; }
        public int    CompletionTokens { get; set; }
        public long   LatencyMs        { get; set; }
        public double EstimatedCostUsd { get; set; }
        public bool   FailedOver       { get; set; }
    }

    public class EmbeddingsResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public List<EmbeddingVector> Vectors { get; set; } = new();
    }

    public class EmbeddingVector
    {
        public List<double> Values { get; set; } = new();
    }

    public class AgentDispatchResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Answer { get; set; } = "";
        public string FinalMermaidPlan { get; set; } = "";
        public string ModeUsed { get; set; } = "";
        public double TotalLatencyMs { get; set; }
    }

    public class StoreMemoryResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Id { get; set; } = "";
    }

    public class ImageGenerationResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Base64Image { get; set; } = "";
    }
}
