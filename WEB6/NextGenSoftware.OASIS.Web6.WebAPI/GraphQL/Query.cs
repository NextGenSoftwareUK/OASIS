using HotChocolate;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
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
}
