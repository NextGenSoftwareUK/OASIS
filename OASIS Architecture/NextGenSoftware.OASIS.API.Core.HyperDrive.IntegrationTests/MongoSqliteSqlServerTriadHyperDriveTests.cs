using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

/// <summary>
/// End-to-end HyperDrive V2 run across three real, independent backends (MongoDB, SQLite, SQL Server):
/// automatic replication to every replica, round-robin load balancing, automatic read and write failover
/// with each provider taken down in turn, and outbox convergence of a replica that was down during the write.
/// Requires owned loopback-only backends; refuses anything else (never the shared Atlas cluster).
/// </summary>
public sealed class MongoSqliteSqlServerTriadHyperDriveTests
{
    [Fact]
    public async Task FailsOverReplicatesAndBalancesAcrossThreeRealProviders()
    {
        var mongoUri = Environment.GetEnvironmentVariable("OASIS_TRIAD_MONGODB_URI");
        var sqlServer = Environment.GetEnvironmentVariable("OASIS_TRIAD_SQLSERVER");
        Assert.False(string.IsNullOrWhiteSpace(mongoUri), "OASIS_TRIAD_MONGODB_URI (owned loopback MongoDB) is required.");
        Assert.False(string.IsNullOrWhiteSpace(sqlServer), "OASIS_TRIAD_SQLSERVER (owned local SQL Server data source) is required.");
        Assert.StartsWith("mongodb://127.0.0.1:", mongoUri);
        Assert.True(sqlServer!.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase) || sqlServer.StartsWith("127.0.0.1"),
            "Only an owned local SQL Server is accepted.");

        // Mongo receives replicas inside multi-document transactions, which require a replica set.
        Assert.Contains("replicaSet=", mongoUri);
        await EnsureSingleNodeReplicaSetAsync(mongoUri!);

        var run = Guid.NewGuid().ToString("N")[..12];
        var mongoDatabase = "oasis_triad_" + run;
        var sqlDatabase = "oasis_triad_" + run;
        var master = new SqlConnectionStringBuilder { DataSource = sqlServer, InitialCatalog = "master",
            IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }.ConnectionString;
        await ExecuteSqlAsync(master, $"CREATE DATABASE [{sqlDatabase}];");
        var sqlConnection = new SqlConnectionStringBuilder(master) { InitialCatalog = sqlDatabase }.ConnectionString;
        var directory = Directory.CreateTempSubdirectory("oasis-triad-");

