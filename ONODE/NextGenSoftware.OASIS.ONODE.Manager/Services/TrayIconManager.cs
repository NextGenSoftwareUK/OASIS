using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NextGenSoftware.OASIS.ONODE.Manager.ViewModels;
using NextGenSoftware.OASIS.ONODE.Manager.Views;

namespace NextGenSoftware.OASIS.ONODE.Manager.Services;

/// <summary>
/// Manages the system tray icon, its state transitions, and the right-click context menu.
/// Icon states: Blue (running), Grey (stopped), Yellow (degraded), Red (crashed).
/// </summary>
public class TrayIconManager
{
    private readonly MainWindowViewModel _vm;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private TrayIcon? _tray;
    private MainWindow? _mainWindow;
    private readonly System.Timers.Timer _iconTimer;

    private static readonly Dictionary<string, string> IconPaths = new()
    {
        ["blue"]   = "avares://NextGenSoftware.OASIS.ONODE.Manager/Assets/Icons/onode-blue.png",
        ["grey"]   = "avares://NextGenSoftware.OASIS.ONODE.Manager/Assets/Icons/onode-grey.png",
        ["yellow"] = "avares://NextGenSoftware.OASIS.ONODE.Manager/Assets/Icons/onode-yellow.png",
        ["red"]    = "avares://NextGenSoftware.OASIS.ONODE.Manager/Assets/Icons/onode-red.png",
    };

    public TrayIconManager(MainWindowViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        _vm = vm;
        _desktop = desktop;
        _iconTimer = new System.Timers.Timer(3000);
        _iconTimer.Elapsed += (_, _) => UpdateIcon();
        _iconTimer.Start();
    }

    public void Initialise()
    {
        _tray = new TrayIcon
        {
            ToolTipText = "ONODE Manager",
            Icon = LoadIcon("grey"),
            Menu = BuildMenu(),
            IsVisible = true,
        };

        _tray.Clicked += (_, _) => ShowMainWindow();
        UpdateIcon();
    }

    NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();

        // Status header (non-clickable)
        menu.Add(new NativeMenuItem("ONODE Manager") { IsEnabled = false });
        menu.Add(new NativeMenuItemSeparator());

        // Global actions
        menu.Add(new NativeMenuItem("▶  Start All")  { Command = _vm.StartAllCommand });
        menu.Add(new NativeMenuItem("■  Stop All")   { Command = _vm.StopAllCommand });
        menu.Add(new NativeMenuItem("↺  Restart All") { Command = _vm.RestartAllCommand });
        menu.Add(new NativeMenuItemSeparator());

        // Per-service entries
        foreach (var svcId in new[] { "web4", "web5", "web6", "web7", "web8", "web9", "web10" })
        {
            var sub = new NativeMenuItem(svcId.ToUpper());
            var subMenu = new NativeMenu();
            subMenu.Add(new NativeMenuItem("▶  Start")   { Command = new RelayCommand(async () => await new NextGenSoftware.OASIS.ONODE.Client.SupervisorClient().StartAsync(svcId)) });
            subMenu.Add(new NativeMenuItem("■  Stop")    { Command = new RelayCommand(async () => await new NextGenSoftware.OASIS.ONODE.Client.SupervisorClient().StopAsync(svcId)) });
            subMenu.Add(new NativeMenuItem("↺  Restart") { Command = new RelayCommand(async () => await new NextGenSoftware.OASIS.ONODE.Client.SupervisorClient().RestartAsync(svcId)) });
            sub.Menu = subMenu;
            menu.Add(sub);
        }

        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Open ONODE Manager…") { Command = new RelayCommand(_ => ShowMainWindow()) });
        menu.Add(new NativeMenuItem("Open OPORTAL…")       { Command = new RelayCommand(_ => OpenOPORTAL()) });
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Quit") { Command = new RelayCommand(_ => Quit()) });

        return menu;
    }

    void UpdateIcon()
    {
        if (_tray == null) return;
        var status = _vm.OverallStatus;
        var iconKey = status switch
        {
            "All Running" => "blue",
            "All Stopped" => "grey",
            "Error"       => "red",
            "Partial"     => "yellow",
            _             => "grey"
        };

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _tray.Icon = LoadIcon(iconKey);
            _tray.ToolTipText = $"ONODE Manager — {status}";
        });
    }

    WindowIcon LoadIcon(string key)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(IconPaths[key]));
            return new WindowIcon(stream);
        }
        catch
        {
            return new WindowIcon(new Bitmap(AssetLoader.Open(new Uri(IconPaths["grey"]))));
        }
    }

    void ShowMainWindow()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_mainWindow == null || !_mainWindow.IsVisible)
            {
                _mainWindow = new MainWindow(_vm);
                _mainWindow.Show();
            }
            else
            {
                _mainWindow.Activate();
            }
        });
    }

    static void OpenOPORTAL()
    {
        var url = "https://oportal.oasisomniverse.one/onode";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }

    void Quit()
    {
        _iconTimer.Dispose();
        _tray?.Dispose();
        _vm.Dispose();
        _desktop.Shutdown();
    }
}

// Minimal ICommand implementation for tray menu items
internal class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? p) => true;
    public void Execute(object? p) => _execute(p);
}
