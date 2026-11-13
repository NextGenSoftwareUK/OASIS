using System.Net;
using System.Net.Sockets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public sealed class HoloNativeRoutingTests
{
    [Theory, InlineData(false), InlineData(true)]
    public async Task RoutesAcrossOfficialConductorAndSqlite(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
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
            var keyReadFailures = new List<string>();
            foreach (var record in fixtures)
            {
                var byId = await holo.LoadHolonAsync(record.Id, loadChildren: false);
                if (byId.Result?.ProviderUniqueStorageKey?.TryGetValue(ProviderType.HoloOASIS, out var key) == true &&
                    !string.IsNullOrWhiteSpace(key))
                {
                    var byKey = sync ? holo.LoadHolon(key, loadChildren: false)
                        : await holo.LoadHolonAsync(key, loadChildren: false);
                    if (byKey.IsError || byKey.Result?.Id != record.Id)
                        keyReadFailures.Add($"{record.Id}: {byKey.ErrorCode} {byKey.Message}");
                }
                else
                    keyReadFailures.Add($"{record.Id}: no HoloOASIS provider key returned");

                var removed = await holo.DeleteHolonAsync(record.Id);
                Assert.True(!removed.IsError && removed.IsDeleted, removed.Message);
                var absent = await holo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.Null(absent.Result);
                Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            }
            Assert.True(keyReadFailures.Count == 0, string.Join(Environment.NewLine, keyReadFailures));
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
            if (unavailable.IsProviderActivated) await unavailable.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static HoloOASIS OpenProvider(Uri endpoint)
        => new(new HoloNETClientAdmin(new HoloNETDNA
        {
            HolochainConductorAdminURI = endpoint.AbsoluteUri,
            AutoStartHolochainConductor = false,
            AutoShutdownHolochainConductor = false
        }), null!, useHoloNetwork: false);
    private static string Required(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing fixture setting {name}.");
}
