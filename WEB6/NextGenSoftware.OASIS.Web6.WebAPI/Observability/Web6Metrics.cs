using Prometheus;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Observability
{
    /// <summary>
    /// Prometheus metrics for the WEB6 AI layer.
    /// Scraped at GET /metrics by Prometheus, Grafana Agent, Datadog Agent, etc.
    /// </summary>
    public static class Web6Metrics
    {
        // ── Request counters ──────────────────────────────────────────────────────────

        /// <summary>Total completion requests, labelled by provider, model, and whether served from cache.</summary>
        public static readonly Counter CompletionRequests = Metrics.CreateCounter(
            "web6_completion_requests_total",
            "Total AI completion requests processed.",
            new CounterConfiguration { LabelNames = new[] { "provider", "model", "cached" } });

        /// <summary>Total FAHRN dispatch invocations, labelled by mode.</summary>
        public static readonly Counter FahrnDispatches = Metrics.CreateCounter(
            "web6_fahrn_dispatches_total",
            "Total FAHRN reasoning-network dispatches.",
            new CounterConfiguration { LabelNames = new[] { "mode" } });

        /// <summary>Total Holonic BRAID graph reuses (existing graph found for task type).</summary>
        public static readonly Counter BraidGraphReuses = Metrics.CreateCounter(
            "web6_braid_graph_reuses_total",
            "Total Holonic BRAID reasoning graph reuses.");

        /// <summary>Total semantic cache hits (zero provider cost).</summary>
        public static readonly Counter CacheHits = Metrics.CreateCounter(
            "web6_cache_hits_total",
            "Total semantic cache hits — requests served without calling a provider.");

        // ── Token usage histograms ────────────────────────────────────────────────────

        public static readonly Histogram PromptTokens = Metrics.CreateHistogram(
            "web6_prompt_tokens",
            "Prompt token counts per completion request.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "provider", "model" },
                Buckets = new double[] { 100, 250, 500, 1000, 2000, 4000, 8000, 16000, 32000 }
            });

        public static readonly Histogram CompletionTokens = Metrics.CreateHistogram(
            "web6_completion_tokens",
            "Completion token counts per completion request.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "provider", "model" },
                Buckets = new double[] { 50, 100, 250, 500, 1000, 2000, 4000 }
            });

        // ── Latency ───────────────────────────────────────────────────────────────────

        public static readonly Histogram RequestLatencyMs = Metrics.CreateHistogram(
            "web6_request_latency_milliseconds",
            "End-to-end latency of AI completion requests in milliseconds.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "provider", "model" },
                Buckets = new double[] { 100, 250, 500, 1000, 2000, 5000, 10000, 30000 }
            });

        // ── Cost ──────────────────────────────────────────────────────────────────────

        public static readonly Histogram EstimatedCostUSD = Metrics.CreateHistogram(
            "web6_estimated_cost_usd",
            "Estimated USD cost per completion request.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "provider", "model" },
                Buckets = new double[] { 0.0001, 0.0005, 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1.0 }
            });

        // ── Errors ────────────────────────────────────────────────────────────────────

        public static readonly Counter Errors = Metrics.CreateCounter(
            "web6_errors_total",
            "Total completion request errors.",
            new CounterConfiguration { LabelNames = new[] { "provider" } });
    }
}
