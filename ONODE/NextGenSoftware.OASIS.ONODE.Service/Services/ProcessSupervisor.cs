using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

public class ProcessSupervisor
{
    private readonly SupervisorConfig _config;
    private readonly LogAggregator _logs;
    private readonly ILogger<ProcessSupervisor> _logger;
    private readonly ConcurrentDictionary<string, ServiceState> _states = new();
    private readonly ConcurrentDictionary<string, Process?> _processes = new();
    private readonly ConcurrentDictionary<string, (int count, DateTime windowStart)> _crashCounters = new();
    private readonly SemaphoreSlim _opLock = new(1, 1);

    public ProcessSupervisor(IOptions<SupervisorConfig> config, LogAggregator logs, ILogger<ProcessSupervisor> logger)
    {
        _config = config.Value;
        _logs = logs;
        _logger = logger;
        InitialiseStates();
    }

    void InitialiseStates()
    {
        foreach (var (id, cfg) in _config.Services)
        {
            var path = _config.ResolveProjectPath(id);
            var installed = !string.IsNullOrEmpty(path) && Directory.Exists(path);
            _states[id] = new ServiceState
            {
                Id = id,
                Status = installed ? ServiceStatus.Stopped : ServiceStatus.NotInstalled,
                Port = cfg.Port,
                WindowMode = cfg.WindowMode,
                Installed = installed
            };
            _processes[id] = null;
        }
    }

    public IReadOnlyDictionary<string, ServiceState> States => _states;

    public ServiceState? GetState(string id) =>
        _states.TryGetValue(id, out var s) ? s : null;

    public async Task StartAsync(string id, WindowMode? windowOverride = null, CancellationToken ct = default)
    {
        await _opLock.WaitAsync(ct);
        try { await StartInternalAsync(id, windowOverride, ct); }
        finally { _opLock.Release(); }
    }

    public async Task StopAsync(string id, CancellationToken ct = default)
    {
        await _opLock.WaitAsync(ct);
        try { await StopInternalAsync(id, ct); }
        finally { _opLock.Release(); }
    }

    public async Task RestartAsync(string id, WindowMode? windowOverride = null, CancellationToken ct = default)
    {
        await StopAsync(id, ct);
        await Task.Delay(1000, ct);
        await StartAsync(id, windowOverride, ct);
    }

    public async Task StartGroupAsync(string group, WindowMode? windowOverride = null, CancellationToken ct = default)
    {
        var ids = ResolveGroup(group);
        foreach (var id in ids) await StartAsync(id, windowOverride, ct);
    }

    public async Task StopGroupAsync(string group, CancellationToken ct = default)
    {
        var ids = ResolveGroup(group);
        foreach (var id in ids) await StopAsync(id, ct);
    }

    public async Task RestartGroupAsync(string group, WindowMode? windowOverride = null, CancellationToken ct = default)
    {
        var ids = ResolveGroup(group);
        foreach (var id in ids) await RestartAsync(id, windowOverride, ct);
    }

    public IEnumerable<string> ResolveGroup(string group)
    {
        if (_config.Groups.TryGetValue(group.ToLower(), out var csv))
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (_states.ContainsKey(group.ToLower()))
            return [group.ToLower()];
        return [];
    }

    // Called by HealthMonitor when it detects a crashed process
    public async Task HandleCrashAsync(string id, CancellationToken ct = default)
    {
        if (!_states.TryGetValue(id, out var state)) return;
        if (!_config.Services.TryGetValue(id, out var cfg)) return;

        var (count, windowStart) = _crashCounters.GetOrAdd(id, _ => (0, DateTime.UtcNow));

        if ((DateTime.UtcNow - windowStart).TotalMinutes > cfg.RestartWindowMinutes)
        {
            count = 0;
            windowStart = DateTime.UtcNow;
        }

        count++;
        _crashCounters[id] = (count, windowStart);

        if (count > cfg.MaxRestartAttempts)
        {
            state.Status = ServiceStatus.Degraded;
            state.LastError = $"Crashed {count} times in {cfg.RestartWindowMinutes} minutes — auto-restart suspended.";
            _logger.LogError("{Service} is degraded: {Error}", id, state.LastError);
            return;
        }

        var delay = count switch { 1 => 5, 2 => 30, _ => 300 };
        _logger.LogWarning("{Service} crashed (attempt {Count}), restarting in {Delay}s", id, count, delay);
        state.Status = ServiceStatus.Stopped;

        await Task.Delay(TimeSpan.FromSeconds(delay), ct);
        await StartAsync(id, ct: ct);
    }

    async Task StartInternalAsync(string id, WindowMode? windowOverride, CancellationToken ct)
    {
        if (!_states.TryGetValue(id, out var state)) return;
        if (!_config.Services.TryGetValue(id, out var cfg)) return;

        if (state.Status is ServiceStatus.Running or ServiceStatus.Starting)
        {
            _logger.LogInformation("{Service} already running", id);
            return;
        }
        if (!state.Installed)
        {
            _logger.LogWarning("{Service} not installed at expected path", id);
            return;
        }

        state.Status = ServiceStatus.Starting;
        state.LastError = null;

        var projectPath = _config.ResolveProjectPath(id);
        var oasisDnaPath = _config.ResolveOASISDNAPath();
        var mode = windowOverride ?? cfg.WindowMode;
        var hidden = mode == WindowMode.Hidden;

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls \"http://localhost:{cfg.Port}\"",
            WorkingDirectory = projectPath,
            UseShellExecute = !hidden,
            CreateNoWindow = hidden,
            RedirectStandardOutput = hidden,
            RedirectStandardError = hidden,
        };

