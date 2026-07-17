using Avalonia;
using NextGenSoftware.OASIS.ONODE.Manager.Services;
using Velopack;

namespace NextGenSoftware.OASIS.ONODE.Manager;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must be called before any other code (handles install/update hooks)
        VelopackApp.Build().Run();

        // Generate icon assets on first launch (SkiaSharp, no external dep)
        IconGenerator.EnsureIconsExist();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
