using System.Net;
using System.Net.Sockets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public sealed class HoloNativeRoutingTests
{
    private static readonly Lazy<Task> InstallIsolatedApp = new(InstallIsolatedAppAsync);

    [Theory, InlineData(false), InlineData(true)]
    public async Task RoutesAcrossOfficialConductorAndSqlite(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var admin = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(admin.IsLoopback);
        var directory = Path.GetFullPath(Required("OASIS_HOLOCHAIN_REPORT_DIR"));
        Assert.Equal(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(directory),
            ignoreCase: OperatingSystem.IsWindows());
        Assert.Equal("oasis-holo-native-results", Path.GetFileName(directory));
        var database = Path.Combine(directory, sync ? "routing-sync.db" : "routing-async.db");
        Assert.False(File.Exists(database));
        using var sqlite = new SqliteProvider($"Data Source={database};Pooling=False");
        var missing = Path.Combine(directory, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        var unavailableAdmin = new UriBuilder(admin) { Host = "127.0.0.1", Port = port }.Uri;
        var holo = OpenProvider(admin);
        var unavailable = OpenProvider(unavailableAdmin);

        try
        {
            var active = await holo.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            active = await sqlite.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            var baseline = sync
                ? holo.LoadAllHolons(HolonType.All, loadChildren: false)
                : await holo.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(baseline.IsError, baseline.Message);
            var baselineIds = baseline.Result.Select(record => record.Id).ToHashSet();
            await NativePriorityRouting.VerifyAsync(holo, sqlite, unavailable, unavailableSqlite, sync);
            Assert.False(File.Exists(missing));
            var records = await sqlite.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(records.IsError, records.Message);
            var fixtures = records.Result.OrderBy(record => record.Name, StringComparer.Ordinal).ToArray();
            var expected = new[] { $"priority routing {holo.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" };
            Assert.Equal(expected.Order(StringComparer.Ordinal), fixtures.Select(record => record.Name));
            var collection = sync
                ? holo.LoadAllHolons(HolonType.All, loadChildren: false)
                : await holo.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(collection.IsError, collection.Message);
            Assert.Equal(baselineIds.Concat(fixtures.Select(record => record.Id)).Order(),
                collection.Result.Select(record => record.Id).Order());
            foreach (var record in fixtures)
            {
                var removed = await holo.DeleteHolonAsync(record.Id);
                Assert.True(!removed.IsError && removed.IsDeleted, removed.Message);
                var absent = await holo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.Null(absent.Result);
                Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            }
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
            if (unavailable.IsProviderActivated) await unavailable.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task LoadsNativeHolonByReturnedActionHash(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var admin = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(admin.IsLoopback);
        var holo = OpenProvider(admin);
        var holon = new Holon { Id = Guid.NewGuid(), Name = "action-hash fixture", CreatedByAvatarId = Guid.NewGuid() };
        try
        {
            var active = await holo.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            var saved = await holo.SaveHolonAsync(holon, saveChildren: false);
            Assert.True(!saved.IsError && saved.IsSaved, saved.Message);
            try
            {
                var key = saved.Result.ProviderUniqueStorageKey[ProviderType.HoloOASIS];
                Assert.False(string.IsNullOrWhiteSpace(key));
                var loaded = sync ? holo.LoadHolon(key, loadChildren: false)
                    : await holo.LoadHolonAsync(key, loadChildren: false);
                Assert.True(!loaded.IsError && loaded.IsLoaded, loaded.Message);
                Assert.Equal(holon.Id, loaded.Result.Id);
                Assert.Equal(holon.Name, loaded.Result.Name);
            }
            finally
            {
                var removed = await holo.DeleteHolonAsync(holon.Id);
                Assert.True(!removed.IsError && removed.IsDeleted, removed.Message);
            }
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
        }
    }

    private static async Task InstallIsolatedAppAsync()
    {
        var endpoint = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(endpoint.IsLoopback);
        var admin = OpenAdmin(endpoint);
        var connected = await admin.ConnectAsync();
        Assert.False(connected.IsError, connected.Message);
        try
        {
            var installed = await admin.InstallAppAsync(HoloOASIS.OASIS_HAPP_ID,
                Required("HOLONET_LIVE_HAPP_PATH"), network_seed: Guid.NewGuid().ToString("N"));
            Assert.False(installed.IsError, installed.Message);
        }
        finally
        {
            await admin.DisconnectAsync();
        }
    }

    private static HoloNETClientAdmin OpenAdmin(Uri endpoint)
        => new(new HoloNETDNA
        {
            HolochainConductorAdminURI = endpoint.AbsoluteUri,
            AutoStartHolochainConductor = false,
            AutoShutdownHolochainConductor = false
        });

    private static HoloOASIS OpenProvider(Uri endpoint)
        => new(OpenAdmin(endpoint), null!, useHoloNetwork: false);
    private static string Required(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing fixture setting {name}.");
}
