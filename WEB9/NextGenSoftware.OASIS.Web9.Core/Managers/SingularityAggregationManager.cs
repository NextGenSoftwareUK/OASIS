using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web9.Core.Managers
{
    /// <summary>
    /// WEB9, made literal: every layer below it (WEB4 OASIS API/ONODE, WEB5 STAR API, WEB6 AI abstraction, WEB7
    /// Symbiotic, WEB8 Inter-Galactic mesh) is polled via real HTTP, and the result is folded into one unified
    /// status report - "the network becomes aware that it IS the universe" implemented honestly as cross-service
    /// health aggregation and live metric collection, not fabricated metaphysics.
    /// </summary>
    public class SingularityAggregationManager
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        /// <summary>The base URL for each layer this aggregator probes, resolved from environment variables with sensible defaults.</summary>
        public Dictionary<string, string> LayerBaseUrls { get; set; } = new Dictionary<string, string>
        {
            ["WEB4"] = Environment.GetEnvironmentVariable("WEB4_API_BASE_URL") ?? "https://api.web4.oasisomniverse.one",
            ["WEB5"] = Environment.GetEnvironmentVariable("WEB5_API_BASE_URL") ?? "https://api.starnet.oasisomniverse.one",
            ["WEB6"] = Environment.GetEnvironmentVariable("WEB6_API_BASE_URL") ?? "https://api.web6.oasisomniverse.one",
            ["WEB7"] = Environment.GetEnvironmentVariable("WEB7_API_BASE_URL") ?? "https://api.web7.oasisomniverse.one",
            ["WEB8"] = Environment.GetEnvironmentVariable("WEB8_API_BASE_URL") ?? "https://api.web8.oasisomniverse.one"
        };

        /// <summary>Probes every registered layer in parallel and folds the results into one unified report.</summary>
        public async Task<UnifiedStatusReport> GetUnifiedStatusAsync()
        {
            IEnumerable<Task<LayerStatus>> probes = LayerBaseUrls.Select(kv => ProbeLayerAsync(kv.Key, kv.Value));
            LayerStatus[] results = await Task.WhenAll(probes);

            UnifiedStatusReport report = new UnifiedStatusReport
            {
                Layers = results.OrderBy(r => r.LayerName).ToList(),
                TotalLayerCount = results.Length,
                HealthyLayerCount = results.Count(r => r.IsReachable)
            };

            report.AllLayersHealthy = report.HealthyLayerCount == report.TotalLayerCount;
            return report;
        }

        private async Task<LayerStatus> ProbeLayerAsync(string layerName, string baseUrl)
        {
            LayerStatus status = new LayerStatus { LayerName = layerName, BaseUrl = baseUrl };
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/swagger/v1/swagger.json");
                sw.Stop();
                status.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
                status.IsReachable = response.IsSuccessStatusCode;

                if (!response.IsSuccessStatusCode)
                    status.Error = $"HTTP {(int)response.StatusCode}";
                else
                    await CollectLayerMetricsAsync(layerName, baseUrl, status);
            }
            catch (Exception ex)
            {
                sw.Stop();
                status.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
                status.IsReachable = false;
                status.Error = ex.Message;
            }

            return status;
        }

        /// <summary>Pulls a few cheap, real metrics from layers that expose a public listing endpoint - skipped silently if unavailable.</summary>
        private async Task CollectLayerMetricsAsync(string layerName, string baseUrl, LayerStatus status)
        {
            try
            {
                switch (layerName)
                {
                    case "WEB6":
                    {
                        string body = await _httpClient.GetStringAsync($"{baseUrl.TrimEnd('/')}/v1/reasoning-network/agents");
                        using JsonDocument doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("result", out JsonElement result) && result.ValueKind == JsonValueKind.Array)
                            status.Metrics["registeredReasoningAgents"] = result.GetArrayLength().ToString();
                        break;
                    }
                    case "WEB8":
                    {
                        string body = await _httpClient.GetStringAsync($"{baseUrl.TrimEnd('/')}/v1/mesh/nodes");
                        using JsonDocument doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("result", out JsonElement result) && result.ValueKind == JsonValueKind.Array)
                            status.Metrics["registeredMeshNodes"] = result.GetArrayLength().ToString();
                        break;
                    }
                }
            }
            catch
            {
                // Metric collection is best-effort - a layer being reachable but not exposing this particular metric is not an error.
            }
        }
    }
}
