using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextGenSoftware.OASIS.ONODE.Client;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace NextGenSoftware.OASIS.ONODE.Manager.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly SupervisorClient _client = new();
    private readonly System.Timers.Timer _pollTimer;

    [ObservableProperty] private string _nodeId = "–";
    [ObservableProperty] private string _version = "–";
    [ObservableProperty] private string _overallStatus = "Checking...";
    [ObservableProperty] private string _overallStatusColour = "Gray";
    [ObservableProperty] private bool _supervisorAvailable;
    [ObservableProperty] private string _aggregatePeers = "0";
    [ObservableProperty] private string _aggregateBytesIn = "0 B/s";
    [ObservableProperty] private string _aggregateBytesOut = "0 B/s";
    [ObservableProperty] private string _configJson = "";
    [ObservableProperty] private string _unifiedLogs = "";

    public ObservableCollection<ServiceViewModel> Services { get; } = [];
    public ObservableCollection<ProviderViewModel> Providers { get; } = [];
    public ObservableCollection<NetworkServiceViewModel> NetworkServices { get; } = [];
    public ObservableCollection<AuditEntryViewModel> AuditEntries { get; } = [];

    // ── Notification event ────────────────────────────────────────────────────
    public event Action<string, string>? NotificationRequested; // (title, message)

    // ── Metrics chart data ─────────────────────────────────────────────────────
    private const int HistoryLength = 60;
    private readonly ObservableCollection<double> _peersHistory;
    private readonly ObservableCollection<double> _bytesInHistory;
    private readonly ObservableCollection<double> _bytesOutHistory;
    private readonly ObservableCollection<double> _requestsHistory;

    public ISeries[] PeersSeries { get; }
    public ISeries[] BandwidthSeries { get; }
    public ISeries[] RequestsSeries { get; }

    public Axis[] TimeXAxis { get; } = [new Axis { Labels = null, MinStep = 1, Labeler = _ => "" }];
    public Axis[] PeersYAxis { get; } = [new Axis { Name = "Peers", MinLimit = 0, Foreground = new SolidColorPaint(SKColors.LightGray) }];
    public Axis[] BytesYAxis { get; } = [new Axis { Name = "KB/s",  MinLimit = 0, Foreground = new SolidColorPaint(SKColors.LightGray), Labeler = v => $"{v / 1000:F0}" }];
    public Axis[] ReqYAxis  { get; } = [new Axis { Name = "req/s", MinLimit = 0, Foreground = new SolidColorPaint(SKColors.LightGray) }];

    public MainWindowViewModel()
    {
        // Initialise rolling chart history
        _peersHistory   = new(Enumerable.Repeat(0.0, HistoryLength));
        _bytesInHistory  = new(Enumerable.Repeat(0.0, HistoryLength));
        _bytesOutHistory = new(Enumerable.Repeat(0.0, HistoryLength));
        _requestsHistory = new(Enumerable.Repeat(0.0, HistoryLength));

        PeersSeries = [new LineSeries<double> {
            Values = _peersHistory, Name = "Peers", Fill = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2), GeometrySize = 0 }];
        BandwidthSeries = [
            new LineSeries<double> { Values = _bytesInHistory,  Name = "In",  Fill = null, Stroke = new SolidColorPaint(SKColors.LimeGreen, 2),  GeometrySize = 0 },
            new LineSeries<double> { Values = _bytesOutHistory, Name = "Out", Fill = null, Stroke = new SolidColorPaint(SKColors.OrangeRed, 2), GeometrySize = 0 }];
        RequestsSeries = [new LineSeries<double> {
            Values = _requestsHistory, Name = "Req/s", Fill = null,
            Stroke = new SolidColorPaint(SKColors.Violet, 2), GeometrySize = 0 }];

        SupervisorAvailable = _client.IsAvailable;
        if (!_client.IsAvailable)
        {
            OverallStatus = "ONODEService not running";
            OverallStatusColour = "Gray";
        }

        _pollTimer = new System.Timers.Timer(3000);
        _pollTimer.Elapsed += async (_, _) => await PollAsync();
        _pollTimer.Start();

        _ = PollAsync();
    }

    async Task PollAsync()
    {
        if (!_client.IsAvailable)
        {
            SupervisorAvailable = false;
            OverallStatus = "ONODEService not running";
            return;
        }

        SupervisorAvailable = true;

        var status = await _client.GetStatusAsync();
        if (status == null) return;

        NodeId = status.NodeId;
        Version = status.Version;
        AggregatePeers = status.Metrics.TotalPeers.ToString();
        AggregateBytesIn = FormatBytes(status.Metrics.TotalBytesReadPerSec) + "/s";
        AggregateBytesOut = FormatBytes(status.Metrics.TotalBytesWrittenPerSec) + "/s";

        // Update service list
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Services.Clear();
            foreach (var svc in status.Services)
                Services.Add(new ServiceViewModel(svc, _client));

            OverallStatus = status.Services.All(s => s.Status == "Running")  ? "All Running"  :
                            status.Services.All(s => s.Status == "Stopped")  ? "All Stopped"  :
                            status.Services.Any(s => s.Status == "Crashed" || s.Status == "Degraded") ? "Error" :
                            "Partial";

            OverallStatusColour = OverallStatus switch
            {
                "All Running"  => "#00BFFF",  // OASIS neon blue
                "All Stopped"  => "#808080",  // grey
                "Error"        => "#FF4444",  // red
                _              => "#FFA500",  // amber (partial)
            };
        });

        // Metrics history update (rolling 60-point)
        void Roll(ObservableCollection<double> col, double value)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (col.Count >= HistoryLength) col.RemoveAt(0);
                col.Add(value);
            });
        }
        Roll(_peersHistory,   status.Metrics.TotalPeers);
        Roll(_bytesInHistory,  status.Metrics.TotalBytesReadPerSec);
        Roll(_bytesOutHistory, status.Metrics.TotalBytesWrittenPerSec);
        Roll(_requestsHistory, status.Metrics.TotalRequestsPerSec);

        // Network (per-service metrics)
        var svcMetrics = await _client.GetServiceMetricsAsync();
        if (svcMetrics != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var existing = NetworkServices.ToDictionary(n => n.ServiceId);
                var incoming = svcMetrics.ToDictionary(m => m.ServiceId);
                foreach (var m in svcMetrics)
                {
                    if (existing.TryGetValue(m.ServiceId, out var vm))
                        vm.Update(m);
                    else
                        NetworkServices.Add(new NetworkServiceViewModel(m));
                }
                foreach (var old in existing.Keys.Except(incoming.Keys).ToList())
                    NetworkServices.Remove(existing[old]);
            });
        }

        // Notifications: detect newly crashed services
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            foreach (var svc in Services)
            {
                if (svc.Status == "Crashed")
                    NotificationRequested?.Invoke("Service Crashed", $"{svc.Id} has crashed.");
            }
        });

        // Providers
        var providers = await _client.GetProvidersAsync();
        if (providers != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Merge: update existing, add new, remove stale
                var existing = Providers.ToDictionary(p => p.ProviderType);
                var incoming = providers.ToDictionary(p => p.ProviderType);
                foreach (var p in providers)
                {
                    if (existing.TryGetValue(p.ProviderType, out var vm))
                        vm.Update(p);
                    else
                        Providers.Add(new ProviderViewModel(p, _client));
                }
                foreach (var old in existing.Keys.Except(incoming.Keys).ToList())
                    Providers.Remove(existing[old]);
            });
        }

        // Logs (unified)
        var logs = await _client.GetLogsAsync(lines: 300);
        if (logs != null)
        {
            UnifiedLogs = string.Join("\n", logs
                .TakeLast(300)
                .Select(e => $"[{e.Timestamp:HH:mm:ss}] [{e.ServiceId.ToUpper(),-5}] {e.Message}"));
        }
    }

    [RelayCommand]
    async Task StartAll() { if (_client.IsAvailable) await _client.StartGroupAsync("all"); }

    [RelayCommand]
    async Task StopAll() { if (_client.IsAvailable) await _client.StopGroupAsync("all"); }

    [RelayCommand]
    async Task RestartAll() { if (_client.IsAvailable) await _client.RestartGroupAsync("all"); }

    [RelayCommand]
    async Task LoadAudit()
    {
        if (!_client.IsAvailable) return;
        // Fetch audit log from Web4 API (supervisor proxies it)
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var port = 5000; // Web4 API default
            var resp = await http.GetAsync($"http://localhost:{port}/api/v1/onode/audit?limit=200");
            if (!resp.IsSuccessStatusCode) return;
            var json = await resp.Content.ReadAsStringAsync();
            var entries = System.Text.Json.JsonSerializer.Deserialize<List<AuditEntryRaw>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (entries == null) return;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AuditEntries.Clear();
                foreach (var e in entries)
                    AuditEntries.Add(new AuditEntryViewModel(e));
            });
        }
        catch { /* audit log is best-effort */ }
    }

    [RelayCommand]
    async Task LoadConfig()
    {
        if (!_client.IsAvailable) return;
        ConfigJson = await _client.GetConfigAsync() ?? "";
    }

    [RelayCommand]
    async Task SaveConfig()
    {
        if (!_client.IsAvailable || string.IsNullOrWhiteSpace(ConfigJson)) return;
        await _client.UpdateConfigAsync(ConfigJson);
    }

    static string FormatBytes(long b) =>
        b >= 1_000_000 ? $"{b / 1_000_000.0:F1} MB" :
        b >= 1_000     ? $"{b / 1_000.0:F1} KB" : $"{b} B";

    public void Dispose()
    {
        _pollTimer.Dispose();
        _client.Dispose();
    }
}

