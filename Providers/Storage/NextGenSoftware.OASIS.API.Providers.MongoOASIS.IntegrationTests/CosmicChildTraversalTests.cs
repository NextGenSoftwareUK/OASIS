using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public class CosmicChildTraversalTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ParentDepthAndRecursionAgreeForEmbeddedAndNativeChildren(bool synchronous, bool byKey)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        var leasePath = Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-child-environment.lock");
        using var lease = File.Open(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string databaseName = "cosmic_child_traversal_fixture";
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.DoesNotContain(databaseName, await names.ToListAsync());
        var database = client.GetDatabase(databaseName);
        await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider = new Provider(uri, databaseName);
        var complete = false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var grand = new Holon { Name = "owned cosmic grandchild" };
            var child = new Holon { Name = "owned cosmic child", Children = new List<IHolon> { grand } };
            var logicalVersion = Guid.NewGuid();
            var parent = new Holon { Name = "owned cosmic parent", Version = 3, VersionId = logicalVersion,
                Children = new List<IHolon> { child } };
            var saved = synchronous ? provider.SaveHolon(parent, saveChildrenOnProvider: byKey)
                : await provider.SaveHolonAsync(parent, saveChildrenOnProvider: byKey);
            Assert.False(saved.IsError, saved.Message + saved.Exception);
            var key = saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            Guid Token(IHolon holon) => CosmicConcurrency.GetProviderRevisionId(holon, ProviderType.MongoDBOASIS)!.Value;
            var firstToken = Token(saved.Result);
            Assert.NotEqual(Guid.Empty, firstToken);
            Assert.NotEqual(Guid.Empty, Token(Assert.Single(saved.Result.Children)));
            Assert.Equal(logicalVersion, saved.Result.VersionId);

            foreach (var fromProvider in new[] { false, true })
            foreach (var recursive in new[] { false, true })
            foreach (var depth in new[] { 0, 1, 2 })
            {
                var found = byKey
                    ? synchronous ? provider.LoadHolonsForParent(key, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                        : await provider.LoadHolonsForParentAsync(key, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                    : synchronous ? provider.LoadHolonsForParent(parent.Id, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                        : await provider.LoadHolonsForParentAsync(parent.Id, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider);
                Assert.False(found.IsError, found.Message + found.Exception);
                Assert.Equal(child.Id, Assert.Single(found.Result).Id);
                Assert.Equal(recursive && (depth == 0 || depth > 1) ? 1 : 0, found.Result.Single().Children.Count());
                var offset = await provider.LoadHolonsForParentAsync(parent.Id, maxChildDepth: 2, curentChildDepth: 1, loadChildrenFromProvider: fromProvider);
                Assert.False(offset.IsError, offset.Message + offset.Exception); Assert.Empty(Assert.Single(offset.Result).Children);
            }
            var records = database.GetCollection<MongoDB.Bson.BsonDocument>("Holon");
            var before = await records.Find(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty).ToListAsync();
            var parentBefore = before.Single(row => row["Name"] == "owned cosmic parent");
            var embeddedBefore = parentBefore["Children"].DeepClone();
            var untouchedBefore = before.Where(row => row["Name"] != "owned cosmic parent")
                .ToDictionary(row => row["_id"].ToString(), row => row.ToBson());
            var rootOnly = byKey
                ? await provider.LoadHolonAsync(key, loadChildren: false)
                : await provider.LoadHolonAsync(parent.Id, loadChildren: false);
            Assert.False(rootOnly.IsError, rootOnly.Message + rootOnly.Exception);
            rootOnly.Result.Name = "owned root-only update";
            Assert.Equal(firstToken, Token(rootOnly.Result));
            CosmicConcurrency.SetProviderRevisionId(rootOnly.Result, ProviderType.MongoDBOASIS, firstToken);

            // Deliberately submit unrelated child data: saveChildren:false must exclude it.
            rootOnly.Result.Children = new List<IHolon> { new Holon { Name = "must never persist" } };
            var updated = synchronous ? provider.SaveHolon(rootOnly.Result, saveChildren: false)
                : await provider.SaveHolonAsync(rootOnly.Result, saveChildren: false);
            Assert.False(updated.IsError, updated.Message + updated.Exception);
            Assert.Empty(updated.Result.Children);
            Assert.NotEqual(firstToken, Token(updated.Result));
            Assert.Equal(firstToken, Token(rootOnly.Result)); // Native preparation cannot mutate submitted observations.
            Assert.Equal(logicalVersion, updated.Result.VersionId);

            var after = await records.Find(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty).ToListAsync();
            Assert.Equal(embeddedBefore, after.Single(row => row["Name"] == "owned root-only update")["Children"]);
            foreach (var row in after.Where(row => row["Name"] != "owned root-only update"))
                Assert.Equal(untouchedBefore[row["_id"].ToString()], row.ToBson());
            foreach (var fromProvider in new[] { false, true })
            {
                var restored = await provider.LoadHolonAsync(parent.Id, loadChildrenFromProvider: fromProvider);
                Assert.False(restored.IsError, restored.Message + restored.Exception);
                Assert.Equal(child.Id, Assert.Single(restored.Result.Children).Id);
                Assert.Equal(grand.Id, Assert.Single(restored.Result.Children.Single().Children).Id);
            }
            var storedChild = after.Single(row => row["Name"] == "owned cosmic child");
            var preservedChildren = storedChild["Children"].DeepClone();
            preservedChildren.AsBsonArray.Single().AsBsonDocument.Add("nativeFrontierOnly", new BsonDocument("exact", new BsonInt64(9007199254740993L)));
            await records.UpdateOneAsync(new BsonDocument("_id", storedChild["_id"]), new BsonDocument("$set", new BsonDocument("Children", preservedChildren)));
            var grandBefore = after.Single(row => row["Name"] == "owned cosmic grandchild").ToBson();
            foreach (var mode in new (bool Bulk, bool Recursive, int Max, int Initial)[] { (false, true, 1, 0),
                (false, false, 0, 0), (true, true, 1, 0), (true, false, 0, 0),
                (true, true, 2, 1), (true, false, 0, 1) })
            {
                var submitted = await provider.LoadHolonAsync(parent.Id, loadChildrenFromProvider: true);
                Assert.False(submitted.IsError, submitted.Message + submitted.Exception);
                var submittedChild = Assert.Single(submitted.Result.Children);
                var excluded = Assert.Single(submittedChild.Children);
                excluded.Name = "excluded graph edit";
                submittedChild.Description = "selected bounded update";
                IHolon acknowledgement;
                if (mode.Bulk)
                {
                    var bounded = synchronous
                        ? provider.SaveHolons(new[] { submitted.Result }, recursive: mode.Recursive, maxChildDepth: mode.Max, curentChildDepth: mode.Initial)
                        : await provider.SaveHolonsAsync(new[] { submitted.Result }, recursive: mode.Recursive, maxChildDepth: mode.Max, curentChildDepth: mode.Initial);
                    Assert.False(bounded.IsError, bounded.Message + bounded.Exception);
                    acknowledgement = Assert.Single(bounded.Result);
                }
                else
                {
                    var bounded = synchronous
                        ? provider.SaveHolon(submitted.Result, recursive: mode.Recursive, maxChildDepth: mode.Max)
                        : await provider.SaveHolonAsync(submitted.Result, recursive: mode.Recursive, maxChildDepth: mode.Max);
                    Assert.False(bounded.IsError, bounded.Message + bounded.Exception);
                    acknowledgement = bounded.Result;
                }
                Assert.Empty(Assert.Single(acknowledgement.Children).Children);
                var acknowledgedChild = Assert.Single(acknowledgement.Children);
                var acknowledgedToken = Token(acknowledgedChild);
                Assert.NotEqual(Guid.Empty, acknowledgedToken);
                Assert.NotEqual(Token(submittedChild), acknowledgedToken);

                Assert.Equal("excluded graph edit", excluded.Name);
                var persisted = await records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                Assert.Equal(grandBefore, persisted.Single(row => row["Name"] == "owned cosmic grandchild").ToBson());
                var nativeChild = persisted.Single(row => row["Name"] == "owned cosmic child");
                Assert.Equal("selected bounded update", nativeChild["Description"].AsString);
                Assert.Equal(acknowledgedToken.ToString("D"), nativeChild["ProviderRevisionId"].AsString);
                var nativeRoot = persisted.Single(row => row["Name"] == "owned root-only update");
                Assert.Equal(Token(acknowledgement).ToString("D"), nativeRoot["ProviderRevisionId"].AsString);
                Assert.Equal(logicalVersion, acknowledgement.VersionId);

                Assert.Equal(preservedChildren, nativeChild["Children"]);
                var embeddedChild = persisted.Single(row => row["Name"] == "owned root-only update")["Children"].AsBsonArray.Single();
                Assert.Equal(preservedChildren, embeddedChild["Children"]);
                foreach (var fromProvider in new[] { false, true })
                {
                    var reloaded = await provider.LoadHolonAsync(parent.Id, loadChildrenFromProvider: fromProvider);
                    Assert.False(reloaded.IsError, reloaded.Message + reloaded.Exception);
                    Assert.Equal("owned cosmic grandchild", Assert.Single(Assert.Single(reloaded.Result.Children).Children).Name);
                }
            }
            foreach (var selectChildren in new[] { false, true })
            {
                var persistedBefore = await records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                var rootBefore = persistedBefore.Single(row => row["Name"] == "owned root-only update");
                var nativeChildBefore = persistedBefore.Single(row => row["Name"] == "owned cosmic child").ToBson();
                var submitted = await provider.LoadHolonAsync(parent.Id, loadChildren: false);
                Assert.False(submitted.IsError, submitted.Message + submitted.Exception);
                submitted.Result.Children = new List<IHolon> { new Holon { Name = "excluded by initial depth" } };
                var bounded = synchronous
                    ? provider.SaveHolons(new[] { submitted.Result }, saveChildren: selectChildren, maxChildDepth: 1, curentChildDepth: 1)
                    : await provider.SaveHolonsAsync(new[] { submitted.Result }, saveChildren: selectChildren, maxChildDepth: 1, curentChildDepth: 1);
                Assert.False(bounded.IsError, bounded.Message + bounded.Exception);
                Assert.Empty(Assert.Single(bounded.Result).Children);
                var persistedAfter = await records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                Assert.Equal(rootBefore["Children"], persistedAfter.Single(row => row["Name"] == "owned root-only update")["Children"]);
                Assert.Equal(nativeChildBefore, persistedAfter.Single(row => row["Name"] == "owned cosmic child").ToBson());
            }
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var providers = new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            providers.RegisterProvider(provider);
            var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna, providers);
            var tracked = await manager.LoadHolonAsync(parent.Id, loadChildrenFromProvider: true, providerType: ProviderType.MongoDBOASIS);
            Assert.False(tracked.IsError, tracked.Message + tracked.Exception);
            var trackedChild = Assert.Single(tracked.Result.Children);
            var pendingGrandchild = Assert.Single(trackedChild.Children);
            tracked.Result.Description = "manager selected root";
            trackedChild.Description = "manager selected child";
            pendingGrandchild.Description = "unsaved descendant edit";
            var managed = synchronous
                ? manager.SaveHolon(tracked.Result, Guid.NewGuid(), maxChildDepth: 1, providerType: ProviderType.MongoDBOASIS)
                : await manager.SaveHolonAsync(tracked.Result, Guid.NewGuid(), maxChildDepth: 1, providerType: ProviderType.MongoDBOASIS);
            Assert.False(managed.IsError, managed.Message + managed.Exception);
            Assert.Same(tracked.Result, managed.Result);
            Assert.Equal("unsaved descendant edit", pendingGrandchild.Description);
            Assert.True(pendingGrandchild.HasHolonChanged());
            var managedRows = await records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(grandBefore, managedRows.Single(row => row["Name"] == "owned cosmic grandchild").ToBson());
            Assert.Equal(preservedChildren, managedRows.Single(row => row["Name"] == "owned cosmic child")["Children"]);
            Assert.Equal("manager selected child", managedRows.Single(row => row["Name"] == "owned cosmic child")["Description"].AsString);
            Assert.Equal(3L, await database.GetCollection<MongoDB.Bson.BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
            complete = true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if (complete) await client.DropDatabaseAsync(databaseName);
        }
    }
}
