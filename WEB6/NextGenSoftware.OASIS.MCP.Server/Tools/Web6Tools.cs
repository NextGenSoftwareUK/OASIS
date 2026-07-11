using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB6 - the unified AI abstraction layer. In-process MCP tools wrapping every public method of
    /// AIProviderManager, HolonicBraidManager, FAHRNManager and HolonicMemoryManager - the full surface of the
    /// WEB6 Core API, called directly (no HTTP round-trip needed since this server shares the same OASIS process).
    /// </summary>
    [McpServerToolType]
    public static class Web6Tools
    {
        private static Guid ParseAvatarId(string? avatarId) => Guid.TryParse(avatarId, out Guid id) ? id : Guid.Empty;

        [McpServerTool(Name = "web6_complete"), Description("WEB6: routes a unified chat completion request to whichever AI provider/model best fits (OpenAI, Anthropic, Gemini, Groq, Mistral, XAI, Ollama, Cohere, AzureOpenAI, HuggingFace, AWSBedrock, or 'auto'), normalising the response.")]
        public static async Task<string> Complete(CompletionRequest request, string? avatarId = null)
        {
            request.AvatarId = request.AvatarId == Guid.Empty ? ParseAvatarId(avatarId) : request.AvatarId;
            AIProviderManager manager = new AIProviderManager(request.AvatarId);
            var result = await manager.CompleteAsync(request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_generate_image"), Description("WEB6: generates an image via StabilityAI or OpenAI (gpt-image-1).")]
        public static async Task<string> GenerateImage(ImageGenerationRequest request, string? avatarId = null)
        {
            AIProviderManager manager = new AIProviderManager(ParseAvatarId(avatarId));
            var result = await manager.GenerateImageAsync(request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_braid_find_graph"), Description("WEB6 Holonic BRAID: looks up the shared reasoning graph already generated for a task type, if any (lookup-or-create pattern - zero generation cost on a hit).")]
        public static async Task<string> BraidFindGraph(string taskType, string? avatarId = null)
        {
            HolonicBraidManager manager = new HolonicBraidManager(ParseAvatarId(avatarId));
            var result = await manager.FindGraphForTaskTypeAsync(taskType);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_braid_save_graph"), Description("WEB6 Holonic BRAID: stores a newly generated Mermaid reasoning graph in the shared library for a task type (the Generator step of the two-stage BRAID protocol).")]
        public static async Task<string> BraidSaveGraph(string taskType, string mermaidDiagram, string generatedByModel, int version = 1, string? avatarId = null)
        {
            HolonicBraidManager manager = new HolonicBraidManager(ParseAvatarId(avatarId));
            var result = await manager.SaveGraphAsync(taskType, mermaidDiagram, generatedByModel, version);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_braid_record_outcome"), Description("WEB6 Holonic BRAID: feeds a real solver outcome back into a graph's quality metadata via EMA (updates avg_solver_accuracy).")]
        public static async Task<string> BraidRecordOutcome(string graphHolonId, bool wasAccurate, double emaAlpha = 0.2, string? avatarId = null)
        {
            HolonicBraidManager manager = new HolonicBraidManager(ParseAvatarId(avatarId));
            var result = await manager.RecordSolverOutcomeAsync(Guid.Parse(graphHolonId), wasAccurate, emaAlpha);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_fahrn_register_agent"), Description("WEB6 FAHRN: registers a new reasoning agent with the Fractal Adaptive Holonic Reasoning Network.")]
        public static async Task<string> FahrnRegisterAgent(ReasoningAgentMetadata agent, string? avatarId = null)
        {
            FAHRNManager manager = new FAHRNManager(ParseAvatarId(avatarId));
            var result = await manager.RegisterAgentAsync(agent);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_fahrn_get_agents"), Description("WEB6 FAHRN: lists every reasoning agent currently registered, with live composite scoring metadata.")]
        public static async Task<string> FahrnGetAgents(string? avatarId = null)
        {
            FAHRNManager manager = new FAHRNManager(ParseAvatarId(avatarId));
            var result = await manager.GetRegisteredAgentsAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_fahrn_seed_openserv_agents"), Description("WEB6 FAHRN: seeds FAHRN with one reasoning agent per model in the OpenServ SERV catalog (skips any AgentName already registered), so the network can immediately score/route/braid across every OpenServ-reachable model (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) behind a single SERV_API_KEY. Safe to call repeatedly.")]
        public static async Task<string> FahrnSeedOpenServAgents(string? avatarId = null)
        {
            FAHRNManager manager = new FAHRNManager(ParseAvatarId(avatarId));
            var result = await manager.SeedDefaultOpenServAgentsAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_list_openserv_models"), Description("WEB6: lists every model reachable through the OpenServ provider (provider: \"openserv\") - the full SERV catalog spanning OpenAI, Anthropic, Google, xAI, Qwen and DeepSeek behind a single SERV_API_KEY.")]
        public static string ListOpenServModels()
        {
            return JsonSerializer.Serialize(OpenServCatalog.Models);
        }

        [McpServerTool(Name = "web6_fahrn_dispatch"), Description("WEB6 FAHRN: dispatches a problem to the reasoning network. The controller agent scores eligible agents, picks Serial/Parallel/Decomposed execution, runs loop detection, assembles the final Mermaid plan and updates every involved agent's score via EMA.")]
        public static async Task<string> FahrnDispatch(DispatchRequest request, string? avatarId = null)
        {
            request.AvatarId = request.AvatarId == Guid.Empty ? ParseAvatarId(avatarId) : request.AvatarId;
            FAHRNManager manager = new FAHRNManager(request.AvatarId);
            var result = await manager.DispatchAsync(request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_memory_get_earth_holon"), Description("WEB6 Holonic BRAID memory hierarchy: gets the single planetary Earth holon, creating it if this is the very first call anywhere.")]
        public static async Task<string> MemoryGetEarthHolon(string? avatarId = null)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(ParseAvatarId(avatarId));
            var result = await manager.GetOrCreateEarthHolonAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_memory_get_or_create_holon"), Description("WEB6 Holonic BRAID memory hierarchy: finds or creates a holon at the given level (Session, Agent, User, Group, Neighbourhood, District, City, County, Country, Continent) under the given parent.")]
        public static async Task<string> MemoryGetOrCreateHolon(string level, string name, string parentHolonId, string? avatarId = null)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(ParseAvatarId(avatarId));
            var result = await manager.GetOrCreateHolonAsync(Enum.Parse<NextGenSoftware.OASIS.Web6.Core.Enums.HolonicMemoryLevel>(level, true), name, Guid.Parse(parentHolonId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_memory_set_membrane_rule"), Description("WEB6 Holonic BRAID memory hierarchy: sets the membrane rule governing what a holon is allowed to propagate upward to its parent (per-field, consent-governed - default is private).")]
        public static async Task<string> MemorySetMembraneRule(string holonId, MembraneRule rule, string? avatarId = null)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(ParseAvatarId(avatarId));
            var result = await manager.SetMembraneRuleAsync(Guid.Parse(holonId), rule);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_memory_record"), Description("WEB6 Holonic BRAID memory hierarchy: records a new memory item at the given holon.")]
        public static async Task<string> MemoryRecord(string holonId, HolonicMemoryItem item, string? avatarId = null)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(ParseAvatarId(avatarId));
            var result = await manager.RecordMemoryAsync(Guid.Parse(holonId), item);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_memory_propagate"), Description("WEB6 Holonic BRAID memory hierarchy: propagates whatever the child holon's membrane rule permits up to its parent holon (a single hop).")]
        public static async Task<string> MemoryPropagate(string childHolonId, string? avatarId = null)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(ParseAvatarId(avatarId));
            var result = await manager.PropagateAsync(Guid.Parse(childHolonId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_orchestrator_register"), Description("WEB6: registers an external agent/orchestrator endpoint (MCP server, A2A agent, LangChain/AutoGen/CrewAI/Semantic Kernel deployment, or generic webhook).")]
        public static async Task<string> OrchestratorRegister(OrchestratorAdapterConfig config, string? avatarId = null)
        {
            OrchestratorManager manager = new OrchestratorManager(ParseAvatarId(avatarId));
            var result = await manager.RegisterAdapterAsync(config);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_orchestrator_list"), Description("WEB6: lists every registered orchestrator adapter.")]
        public static async Task<string> OrchestratorList(string? avatarId = null)
        {
            OrchestratorManager manager = new OrchestratorManager(ParseAvatarId(avatarId));
            var result = await manager.GetAdaptersAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web6_orchestrator_invoke"), Description("WEB6: invokes a registered orchestrator adapter with a normalised request, translating to/from its native protocol wire format (MCP, A2A, LangChain, AutoGen, CrewAI, Semantic Kernel or webhook) under the hood.")]
        public static async Task<string> OrchestratorInvoke(OrchestratorInvokeRequest request, string? avatarId = null)
        {
            OrchestratorManager manager = new OrchestratorManager(ParseAvatarId(avatarId));
            var result = await manager.InvokeAsync(request);
            return JsonSerializer.Serialize(result);
        }

        // ── Priority 2: FAHRN hero endpoint ─────────────────────────────────────────

        [McpServerTool(Name = "web6_fahrn_solve"), Description("WEB6 FAHRN hero endpoint: takes a natural-language problem and runs the full pipeline — auto-classify task type, inject avatar context (Web4+Web5), look up Holonic BRAID graph, dispatch to the reasoning network (Serial/Parallel/Decomposed/Debate/Voting), EMA-update agent scores, record session memory — returning the answer, reasoning trace, Mermaid plan, and full telemetry in one call.")]
        public static async Task<string> FahrnSolve(FahrnSolveRequest request, string? avatarId = null)
        {
            request.AvatarId = request.AvatarId == Guid.Empty ? ParseAvatarId(avatarId) : request.AvatarId;
            FahrnSolveManager manager = new FahrnSolveManager(request.AvatarId);
            var result = await manager.SolveAsync(request);
            return JsonSerializer.Serialize(result);
        }

        // ── Priority 7: Embeddings ───────────────────────────────────────────────────

        [McpServerTool(Name = "web6_embed"), Description("WEB6: generates embeddings for one or more texts via the configured provider (OpenAI, Cohere, or HuggingFace). Returns float arrays suitable for semantic search, RAG pipelines, or cosine-similarity comparisons.")]
        public static async Task<string> Embed(EmbeddingRequest request, string? avatarId = null)
        {
            EmbeddingManager manager = new EmbeddingManager(ParseAvatarId(avatarId));
            var result = await manager.EmbedAsync(request);
            return JsonSerializer.Serialize(result);
        }

        // ── Priority 23b: StarnetContextManager ─────────────────────────────────────

        [McpServerTool(Name = "web6_get_avatar_context"), Description("WEB6: assembles and returns a rich context block for an OASIS avatar — karma, karma level, active quests, world memberships — assembled from Web4 and Web5 in parallel. Use this to ground AI prompts in the avatar's real OASIS state.")]
        public static async Task<string> GetAvatarContext(string avatarId)
        {
            Guid id = ParseAvatarId(avatarId);
            StarnetContextManager manager = new StarnetContextManager(id);
            var result = await manager.GetAvatarContextAsync(id);
            return JsonSerializer.Serialize(result);
        }

        // ── Priority 15: External memory providers ──────────────────────────────────

        [McpServerTool(Name = "web6_memory_external_search"), Description("WEB6 External Memory: searches one or more configured external memory providers (Mem0, Zep, Letta, LangMem, Graphiti) for memories relevant to the given query, scoped to the avatar. Returns merged, score-ranked results.")]
        public static async Task<string> MemoryExternalSearch(string query, string? avatarId = null, List<string>? providers = null, int topK = 5)
        {
            Guid id = ParseAvatarId(avatarId);
            var results = await MemoryProviderManager.Instance.SearchAllAsync(id, query, providers, topK);
            return JsonSerializer.Serialize(results);
        }

        [McpServerTool(Name = "web6_memory_external_add"), Description("WEB6 External Memory: adds a memory to the specified external memory provider (Mem0, Zep, Letta, LangMem, Graphiti), scoped to the avatar.")]
        public static async Task<string> MemoryExternalAdd(string provider, string content, string? avatarId = null)
        {
            Guid id = ParseAvatarId(avatarId);
            var p = MemoryProviderManager.Instance.Get(provider);
            if (p == null)
                return JsonSerializer.Serialize(new { error = $"Provider '{provider}' is not registered" });
            await p.AddAsync(id, content);
            return JsonSerializer.Serialize(new { ok = true });
        }

        [McpServerTool(Name = "web6_memory_external_list_providers"), Description("WEB6 External Memory: lists the names of all external memory providers currently registered (auto-detected from environment variables on startup).")]
        public static string MemoryExternalListProviders()
        {
            return JsonSerializer.Serialize(MemoryProviderManager.Instance.ProviderNames);
        }
    }
}