public partial class ServiceViewModel : ObservableObject
{
    private readonly SupervisorClient _client;

    public string Id { get; }
    [ObservableProperty] private string _status;
    [ObservableProperty] private string _statusColour;
    [ObservableProperty] private string _uptime;
    [ObservableProperty] private int _port;
    [ObservableProperty] private string _windowMode;
    [ObservableProperty] private bool _installed;
    [ObservableProperty] private int? _pid;

    public ServiceViewModel(ServiceStateDto state, SupervisorClient client)
    {
        _client = client;
        Id = state.Id.ToUpper();
        _status = state.Status;
        _port = state.Port;
        _windowMode = state.WindowMode;
        _installed = state.Installed;
        _pid = state.Pid;
        _uptime = FormatUptime(state.UptimeSeconds);
        _statusColour = StatusColour(state.Status);
    }

    [RelayCommand] async Task Start()   => await _client.StartAsync(Id.ToLower());
    [RelayCommand] async Task Stop()    => await _client.StopAsync(Id.ToLower());
    [RelayCommand] async Task Restart() => await _client.RestartAsync(Id.ToLower());

    [RelayCommand]
    async Task ToggleWindowMode()
    {
        var next = WindowMode == "Hidden" ? "Visible" : "Hidden";
        await _client.StartAsync(Id.ToLower(), next);
    }

