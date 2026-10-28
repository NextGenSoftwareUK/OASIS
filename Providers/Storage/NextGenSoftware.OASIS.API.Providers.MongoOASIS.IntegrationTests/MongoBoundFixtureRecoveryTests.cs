using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

/// <summary>Explicit reconciliation of the recorded 28October bound-fixture run,
/// not a setup/reset step. Never included in the normal bound acceptance filter.</summary>
public class MongoBoundFixtureRecoveryTests
{
    [Fact]
    public async Task ReconcileRecordedBindingAndPublicationFailure()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("2026-10-28-bound-authoring-failure", Environment.GetEnvironmentVariable("OASIS_BOUND_FIXTURE_RECONCILE"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent/mongo-storage-identity.pending.json");
        Assert.True(File.Exists(journal));
        using var lease = new FileStream(Path.Combine(Path.GetDirectoryName(journal)!, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var client = new MongoClient(uri);
        var db = client.GetDatabase("cosmic_storage_identity_fixture_a");
        var rows = await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var receipts = await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var heads = await db.GetCollection<BsonDocument>("CosmicReplicaHead").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Single(rows); Assert.Single(receipts); Assert.Single(heads);
        Assert.Equal("native-bound-mongo", rows[0]["Name"].AsString);
        var head = heads[0];
        Assert.StartsWith("bound-mongo-", head["SourceId"].AsString);
        var snapshot = StoredRecordJson.DeserializeStored<Holon>(head["SnapshotJson"].AsString);
        Assert.Equal(snapshot.Id.ToString("D"), rows[0]["HolonId"].AsString);
        Assert.Equal(snapshot.Id.ToString("D"), head["_id"].AsString);
        var request = new CommittedHolonReplicaRequest(Guid.Parse(receipts[0]["_id"].AsString), head["SourceId"].AsString,
            ProviderType.SQLLiteDBOASIS, snapshot.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { snapshot }, saveChildren: false);
        var plan = CommittedHolonReplicaPlan.Create(request, ProviderType.MongoDBOASIS);
        Assert.Equal(plan.Fingerprint, receipts[0]["Fingerprint"].AsString);
        var acknowledged = Assert.Single(ConditionalHolonReceipt.Read(receipts[0]["ResultJson"].AsString, plan));
        Assert.Equal(snapshot.Id, acknowledged.Id); Assert.Equal(snapshot.VersionId, acknowledged.VersionId);
        Assert.Equal(CosmicConcurrency.GetProviderRevisionId(snapshot, ProviderType.MongoDBOASIS),
            CosmicConcurrency.GetProviderRevisionId(acknowledged, ProviderType.MongoDBOASIS));
        using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain("cosmic_storage_identity_fixture_b", await names.ToListAsync());

        var source = client.GetDatabase("cosmic_committed_replica_source_fixture");
        var sourceRows = await source.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(2, sourceRows.Count);
        var parent = Assert.Single(sourceRows.Where(row => row["Name"].AsString == "owned captured root"));
        var child = Assert.Single(sourceRows.Where(row => row["Name"].AsString == "owned captured child"));
        Assert.Equal(parent["HolonId"], child["ParentHolonId"]);
        Assert.Contains(child["HolonId"].AsString, parent["ChildRoleManifestJson"].AsString);
        foreach (var table in new[] { "CosmicWriteReceipt", "CosmicReplicaHead" })
        {
            Assert.Equal(0L, await source.GetCollection<BsonDocument>(table).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(0L, await client.GetDatabase("cosmic_committed_replica_target_fixture").GetCollection<BsonDocument>(table)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        Assert.Equal(0L, await client.GetDatabase("cosmic_committed_replica_target_fixture").GetCollection<BsonDocument>("Holon")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        // Evidence is persisted before deleting only these recorded owned DBs.
        var evidence = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/bound-fixture-reconciliation.json");
        await File.WriteAllTextAsync(evidence, new BsonDocument { { "boundRows", new BsonArray(rows) },
            { "receipts", new BsonArray(receipts) }, { "heads", new BsonArray(heads) }, { "publicationRows", new BsonArray(sourceRows) } }.ToJson());
        Console.WriteLine("Reconciliation evidence SHA256=" + Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(evidence))));
        var owned = new[] { "cosmic_storage_identity_fixture_a", "cosmic_storage_identity_fixture_b",
            "cosmic_committed_replica_source_fixture", "cosmic_committed_replica_target_fixture" };
        foreach (var name in owned) await client.DropDatabaseAsync(name);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.Empty((await names.ToListAsync()).Intersect(owned));
        File.Delete(journal);
    }
    [Fact]
    public async Task ReconcileRecordedPreFaultHarnessFixture()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("2026-10-28-pre-fault-harness", Environment.GetEnvironmentVariable("OASIS_BOUND_FIXTURE_RECONCILE"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))root=root.Parent;
        Assert.NotNull(root);
        var state=Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent");
        using var lease=new FileStream(Path.Combine(state,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        Assert.False(File.Exists(Path.Combine(state,"mongo-storage-identity.pending.json")));
        var client=new MongoClient(uri);
        var captured=new BsonDocument();
        foreach(var side in new[]{"source","target"})
        {
            var db=client.GetDatabase("cosmic_committed_replica_"+side+"_fixture");
            using var collections=await db.ListCollectionNamesAsync();
            Assert.Contains("CosmicOwnedFixture",await collections.ToListAsync());
            var rows=await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(side=="source"?4:2,rows.Count);
            if(side=="source")
            {
                Assert.Single(rows.Where(row=>row["Name"].AsString=="owned captured root"));
                Assert.Single(rows.Where(row=>row["Name"].AsString=="owned captured child"));
                Assert.Single(rows.Where(row=>row["Name"].AsString=="owned source third" && row["Version"].ToInt32()==3));
            }
            else Assert.Single(rows.Where(row=>row["Name"].AsString=="owned source second" && row["Version"].ToInt32()==2));
            Assert.Single(rows.Where(row=>row["Name"].AsString=="owned replica child"));
            var receipts=await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var heads=await db.GetCollection<BsonDocument>("CosmicReplicaHead").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(side=="source"?3:2,receipts.Count);
            Assert.Equal(side=="source"?0:2,heads.Count);
            foreach(var head in heads)Assert.Equal("owned mongo source",head["SourceId"].AsString);
            captured.Add(side,new BsonDocument{{"rows",new BsonArray(rows)},{"receipts",new BsonArray(receipts)},{"heads",new BsonArray(heads)}});
        }
        var evidence=Path.Combine(root.FullName,"TestResults/OwnedRoutingCurrent/pre-fault-fixture-reconciliation.json");
        await File.WriteAllTextAsync(evidence,captured.ToJson());
        Console.WriteLine("Pre-fault evidence SHA256="+Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(evidence))));
        var owned=new[]{"cosmic_committed_replica_source_fixture","cosmic_committed_replica_target_fixture"};
        foreach(var name in owned)await client.DropDatabaseAsync(name);
        using var names=await client.ListDatabaseNamesAsync();Assert.Empty((await names.ToListAsync()).Intersect(owned));
    }

}
