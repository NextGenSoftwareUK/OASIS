using System.Net.Http.Json;
using System.Text.Json;

namespace NextGenSoftware.OASIS.ONODE.Client;

/// <summary>
/// Typed HTTP client for the ONODEService supervisor API (127.0.0.1:8765).
/// Used by both STAR CLI and ONODE Manager.
/// Reads the auth token from ~/.oasis/supervisor-token automatically.
/// </summary>
public class SupervisorClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _available;
    public bool IsAvailable => _available;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SupervisorClient(int port = 8765)
    {
        var token = ReadToken();
        _http = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _available = Ping().GetAwaiter().GetResult();
    }

    static string ReadToken()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "supervisor-token");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
    }

    async Task<bool> Ping()
    {
        try
        {
            var resp = await _http.GetAsync("supervisor/status");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Status & info ──────────────────────────────────────────────────────────

    public async Task<SupervisorStatusDto?> GetStatusAsync(CancellationToken ct = default)
        => await GetAsync<SupervisorStatusDto>("supervisor/status", ct);

    public async Task<List<ServiceStateDto>?> GetServicesAsync(CancellationToken ct = default)
        => await GetAsync<List<ServiceStateDto>>("supervisor/services", ct);

    public async Task<ServiceStateDto?> GetServiceAsync(string id, CancellationToken ct = default)
        => await GetAsync<ServiceStateDto>($"supervisor/services/{id}", ct);

    public async Task<string?> GetConfigAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("supervisor/config", ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
    }

    public async Task<MetricsDto?> GetMetricsAsync(CancellationToken ct = default)
        => await GetAsync<MetricsDto>("supervisor/metrics", ct);

    public async Task<List<LogEntryDto>?> GetLogsAsync(string? serviceId = null, int lines = 200, CancellationToken ct = default)
    {
        var url = serviceId == null
            ? $"supervisor/logs?lines={lines}"
            : $"supervisor/logs/{serviceId}?lines={lines}";
        return await GetAsync<List<LogEntryDto>>(url, ct);
    }

    // ── Start / Stop / Restart ─────────────────────────────────────────────────

    public Task StartAsync(string id, string? windowMode = null, CancellationToken ct = default)
    {
        var qs = windowMode != null ? $"?windowMode={windowMode}" : "";
        return PostAsync($"supervisor/start/{id}{qs}", ct);
    }

    public Task StopAsync(string id, CancellationToken ct = default)
        => PostAsync($"supervisor/stop/{id}", ct);

    public Task RestartAsync(string id, string? windowMode = null, CancellationToken ct = default)
    {
        var qs = windowMode != null ? $"?windowMode={windowMode}" : "";
        return PostAsync($"supervisor/restart/{id}{qs}", ct);
    }

    public Task StartGroupAsync(string group, string? windowMode = null, CancellationToken ct = default)
    {
        var qs = windowMode != null ? $"?windowMode={windowMode}" : "";
        return PostAsync($"supervisor/start-group/{group}{qs}", ct);
    }

    public Task StopGroupAsync(string group, CancellationToken ct = default)
        => PostAsync($"supervisor/stop-group/{group}", ct);

    public Task RestartGroupAsync(string group, string? windowMode = null, CancellationToken ct = default)
    {
        var qs = windowMode != null ? $"?windowMode={windowMode}" : "";
        return PostAsync($"supervisor/restart-group/{group}{qs}", ct);
    }

    public Task StartManyAsync(IEnumerable<string> ids, string? windowMode = null, CancellationToken ct = default)
        => PostJsonAsync("supervisor/start", new { ids = ids.ToList(), windowMode }, ct);

    public Task StopManyAsync(IEnumerable<string> ids, CancellationToken ct = default)
        => PostJsonAsync("supervisor/stop", new { ids = ids.ToList() }, ct);

    public async Task UpdateConfigAsync(string json, CancellationToken ct = default)
    {
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await _http.PutAsync("supervisor/config", content, ct);
    }

    // ── Metrics (per-service) ──────────────────────────────────────────────────────

    public async Task<List<ServiceMetricsDto>?> GetServiceMetricsAsync(CancellationToken ct = default)
    {
        var result = await GetAsync<MetricsDto>("supervisor/metrics", ct);
        if (result?.Services == null) return null;
        return result.Services.Values.ToList();
    }

    // ── Providers ──────────────────────────────────────────────────────────────────

    public async Task<List<ProviderDto>?> GetProvidersAsync(CancellationToken ct = default)
        => await GetAsync<List<ProviderDto>>("supervisor/providers", ct);

    public async Task<List<string>?> GetProviderTypesAsync(CancellationToken ct = default)
        => await GetAsync<List<string>>("supervisor/providers/types", ct);

    public async Task<ProviderActionResult?> EnableProviderAsync(string providerType, CancellationToken ct = default)
        => await PutAsync<ProviderActionResult>($"supervisor/providers/{Uri.EscapeDataString(providerType)}/enable", ct);

    public async Task<ProviderActionResult?> DisableProviderAsync(string providerType, CancellationToken ct = default)
        => await PutAsync<ProviderActionResult>($"supervisor/providers/{Uri.EscapeDataString(providerType)}/disable", ct);

    public async Task<ProviderActionResult?> SetProviderPriorityAsync(string providerType, int priority, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { priority }),
                System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync($"supervisor/providers/{Uri.EscapeDataString(providerType)}/priority", content, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<ProviderActionResult>(JsonOpts, ct);
        }
        catch { return null; }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch { return default; }
    }

    async Task PostAsync(string url, CancellationToken ct)
    {
        try { await _http.PostAsync(url, null, ct); }
        catch { }
    }

    async Task PostJsonAsync(string url, object body, CancellationToken ct)
    {
        try { await _http.PostAsJsonAsync(url, body, ct); }
        catch { }
    }

    async Task<T?> PutAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var resp = await _http.PutAsync(url, null, ct);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch { return default; }
    }

    public void Dispose() => _http.Dispose();
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class SupervisorStatusDto
{
    public string NodeId { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public List<ServiceStateDto> Services { get; set; } = [];
    public AggregateMetricsDto Metrics { get; set; } = new();
}

public class ServiceStateDto
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public int? Pid { get; set; }
    public int Port { get; set; }
    public string WindowMode { get; set; } = "";
    public DateTime? StartedAt { get; set; }
    public int RestartCount { get; set; }
    public string? LastError { get; set; }
    public bool Installed { get; set; }
    public double UptimeSeconds { get; set; }
}

public class MetricsDto
{
    public AggregateMetricsDto Aggregate { get; set; } = new();
    public Dictionary<string, ServiceMetricsDto> Services { get; set; } = [];
}

public class AggregateMetricsDto
{
    public int TotalPeers { get; set; }
    public long TotalBytesReadPerSec { get; set; }
    public long TotalBytesWrittenPerSec { get; set; }
    public double TotalRequestsPerSec { get; set; }
}

public class ServiceMetricsDto
{
    public string ServiceId { get; set; } = "";
    public int PeersConnected { get; set; }
    public long BytesReadPerSec { get; set; }
    public long BytesWrittenPerSec { get; set; }
    public double RequestsPerSec { get; set; }
    public double AvgLatencyMs { get; set; }
}

public class LogEntryDto
{
    public string ServiceId { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ProviderDto
{
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }

    public string FriendlyName => ProviderType
        .Replace("OASIS", "")
        .TrimEnd();
}

public class ProviderActionResult
{
    public string Message { get; set; } = "";
    public bool ReloadRequired { get; set; }
}