    static string StatusColour(string status) => status switch
    {
        "Running"   => "#00BFFF",
        "Stopped"   => "#808080",
        "Starting"  => "#87CEEB",
        "Stopping"  => "#87CEEB",
        "Crashed"   => "#FF4444",
        "Degraded"  => "#FFA500",
        _ => "#808080"
    };

    static string FormatUptime(double sec)
    {
        var ts = TimeSpan.FromSeconds(sec);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{ts.Seconds}s";
    }
}

public partial class NetworkServiceViewModel : ObservableObject
{
    public string ServiceId { get; }
    [ObservableProperty] private int _peers;
    [ObservableProperty] private string _bytesIn  = "0 B/s";
    [ObservableProperty] private string _bytesOut = "0 B/s";
    [ObservableProperty] private string _requests = "0";
    [ObservableProperty] private string _latency  = "—";

    public NetworkServiceViewModel(ServiceMetricsDto m)
    {
        ServiceId = m.ServiceId.ToUpper();
        Update(m);
    }

    public void Update(ServiceMetricsDto m)
    {
        Peers    = m.PeersConnected;
        BytesIn  = FormatBytes(m.BytesReadPerSec) + "/s";
        BytesOut = FormatBytes(m.BytesWrittenPerSec) + "/s";
        Requests = $"{m.RequestsPerSec:F1}";
        Latency  = $"{m.AvgLatencyMs:F0} ms";
    }

    static string FormatBytes(long b) =>
        b >= 1_000_000 ? $"{b / 1_000_000.0:F1} MB" :
        b >= 1_000     ? $"{b / 1_000.0:F1} KB" : $"{b} B";
}

public class AuditEntryRaw
{
    public string NodeId    { get; set; } = "";
    public string AvatarId  { get; set; } = "";
    public string Action    { get; set; } = "";
    public string? Detail   { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditEntryViewModel
{
    public string Time     { get; }
    public string NodeId   { get; }
    public string AvatarId { get; }
    public string Action   { get; }
    public string Detail   { get; }

    public AuditEntryViewModel(AuditEntryRaw e)
    {
        Time     = e.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        NodeId   = e.NodeId.Length > 12 ? e.NodeId[..8] + "…" : e.NodeId;
        AvatarId = e.AvatarId.Length > 12 ? e.AvatarId[..8] + "…" : e.AvatarId;
        Action   = e.Action;
        Detail   = e.Detail ?? "";
    }
}

public partial class ProviderViewModel : ObservableObject
{
    private readonly SupervisorClient _client;

    public string ProviderType { get; }
    public string FriendlyName { get; }
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private int _priority;
    [ObservableProperty] private string _statusText;
    [ObservableProperty] private string _dotColour;

    public ProviderViewModel(ProviderDto dto, SupervisorClient client)
    {
        _client = client;
        ProviderType = dto.ProviderType;
        FriendlyName = dto.FriendlyName;
        _isEnabled = dto.IsEnabled;
        _priority = dto.Priority;
        _statusText = dto.IsEnabled ? "Enabled" : "Disabled";
        _dotColour = dto.IsEnabled ? "#00BFFF" : "#555566";
    }

    public void Update(ProviderDto dto)
    {
        IsEnabled = dto.IsEnabled;
        Priority = dto.Priority;
        StatusText = dto.IsEnabled ? "Enabled" : "Disabled";
        DotColour = dto.IsEnabled ? "#00BFFF" : "#555566";
    }

    [RelayCommand]
    async Task Toggle()
    {
        ProviderActionResult? result;
        if (IsEnabled)
            result = await _client.DisableProviderAsync(ProviderType);
        else
            result = await _client.EnableProviderAsync(ProviderType);

        if (result != null)
        {
            IsEnabled = !IsEnabled;
            StatusText = IsEnabled ? "Enabled" : "Disabled";
            DotColour = IsEnabled ? "#00BFFF" : "#555566";
        }
    }

    [RelayCommand]
    async Task IncreasePriority()
    {
        var result = await _client.SetProviderPriorityAsync(ProviderType, Priority - 1);
        if (result != null && Priority > 1) Priority--;
    }

    [RelayCommand]
    async Task DecreasePriority()
    {
        var result = await _client.SetProviderPriorityAsync(ProviderType, Priority + 1);
        if (result != null) Priority++;
    }
}
