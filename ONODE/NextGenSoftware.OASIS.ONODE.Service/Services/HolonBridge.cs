using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

/// <summary>
/// Pushes AvatarNodeStateHolon to the OASIS Data Layer via the Web4 API so OPORTAL can
/// read live node state remotely. Also polls for CommandHolons (remote commands from OPORTAL).
/// The Web4 API at /api/v1/onode is used as the transport — this avoids a direct OASIS SDK
/// dependency in the supervisor and keeps it decoupled.
/// </summary>
public class HolonBridge : BackgroundService
{
    private readonly ProcessSupervisor _supervisor;
    private readonly MetricsCollector _metrics;
    private readonly ProviderService _providers;
    private readonly SupervisorConfig _config;
    private readonly CommandExecutor _executor;
    private readonly ILogger<HolonBridge> _logger;

    // Web4 base URL — primary node for Holon persistence
    private const string Web4Base = "http://localhost:5000";

    public HolonBridge(
        ProcessSupervisor supervisor,
        MetricsCollector metrics,
        ProviderService providers,
        IOptions<SupervisorConfig> config,
        CommandExecutor executor,
        ILogger<HolonBridge> logger)
    {
        _supervisor = supervisor;
        _metrics = metrics;
        _providers = providers;
        _config = config.Value;
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Push state and poll commands in parallel loops
        await Task.WhenAll(
            PushLoopAsync(ct),
            CommandPollLoopAsync(ct));
    }

    async Task PushLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PushStateAsync(ct); }
            catch (Exception ex) { _logger.LogDebug(ex, "HolonBridge push error"); }
            await Task.Delay(TimeSpan.FromSeconds(_config.HolonPushIntervalSeconds), ct);
        }
    }

    async Task CommandPollLoopAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct); // wait for Web4 to be ready
        while (!ct.IsCancellationRequested)
        {
            try { await PollCommandsAsync(ct); }
            catch (Exception ex) { _logger.LogDebug(ex, "HolonBridge command poll error"); }
            await Task.Delay(TimeSpan.FromSeconds(_config.CommandPollIntervalSeconds), ct);
        }
    }

    async Task PushStateAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_config.AvatarId)) return;

        var agg = _metrics.Aggregate;
        var providerList = _providers.GetProviders();
        var holon = new AvatarNodeStateHolon
        {
            AvatarId = _config.AvatarId,
            NodeId = _config.NodeId,
            LastSeen = DateTime.UtcNow,
            Services = _supervisor.States.Values.Select(s => new HolonServiceState
            {
                Id = s.Id,
                Status = s.Status.ToString(),
                UptimeSeconds = s.UptimeSeconds,
                Port = s.Port,
                WindowMode = s.WindowMode.ToString(),
                Pid = s.Pid,
                Providers = providerList.Select(p => new HolonProviderState
                {
                    ProviderType = p.ProviderType,
                    IsEnabled    = p.IsEnabled,
                    Priority     = p.Priority
                }).ToList()
            }).ToList(),
            Metrics = new HolonMetrics
            {
                PeersConnected = agg.TotalPeers,
                BytesReadPerSec = agg.TotalBytesReadPerSec,
                BytesWrittenPerSec = agg.TotalBytesWrittenPerSec,
                RequestsPerSec = agg.TotalRequestsPerSec,
            }
        };

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var json = JsonSerializer.Serialize(holon);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await http.PutAsync($"{Web4Base}/api/v1/onode/node-state/{_config.NodeId}", content, ct);
    }

    async Task PollCommandsAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_config.NodeId)) return;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var resp = await http.GetAsync($"{Web4Base}/api/v1/onode/commands/pending/{_config.NodeId}", ct);
        if (!resp.IsSuccessStatusCode) return;

        var json = await resp.Content.ReadAsStringAsync(ct);
        var commands = JsonSerializer.Deserialize<List<CommandHolon>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (commands == null) return;

        foreach (var cmd in commands)
        {
            // Mark as executing
            cmd.Status = CommandStatus.Executing;
            await UpdateCommandAsync(http, cmd, ct);

            try
            {
                cmd.Result = await _executor.ExecuteAsync(cmd, ct);
                cmd.Status = CommandStatus.Done;
            }
            catch (Exception ex)
            {
                cmd.Status = CommandStatus.Error;
                cmd.Result = ex.Message;
            }

            cmd.CompletedAt = DateTime.UtcNow;
            await UpdateCommandAsync(http, cmd, ct);
        }
    }

    async Task UpdateCommandAsync(HttpClient http, CommandHolon cmd, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(cmd);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await http.PatchAsync($"{Web4Base}/api/v1/onode/commands/{cmd.Id}", content, ct);
        }
        catch { /* best effort */ }
    }
}