        if (!hidden)
            psi.WindowStyle = mode == WindowMode.Minimised
                ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;

        psi.Environment["OASISDNA_PATH"] = oasisDnaPath;
        psi.Environment["ASPNETCORE_URLS"] = $"http://localhost:{cfg.Port}";

        // macOS: open a terminal window
        if (!hidden && OperatingSystem.IsMacOS())
        {
            psi = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-a Terminal \"{projectPath}\"",
                UseShellExecute = true
            };
        }

        try
        {
            var process = Process.Start(psi);
            if (process == null)
            {
                state.Status = ServiceStatus.Stopped;
                state.LastError = "Process.Start returned null";
                return;
            }

            _processes[id] = process;
            state.Pid = process.Id;
            state.StartedAt = DateTime.UtcNow;
            state.Status = ServiceStatus.Running;
            state.WindowMode = mode;

            if (hidden)
            {
                _ = PipeOutputAsync(id, process, ct);
            }

            _logger.LogInformation("{Service} started (pid {Pid}, port {Port}, mode {Mode})", id, process.Id, cfg.Port, mode);

            // Step 2 of start sequence: tell Web4 to join ONET after API is ready
            _ = Task.Run(async () => await NotifyStartAsync(id, cfg.Port, ct), ct);
        }
        catch (Exception ex)
        {
            state.Status = ServiceStatus.Stopped;
            state.LastError = ex.Message;
            _logger.LogError(ex, "Failed to start {Service}", id);
        }
    }

    async Task StopInternalAsync(string id, CancellationToken ct)
    {
        if (!_states.TryGetValue(id, out var state)) return;
        if (state.Status is ServiceStatus.Stopped or ServiceStatus.NotInstalled) return;

        state.Status = ServiceStatus.Stopping;

        // Step 1: graceful P2P disconnect via Web4 API
        if (_config.Services.TryGetValue(id, out var cfg))
            await NotifyStopAsync(id, cfg.Port, ct);

        _processes.TryGetValue(id, out var process);
        if (process != null && !process.HasExited)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    process.Kill(entireProcessTree: true);
                else
                    process.Kill();

                await process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
            }
            catch { /* process may have already exited */ }
        }

        _processes[id] = null;
        state.Status = ServiceStatus.Stopped;
        state.Pid = null;
        state.StartedAt = null;
        _logger.LogInformation("{Service} stopped", id);
    }

    async Task PipeOutputAsync(string id, Process process, CancellationToken ct)
    {
        var outTask = ReadStreamAsync(id, process.StandardOutput, false, ct);
        var errTask = ReadStreamAsync(id, process.StandardError, true, ct);
        await Task.WhenAll(outTask, errTask);
    }

    async Task ReadStreamAsync(string id, StreamReader reader, bool isError, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                _logs.Append(id, line, isError);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogDebug(ex, "Log pipe ended for {Service}", id); }
    }

    // Notify the running service to join ONET (Switch 2 start)
    async Task NotifyStartAsync(string id, int port, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        for (var i = 0; i < 12; i++) // wait up to 60s for API to be ready
        {
            try
            {
                var resp = await http.GetAsync($"http://localhost:{port}/api/v1/onode/status", ct);
                if (resp.IsSuccessStatusCode)
                {
                    await http.PostAsync($"http://localhost:{port}/api/v1/onode/start", null, ct);
                    _logger.LogInformation("{Service} ONET P2P network started", id);
                    return;
                }
            }
            catch { }
            await Task.Delay(5000, ct);
        }
        _logger.LogWarning("{Service} API not ready after 60s — ONET start skipped", id);
    }

    // Notify the running service to disconnect from ONET (Switch 2 stop)
    async Task NotifyStopAsync(string id, int port, CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            await http.PostAsync($"http://localhost:{port}/api/v1/onode/stop", null, ct);
            _logger.LogInformation("{Service} ONET P2P network stopped gracefully", id);
            await Task.Delay(1000, ct); // allow peers to be notified
        }
        catch { /* service may already be down */ }
    }

    // Called by HealthMonitor
    public void UpdateStatus(string id, ServiceStatus status, string? error = null)
    {
        if (_states.TryGetValue(id, out var state))
        {
            state.Status = status;
            if (error != null) state.LastError = error;
        }
    }

    public async Task StartEnabledServicesAsync(CancellationToken ct)
    {
        foreach (var (id, cfg) in _config.Services.Where(s => s.Value.Enabled))
            await StartAsync(id, ct: ct);
    }

    public async Task StopAllAsync(CancellationToken ct)
    {
        foreach (var id in _states.Keys.ToList())
            await StopAsync(id, ct);
    }
}
