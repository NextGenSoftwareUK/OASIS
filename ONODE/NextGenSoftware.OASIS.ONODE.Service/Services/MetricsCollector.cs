using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

public class MetricsCollector : BackgroundService
{
    private readonly ProcessSupervisor _supervisor;
    private readonly SupervisorConfig _config;
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, ServiceMetrics> _metrics = new();

    public MetricsCollector(ProcessSupervisor supervisor, IOptions<SupervisorConfig> config, ILogger<MetricsCollector> logger)
    {
        _supervisor = supervisor;
        _config = config.Value;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, ServiceMetrics> Current => _metrics;

    public AggregateMetrics Aggregate => new()
    {
        TotalPeers = _metrics.Values.Sum(m => m.PeersConnected),
        TotalBytesReadPerSec = _metrics.Values.Sum(m => m.BytesReadPerSec),
        TotalBytesWrittenPerSec = _metrics.Values.Sum(m => m.BytesWrittenPerSec),
        TotalRequestsPerSec = _metrics.Values.Sum(m => m.RequestsPerSec),
    };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), ct);
        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "MetricsCollector error"); }
            await Task.Delay(TimeSpan.FromSeconds(_config.MetricsPollIntervalSeconds), ct);
        }
    }

    async Task PollAsync(CancellationToken ct)
    {
        foreach (var (id, state) in _supervisor.States)
        {
            if (state.Status != ServiceStatus.Running) continue;
            if (!_config.Services.TryGetValue(id, out var cfg)) continue;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var json = await http.GetStringAsync($"http://localhost:{cfg.Port}/api/v1/onode/metrics", ct);
                var doc = JsonDocument.Parse(json).RootElement;

                _metrics[id] = new ServiceMetrics
                {
                    ServiceId = id,
                    PeersConnected = doc.TryGetProperty("peersConnected", out var p) ? p.GetInt32() : 0,
                    BytesReadPerSec = doc.TryGetProperty("bytesReadPerSec", out var r) ? r.GetInt64() : 0,
                    BytesWrittenPerSec = doc.TryGetProperty("bytesWrittenPerSec", out var w) ? w.GetInt64() : 0,
                    RequestsPerSec = doc.TryGetProperty("requestsPerSec", out var rps) ? rps.GetDouble() : 0,
                    AvgLatencyMs = doc.TryGetProperty("avgLatencyMs", out var lat) ? lat.GetDouble() : 0,
                    CollectedAt = DateTime.UtcNow
                };
            }
            catch { /* service may not expose metrics yet */ }
        }
    }
}
