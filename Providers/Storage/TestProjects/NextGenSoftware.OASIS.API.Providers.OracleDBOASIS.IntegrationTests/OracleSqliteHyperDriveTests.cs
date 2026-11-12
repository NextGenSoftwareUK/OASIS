using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using OracleProvider = NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.OracleDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class OracleSqliteHyperDriveTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RoutesAcrossNativeOracleAndSqlite(bool sync)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_ORACLE_ROUTING_DIRECTORY");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixture));
        fixture = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-priority-oracle.");
        var connection = new OracleConnectionStringBuilder(Environment.GetEnvironmentVariable("OASIS_ORACLE_ROUTING_CONNECTION"));
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1", connection.DataSource);
        Assert.AreEqual("OASIS_ROUTING", connection.UserID);

        var sqlitePath = Path.Combine(fixture, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        var oracle = new OracleProvider(connection.ConnectionString);
        var attempted = new List<Guid>();
        try
        {
            var oracleActivation = await oracle.ActivateProviderAsync();
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!oracleActivation.IsError && oracleActivation.Result, oracleActivation.Message);
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var providerTypes = new[] { ProviderType.OracleDBOASIS, ProviderType.SQLLiteDBOASIS };
            var candidates = providerTypes.Select(type => new EnumValue<ProviderType>(type)).ToArray();
            var replication = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
            replication.RegisterProvider(oracle);
            replication.RegisterProvider(sqlite);
            Assert.IsFalse(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
            var replicationRouter = new OASISHyperDrive(replication);
            var records = new List<Holon>();
            foreach (var primary in providerTypes)
            {
                var holon = new Holon { Id = Guid.NewGuid(), Name = $"priority routing {primary}", CreatedByAvatarId = Guid.NewGuid() };
                attempted.Add(holon.Id);
                var request = new StorageOperationRequest { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false };
                var saved = sync ? replicationRouter.RouteRequest<IHolon>(request) : await replicationRouter.RouteRequestAsync<IHolon>(request);
                Assert.IsFalse(saved.IsError || saved.IsWarning, saved.Message);
                foreach (var provider in new IOASISStorageProvider[] { oracle, sqlite })
                {
                    var direct = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                    Assert.IsFalse(direct.IsError, direct.Message);
                    AssertRecord(holon, direct.Result);
                }
                records.Add(holon);
            }

            var balancing = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
            balancing.RegisterProvider(oracle);
            balancing.RegisterProvider(sqlite);
            Assert.IsFalse(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
            var balancedRouter = new OASISHyperDrive(balancing);
            var selected = records[0];
            var served = new List<string>();
            for (var index = 0; index < 6; index++)
            {
                var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected.Id, LoadChildren = false };
                var result = sync ? balancedRouter.RouteRequest<IHolon>(read, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<IHolon>(read, LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(result.IsError, result.Message);
                AssertRecord(selected, result.Result);
                using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
                var initial = route.RootElement.GetProperty("InitialProvider").GetString();
                Assert.AreEqual(initial, route.RootElement.GetProperty("ServingProvider").GetString());
                Assert.AreEqual("RoundRobin", route.RootElement.GetProperty("RequestedStrategy").GetString());
                served.Add(initial!);
            }
            foreach (var provider in providerTypes)
                Assert.AreEqual(3, served.Count(value => value == provider.ToString()));

            using var unavailableEndpoint = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unavailableEndpoint.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var closedPort = ((IPEndPoint)unavailableEndpoint.LocalEndPoint!).Port;
            var unavailableConnection = new OracleConnectionStringBuilder(connection.ConnectionString)
            {
                DataSource = $"(DESCRIPTION=(CONNECT_TIMEOUT=1)(RETRY_COUNT=0)(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT={closedPort}))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)))"
            };
            var unavailableOracle = new OracleProvider(unavailableConnection.ConnectionString);
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableOracle, sqlite);

            var unavailablePath = Path.Combine(fixture, "missing.db");
            using var unavailableSqlite = new SqliteProvider($"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableSqlite, oracle);
            Assert.IsFalse(File.Exists(unavailablePath));
        }
        finally
        {
            try
            {
                if (attempted.Count > 0)
                {
                    await using var native = new OracleConnection(connection.ConnectionString);
                    await native.OpenAsync();
                    await using var transaction = native.BeginTransaction();
                    foreach (var id in attempted)
                    {
                        await using var delete = native.CreateCommand();
                        delete.Transaction = transaction;
                        delete.CommandText = "DELETE FROM OASIS_HOLONS WHERE ID = :Id";
                        delete.Parameters.Add("Id", OracleDbType.Raw).Value = id.ToByteArray();
                        await delete.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync();
                    await using var count = native.CreateCommand();
                    count.CommandText = "SELECT COUNT(*) FROM OASIS_HOLONS";
                    Assert.AreEqual(0, Convert.ToInt32(await count.ExecuteScalarAsync()));
                }
            }
            finally
            {
                await oracle.DeActivateProviderAsync();
            }
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyFailoverAsync(OASISDNA dna, EnumValue<ProviderType>[] candidates,
        Holon expected, bool sync, IOASISStorageProvider unavailable, IOASISStorageProvider replica)
    {
        var failover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
        failover.RegisterProvider(unavailable);
        failover.RegisterProvider(replica);
        Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
        var router = new OASISHyperDrive(failover);
        var source = unavailable.ProviderType.Value;
        StorageOperationRequest ReadSource() => new() { Operation = "LoadHolon", HolonId = expected.Id,
            LoadChildren = false, PreferredProvider = source };

        var rejected = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsTrue(rejected.IsError, rejected.Message);
        failover.IsAutoFailOverEnabled = true;
        var recovered = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsFalse(recovered.IsError, recovered.Message);
        AssertRecord(expected, recovered.Result);
        using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
        Assert.AreEqual(source.ToString(), route.RootElement.GetProperty("RequestedProvider").GetString());
        Assert.AreEqual(replica.ProviderType.Value.ToString(), route.RootElement.GetProperty("ServingProvider").GetString());
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
