using HotChocolate;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.Controllers;
using NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GraphQL
{
    /// <summary>Root Query type for the Web6 AI/ML/FAHRN/A2A GraphQL endpoint.</summary>
    public class Query
    {
        private static OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        /// <summary>Returns the list of OpenServ AI models available in the FAHRN catalog.</summary>
        public IEnumerable<AiModel> GetAiModels()
        {
            return OpenServCatalog.Models.Select(m => new AiModel
            {
                Id          = m?.Id ?? m?.ToString() ?? "",
                Name        = m?.Id ?? m?.ToString() ?? "",
                Provider    = "OpenServ",
                Type        = "Completion",
                Status      = "available",
                Description = "OpenServ catalog model available for FAHRN dispatch."
            });
        }

        /// <summary>Returns built-in ML.NET models registered in the Web6 layer.</summary>
        public IEnumerable<AiModel> GetMlModels()
        {
            return new[]
            {
                new AiModel { Id = "task_classifier",    Name = "Task Classifier",    Provider = "ML.NET", Type = "MulticlassClassification", Status = "heuristic_fallback", Description = "Classifies FAHRN task type." },
                new AiModel { Id = "loop_anomaly",       Name = "Loop Anomaly",       Provider = "ML.NET", Type = "AnomalyDetection",         Status = "heuristic_fallback", Description = "Scores FAHRN loop anomaly probability." },
                new AiModel { Id = "sentiment_analysis", Name = "Sentiment Analysis", Provider = "ML.NET", Type = "Classification",           Status = "heuristic_fallback", Description = "Classifies text sentiment." }
            };
        }

        /// <summary>Returns recent telemetry events (AI request/response records).</summary>
        public IEnumerable<TelemetryEventDto> GetTelemetry(
            [GraphQLDescription("Maximum number of events to return (1-500).")] int limit = 50)
        {
            int safeLimit = Math.Max(1, Math.Min(limit, 500));
            return TelemetryController._events
                .ToArray()
                .TakeLast(safeLimit)
                .Select(e => new TelemetryEventDto
                {
                    RequestId        = e.RequestId ?? "",
                    TimestampUtc     = e.Timestamp.ToUniversalTime().ToString("O"),
                    Provider         = e.Provider ?? "",
                    Model            = e.Model ?? "",
                    LatencyMs        = e.LatencyMs,
                    PromptTokens     = e.PromptTokens,
                    CompletionTokens = e.CompletionTokens,
                    EstimatedCostUsd = e.EstimatedCostUSD,
                    FahrnMode        = e.FahrnMode ?? "",
                    BraidGraphReused = e.BraidGraphReused,
                    WinningAgent     = e.WinningAgent ?? "",
                    AvatarId         = e.AvatarId.ToString()
                });
        }

        /// <summary>Returns all FAHRN reasoning agents currently registered in the network.</summary>
        public async Task<IEnumerable<Agent>> GetAgentsAsync()
        {
            var manager = new FAHRNManager(Guid.Empty, DNA);
            var result = await manager.GetRegisteredAgentsAsync();
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<Agent>();

            return result.Result.Select(a => new Agent
            {
                AgentId        = a.Id.ToString(),
                AgentName      = a.AgentName ?? "",
                Provider       = a.Provider.ToString(),
                Model          = a.Model ?? "",
                CompositeScore = a.SpeedScore,
                State          = "registered"
            });
        }

        /// <summary>
        /// Searches external memory providers for entries matching a query string.
        /// Queries all configured providers (Mem0, Zep, Letta, etc.) and returns ranked results.
        /// </summary>
        public async Task<IEnumerable<MemoryEntryDto>> SearchMemoryAsync(
            [GraphQLDescription("Free-text query to match against stored memory entries.")] string query,
            [GraphQLDescription("Avatar GUID used to scope the memory search.")] string avatarId = "",
            [GraphQLDescription("Maximum results to return.")] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<MemoryEntryDto>();

            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            int topK = Math.Max(1, Math.Min(limit, 100));

            var results = await MemoryProviderManager.Instance.SearchAllAsync(aid, query, null, topK);
            if (results == null)
                return Enumerable.Empty<MemoryEntryDto>();

            return results.Select(r => new MemoryEntryDto
            {
                Id      = r.Entry?.Id ?? "",
                Content = r.Entry?.Content ?? "",
                Source  = r.Provider ?? "",
                Score   = r.Entry?.Score ?? 0
            });
        }

        // ── Avatar Context ────────────────────────────────────────────────────

        public async Task<object?> GetAvatarContextAsync(
            [GraphQLDescription("Avatar GUID to build context for.")] string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var aid))
                return null;
            var manager = new StarnetContextManager(aid, DNA);
            var result = await manager.GetAvatarContextAsync(aid, "");
            return result.IsError ? null : result.Result;
        }

        // ── DID ──────────────────────────────────────────────────────────────

        public async Task<object?> ResolveDidAsync(
            [GraphQLDescription("DID to resolve (e.g. did:oasis:...).")] string did)
        {
            var manager = new DidManager(Guid.Empty, DNA);
            var result = await manager.ResolveDid(did);
            return result.IsError ? null : result.Result;
        }

        // ── External Memory Providers ─────────────────────────────────────────

        public IEnumerable<string> GetExternalMemoryProviders()
        {
            return MemoryProviderManager.Instance.ProviderNames ?? Enumerable.Empty<string>();
        }

        // ── FAHRN Budget Estimate ─────────────────────────────────────────────

        public object GetFahrnBudgetEstimate(
            [GraphQLDescription("Task type (general, code, reasoning, writing, etc.).")] string taskType = "general",
            [GraphQLDescription("Execution mode (auto, serial, parallel, decomposed).")] string mode = "auto",
            [GraphQLDescription("Number of agents to consider.")] int agentCount = 1)
        {
            int tokens = taskType switch { "code" => 4000, "reasoning" => 3500, "writing" => 2000, "mathematics" => 3000, _ => 2500 };
            int total = tokens * agentCount;
            return new { taskType, mode, agentCount, estimatedTokens = total, estimatedCostUsd = Math.Round(total / 1000.0 * 0.0075 * agentCount, 5) };
        }

        // ── Holonic Braid ─────────────────────────────────────────────────────

        public async Task<object?> GetHolonicBraidGraphAsync(
            [GraphQLDescription("Task type to look up the reasoning graph for.")] string taskType)
        {
            var manager = new HolonicBraidManager(Guid.Empty);
            var result = await manager.FindGraphForTaskTypeAsync(taskType);
            return result.IsError || result.Result == null ? null : result.Result;
        }

        // ── Holonic Memory ────────────────────────────────────────────────────

        public async Task<object?> GetEarthHolonAsync()
        {
            var manager = new HolonicMemoryManager(Guid.Empty);
            var result = await manager.GetOrCreateEarthHolonAsync();
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<object>> SearchHolonMemoryAsync(
            [GraphQLDescription("Holon GUID to search memory within.")] string holonId,
            [GraphQLDescription("Search query.")] string query,
            [GraphQLDescription("Max results to return.")] int topK = 5,
            [GraphQLDescription("Embedding provider (auto, openai, etc.).")] string provider = "auto")
        {
            if (!Guid.TryParse(holonId, out var hid) || string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<object>();
            var manager = new HolonicMemoryManager(Guid.Empty, DNA);
            var result = await manager.QueryMemoryAsync(hid, query, topK, provider);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        // ── Key Vault ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<string>> GetStoredKeyProvidersAsync(
            [GraphQLDescription("Avatar GUID whose stored providers to list.")] string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var aid))
                return Enumerable.Empty<string>();
            var vault = new KeyVaultManager(aid, DNA);
            var result = await vault.ListStoredProvidersAsync();
            return result.IsError || result.Result == null ? Enumerable.Empty<string>() : result.Result;
        }

        // ── ML.NET ────────────────────────────────────────────────────────────

        public string ClassifyTask([GraphQLDescription("Problem text to classify.")] string text)
        {
            var manager = new MLNetManager(Guid.Empty, DNA);
            return manager.ClassifyTaskType(text);
        }

        public string AnalyseSentiment([GraphQLDescription("Text to analyse.")] string text)
        {
            var manager = new MLNetManager(Guid.Empty, DNA);
            return manager.AnalyseSentiment(text);
        }

        // ── Orchestrators ─────────────────────────────────────────────────────

        public async Task<IEnumerable<object>> GetOrchestratorAdaptersAsync()
        {
            var manager = new OrchestratorManager(Guid.Empty);
            var result = await manager.GetAdaptersAsync();
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        // ── Reasoning Network Skills ──────────────────────────────────────────

        public async Task<object?> GetAgentSkillAsync(
            [GraphQLDescription("Agent GUID.")] string agentId,
            [GraphQLDescription("Skill category (e.g. 'code', 'reasoning').")] string category)
        {
            if (!Guid.TryParse(agentId, out var aid))
                return null;
            var manager = new SkillOptManager(Guid.Empty, DNA);
            var result = await manager.LoadSkillAsync(aid, category);
            return result.IsError ? null : result.Result;
        }

        // ── Telemetry History ─────────────────────────────────────────────────

        public IEnumerable<TelemetryEventDto> GetTelemetryHistory(
            [GraphQLDescription("Max events to return (1-500).")] int limit = 50)
        {
            return GetTelemetry(limit);
        }

        // ── Discovery ─────────────────────────────────────────────────────────

        public object GetMcpDiscovery() => new { protocol = "mcp", version = "2024-11-05", name = "OASIS Web6 AI/ML API", description = "OASIS Web6 MCP-compatible AI endpoint" };
        public object GetA2AAgentCard() => new { name = "OASIS FAHRN Agent", version = "1.0", capabilities = new[] { "a2a-tasks", "fahrn-solve", "embeddings", "completions" } };

        // ── A2A Tasks ─────────────────────────────────────────────────────────

        public object? GetA2ATask([GraphQLDescription("A2A task ID.")] string id)
        {
            return A2AController._tasks.TryGetValue(id, out var task) ? (object)task : null;
        }

        /// <summary>Returns health and latency status for all configured AI providers.</summary>
        public async Task<IEnumerable<ProviderStatusDto>> GetProviderStatusAsync(
            [GraphQLDescription("Set true to force a live re-ping of all providers.")] bool refresh = false)
        {
            var monitor = new NextGenSoftware.OASIS.Web6.Core.Managers.ProviderHealthMonitor(Guid.Empty, DNA);
            var status = await monitor.GetStatusAsync(forceRefresh: refresh);
            return status.Select(s => new ProviderStatusDto
            {
                Provider      = s.Provider ?? "",
                Healthy       = s.Healthy,
                LatencyMs     = s.LatencyMs ?? 0,
                Error         = s.Error ?? "",
                LastCheckedUtc = s.LastCheckedUtc.ToString("O")
            });
        }

        /// <summary>Returns token usage and spend summary for an avatar in the current billing period.</summary>
        public async Task<UsageSummaryDto?> GetUsageSummaryAsync(
            [GraphQLDescription("Avatar GUID to look up usage for.")] string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var aid))
                return null;

            var manager = new NextGenSoftware.OASIS.Web6.Core.Managers.UsageMeteringManager(aid, DNA);
            var result = await manager.GetUsageSummaryAsync();
            if (result.IsError || result.Result == null)
                return null;

            var s = result.Result;
            return new UsageSummaryDto
            {
                AvatarId           = s.AvatarId.ToString(),
                PeriodMonth        = s.PeriodMonth ?? "",
                MonthlySpendUSD    = s.MonthlySpendUSD,
                MonthlyBudgetUSD   = s.MonthlyBudgetUSD,
                RemainingBudgetUSD = s.RemainingBudgetUSD,
                DailyTokensUsed    = s.DailyTokensUsed,
                DailyTokenLimit    = s.DailyTokenLimit,
                RemainingTokensToday = s.RemainingTokensToday
            };
        }
    }

    // ──────────────────────────── DTO types ───────────────────────────────────

    public class TelemetryEventDto
    {
        public string RequestId        { get; set; } = "";
        public string TimestampUtc     { get; set; } = "";
        public string Provider         { get; set; } = "";
        public string Model            { get; set; } = "";
        public double LatencyMs        { get; set; }
        public int    PromptTokens     { get; set; }
        public int    CompletionTokens { get; set; }
        public double EstimatedCostUsd { get; set; }
        public string FahrnMode        { get; set; } = "";
        public bool   BraidGraphReused { get; set; }
        public string WinningAgent     { get; set; } = "";
        public string AvatarId         { get; set; } = "";
    }

    public class MemoryEntryDto
    {
        public string Id      { get; set; } = "";
        public string Content { get; set; } = "";
        public string Source  { get; set; } = "";
        public double Score   { get; set; }
    }

    public class ProviderStatusDto
    {
        public string Provider       { get; set; } = "";
        public bool   Healthy        { get; set; }
        public long   LatencyMs      { get; set; }
        public string Error          { get; set; } = "";
        public string LastCheckedUtc { get; set; } = "";
    }

    public class UsageSummaryDto
    {
        public string AvatarId            { get; set; } = "";
        public string PeriodMonth         { get; set; } = "";
        public double MonthlySpendUSD     { get; set; }
        public double MonthlyBudgetUSD    { get; set; }
        public double RemainingBudgetUSD  { get; set; }
        public int    DailyTokensUsed     { get; set; }
        public int    DailyTokenLimit     { get; set; }
        public int    RemainingTokensToday { get; set; }
    }
}
