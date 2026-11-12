using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.Holochain.HoloNET.Client;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal sealed class HoloOasisDevelopmentConductor : IHostedService
{
    private const string StateDirectory = "/data/COSMIC/HoloOasisCql";
    private const string AdminEndpoint = "ws://127.0.0.1:65464";
    private const string ConductorConfiguration = """
        data_root_path: /data/COSMIC/HoloOasisCql
        keystore:
          type: lair_server_in_proc
        admin_interfaces:
          - driver:
              type: websocket
              port: 65464
              danger_bind_addr: 127.0.0.1
              allowed_origins: "*"
        network:
          bootstrap_url: https://dev-test-bootstrap2.holochain.org
          relay_url: https://use1-1.relay.n0.iroh-canary.iroh.link./
        """;

    private HoloNETClientAdmin _admin;
    private FileStream _stateLease;

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("CQL_HOLOOASIS_LOCAL_DEMO:Enabled"))
            return;

        if (!OperatingSystem.IsLinux() ||
            !string.Equals(configuration["RAILWAY_ENVIRONMENT_NAME"], "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The local HoloOASIS teaching conductor is only enabled on Linux WEB4 Development.");

        services.AddHostedService(_ => new HoloOasisDevelopmentConductor());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists("/data"))
            throw new InvalidOperationException("The HoloOASIS teaching conductor requires the WEB4 data volume.");

        Directory.CreateDirectory(StateDirectory);
        _stateLease = File.Open(Path.Combine(StateDirectory, "web4.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        try
        {
            var configPath = Path.Combine(StateDirectory, "conductor-config.yaml");
            await File.WriteAllTextAsync(configPath, ConductorConfiguration, cancellationToken);
            _admin = new HoloNETClientAdmin(new HoloNETDNA
            {
                HolochainConductorMode = HolochainConductorModeEnum.UseSystemGlobal,
                HolochainConductorAdminURI = AdminEndpoint,
                HolochainConductorConfigPath = configPath,
                HolochainConductorVersion = "0.7.1",
                AutoStartHolochainConductor = true,
                AutoShutdownHolochainConductor = true,
                AutoDownloadConductor = true,
                SecondsToWaitForHolochainConductorToStart = 60,
                LogToConsole = false,
                LogToFile = false
            });
            var connection = await _admin.ConnectAsync(AdminEndpoint);
            cancellationToken.ThrowIfCancellationRequested();
            if (connection == null || connection.IsError || !connection.IsConnected)
                throw new InvalidOperationException("The local HoloOASIS teaching conductor did not accept its admin connection.");
        }
        catch
        {
            await StopAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_admin != null)
                await _admin.DisconnectAsync();
        }
        finally
        {
            _stateLease?.Dispose();
        }
    }
}
