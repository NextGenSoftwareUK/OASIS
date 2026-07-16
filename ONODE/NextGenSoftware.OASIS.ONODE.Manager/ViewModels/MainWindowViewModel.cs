using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextGenSoftware.OASIS.ONODE.Client;

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

    public MainWindowViewModel()
    {
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
