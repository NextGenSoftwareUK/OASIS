using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SqlServerSqliteHyperDriveTests
{
    [Fact]
    public async Task RoutesReplicatesAndBalancesAcrossRealSqlStores()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_PRIORITY_SQL_DIRECTORY");
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.False(string.IsNullOrWhiteSpace(fixture));
        fixture = Path.GetFullPath(fixture!);
        Assert.Equal(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        Assert.StartsWith("oasis-priority-sql.", Path.GetFileName(fixture));
        var mode = Environment.GetEnvironmentVariable("OASIS_PRIORITY_SQL_MODE");
        Assert.True(mode is "sync" or "async");
        var sync = mode == "sync";
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("OASIS_PRIORITY_SQL_CONNECTION"));
        Assert.Equal("127.0.0.1,11433", connection.DataSource);
        Assert.Equal("oasis_priority_routing", connection.InitialCatalog);

        var sqlitePath = Path.Combine(fixture, "priority.db");
        Assert.False(File.Exists(sqlitePath));
        using var sqlite = new SQLLiteDBOASIS($"Data Source={sqlitePath};Pooling=False");
        var sql = new SQLServerDBOASIS(connection.ConnectionString);
        try
        {
            var sqlActivation = await sql.ActivateProviderAsync();
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.True(!sqlActivation.IsError && sqlActivation.Result, sqlActivation.Message);
            Assert.True(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var providerTypes = new[] { ProviderType.SQLServerDBOASIS, ProviderType.SQLLiteDBOASIS };
            var candidates = providerTypes.Select(type => new EnumValue<ProviderType>(type)).ToArray();
            var replication = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
            replication.RegisterProvider(sql);
            replication.RegisterProvider(sqlite);
            Assert.False(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
            var replicationRouter = new OASISHyperDrive(replication);
            var records = new List<Holon>();
            foreach (var primary in providerTypes)
            {
                var holon = new Holon { Id = Guid.NewGuid(), Name = $"priority routing {primary}", CreatedByAvatarId = Guid.NewGuid() };
                var request = new StorageOperationRequest { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false };
                var saved = sync ? replicationRouter.RouteRequest<IHolon>(request) : await replicationRouter.RouteRequestAsync<IHolon>(request);
                Assert.False(saved.IsError || saved.IsWarning, saved.Message);
                foreach (var provider in new IOASISStorageProvider[] { sql, sqlite })
                {
                    var direct = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                    Assert.False(direct.IsError, direct.Message);
                    AssertRecord(holon, direct.Result);
                }
                records.Add(holon);
            }

            var balancing = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
            balancing.RegisterProvider(sql);
            balancing.RegisterProvider(sqlite);
            Assert.False(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
            var balancedRouter = new OASISHyperDrive(balancing);
            var selected = records[0];
            var served = new List<string>();
            for (var index = 0; index < 6; index++)
            {
                var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected.Id, LoadChildren = false };
                var result = sync ? balancedRouter.RouteRequest<IHolon>(read, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<IHolon>(read, LoadBalancingStrategy.RoundRobin);
                Assert.False(result.IsError, result.Message);
                AssertRecord(selected, result.Result);
                using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
                var initial = route.RootElement.GetProperty("InitialProvider").GetString();
                Assert.Equal(initial, route.RootElement.GetProperty("ServingProvider").GetString());
                Assert.Equal("RoundRobin", route.RootElement.GetProperty("RequestedStrategy").GetString());
                served.Add(initial!);
            }
            foreach (var provider in providerTypes)
                Assert.Equal(3, served.Count(value => value == provider.ToString()));

            using var unavailableEndpoint = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unavailableEndpoint.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            connection.DataSource = $"127.0.0.1,{((IPEndPoint)unavailableEndpoint.LocalEndPoint!).Port}";
            connection.ConnectTimeout = 1;
            connection.ConnectRetryCount = 0;
            var unavailable = new SQLServerDBOASIS(connection.ConnectionString);
            var failover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
            failover.RegisterProvider(unavailable);
            failover.RegisterProvider(sqlite);
            Assert.False(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
            var failoverRouter = new OASISHyperDrive(failover);
            StorageOperationRequest ReadSql() => new() { Operation = "LoadHolon", HolonId = selected.Id,
                LoadChildren = false, PreferredProvider = ProviderType.SQLServerDBOASIS };
            var rejected = sync ? failoverRouter.RouteRequest<IHolon>(ReadSql()) : await failoverRouter.RouteRequestAsync<IHolon>(ReadSql());
            Assert.True(rejected.IsError, rejected.Message);
            failover.IsAutoFailOverEnabled = true;
            var recovered = sync ? failoverRouter.RouteRequest<IHolon>(ReadSql()) : await failoverRouter.RouteRequestAsync<IHolon>(ReadSql());
            Assert.False(recovered.IsError, recovered.Message);
            AssertRecord(selected, recovered.Result);
            using var recoveryRoute = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
            Assert.Equal(nameof(ProviderType.SQLServerDBOASIS), recoveryRoute.RootElement.GetProperty("RequestedProvider").GetString());
            Assert.Equal(nameof(ProviderType.SQLLiteDBOASIS), recoveryRoute.RootElement.GetProperty("ServingProvider").GetString());
        }
        finally
        {
            await sql.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
