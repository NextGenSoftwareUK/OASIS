using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.Utilities;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class NearSqliteHyperDriveTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task RoutesAcrossNativeNearAndSqlite(bool sync)
    {
        Assert.AreEqual("true", Required("GITHUB_ACTIONS"));
        Assert.AreEqual("1", Required("OASIS_NEAR_FRESH_ROUTING"));
        var endpoint = Required("NEAROASIS_RPCENDPOINT");
        Assert.AreEqual("http://127.0.0.1:18830", endpoint);
        var chain = Required("NEAROASIS_CHAINID");
        Assert.AreEqual(chain, Required("NEAROASIS_NETWORKID"));
        Assert.IsFalse(chain is "mainnet" or "testnet");
        var contract = Required("NEAROASIS_CONTRACTADDRESS");
        var account = Required("NEAROASIS_ACCOUNTID");
        var key = Required("NEAROASIS_PRIVATEKEY");

        var root = Path.GetFullPath(Required("OASIS_NEAR_LIFECYCLE_REPORT_ROOT"));
        var fixture = Directory.GetParent(root)!;
        Assert.AreEqual(Path.GetFullPath(Required("RUNNER_TEMP")), fixture.Parent!.FullName);
        StringAssert.StartsWith(fixture.Name, "oasis-near.");
        Directory.CreateDirectory(root);
        var sqlitePath = Path.Combine(root, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        var missingPath = Path.Combine(root, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missingPath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        var near = new NEAROASIS(endpoint, chain, chain, contract, account, key);
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        var unavailableNear = new NEAROASIS($"http://127.0.0.1:{port}", chain, chain, contract, account, key);

        try
        {
            var nearActivation = await near.ActivateProviderAsync();
            Assert.IsTrue(!nearActivation.IsError && nearActivation.Result, nearActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            await NativePriorityRouting.VerifyAsync(near, sqlite, unavailableNear, unavailableSqlite, sync);
            Assert.IsFalse(File.Exists(missingPath));
            var records = await near.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {near.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await near.DeleteHolonByIdAsync(record.Id, softDelete: false);
                Assert.IsTrue(!removed.IsError && removed.Result && removed.IsDeleted && removed.DeletedCount == 1,
                    removed.Message);
            }

            var staleId = fixtures[0].Id;
            Assert.AreEqual(staleId, (await sqlite.LoadHolonAsync(staleId, loadChildren: false)).Result?.Id);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            manager.RegisterProvider(near);
            manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(near.ProviderType.Value),
                new EnumValue<ProviderType>(sqlite.ProviderType.Value)
            }).IsError);
            var router = new OASISHyperDrive(manager);
            var request = new StorageOperationRequest
            {
                Operation = "LoadHolon", HolonId = staleId,
                PreferredProvider = near.ProviderType.Value, LoadChildren = false
            };
            var absent = sync ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode, absent.Message);
            Assert.IsNull(absent.Result);
            Assert.AreEqual(0L, manager.PerformanceMonitor.GetMetrics(sqlite.ProviderType.Value)?.TotalRequests ?? 0);
        }
        finally
        {
            await near.DeActivateProviderAsync();
            await unavailableNear.DeActivateProviderAsync();
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
