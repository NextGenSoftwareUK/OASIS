using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task OwnedNativeCommittedTombstonePreservesReplicaFieldsAndRejectsConflict(bool synchronous,bool history)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var check = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await check.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only"){IsVersionControlEnabled=history};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var id = Guid.NewGuid(); var actor = Guid.NewGuid();
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar { Id = id, Username = "replica-owned", FirstName = "keep replica profile" })).IsError);
            var detail = new AvatarDetail { Id = id, Username = "replica-owned", XP = 888 };
            detail.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem { Id = Guid.NewGuid(), Name = "replica inventory" });
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            var committed = new NextGenSoftware.OASIS.API.Core.Objects.AvatarTombstone(id, new DateTime(2026, 10, 16, 1, 2, 3, DateTimeKind.Utc), actor);
            var applied = synchronous ? provider.ApplyAvatarTombstone(committed) : await provider.ApplyAvatarTombstoneAsync(committed);
            Assert.IsFalse(applied.IsError, applied.Message); Assert.IsTrue(applied.Result);
            Assert.IsFalse((await provider.ApplyAvatarTombstoneAsync(committed)).IsError, "Same committed tombstone is idempotent.");
            var profile = await provider.LoadAvatarAsync(id); var replica = await provider.LoadAvatarDetailAsync(id);
            Assert.AreEqual(committed.DeletedAtUtc, profile.Result.DeletedDate); Assert.AreEqual(actor, profile.Result.DeletedByAvatarId);
            Assert.AreEqual(committed.DeletedAtUtc, replica.Result.DeletedDate); Assert.AreEqual(actor, replica.Result.DeletedByAvatarId);
            Assert.AreEqual("keep replica profile", profile.Result.FirstName); Assert.AreEqual(888L, replica.Result.XP);
            Assert.AreEqual("replica inventory", replica.Result.Inventory.Single().Name);
            if(history)
            {
                Assert.AreEqual(2,profile.Result.Version);Assert.AreEqual(2,replica.Result.Version);
                var oldProfile=await provider.LoadAvatarAsync(id,1);
                var oldDetail=await provider.LoadAvatarDetailAsync(id,1);
                Assert.IsFalse(oldProfile.IsError,oldProfile.Message);Assert.IsFalse(oldDetail.IsError,oldDetail.Message);
                Assert.AreEqual(DateTime.MinValue,oldProfile.Result.DeletedDate);Assert.AreEqual(DateTime.MinValue,oldDetail.Result.DeletedDate);
                Assert.AreEqual(888L,oldDetail.Result.XP);Assert.AreEqual("replica inventory",oldDetail.Result.Inventory.Single().Name);
                Assert.AreEqual(2L,(await(await check.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=id.ToString()})).SingleAsync())["count"].As<long>(),"Repeated identical tombstone must not create additional versions.");
            }
            var conflict = await provider.ApplyAvatarTombstoneAsync(new NextGenSoftware.OASIS.API.Core.Objects.AvatarTombstone(id, committed.DeletedAtUtc.AddSeconds(1), actor));
            Assert.IsTrue(conflict.IsError); Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode, conflict.ErrorCode);
            Assert.AreEqual(committed.DeletedAtUtc, (await provider.LoadAvatarAsync(id)).Result.DeletedDate);
            Assert.IsFalse((await provider.DeleteAvatarAsync(id, false)).IsError);
            Assert.AreEqual(0L,(await(await check.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=id.ToString()})).SingleAsync())["count"].As<long>());
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task OwnedNativeDetailConflictRollsBackProfileDeletion(bool softDelete)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var heldSession = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await heldSession.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var id = Guid.NewGuid();
        var avatar = new Avatar { Id = id, Username = "owned-rollback", IsActive = true };
        var detail = new AvatarDetail { Id = id, Username = "owned-rollback", IsActive = true, XP = 10 };
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
        using var transaction = await heldSession.BeginTransactionAsync();
        Task<NextGenSoftware.OASIS.Common.OASISResult<bool>>? deletion = null;
        var committed = false;
        try
        {
            await (await transaction.RunAsync("MATCH (a:AvatarDetail {Id:$id}) SET a.__heldTestLock=true REMOVE a.__heldTestLock RETURN a.Id", new { id = id.ToString() })).ConsumeAsync();
            deletion = provider.DeleteAvatarAsync(id, softDelete);
            var deadline = DateTime.UtcNow.AddSeconds(10); var blocked = false;
            do
            {
                using var inspection = raw.AsyncSession();
                var rows = await (await inspection.RunAsync("SHOW TRANSACTIONS YIELD currentQuery, status WHERE currentQuery CONTAINS '__oasisAvatarLock' AND currentQuery CONTAINS 'AvatarDetail' RETURN status")).ToListAsync();
                blocked = rows.Any(row => row["status"].As<string>().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase));
                if (!blocked) await Task.Delay(50);
            } while (!blocked && !deletion.IsCompleted && DateTime.UtcNow < deadline);
            Assert.IsTrue(blocked, "Actual paired-delete detail mutation must block after profile mutation, before concurrent update commits.");
            detail.XP = 999;
            var snapshot = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(detail);
            await (await transaction.RunAsync("MATCH (a:AvatarDetail {Id:$id}) SET a.SnapshotJson=$snapshot RETURN a.Id", new { id = id.ToString(), snapshot })).ConsumeAsync();
            await transaction.CommitAsync(); committed = true;
            var rejected = await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(rejected.IsError); Assert.IsFalse(rejected.Result); Assert.IsFalse(rejected.IsDeleted);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode, rejected.ErrorCode);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(rejected, out _));
            var profile = await provider.LoadAvatarAsync(id); var currentDetail = await provider.LoadAvatarDetailAsync(id);
            Assert.IsFalse(profile.IsError, profile.Message); Assert.IsFalse(currentDetail.IsError, currentDetail.Message);
            Assert.IsTrue(profile.Result.IsActive); Assert.IsFalse(profile.Result.IsDeleted);
            Assert.IsTrue(currentDetail.Result.IsActive); Assert.IsFalse(currentDetail.Result.IsDeleted); Assert.AreEqual(999L, currentDetail.Result.XP);
            Assert.IsFalse((await provider.DeleteAvatarAsync(id, false)).IsError);
        }
        finally
        {
            if (!committed) await transaction.RollbackAsync();
            if (deletion != null) await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task OwnedNativeAvatarDeletionPreservesOrRemovesItsDetailInSameCommit(bool softDelete)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        var before = await (await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail RETURN count(a) AS count")).SingleAsync();
        Assert.AreEqual(0L, before["count"].As<long>(), "Retained pair evidence exists; no overwrite allowed.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var id = Guid.NewGuid();
            var avatar = new Avatar { Id = id, Username = "owned-pair", FirstName = "preserved profile" };
            var detail = new AvatarDetail { Id = id, Username = "owned-pair", XP = 4321, Karma = 321, ActiveQuestId = Guid.NewGuid() };
            detail.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem { Id = Guid.NewGuid(), Name = "preserved inventory" });
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            var deleted = await provider.DeleteAvatarAsync(id, softDelete);
            Assert.IsFalse(deleted.IsError, deleted.Message); Assert.IsTrue(deleted.Result); Assert.IsTrue(deleted.IsDeleted);
            if (softDelete)
            {
                var profile = await provider.LoadAvatarAsync(id); var storedDetail = await provider.LoadAvatarDetailAsync(id);
                Assert.IsFalse(profile.IsError, profile.Message); Assert.IsFalse(storedDetail.IsError, storedDetail.Message);
                Assert.IsFalse(profile.Result.IsActive); Assert.IsFalse(storedDetail.Result.IsActive);
                Assert.IsTrue(profile.Result.IsDeleted); Assert.IsTrue(storedDetail.Result.IsDeleted);
                Assert.AreEqual(profile.Result.DeletedDate, storedDetail.Result.DeletedDate);
                Assert.AreEqual("preserved profile", profile.Result.FirstName);
                Assert.AreEqual(4321L, storedDetail.Result.XP); Assert.AreEqual(321L, storedDetail.Result.Karma);
                Assert.AreEqual(detail.ActiveQuestId, storedDetail.Result.ActiveQuestId);
                Assert.AreEqual("preserved inventory", storedDetail.Result.Inventory.Single().Name);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted, out var evidence));
                Assert.AreEqual(profile.Result.DeletedDate, evidence.DeletedAtUtc);
                Assert.IsFalse((await provider.DeleteAvatarAsync(id, false)).IsError);
            }
            var after = await (await inspection.RunAsync("MATCH (a) WHERE (a:Avatar OR a:AvatarDetail) AND a.Id=$id RETURN count(a) AS count", new { id = id.ToString() })).SingleAsync();
            Assert.AreEqual(0L, after["count"].As<long>());
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeSelectedChildReparentingMovesOnlyItsIncomingEdge()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var oldParent = new Holon { Id = Guid.NewGuid(), Name = "old parent" };
            var newParent = new Holon { Id = Guid.NewGuid(), Name = "new parent" };
            var child = new Holon { Id = Guid.NewGuid(), Name = "moving child" };
            oldParent.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
            newParent.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
            Assert.IsFalse((await provider.SaveHolonAsync(oldParent)).IsError);
            Assert.IsFalse((await provider.SaveHolonAsync(newParent, saveChildren:false)).IsError);
            Assert.AreEqual(child.Id, (await provider.LoadHolonsForParentAsync(oldParent.Id, loadChildren:false)).Result.Single().Id);
            Assert.AreEqual(0, (await provider.LoadHolonsForParentAsync(newParent.Id, loadChildren:false)).Result.Count());
            child.ParentHolonId = newParent.Id;
            var moved = await provider.SaveHolonAsync(newParent);
            Assert.IsFalse(moved.IsError, moved.Message);
            Assert.AreEqual(0, (await provider.LoadHolonsForParentAsync(oldParent.Id, loadChildren:false)).Result.Count());
            Assert.AreEqual(child.Id, (await provider.LoadHolonsForParentAsync(newParent.Id, loadChildren:false)).Result.Single().Id);
            Assert.AreEqual(newParent.Id, (await provider.LoadHolonAsync(child.Id, loadChildren:false)).Result.ParentHolonId);
            var incoming = await (await inspection.RunAsync("MATCH (:Holon)-[r:HAS_CHILD]->(h:Holon {Id:$id}) RETURN count(r) AS count", new { id = child.Id.ToString() })).SingleAsync();
            Assert.AreEqual(1L, incoming["count"].As<long>());
            await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = new[] { oldParent.Id.ToString(), newParent.Id.ToString(), child.Id.ToString() } })).ConsumeAsync();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeCollectionParentAndMetadataReadsHydrateSelectedGraphs()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var root = new Holon { Id = Guid.NewGuid(), HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon };
            var child = new Holon { Id = Guid.NewGuid(), HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar };
            var leaf = new Holon { Id = Guid.NewGuid(), HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon };
            root.MetaData["selected"] = "root";
            root.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
            child.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { leaf };
            Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
            foreach (var sync in new[] { false, true })
            foreach (var option in new[] { (Children:false, Recursive:true, Depth:0, Levels:0),
                (Children:true, Recursive:false, Depth:0, Levels:1), (Children:true, Recursive:true, Depth:1, Levels:1),
                (Children:true, Recursive:true, Depth:0, Levels:2) })
            {
                var collection = sync ? provider.LoadAllHolons(loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth)
                    : await provider.LoadAllHolonsAsync(loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth);
                Assert.IsFalse(collection.IsError, collection.Message); Assert.IsTrue(collection.IsLoaded);
                var loaded = collection.Result.Single(record => record.Id == root.Id);
                Assert.AreEqual(option.Levels > 0 ? 1 : 0, loaded.Children.Count());
                if (option.Levels > 0) Assert.AreEqual(option.Levels > 1 ? 1 : 0, loaded.Children.Single().Children.Count());
                var metadata = sync ? provider.LoadHolonsByMetaData("selected", "root", loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth)
                    : await provider.LoadHolonsByMetaDataAsync("selected", "root", loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth);
                Assert.IsFalse(metadata.IsError, metadata.Message); Assert.AreEqual(root.Id, metadata.Result.Single().Id);
                Assert.AreEqual(option.Levels > 0 ? 1 : 0, metadata.Result.Single().Children.Count());
                var parent = sync ? provider.LoadHolonsForParent(root.Id, loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth)
                    : await provider.LoadHolonsForParentAsync(root.Id, loadChildren:option.Children, recursive:option.Recursive, maxChildDepth:option.Depth);
                Assert.IsFalse(parent.IsError, parent.Message); Assert.AreEqual(child.Id, parent.Result.Single().Id);
                Assert.AreEqual(option.Children && option.Recursive && option.Depth == 0 ? 1 : 0, parent.Result.Single().Children.Count());
            }
            var typed = await provider.LoadAllHolonsAsync(NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar, loadChildren:false);
            Assert.IsFalse(typed.IsError, typed.Message); Assert.AreEqual(child.Id, typed.Result.Single().Id);
            var typedParent = await provider.LoadHolonsForParentAsync(root.Id, NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon, loadChildren:false);
            Assert.IsFalse(typedParent.IsError, typedParent.Message); Assert.AreEqual(0, typedParent.Result.Count());
            var initial = await provider.LoadAllHolonsAsync(maxChildDepth:1, curentChildDepth:1);
            Assert.IsFalse(initial.IsError, initial.Message);
            Assert.AreEqual(1, initial.Result.Single(record => record.Id == root.Id).Children.Count());
            Assert.AreEqual(0, initial.Result.Single(record => record.Id == root.Id).Children.Single().Children.Count());
            Assert.IsTrue((await provider.LoadAllHolonsAsync(maxChildDepth:-1)).IsError);
            Assert.IsTrue((await provider.LoadHolonsForParentAsync(root.Id, curentChildDepth:-1)).IsError);
            Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync("selected", "root", maxChildDepth:-1)).IsError);
            await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",
                new { ids = new[] { root.Id.ToString(), child.Id.ToString(), leaf.Id.ToString() } })).ConsumeAsync();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativePointGraphReadsHonorDepthAndRejectCorruptEdges()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var root = new Holon { Id = Guid.NewGuid() }; var child = new Holon { Id = Guid.NewGuid() }; var leaf = new Holon { Id = Guid.NewGuid() };
            root.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
            child.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { leaf };
            Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
            foreach (var key in new[] { false, true })
            foreach (var sync in new[] { false, true })
            foreach (var fromProvider in new[] { false, true })
            foreach (var option in new[] { (Children:false, Recursive:true, Depth:0, Expected:1),
                (Children:true, Recursive:false, Depth:0, Expected:2), (Children:true, Recursive:true, Depth:1, Expected:2),
                (Children:true, Recursive:true, Depth:0, Expected:3) })
            {
                var read = key
                    ? sync ? provider.LoadHolon(root.Id.ToString(), option.Children, option.Recursive, option.Depth, loadChildrenFromProvider:fromProvider)
                        : await provider.LoadHolonAsync(root.Id.ToString(), option.Children, option.Recursive, option.Depth, loadChildrenFromProvider:fromProvider)
                    : sync ? provider.LoadHolon(root.Id, option.Children, option.Recursive, option.Depth, loadChildrenFromProvider:fromProvider)
                        : await provider.LoadHolonAsync(root.Id, option.Children, option.Recursive, option.Depth, loadChildrenFromProvider:fromProvider);
                Assert.IsFalse(read.IsError, read.Message); Assert.IsTrue(read.IsLoaded);
                Assert.AreEqual(option.Expected > 1 ? 1 : 0, read.Result.Children.Count());
                if (option.Expected > 1)
                {
                    Assert.AreEqual(child.Id, read.Result.Children.Single().Id);
                    Assert.AreEqual(option.Expected > 2 ? 1 : 0, read.Result.Children.Single().Children.Count());
                }
            }
            var badDepth = await provider.LoadHolonAsync(root.Id, maxChildDepth:-1);
            Assert.IsTrue(badDepth.IsError); Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", badDepth.ErrorCode);
            await (await inspection.RunAsync("MATCH (a:Holon {Id:$leaf}), (b:Holon {Id:$root}) MERGE (a)-[:HAS_CHILD]->(b)",
                new { leaf = leaf.Id.ToString(), root = root.Id.ToString() })).ConsumeAsync();
            var cycle = await provider.LoadHolonAsync(root.Id);
            Assert.IsTrue(cycle.IsError); Assert.IsFalse(cycle.IsLoaded);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode, cycle.ErrorCode);
            await (await inspection.RunAsync("MATCH (a:Holon {Id:$leaf})-[r:HAS_CHILD]->(b:Holon {Id:$root}) DELETE r",
                new { leaf = leaf.Id.ToString(), root = root.Id.ToString() })).ConsumeAsync();
            Assert.IsFalse((await provider.LoadHolonAsync(root.Id)).IsError);
            await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",
                new { ids = new[] { root.Id.ToString(), child.Id.ToString(), leaf.Id.ToString() } })).ConsumeAsync();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeSelectedGraphsHonorSaveDepthRecursionAndBulkInitialDepth()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach (var bulk in new[] { false, true })
            foreach (var sync in new[] { false, true })
            foreach (var option in new[] { (Children:false, Recursive:true, Depth:0, Expected:1),
                (Children:true, Recursive:false, Depth:0, Expected:2),
                (Children:true, Recursive:true, Depth:1, Expected:2),
                (Children:true, Recursive:true, Depth:0, Expected:3) })
            {
                var root = new Holon { Id = Guid.NewGuid(), Name = "owned graph root" };
                var child = new Holon { Id = Guid.NewGuid(), Name = "owned graph child" };
                var grandchild = new Holon { Id = Guid.NewGuid(), Name = "owned graph grandchild" };
                root.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
                child.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { grandchild };
                if (bulk)
                {
                    var saved = sync ? provider.SaveHolons(new[] { root }, option.Children, option.Recursive, option.Depth)
                        : await provider.SaveHolonsAsync(new[] { root }, option.Children, option.Recursive, option.Depth);
                    Assert.IsFalse(saved.IsError, saved.Message); Assert.AreEqual(1, saved.Result.Count());
                }
                else
                {
                    var saved = sync ? provider.SaveHolon(root, option.Children, option.Recursive, option.Depth)
                        : await provider.SaveHolonAsync(root, option.Children, option.Recursive, option.Depth);
                    Assert.IsFalse(saved.IsError, saved.Message);
                    Assert.AreEqual(option.Expected > 1 ? 1 : 0, saved.Result.Children.Count());
                }
                Assert.AreEqual((long)option.Expected, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
                Assert.AreEqual((long)option.Expected - 1, (await (await inspection.RunAsync("MATCH (:Holon)-[r:HAS_CHILD]->(:Holon) RETURN count(r) AS count")).SingleAsync())["count"].As<long>());
                if (option.Expected > 1)
                {
                    Assert.AreEqual(root.Id, child.ParentHolonId);
                    var nativeChild = await provider.LoadHolonAsync(child.Id, loadChildren:false);
                    Assert.IsFalse(nativeChild.IsError, nativeChild.Message); Assert.AreEqual(root.Id, nativeChild.Result.ParentHolonId);
                }
                await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = new[] { root.Id.ToString(), child.Id.ToString(), grandchild.Id.ToString() } })).ConsumeAsync();
            }
            foreach (var maximum in new[] { 1, 2 })
            {
                var root = new Holon { Id = Guid.NewGuid(), Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { new Holon { Id = Guid.NewGuid() } } };
                var child = root.Children.Single();
                var saved = await provider.SaveHolonsAsync(new[] { root }, maxChildDepth:maximum, curentChildDepth:1);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.AreEqual((long)maximum, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
                await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = new[] { root.Id.ToString(), child.Id.ToString() } })).ConsumeAsync();
            }
            var cyclic = new Holon { Id = Guid.NewGuid() };
            cyclic.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { cyclic };
            var rejected = await provider.SaveHolonAsync(cyclic);
            Assert.IsTrue(rejected.IsError); Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", rejected.ErrorCode);
            Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeMetadataQueriesAreExactParameterizedAndRefreshOnUpdate()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using (var preflight = raw.AsyncSession())
            Assert.AreEqual(0L, (await (await preflight.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var first = new Holon { Id = Guid.NewGuid(), HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon };
            var second = new Holon { Id = Guid.NewGuid(), HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar };
            const string hostileKey = "key`}) DETACH DELETE h // 雨";
            first.MetaData["biome"] = "park"; first.MetaData["region"] = "north"; first.MetaData[hostileKey] = "literal 'value'";
            second.MetaData["biome"] = "parkland"; second.MetaData["region"] = "north";
            var saved = await provider.SaveHolonsAsync(new[] { first, second }, saveChildren:false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var exact = await provider.LoadHolonsByMetaDataAsync("biome", "park", loadChildren:false);
            Assert.IsFalse(exact.IsError, exact.Message); Assert.IsTrue(exact.IsLoaded); Assert.AreEqual(first.Id, exact.Result.Single().Id);
            var pairs = new Dictionary<string, string> { ["biome"] = "park", ["region"] = "north" };
            var all = await provider.LoadHolonsByMetaDataAsync(pairs, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, loadChildren:false);
            Assert.IsFalse(all.IsError, all.Message); Assert.AreEqual(first.Id, all.Result.Single().Id);
            var any = provider.LoadHolonsByMetaData(pairs, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any, loadChildren:false);
            Assert.IsFalse(any.IsError, any.Message); Assert.AreEqual(2, any.Result.Count());
            var typed = await provider.LoadHolonsByMetaDataAsync("region", "north", NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar, loadChildren:false);
            Assert.IsFalse(typed.IsError, typed.Message); Assert.AreEqual(second.Id, typed.Result.Single().Id);
            var literal = await provider.LoadHolonsByMetaDataAsync(hostileKey, "literal 'value'", loadChildren:false);
            Assert.IsFalse(literal.IsError, literal.Message); Assert.AreEqual(first.Id, literal.Result.Single().Id);
            pairs["biome"] = "absent";
            var noAll = await provider.LoadHolonsByMetaDataAsync(pairs, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, loadChildren:false);
            Assert.IsFalse(noAll.IsError, noAll.Message); Assert.AreEqual(0, noAll.Result.Count());
            first.MetaData.Clear(); Assert.IsFalse((await provider.SaveHolonAsync(first, saveChildren:false)).IsError);
            var removed = await provider.LoadHolonsByMetaDataAsync("biome", "park", loadChildren:false);
            Assert.IsFalse(removed.IsError, removed.Message); Assert.AreEqual(0, removed.Result.Count());
            using var cleanup = raw.AsyncSession();
            await (await cleanup.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = new[] { first.Id.ToString(), second.Id.ToString() } })).ConsumeAsync();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeHolonRootSnapshotsRoundtripAcrossSingleBulkSyncAsync()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using (var preflight = raw.AsyncSession())
        {
            var count = await (await preflight.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync();
            Assert.AreEqual(0L, count["count"].As<long>(), "Retained holon evidence exists; no overwrite allowed.");
        }
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            for (var mode = 0; mode < 4; mode++)
            {
                var roots = Enumerable.Range(0, mode < 2 ? 1 : 2).Select(index => {
                    var record = new Holon { Id = Guid.NewGuid(), Name = "owned root " + index,
                        Description = "full root snapshot", ParentHolonId = Guid.NewGuid(),
                        Version = 5, VersionId = Guid.NewGuid(), IsPublic = false,
                        HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon };
                    record.MetaData["biome"] = "park";
                    return record;
                }).ToList();
                if (mode < 2)
                {
                    var saved = mode == 0 ? await provider.SaveHolonAsync(roots[0], saveChildren:false)
                        : provider.SaveHolon(roots[0], saveChildren:false);
                    Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved);
                }
                else
                {
                    var saved = mode == 2 ? await provider.SaveHolonsAsync(roots, saveChildren:false)
                        : provider.SaveHolons(roots, saveChildren:false);
                    Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved);
                    Assert.AreEqual(roots.Count, saved.Result.Count());
                }
                foreach (var source in roots)
                {
                    foreach (var read in new[] { await provider.LoadHolonAsync(source.Id, loadChildren:false),
                        provider.LoadHolon(source.Id, loadChildren:false),
                        await provider.LoadHolonAsync(source.Id.ToString(), loadChildren:false),
                        provider.LoadHolon(source.Id.ToString(), loadChildren:false) })
                    {
                        Assert.IsFalse(read.IsError, read.Message); Assert.IsTrue(read.IsLoaded);
                        Assert.AreEqual(source.ParentHolonId, read.Result.ParentHolonId);
                        Assert.AreEqual(source.VersionId, read.Result.VersionId); Assert.AreEqual(5, read.Result.Version);
                        Assert.AreEqual("park", read.Result.MetaData["biome"]);
                        Assert.AreEqual("full root snapshot", read.Result.Description);
                    }
                }
                var all = await provider.LoadAllHolonsAsync(loadChildren:false);
                Assert.IsFalse(all.IsError, all.Message); Assert.IsTrue(all.IsLoaded); Assert.AreEqual(roots.Count, all.Result.Count());
                roots[0].Description = null; roots[0].MetaData.Clear();
                Assert.IsFalse((await provider.SaveHolonAsync(roots[0], saveChildren:false)).IsError);
                var cleared = await provider.LoadHolonAsync(roots[0].Id, loadChildren:false);
                Assert.IsFalse(cleared.IsError, cleared.Message); Assert.IsNull(cleared.Result.Description); Assert.AreEqual(0, cleared.Result.MetaData.Count);
                using var cleanup = raw.AsyncSession();
                await (await cleanup.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = roots.Select(record => record.Id.ToString()).ToList() })).ConsumeAsync();
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task OwnedNativeAvatarDetailsPreserveInventoryTrackingAndNullClearing()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using (var preflight = raw.AsyncSession())
        {
            var count = await (await preflight.RunAsync("MATCH (a:AvatarDetail) RETURN count(a) AS count")).SingleAsync();
            Assert.AreEqual(0L, count["count"].As<long>(), "Retained detail evidence exists; no overwrite allowed.");
        }
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message);
            foreach (var synchronous in new[] { false, true })
            {
                var id = Guid.Parse(synchronous ? "00000000-0000-4000-8000-000000000202" : "00000000-0000-4000-8000-000000000201");
                var detail = new AvatarDetail { Id = id, Username = "owned-detail", Email = "owned-detail@example.invalid",
                    XP = 4321, Karma = 321, ActiveQuestId = Guid.NewGuid(), ActiveObjectiveId = Guid.NewGuid(),
                    Description = "full detail profile", IsActive = true };
                detail.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem { Id = Guid.NewGuid(), Name = "owned oak seed" });
                detail.MetaData["biome"] = "park";
                var saved = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
                Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved);
                foreach (var read in new[] { await provider.LoadAvatarDetailAsync(id), provider.LoadAvatarDetail(id),
                    await provider.LoadAvatarDetailByEmailAsync(detail.Email), provider.LoadAvatarDetailByEmail(detail.Email),
                    await provider.LoadAvatarDetailByUsernameAsync(detail.Username), provider.LoadAvatarDetailByUsername(detail.Username) })
                {
                    Assert.IsFalse(read.IsError, read.Message); Assert.IsTrue(read.IsLoaded);
                    Assert.AreEqual(4321L, read.Result.XP); Assert.AreEqual(321L, read.Result.Karma);
                    Assert.AreEqual(detail.ActiveQuestId, read.Result.ActiveQuestId); Assert.AreEqual(detail.ActiveObjectiveId, read.Result.ActiveObjectiveId);
                    Assert.AreEqual("owned oak seed", read.Result.Inventory.Single().Name);
                    Assert.AreEqual("park", read.Result.MetaData["biome"]);
                }
                var all = await provider.LoadAllAvatarDetailsAsync();
                Assert.IsFalse(all.IsError, all.Message); Assert.IsTrue(all.IsLoaded); Assert.AreEqual(id, all.Result.Single().Id);
                detail.ActiveQuestId = null; detail.ActiveObjectiveId = null; detail.Inventory.Clear(); detail.Description = null;
                var updated = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
                Assert.IsFalse(updated.IsError, updated.Message);
                var cleared = await provider.LoadAvatarDetailAsync(id);
                Assert.IsFalse(cleared.IsError, cleared.Message); Assert.IsNull(cleared.Result.ActiveQuestId); Assert.IsNull(cleared.Result.ActiveObjectiveId);
                Assert.AreEqual(0, cleared.Result.Inventory.Count); Assert.IsNull(cleared.Result.Description);
                Assert.AreEqual(4321L, cleared.Result.XP); Assert.AreEqual(321L, cleared.Result.Karma);
                using var cleanup = raw.AsyncSession();
                await (await cleanup.RunAsync("MATCH (a:AvatarDetail {Id:$id}) DETACH DELETE a", new { id = id.ToString() })).ConsumeAsync();
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ConcurrentProfileUpdateCannotBeLostToStaleDelete(bool softDelete)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var heldSession = raw.AsyncSession();
        var count = await (await heldSession.RunAsync("MATCH (a:Avatar) RETURN count(a) AS count")).SingleAsync();
        Assert.AreEqual(0L, count["count"].As<long>(), "Retained evidence exists; no overwrite allowed.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var avatar = new Avatar { Id = Guid.Parse("00000000-0000-4000-8000-000000000100"),
            Username = "owned-concurrent", FirstName = "before", IsActive = true };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        using var transaction = await heldSession.BeginTransactionAsync();
        Task<NextGenSoftware.OASIS.Common.OASISResult<bool>>? deletion = null;
        var committed = false;
        try
        {
            await (await transaction.RunAsync("MATCH (a:Avatar {Id:$id}) SET a.__heldTestLock = true REMOVE a.__heldTestLock RETURN a.Id", new { id = avatar.Id.ToString() })).ConsumeAsync();
            deletion = provider.DeleteAvatarAsync(avatar.Id, softDelete);
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var blocked = false;
            do
            {
                using var inspection = raw.AsyncSession();
                var rows = await (await inspection.RunAsync("SHOW TRANSACTIONS YIELD currentQuery, status WHERE currentQuery CONTAINS '__oasisAvatarLock' RETURN status")).ToListAsync();
                blocked = rows.Any(row => row["status"].As<string>().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase));
                if (!blocked) await Task.Delay(50);
            } while (!blocked && !deletion.IsCompleted && DateTime.UtcNow < deadline);
            Assert.IsTrue(blocked, "Delete must be observed waiting on the actual held node lock before update commits.");
            avatar.FirstName = "concurrent committed profile";
            var snapshot = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(avatar);
            await (await transaction.RunAsync("MATCH (a:Avatar {Id:$id}) SET a.SnapshotJson=$snapshot RETURN a.Id", new { id = avatar.Id.ToString(), snapshot })).ConsumeAsync();
            await transaction.CommitAsync(); committed = true;
            var rejected = await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(rejected.IsError); Assert.IsFalse(rejected.Result); Assert.IsFalse(rejected.IsSaved);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode, rejected.ErrorCode);
            var current = await provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(current.IsError, current.Message);
            Assert.IsTrue(current.Result.IsActive); Assert.IsFalse(current.Result.IsDeleted);
            Assert.AreEqual("concurrent committed profile", current.Result.FirstName);
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id, false)).IsError);
        }
        finally
        {
            if (!committed) await transaction.RollbackAsync();
            if (deletion != null) await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task OwnedNativeAvatarSnapshotsSelectorsDeletionAndReactivation()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        const string password = "oasis-local-test-only"; // Public credential for the loopback-only disposable test service.
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", password));
        using (var preflight = raw.AsyncSession())
        {
            var row = await (await preflight.RunAsync("MATCH (a:Avatar) RETURN count(a) AS count")).SingleAsync();
            Assert.AreEqual(0L, row["count"].As<long>(), "Retained avatar evidence exists; refuse overwrite or deletion.");
        }
        var provider = new Provider(uri, "neo4j", password);
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            for (var index = 0; index < 8; index++)
            {
                var id = Guid.Parse("00000000-0000-4000-8000-" + (index + 1).ToString("D12"));
                var avatar = new Avatar { Id = id, Username = "owned-native-" + index,
                    Email = "owned-native-" + index + "@example.invalid", FirstName = "Full", LastName = "Snapshot",
                    Description = "native complete profile", IsActive = true, Version = 3, VersionId = Guid.NewGuid() };
                avatar.MetaData["biome"] = "woodland";
                var saved = await provider.SaveAvatarAsync(avatar);
                Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved);
                foreach (var read in new[] { await provider.LoadAvatarAsync(id), await provider.LoadAvatarByProviderKeyAsync(id.ToString()),
                    await provider.LoadAvatarByUsernameAsync(avatar.Username), await provider.LoadAvatarByEmailAsync(avatar.Email) })
                {
                    Assert.IsFalse(read.IsError, read.Message); Assert.IsTrue(read.IsLoaded);
                    Assert.AreEqual("Full", read.Result.FirstName); Assert.AreEqual(avatar.VersionId, read.Result.VersionId);
                    Assert.AreEqual("woodland", read.Result.MetaData["biome"]);
                }
                if (index == 0)
                {
                    Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                    Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
                    Assert.AreEqual("Snapshot", (await provider.LoadAvatarAsync(id)).Result.LastName);
                    avatar.FirstName = null;
                    Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
                    Assert.IsNull((await provider.LoadAvatarAsync(id)).Result.FirstName);
                }
                var collection = await provider.LoadAllAvatarsAsync();
                Assert.IsFalse(collection.IsError, collection.Message); Assert.IsTrue(collection.IsLoaded);
                Assert.AreEqual(id, collection.Result.Single().Id);
                var soft = index switch {
                    0 => await provider.DeleteAvatarAsync(id),
                    1 => await provider.DeleteAvatarAsync(id.ToString()),
                    2 => await provider.DeleteAvatarByEmailAsync(avatar.Email),
                    3 => await provider.DeleteAvatarByUsernameAsync(avatar.Username),
                    4 => provider.DeleteAvatar(id),
                    5 => provider.DeleteAvatar(id.ToString()),
                    6 => provider.DeleteAvatarByEmail(avatar.Email),
                    _ => provider.DeleteAvatarByUsername(avatar.Username)
                };
                Assert.IsFalse(soft.IsError, soft.Message); Assert.IsTrue(soft.Result); Assert.IsTrue(soft.IsSaved);
                var deleted = await provider.LoadAvatarAsync(id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsFalse(deleted.Result.IsActive); Assert.IsTrue(deleted.Result.IsDeleted);
                Assert.AreEqual("Snapshot", deleted.Result.LastName); Assert.AreEqual("woodland", deleted.Result.MetaData["biome"]);
                var hard = await provider.DeleteAvatarAsync(id, false);
                Assert.IsFalse(hard.IsError, hard.Message); Assert.IsTrue(hard.Result);
                using var check = raw.AsyncSession();
                var remaining = await (await check.RunAsync("MATCH (a:Avatar {Id:$id}) RETURN count(a) AS count", new { id = id.ToString() })).SingleAsync();
                Assert.AreEqual(0L, remaining["count"].As<long>());
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
        // Failure leaves owned records intact for inspection; never sweep a database or unknown fixtures.
    }
}