        var mongo = new MongoDBOASIS(mongoUri, mongoDatabase);
        var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "triad.db")};Pooling=False");
        var sql = new SQLServerDBOASIS(sqlConnection);
        var providers = new IOASISStorageProvider[] { mongo, sqlite, sql };
        try
        {
            foreach (var provider in providers)
            {
                var activated = await provider.ActivateProviderAsync();
                Assert.True(!activated.IsError && activated.Result, $"{provider.ProviderType.Value}: {activated.Message}");
            }

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var types = providers.Select(provider => provider.ProviderType.Value).ToArray();
            var candidates = types.Select(type => new EnumValue<ProviderType>(type)).ToArray();

            // 1. Automatic replication: a write routed to any primary lands on all three backends.
            var replication = Manager(dna, providers);
            replication.IsAutoReplicationEnabled = true;
            Assert.False(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
            var replicationRouter = new OASISHyperDrive(replication);
            var records = new List<Holon>();
            foreach (var primary in types)
            {
                var holon = NewHolon($"triad replicated via {primary}");
                var saved = await replicationRouter.RouteRequestAsync<IHolon>(new StorageOperationRequest
                    { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false });
                Assert.False(saved.IsError || saved.IsWarning, $"{primary}: {saved.Message}");
                foreach (var provider in providers)
                    AssertRecord(holon, (await provider.LoadHolonAsync(holon.Id, loadChildren: false)).Result);
                records.Add(holon);
            }

            // 2. Load balancing: round-robin reads spread evenly over the three providers.
            var balancing = Manager(dna, providers);
            balancing.IsAutoLoadBalanceEnabled = true;
            Assert.False(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
            var balancedRouter = new OASISHyperDrive(balancing);
            var served = new List<string>();
            for (var index = 0; index < 9; index++)
            {
                var read = await balancedRouter.RouteRequestAsync<IHolon>(new StorageOperationRequest
                    { Operation = "LoadHolon", HolonId = records[0].Id, LoadChildren = false }, LoadBalancingStrategy.RoundRobin);
                Assert.False(read.IsError, read.Message);
                AssertRecord(records[0], read.Result);
                served.Add(Route(read, "ServingProvider"));
            }
            foreach (var type in types)
                Assert.Equal(3, served.Count(value => value == type.ToString()));

            // 3. Failover: take each provider down in turn; reads and writes requested from it keep working.
            using var deadPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            deadPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var port = ((IPEndPoint)deadPort.LocalEndPoint!).Port;
            var downMongo = new MongoDBOASIS($"mongodb://127.0.0.1:{port}/?serverSelectionTimeoutMS=1000&connectTimeoutMS=1000", mongoDatabase);
            var downSqlite = new SQLLiteDBOASIS(
                $"Data Source={Path.Combine(directory.FullName, "missing", "down.db")};Mode=ReadWrite;Pooling=False;Default Timeout=1");
            var downSql = new SQLServerDBOASIS(new SqlConnectionStringBuilder(sqlConnection)
                { DataSource = $"127.0.0.1,{port}", ConnectTimeout = 1, ConnectRetryCount = 0 }.ConnectionString);
            foreach (var (down, healthy) in new (IOASISStorageProvider, IOASISStorageProvider[])[]
                     { (downMongo, new IOASISStorageProvider[] { sqlite, sql }),
                       (downSqlite, new IOASISStorageProvider[] { mongo, sql }),
                       (downSql, new IOASISStorageProvider[] { mongo, sqlite }) })
            {
                var failover = Manager(dna, healthy.Prepend(down).ToArray());
                failover.IsAutoFailOverEnabled = true;
                Assert.False(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
                var router = new OASISHyperDrive(failover);
                var source = down.ProviderType.Value;

                var read = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                    { Operation = "LoadHolon", HolonId = records[1].Id, LoadChildren = false, PreferredProvider = source });
                Assert.False(read.IsError, $"{source} down, read: {read.Message}");
                AssertRecord(records[1], read.Result);
                Assert.Equal(source.ToString(), Route(read, "RequestedProvider"));
                Assert.Contains(Route(read, "ServingProvider"), healthy.Select(p => p.ProviderType.Value.ToString()));

                var written = NewHolon($"triad written while {source} down");
                var write = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                    { Operation = "SaveHolon", PreferredProvider = source, Payload = written, SaveChildren = false });
                Assert.False(write.IsError, $"{source} down, write: {write.Message}");
                var servingWriter = Route(write, "ServingProvider");
                var writer = healthy.Single(p => p.ProviderType.Value.ToString() == servingWriter);
                AssertRecord(written, (await writer.LoadHolonAsync(written.Id, loadChildren: false)).Result);
            }

            // 4. Convergence: SQLite commits with an outbox enrolled for SQL Server and MongoDB while MongoDB is down.
            //    SQL Server converges immediately; MongoDB's delivery is retained and converges once it is back.
            var sourceIdentity = (await sqlite.CaptureStorageIdentityAsync()).Result;
            var sqlIdentity = await sql.CaptureStorageIdentityAsync();
            var mongoIdentity = await mongo.CaptureStorageIdentityAsync();
            Assert.False(sqlIdentity.IsError, sqlIdentity.Message);
            Assert.False(mongoIdentity.IsError, mongoIdentity.Message);
            var actor = Guid.NewGuid();
            var grant = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
            var toSql = new HolonReplicationRouteBinding(sourceIdentity, sqlIdentity.Result, grant.BindingIdentity);
            var toMongo = new HolonReplicationRouteBinding(sourceIdentity, mongoIdentity.Result, grant.BindingIdentity);
            var enrollment = new HolonReplicationEnrollment(toSql.SourceId, new[] { toSql.TargetId, toMongo.TargetId },
                sourceIdentity, new[] { toSql, toMongo });
            var converging = NewHolon("triad outbox convergence");
            var intent = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor,
                ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(converging.Id, null) }),
                ConditionalHolonOperation.Save, new[] { converging }, saveChildren: false);
            var committed = await sqlite.ExecuteReplicatingHolonWriteAsync(intent, enrollment);
            Assert.True(!committed.IsError && committed.IsSaved, committed.Message);
            var versionId = committed.Result.Single().VersionId;
            var outbox = sqlite.GetBoundHolonReplicationOutbox(sourceIdentity);
            Assert.False(outbox.IsError, outbox.Message);

            CommittedHolonReplicaTarget Target(HolonReplicationRouteBinding binding, ICommittedHolonReplicaStorageProvider receiver,
                CosmicStorageIdentity identity, string name) => new(binding.TargetId, receiver, identity.Authority,
                binding.SourceId, sourceIdentity.Authority, grant, name, Guid.NewGuid(), grant.BindingIdentity, identity);

            var sqlDelivery = await new HolonOutboxDispatcher(outbox.Result,
                new[] { Target(toSql, sql, sqlIdentity.Result, "triad-sql") }, "triad-sql-worker")
                .DispatchOnceAsync(toSql.TargetId, maximumCount: 1);
            Assert.False(sqlDelivery.IsError, sqlDelivery.Message);
            Assert.True(sqlDelivery.Result.CompletedCount == 1,
                $"claimed={sqlDelivery.Result.ClaimedCount} deferred={sqlDelivery.Result.DeferredCount} reconcile={sqlDelivery.Result.ReconciliationCount} {sqlDelivery.Message} {string.Join(" | ", sqlDelivery.InnerMessages)}");
            Assert.Equal(versionId, (await sql.LoadHolonAsync(converging.Id, loadChildren: false)).Result?.VersionId);

            var outage = await new HolonOutboxDispatcher(outbox.Result,
                new[] { Target(toMongo, downMongo, mongoIdentity.Result, "triad-mongo") }, "triad-mongo-outage")
                .DispatchOnceAsync(toMongo.TargetId, maximumCount: 1, leaseDuration: TimeSpan.FromSeconds(1));
            Assert.Equal(0, outage.Result?.CompletedCount ?? 0);
            Assert.Null((await mongo.LoadHolonAsync(converging.Id, loadChildren: false)).Result);

            OASISResult<HolonOutboxDispatchResult> recovered = null!;
            for (var attempt = 0; attempt < 20 && (recovered == null || recovered.Result?.CompletedCount != 1); attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                recovered = await new HolonOutboxDispatcher(outbox.Result,
                    new[] { Target(toMongo, mongo, mongoIdentity.Result, "triad-mongo") }, "triad-mongo-recovered")
                    .DispatchOnceAsync(toMongo.TargetId, maximumCount: 1);
                Assert.False(recovered.IsError, recovered.Message);
            }
            var heads = await outbox.Result.ReadRouteHeadsAsync(pageSize: 8);
            var inspected = new List<string>();
            foreach (var head in heads.Result?.Heads ?? Enumerable.Empty<HolonDeliveryInspection>())
                inspected.Add(JsonSerializer.Serialize(head) + " now=" + DateTime.UtcNow.ToString("O"));
            Assert.True(recovered.Result.CompletedCount == 1,
                $"heads: {string.Join(" || ", inspected)}; outage: {outage.Message} {string.Join(" | ", outage.InnerMessages)}; recovered: claimed={recovered.Result.ClaimedCount} deferred={recovered.Result.DeferredCount} reconcile={recovered.Result.ReconciliationCount} {recovered.Message} {string.Join(" | ", recovered.InnerMessages)}");
            foreach (var provider in providers)
                Assert.Equal(versionId, (await provider.LoadHolonAsync(converging.Id, loadChildren: false)).Result?.VersionId);
        }
        finally
        {
            foreach (var provider in providers)
                await provider.DeActivateProviderAsync();
            sqlite.Dispose();
            SqlConnection.ClearAllPools();
            await new MongoClient(mongoUri).DropDatabaseAsync(mongoDatabase);
            await ExecuteSqlAsync(master,
                $"ALTER DATABASE [{sqlDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{sqlDatabase}];");
            directory.Delete(recursive: true);
        }
    }

    private static async Task EnsureSingleNodeReplicaSetAsync(string uri)
    {
        var url = MongoUrl.Create(uri);
        var direct = new MongoClient(new MongoClientSettings { Server = url.Server, DirectConnection = true,
            ServerSelectionTimeout = TimeSpan.FromSeconds(5) });
        var admin = direct.GetDatabase("admin");
        try { await admin.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("replSetGetStatus", 1)); }
        catch (MongoCommandException error) when (error.Code == 94)
        {
            await admin.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("replSetInitiate",
                new MongoDB.Bson.BsonDocument { { "_id", url.ReplicaSetName },
                    { "members", new MongoDB.Bson.BsonArray { new MongoDB.Bson.BsonDocument { { "_id", 0 }, { "host", url.Server.ToString() } } } } }));
        }
        var client = new MongoClient(uri);
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try { await client.GetDatabase("admin").RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1)); return; }
            catch (TimeoutException) { await Task.Delay(1000); }
        }
        throw new TimeoutException("The owned replica set did not elect a primary.");
    }

    private static ProviderManager Manager(OASISDNA dna, IEnumerable<IOASISStorageProvider> providers)
    {
        var manager = new ProviderManager(null, dna);
        foreach (var provider in providers)
            manager.RegisterProvider(provider);
        return manager;
    }

    private static Holon NewHolon(string name) => new() { Id = Guid.NewGuid(), Name = name, CreatedByAvatarId = Guid.NewGuid(), IsActive = true };

    private static string Route<T>(OASISResult<T> result, string property)
    {
        using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
        return route.RootElement.GetProperty(property).GetString()!;
    }

    private static async Task ExecuteSqlAsync(string connectionString, string text)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(text, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
