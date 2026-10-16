using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoGraphAcknowledgementTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;
    public MongoGraphAcknowledgementTests(Xunit.Abstractions.ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task ReconcileAcknowledgedRootOnlyFixture()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION") != uri)
            throw new InvalidOperationException("Owned reusable runner required.");
        var client = new MongoClient(uri);
        const string name = "hyperdrive_mongo_graph_ack_fixture";
        var database = client.GetDatabase(name);
        using var collections = await database.ListCollectionNamesAsync();
        foreach (var collection in await collections.ToListAsync())
            if (collection != "Holon")
                Assert.Equal(0L, await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var rows = await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(8, rows.Count);
        var expected = new Dictionary<string, int> {
            {"owned root",1}, {"reject-owned-child",1}, {"owned leaf",1},
            {"selected owned root",2}, {"selected owned child",2}, {"owned root-only",1} };
        Assert.Equal(expected.Count, rows.Select(row => row["Name"].AsString).Distinct().Count());
        foreach (var pair in expected) Assert.Equal(pair.Value, rows.Count(row => row["Name"].AsString == pair.Key));
        foreach (var row in rows)
            _output.WriteLine(row.ToJson()); // Preserve exact synthetic native snapshots in the compact reconciliation TRX.
        await client.DropDatabaseAsync(name);
        using var remaining = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name, await remaining.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RolledBackGraphDoesNotPublishCallerIdentities(bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION") != uri)
            throw new InvalidOperationException("Existing owned Mongo runner required.");
        const string name = "hyperdrive_mongo_graph_ack_fixture";
        var client = new MongoClient(uri);
        using var databases = await client.ListDatabaseNamesAsync();
        if ((await databases.ToListAsync()).Contains(name))
            throw new InvalidOperationException("Retained fixture requires inspection; refusing overwrite.");
        var database = client.GetDatabase(name);
        var verified = false;
        var provider = new Provider(uri, name);
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            using (var collections = await database.ListCollectionNamesAsync())
                if (!(await collections.ToListAsync()).Contains("Holon"))
                    await database.CreateCollectionAsync("Holon");
            await database.RunCommandAsync<BsonDocument>(new BsonDocument {
                {"collMod", "Holon"}, {"validator", new BsonDocument("Name", new BsonDocument("$ne", "reject-owned-child"))},
                {"validationLevel", "strict"}, {"validationAction", "error"} });
            var leaf = new Holon { Name = "owned leaf" };
            var child = new Holon { Name = "reject-owned-child", Children = new List<IHolon> { leaf } };
            var root = new Holon { Name = "owned root", Children = new List<IHolon> { child } };
            var failed = synchronous ? provider.SaveHolon(root, saveChildrenOnProvider: true)
                : await provider.SaveHolonAsync(root, saveChildrenOnProvider: true);
            Assert.True(failed.IsError);
            Assert.False(failed.IsSaved);
            foreach (var node in new[] { root, child, leaf })
            {
                Assert.Equal(Guid.Empty, node.Id);
                Assert.Equal(Guid.Empty, node.ParentHolonId);
            }
            var native = database.GetCollection<BsonDocument>("Holon");
            Assert.Equal(0L, await native.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            await database.RunCommandAsync<BsonDocument>(new BsonDocument {
                {"collMod", "Holon"}, {"validator", new BsonDocument()} });
            var saved = synchronous ? provider.SaveHolon(root, saveChildrenOnProvider: true)
                : await provider.SaveHolonAsync(root, saveChildrenOnProvider: true);
            Assert.False(saved.IsError, saved.Message + " " + saved.Exception);
            Assert.True(saved.IsSaved);
            Assert.Equal(3L, await native.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.NotEqual(Guid.Empty, root.Id);
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotEqual(Guid.Empty, leaf.Id);
            Assert.Equal(root.Id, child.ParentHolonId);
            Assert.Equal(child.Id, leaf.ParentHolonId);
            var loaded = await provider.LoadHolonAsync(root.Id, loadChildrenFromProvider: true);
            Assert.False(loaded.IsError, loaded.Message);
            Assert.Equal(child.Id, Assert.Single(loaded.Result.Children).Id);
            Assert.Equal(leaf.Id, Assert.Single(Assert.Single(loaded.Result.Children).Children).Id);
            foreach (var bounded in new[] { false, true })
            {
                var unselected = new Holon { Name = "unselected owned descendant" };
                var selected = new Holon { Name = "selected owned child", Children = new List<IHolon> { unselected } };
                var parent = new Holon { Name = "selected owned root", Children = new List<IHolon> { selected } };
                var limited = synchronous
                    ? provider.SaveHolon(parent, recursive: bounded, maxChildDepth: bounded ? 1 : 0, saveChildrenOnProvider: true)
                    : await provider.SaveHolonAsync(parent, recursive: bounded, maxChildDepth: bounded ? 1 : 0, saveChildrenOnProvider: true);
                Assert.False(limited.IsError, limited.Message + " " + limited.Exception);
                Assert.Equal(2, limited.SavedCount);
                Assert.NotEqual(Guid.Empty, selected.Id);
                Assert.Equal(parent.Id, selected.ParentHolonId);
                Assert.Equal(Guid.Empty, unselected.Id);
                Assert.Equal(Guid.Empty, unselected.ParentHolonId);
                foreach (var fromProvider in new[] { false, true })
                {
                    var persisted = await provider.LoadHolonAsync(parent.Id, loadChildrenFromProvider: fromProvider);
                    Assert.False(persisted.IsError, persisted.Message);
                    var persistedChild = Assert.Single(persisted.Result.Children);
                    Assert.Equal(selected.Id, persistedChild.Id);
                    Assert.Empty(persistedChild.Children);
                }
            }
            Assert.Equal(7L, await native.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var onProvider in new[] { false, true })
            {
                var excluded = new Holon { Name = "excluded root-only child" };
                var rootOnly = new Holon { Id = Guid.NewGuid(), Name = "owned root-only", Children = new List<IHolon> { excluded } };
                var rootSaved = synchronous
                    ? provider.SaveHolon(rootOnly, saveChildren: false, saveChildrenOnProvider: onProvider)
                    : await provider.SaveHolonAsync(rootOnly, saveChildren: false, saveChildrenOnProvider: onProvider);
                Assert.False(rootSaved.IsError, rootSaved.Message + " " + rootSaved.Exception);
                Assert.True(rootSaved.IsSaved);
                Assert.Equal(Guid.Empty, excluded.Id);
                Assert.Equal(Guid.Empty, excluded.ParentHolonId);
                foreach (var fromProvider in new[] { false, true })
                {
                    var rootRead = await provider.LoadHolonAsync(rootOnly.Id, loadChildrenFromProvider: fromProvider);
                    Assert.False(rootRead.IsError, rootRead.Message + " " + rootRead.Exception);
                    Assert.Empty(rootRead.Result.Children);
                }
            }
            Assert.Equal(9L, await native.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            verified = true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            // Preserve unknown/failed evidence; only dispose a fully verified owned fixture.
            if (verified) await client.DropDatabaseAsync(name);
        }
    }
}
