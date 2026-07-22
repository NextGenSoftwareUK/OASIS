using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

public class HealthMonitor : BackgroundService
{
    private readonly ProcessSupervisor _supervisor;
    private readonly SupervisorConfig _config;
    private readonly ILogger<HealthMonitor> _logger;

    public HealthMonitor(ProcessSupervisor supervisor, IOptions<SupervisorConfig> config, ILogger<HealthMonitor> logger)
    {
        _supervisor = supervisor;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), ct); // give processes time to start first
        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "HealthMonitor error"); }
            await Task.Delay(TimeSpan.FromSeconds(_config.HealthPollIntervalSeconds), ct);
        }
    }

    async Task PollAsync(CancellationToken ct)
    {
        foreach (var (id, state) in _supervisor.States)
        {
            if (state.Status is ServiceStatus.Stopped or ServiceStatus.NotInstalled
                or ServiceStatus.Starting or ServiceStatus.Stopping or ServiceStatus.Degraded)
                continue;

            if (!_config.Services.TryGetValue(id, out var cfg)) continue;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var resp = await http.GetAsync($"http://localhost:{cfg.Port}/api/v1/onode/status", ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("{Service} health check returned {Status}", id, resp.StatusCode);
                    await _supervisor.HandleCrashAsync(id, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{Service} health check failed: {Message}", id, ex.Message);
                await _supervisor.HandleCrashAsync(id, ct);
            }
        }
    }
}
