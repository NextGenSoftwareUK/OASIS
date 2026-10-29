using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData("restart-rebind")]
    [InlineData("second-target-rollback")]
    [InlineData("history-bootstrap")]
    [InlineData("corrupt-record")]
    [InlineData("new-policy")]
    [InlineData("concurrent-first")]
    public async Task MongoDestinationCatalogueRetainsNativeMeaningAtomically(string scenario)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string name = "cosmic_destination_catalogue_fixture";
        var journal = Path.Combine(workspace, "LocalState/COSMIC/OwnedRoutingCurrent/mongo-destination.pending.json");
        Assert.False(File.Exists(journal), "Reconcile the original outcome before another write.");
        var client = new MongoClient(uri);
        using var names = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name, await names.ToListAsync());
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var operations = new[] { Guid.NewGuid(), Guid.NewGuid() };
        ConditionalHolonWriteRequest Write(int index) => new(new ConditionalHolonWrite(operations[index], actor,
            ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(ids[index], null) }), ConditionalHolonOperation.Save,
            new[] { new Holon { Id = ids[index], Name = "destination-catalogue-" + index } }, saveChildren: false);
        await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { database = name, actor, ids, operations, scenario }));
        var database = client.GetDatabase(name);
        await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider = new Provider(uri, name);
        var complete = false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var captured = await provider.CaptureStorageIdentityAsync();
            Assert.False(captured.IsError, captured.Message);
            var source = captured.Result;
            var sqlite = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('B', 64), Guid.NewGuid());
            var neo = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('C', 64), "declaration-only");
            HolonReplicationRouteBinding Route(CosmicStorageIdentity target, string policy = "policy-one") => new(source, target, policy);
            HolonReplicationEnrollment Enrol(params HolonReplicationRouteBinding[] routes)
                => new(routes[0].SourceId, routes.Select(route => route.TargetId), source, routes);
            var initial = Enrol(Route(sqlite));
            if (scenario == "concurrent-first")
            {
                var independent = new Provider(uri, name);
                try
                {
                    Assert.False((await independent.ActivateProviderAsync()).IsError);
                    var independentSource = await independent.CaptureStorageIdentityAsync();
                    Assert.False(independentSource.IsError, independentSource.Message);
                    Assert.Equal(source.BindingFingerprint, independentSource.Result.BindingFingerprint);
                    var competing = new CosmicStorageIdentity(sqlite.Authority, sqlite.EndpointFingerprint, Guid.NewGuid());
                    var enrollments = new[] { initial, Enrol(Route(competing)) };
                    var providers = new[] { provider, independent };
                    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var arrivals = 0;
                    var writes = Enumerable.Range(0, 2).Select(index => Task.Run(async () => {
                        if (System.Threading.Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
                        await gate.Task.WaitAsync(TimeSpan.FromSeconds(10));
                        return await providers[index].ExecuteReplicatingHolonWriteAsync(Write(index), enrollments[index]);
                    })).ToArray();
                    await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    gate.SetResult();
                    var outcomes = await Task.WhenAll(writes);
                    var winner = Assert.Single(Enumerable.Range(0, 2).Where(index => !outcomes[index].IsError && outcomes[index].IsSaved));
                    var loser = 1 - winner;
                    Assert.True(outcomes[loser].IsError);
                    Assert.False(outcomes[loser].IsSaved);
                    Assert.Contains(outcomes[loser].ErrorCode, new[] { HolonDestinationRebindRequiredException.Code, "COSMIC_CONCURRENT_WRITE" });
                    var raceCatalogue = (IHolonReplicationDestinationCatalogue)provider.GetBoundHolonReplicationOutbox(source).Result;
                    var raceApproval = await raceCatalogue.ReadDestinationBindingAsync(sqlite.Authority);
                    Assert.False(raceApproval.IsError, raceApproval.Message + raceApproval.Exception);
                    Assert.Equal(operations[winner], raceApproval.Result.EstablishedOperationId);
                    Assert.Equal(enrollments[winner].NativeRoutes.Single().TargetStorage.BindingFingerprint,
                        raceApproval.Result.Route.TargetStorage.BindingFingerprint);
                    // Replay the winning exact intent through the other native client;
                    // an explicit losing-target retry must remain a rebind refusal.
                    Assert.False((await providers[loser].ExecuteReplicatingHolonWriteAsync(Write(winner), enrollments[winner])).IsError);
                    Assert.Equal(HolonDestinationRebindRequiredException.Code,
                        (await providers[loser].ExecuteReplicatingHolonWriteAsync(Write(loser), enrollments[loser])).ErrorCode);
                    var allRows = await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                    Assert.Equal(ids[winner].ToString("D"), Assert.Single(allRows)["HolonId"].AsString);
                    foreach (var collection in new[] { "CosmicWriteReceipt", "CosmicHolonOutbox" })
                    {
                        var rows = await database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                        var row = Assert.Single(rows);
                        Assert.Equal(operations[winner].ToString("D"), row[collection == "CosmicWriteReceipt" ? "_id" : "OperationId"].AsString);
                    }
                    Assert.Equal(1, await database.GetCollection<BsonDocument>("CosmicDestinationBinding")
                        .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                }
                finally { Assert.False((await independent.DeActivateProviderAsync()).IsError); }
                complete = true;
                return;
            }
            var first = Write(0);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(first, initial);
            Assert.False(saved.IsError, saved.Message + saved.Exception);
            var catalogue = (IHolonReplicationDestinationCatalogue)provider.GetBoundHolonReplicationOutbox(source).Result;
            var authority = initial.NativeRoutes.Single().TargetStorage.Authority;
            var approval = await catalogue.ReadDestinationBindingAsync(authority);
            Assert.False(approval.IsError, approval.Message + approval.Exception);
            Assert.Equal(operations[0], approval.Result.EstablishedOperationId);
            var approvals = database.GetCollection<BsonDocument>("CosmicDestinationBinding");
            var all = FilterDefinition<BsonDocument>.Empty;
            Assert.Equal(1, await approvals.CountDocumentsAsync(all));

            if (scenario == "restart-rebind")
            {
                var reopened = new Provider(uri, name);
                try
                {
                    Assert.False((await reopened.ActivateProviderAsync()).IsError);
                    var reread = await ((IHolonReplicationDestinationCatalogue)reopened.GetBoundHolonReplicationOutbox(source).Result)
                        .ReadDestinationBindingAsync(authority);
                    Assert.Equal(sqlite.BindingFingerprint, reread.Result.Route.TargetStorage.BindingFingerprint);
                    Assert.False((await reopened.ExecuteReplicatingHolonWriteAsync(first, initial)).IsError);
                    var replacement = new CosmicStorageIdentity(authority, sqlite.EndpointFingerprint, Guid.NewGuid());
                    var refused = await reopened.ExecuteReplicatingHolonWriteAsync(Write(1), Enrol(Route(replacement)));
                    Assert.Equal(HolonDestinationRebindRequiredException.Code, refused.ErrorCode);
                }
                finally { Assert.False((await reopened.DeActivateProviderAsync()).IsError); }
            }
            else if (scenario == "second-target-rollback")
            {
                var replacement = new CosmicStorageIdentity(sqlite.Authority, sqlite.EndpointFingerprint, "other-native-store");
                // Neo sorts before SQLite: its newly inserted approval must roll
                // back when the second destination conflicts with its original.
                var refused = await provider.ExecuteReplicatingHolonWriteAsync(Write(1), Enrol(Route(neo), Route(replacement)));
                Assert.Equal(HolonDestinationRebindRequiredException.Code, refused.ErrorCode);
                Assert.Null((await catalogue.ReadDestinationBindingAsync(neo.Authority)).Result);
            }
            else if (scenario == "history-bootstrap")
            {
                Assert.Equal(1, (await approvals.DeleteManyAsync(all)).DeletedCount);
                var historical = await catalogue.ReadDestinationBindingAsync(authority);
                Assert.False(historical.IsError, historical.Message + historical.Exception);
                Assert.Equal(operations[0], historical.Result.EstablishedOperationId);
                Assert.Equal(0, await approvals.CountDocumentsAsync(all));
                var written = await provider.ExecuteReplicatingHolonWriteAsync(Write(1), initial);
                Assert.False(written.IsError, written.Message + written.Exception);
                Assert.Equal(operations[0], (await catalogue.ReadDestinationBindingAsync(authority)).Result.EstablishedOperationId);
            }
            else if (scenario == "corrupt-record")
            {
                Assert.Equal(1, (await approvals.UpdateOneAsync(all, new BsonDocument("$set", new BsonDocument("RecordDigest", new string('0', 64))))).ModifiedCount);
                Assert.Equal("COSMIC_OUTBOX_CORRUPT", (await catalogue.ReadDestinationBindingAsync(authority)).ErrorCode);
                Assert.True((await provider.ExecuteReplicatingHolonWriteAsync(Write(1), initial)).IsError);
                Assert.Equal(new string('0', 64), (await approvals.Find(all).SingleAsync())["RecordDigest"].AsString);
            }
            else
            {
                var written = await provider.ExecuteReplicatingHolonWriteAsync(Write(1), Enrol(Route(sqlite, "policy-two")));
                Assert.False(written.IsError, written.Message + written.Exception);
                Assert.Equal("policy-one", (await catalogue.ReadDestinationBindingAsync(authority)).Result.Route.PolicyBinding);
                var queued = await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                    .Find(new BsonDocument("OperationId", operations[1].ToString("D"))).SingleAsync();
                Assert.Equal("policy-two", CommittedHolonReplicaPayload.Read(queued["PayloadJson"].AsString).NativeRoutes.Single().PolicyBinding);
            }
            var expected = scenario is "history-bootstrap" or "new-policy" ? 2 : 1;
            var native = await database.GetCollection<BsonDocument>("Holon").Find(all).ToListAsync();
            Assert.Equal(expected, native.Count);
            Assert.All(native, row => Assert.Contains(Guid.Parse(row["HolonId"].AsString), ids));
            Assert.Equal(expected, await database.GetCollection<BsonDocument>("CosmicWriteReceipt").CountDocumentsAsync(all));
            Assert.Equal(expected, await database.GetCollection<BsonDocument>("CosmicHolonOutbox").CountDocumentsAsync(all));
            Assert.Equal(1, await approvals.CountDocumentsAsync(all));
            complete = true;
        }
        finally
        {
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
            if (complete)
            {
                await client.DropDatabaseAsync(name);
                using var remaining = await client.ListDatabaseNamesAsync();
                Assert.DoesNotContain(name, await remaining.ToListAsync());
                File.Delete(journal);
            }
        }
    }
}
