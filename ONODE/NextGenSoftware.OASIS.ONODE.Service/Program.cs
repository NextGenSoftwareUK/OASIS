using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;
using NextGenSoftware.OASIS.ONODE.Service.Services;

namespace NextGenSoftware.OASIS.ONODE.Service;

public class Program
{
    public static DateTime StartedAt { get; } = DateTime.UtcNow;

    public static async Task Main(string[] args)
    {
        // Handle install/uninstall commands when run as a CLI tool
        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "install":   Platform.ServiceInstaller.Install();   return;
                case "uninstall": Platform.ServiceInstaller.Uninstall(); return;
            }
        }

        var builder = WebApplication.CreateBuilder(args);

        // Support Windows Service, macOS LaunchAgent, Linux systemd all from one binary
        builder.Host
            .UseWindowsService(o => o.ServiceName = "ONODEService")
            .UseSystemd();

        // Loopback-only binding — never expose to external network
        builder.WebHost.ConfigureKestrel(k =>
        {
            var port = builder.Configuration.GetValue<int>("Supervisor:Port", 8765);
            k.ListenLocalhost(port, o => o.Protocols = HttpProtocols.Http1AndHttp2);
        });

        // Config
        builder.Services.Configure<SupervisorConfig>(builder.Configuration.GetSection("Supervisor"));

        // Core services
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSingleton<LogAggregator>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<SupervisorConfig>>().Value;
            return new LogAggregator(cfg.LogRingBufferLines);
        });
        builder.Services.AddSingleton<ProcessSupervisor>();
        builder.Services.AddSingleton<ProviderService>();
        builder.Services.AddSingleton<CommandExecutor>();
        builder.Services.AddSingleton<MetricsCollector>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<MetricsCollector>());
        builder.Services.AddHostedService<HealthMonitor>();
        builder.Services.AddHostedService<HolonBridge>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add IOptions<> for inline use
        builder.Services.AddOptions();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();

        // Initialise auth token
        var tokenService = app.Services.GetRequiredService<TokenService>();
        var config = app.Services.GetRequiredService<IOptions<SupervisorConfig>>().Value;
        var tokenPath = string.IsNullOrEmpty(config.TokenPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "supervisor-token")
            : config.TokenPath;
        tokenService.Initialise(tokenPath);

        // Ensure NodeId is set
        if (string.IsNullOrEmpty(config.NodeId))
        {
            var nodeIdPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "node-id");
            Directory.CreateDirectory(Path.GetDirectoryName(nodeIdPath)!);
            if (File.Exists(nodeIdPath))
                config.NodeId = File.ReadAllText(nodeIdPath).Trim();
            else
            {
                config.NodeId = Guid.NewGuid().ToString();
                File.WriteAllText(nodeIdPath, config.NodeId);
            }
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
        logger.LogInformation("ONODEService starting — NodeId: {NodeId}", config.NodeId);
        logger.LogInformation("Supervisor API: http://localhost:{Port}", config.Port);
        logger.LogInformation("Auth token path: {Path}", tokenPath);
        logger.LogInformation("OASISDNA: {Path}", config.ResolveOASISDNAPath());

        // Start enabled services on boot
        var supervisor = app.Services.GetRequiredService<ProcessSupervisor>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2)); // let Kestrel bind first
            await supervisor.StartEnabledServicesAsync(CancellationToken.None);
        });

        await app.RunAsync();

        // Graceful shutdown — stop all services cleanly
        logger.LogInformation("ONODEService shutting down — stopping all services...");
        await supervisor.StopAllAsync(CancellationToken.None);
    }
}

