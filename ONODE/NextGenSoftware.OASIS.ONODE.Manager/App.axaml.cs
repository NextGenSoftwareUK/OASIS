using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NextGenSoftware.OASIS.ONODE.Manager.Services;
using NextGenSoftware.OASIS.ONODE.Manager.ViewModels;
using NextGenSoftware.OASIS.ONODE.Manager.Views;

namespace NextGenSoftware.OASIS.ONODE.Manager;

public class App : Application
{
    private TrayIconManager? _tray;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Don't show main window on startup — tray only
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            var vm = new MainWindowViewModel();
            _tray = new TrayIconManager(vm, desktop);
            _tray.Initialise();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
