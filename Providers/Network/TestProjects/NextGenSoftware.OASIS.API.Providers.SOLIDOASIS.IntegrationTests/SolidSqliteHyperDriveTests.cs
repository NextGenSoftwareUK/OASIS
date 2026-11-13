using System.Net;
using System.Net.Sockets;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using CollectionAssert = Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;


public sealed class SolidSqliteHyperDriveTests
{
    [Theory, InlineData(false), InlineData(true)]
    public async Task RoutesAcrossNativeSolidAndSqlite(bool sync)
    {
        Assert.AreEqual("true", Required("GITHUB_ACTIONS"));
        var root = Path.GetFullPath(Required("OASIS_SOLID_ROUTING_DIRECTORY"));
        Assert.AreEqual(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(root));
        Assert.IsTrue(Path.GetFileName(root).StartsWith("oasis-solid-routing.", StringComparison.Ordinal));
        using var solid = SolidProviderFactory.Create(new SolidOASISProviderSettings
        {
            PodRoot = "http://localhost:13000/oasis-routing/",
            AuthenticationMode = "DpopClientCredentials",
            TokenEndpoint = "http://localhost:13000/.oidc/token",
            ClientId = Required("OASIS_SOLID_ROUTING_CLIENT_ID"),
            ClientSecretEnvironmentVariable = "OASIS_SOLID_ROUTING_CLIENT_SECRET",
            StorageKeyEnvironmentVariable = "OASIS_SOLID_ROUTING_STORAGE_KEY"
        });
        var sqlitePath = Path.Combine(root, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        var missingPath = Path.Combine(root, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missingPath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        using var unavailableSolid = SolidProviderFactory.Create(new SolidOASISProviderSettings
        {
            PodRoot = $"http://localhost:{port}/oasis-routing/",
            AuthenticationMode = "Anonymous",
            StorageKeyEnvironmentVariable = "OASIS_SOLID_ROUTING_STORAGE_KEY"
        });

        try
        {
            var solidActivation = await solid.ActivateProviderAsync();
            Assert.IsTrue(!solidActivation.IsError && solidActivation.Result, solidActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);


            await NativePriorityRouting.VerifyAsync(solid, sqlite, unavailableSolid, unavailableSqlite, sync);
            Assert.IsFalse(File.Exists(missingPath));
            var records = await solid.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {solid.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await solid.DeleteHolonAsync(record.Id);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.Result?.Id == record.Id,
                    removed.Message);
            }
            foreach (var record in fixtures)
            {
                var absent = await solid.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.IsTrue(absent.IsError && absent.Result == null);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
                Assert.IsNull(absent.Exception);
                Assert.AreEqual(record.Id, (await sqlite.LoadHolonAsync(record.Id, loadChildren: false)).Result?.Id);
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                    { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
                dna.OASIS.SubscriptionConfig.PlanType = "Gold";
                var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
                manager.RegisterProvider(solid);
                manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]
                {
                    new EnumValue<ProviderType>(solid.ProviderType.Value),
                    new EnumValue<ProviderType>(sqlite.ProviderType.Value)
                }).IsError);
                var router = new OASISHyperDrive(manager);
                var request = new StorageOperationRequest
                {
                    Operation = "LoadHolon", HolonId = record.Id,
                    PreferredProvider = solid.ProviderType.Value, LoadChildren = false
                };
                var routed = sync ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, routed.ErrorCode, routed.Message);
                Assert.IsNull(routed.Result);
                Assert.AreEqual(0L, manager.PerformanceMonitor.GetMetrics(sqlite.ProviderType.Value)?.TotalRequests ?? 0);
            }
        }
        finally
        {
            await solid.DeActivateProviderAsync();
            await unavailableSolid.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Missing fixture setting {name}.");
        return value!;
    }
}
