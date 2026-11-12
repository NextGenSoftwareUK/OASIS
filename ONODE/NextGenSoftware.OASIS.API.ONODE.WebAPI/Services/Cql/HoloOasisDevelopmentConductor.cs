using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using CanonicalHoloOasis = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal sealed class HoloOasisDevelopmentConductor
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

    private TeachingAdmin _admin;
    private FileStream _stateLease;

    public static HoloOasisDevelopmentConductor CreateIfEnabled(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("CQL_HOLOOASIS_LOCAL_DEMO:Enabled"))
            return null;

        if (!OperatingSystem.IsLinux() ||
            !string.Equals(configuration["RAILWAY_ENVIRONMENT_NAME"], "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The local HoloOASIS teaching conductor is only enabled on Linux WEB4 Development.");

        return new HoloOasisDevelopmentConductor();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists("/data"))
            throw new InvalidOperationException("The HoloOASIS teaching conductor requires the WEB4 data volume.");

        Directory.CreateDirectory(StateDirectory);
        _stateLease = IoFile.Open(Path.Combine(StateDirectory, "web4.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        try
        {
            var configPath = Path.Combine(StateDirectory, "conductor-config.yaml");
            await IoFile.WriteAllTextAsync(configPath, ConductorConfiguration, cancellationToken);
            _admin = new TeachingAdmin(new HoloNETDNA
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
            await StopAsync();
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> ProvisionReaderAsync(
        IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (configuration.GetSection("CQL_HOLOOASIS_QUERY").Exists())
            throw new InvalidOperationException("Select one HoloOASIS reader configuration.");

        var catalogueServices = new ServiceCollection();
        CqlPublicReadCatalogue.Enroll(catalogueServices, configuration);
        using var catalogueProvider = catalogueServices.BuildServiceProvider();
        var ids = catalogueProvider.GetServices<CqlPublicReadCatalogue>()
            .SingleOrDefault(catalogue => catalogue.Provider == ProviderType.HoloOASIS)?.CopyRecordIds();
        if (ids is not { Length: 3 })
            throw new InvalidOperationException("The HoloOASIS teaching catalogue requires three reviewed public IDs.");

        cancellationToken.ThrowIfCancellationRequested();
        const string appId = CanonicalHoloOasis.OASIS_HAPP_ID;
        var bundle = Path.Combine(AppContext.BaseDirectory, "OASIS_hAPP", "oasis.happ");
        HoloNETClientAppAgent app = null;
        try
        {
            var installed = await _admin.InstallEnableSignAttachAndConnectToHappAsync(appId, bundle, "oasis",
                CapGrantAccessType.Assigned, uninstallhAppIfAlreadyInstalled: false, log: false);
            app = installed?.HoloNETClientAppAgent as HoloNETClientAppAgent;
            if (installed == null || installed.IsError || !installed.IsSuccess || app == null)
                throw new InvalidOperationException("The verified HoloOASIS teaching app could not be installed or resumed.");
            var writer = new CanonicalHoloOasis(null, app, useHoloNetwork: false);
            var activation = await writer.ActivateProviderAsync();
            if (activation.IsError || !activation.Result)
                throw new InvalidOperationException("The verified HoloOASIS teaching app could not be activated.");

            for (var index = 0; index < ids.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = ids[index];
                var name = $"HoloOASIS teaching holon {index + 1}";
                var existing = await writer.LoadHolonAsync(id, loadChildren: false, recursive: false);
                if (existing.ErrorCode == OASISResultErrorCodes.RecordNotFound && existing.Exception == null &&
                    existing.Result == null)
                {
                    var saved = await writer.SaveHolonAsync(new Holon { Id = id, Name = name, IsPublic = true },
                        saveChildren: false);
                    if (saved.IsError || saved.Result?.Id != id)
                        throw new InvalidOperationException("A synthetic HoloOASIS teaching holon could not be saved.");
                }
                else if (existing.IsError || existing.Result is not { IsPublic: true, IsDeleted: false } row ||
                    row.Id != id || row.Name != name)
                    throw new InvalidOperationException("An existing holon does not match the dedicated teaching fixture.");
            }

            var endpoint = app.EndPoint;
            if (!endpoint.IsLoopback || endpoint.Scheme != "ws")
                throw new InvalidOperationException("The local HoloOASIS teaching app interface must use loopback.");
            var dna = app.ConvertHoloHashToBytes(app.HoloNETDNA.DnaHash);
            var agent = app.ConvertHoloHashToBytes(app.HoloNETDNA.AgentPubKey);
            var credentials = await _admin.GrantReaderAsync(dna, agent);
            var token = await _admin.IssueAppAuthenticationTokenAsync(appId);
            if (token.IsError || token.TokenIssued?.token is not { Length: > 0 } appToken)
                throw new InvalidOperationException("The conductor did not issue a teaching app token.");

            const string prefix = "CQL_HOLOOASIS_QUERY:";
            return new Dictionary<string, string>
            {
                [prefix + "Endpoint"] = endpoint.AbsoluteUri,
                [prefix + "InstalledAppId"] = appId,
                [prefix + "Zome"] = "oasis",
                [prefix + "ProbeId"] = ids[0].ToString("D"),
                [prefix + "DnaHash"] = Convert.ToBase64String(dna),
                [prefix + "AgentKey"] = Convert.ToBase64String(agent),
                [prefix + "AppToken"] = Convert.ToBase64String(appToken),
                [prefix + "CapabilitySecret"] = Convert.ToBase64String(credentials.CapSecret),
                [prefix + "SigningKey"] = Convert.ToBase64String(credentials.SigningKey),
                [prefix + "SigningPrivateKey"] = Convert.ToBase64String(credentials.KeyPair.PrivateKey)
            };
        }
        finally
        {
            if (app != null)
                await app.DisconnectAsync();
        }
    }

    public async Task StopAsync()
    {
        var admin = Interlocked.Exchange(ref _admin, null);
        try
        {
            if (admin != null)
                await admin.DisconnectAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _stateLease, null)?.Dispose();
        }
    }

    private sealed class TeachingAdmin(HoloNETDNA dna) : HoloNETClientAdmin(dna)
    {
        public async Task<SigningCredentials> GrantReaderAsync(byte[] dnaHash, byte[] agentKey)
        {
            var key = $"{ConvertHoloHashToString(agentKey)}:{ConvertHoloHashToString(dnaHash)}";
            var writer = _signingCredentialsForCell[key];
            try
            {
                var grant = await AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(
                    [dnaHash, agentKey], CapGrantAccessType.Assigned, GrantedFunctionsType.Listed,
                    [("oasis", "get_holon_by_id")]);
                if (grant == null || grant.IsError)
                    throw new InvalidOperationException("The conductor did not acknowledge the assigned teaching read grant.");
                return _signingCredentialsForCell[key];
            }
            finally
            {
                _signingCredentialsForCell[key] = writer;
            }
        }
    }
}
