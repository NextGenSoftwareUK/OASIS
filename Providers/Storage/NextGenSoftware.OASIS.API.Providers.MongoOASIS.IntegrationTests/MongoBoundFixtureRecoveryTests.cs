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

    [Fact]
    public async Task InspectPreservedPrimaryOutboxWithoutMutation()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("inspect-primary-fixture-2026-10-28",Environment.GetEnvironmentVariable("OASIS_BOUND_FIXTURE_RECONCILE"));
        var db=new MongoClient(uri).GetDatabase("cosmic_primary_outbox_fixture");
        using var names=await db.ListCollectionNamesAsync();
        foreach(var name in await names.ToListAsync())Console.WriteLine("COLLECTION "+name+" "+await db.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var rows=await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach(var row in rows)Console.WriteLine("ROW "+row["Name"].AsString+" "+row["HolonId"].AsString+" version="+row["Version"]);
        var queue=await db.GetCollection<BsonDocument>("CosmicHolonOutbox").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach(var row in queue)Console.WriteLine("OBLIGATION "+row["OperationId"]+" "+row["TargetId"]+" "+row["DeliveryState"]+" attempts="+row["AttemptCount"]);
    }

    [Fact]
    public async Task ReconcileEmptyRecordedPrimaryOutboxNamespace()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("reconcile-empty-primary-fixture-2026-10-28",Environment.GetEnvironmentVariable("OASIS_BOUND_FIXTURE_RECONCILE"));
        var settings=MongoClientSettings.FromConnectionString(uri);settings.WriteConcern=WriteConcern.WMajority.With(journal:true);
        var client=new MongoClient(settings);const string name="cosmic_primary_outbox_fixture";var db=client.GetDatabase(name);
        using var cursor=await db.ListCollectionNamesAsync();var names=await cursor.ToListAsync();
        Assert.True(new System.Collections.Generic.HashSet<string>(names).SetEquals(new[]{"CosmicOwnedFixture","CosmicHolonOutbox","Avatar","AvatarDetail","Holon"}));
        foreach(var collection in names)Assert.Equal(0L,await db.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await client.DropDatabaseAsync(name);
        using var remaining=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await remaining.ToListAsync());
    }

    [Fact]
    public async Task ReconcileRecordedSourceBindingClassificationFailure()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("source-classification-2026-10-28",Environment.GetEnvironmentVariable("OASIS_BOUND_FIXTURE_RECONCILE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))root=root.Parent;
        Assert.NotNull(root);
        var state=Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent");
        using var lease=new FileStream(Path.Combine(state,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var physical=Path.Combine(state,"source-binding-physical.pending.json");
        using var intent=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(physical));
        var op=intent.RootElement.GetProperty("OperationId").GetGuid();var id=intent.RootElement.GetProperty("Id").GetGuid();
        var original=intent.RootElement.GetProperty("identity");
        var identity=new CosmicStorageIdentity((ProviderType)original.GetProperty("Authority").GetInt32(),
            original.GetProperty("EndpointFingerprint").GetString()!,original.GetProperty("StoreId").GetString()!);
        var settings=MongoClientSettings.FromConnectionString(uri);settings.WriteConcern=WriteConcern.WMajority.With(journal:true);
        var client=new MongoClient(settings);var db=client.GetDatabase("cosmic_storage_identity_fixture_a");
        using(var names=await db.ListCollectionNamesAsync())Assert.DoesNotContain("Holon",await names.ToListAsync());
        var receipts=await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var obligations=await db.GetCollection<BsonDocument>("CosmicHolonOutbox").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Single(receipts);Assert.Single(obligations);
        Assert.Equal(op.ToString("D"),receipts[0]["_id"].AsString);Assert.Equal(op.ToString("D"),obligations[0]["OperationId"].AsString);
        var payload=obligations[0]["PayloadJson"].AsString;
        Assert.Equal(Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))),obligations[0]["PayloadDigest"].AsString);
        var mutation=CommittedHolonReplicaPayload.Read(payload);Assert.Equal(id,mutation.Roots.Single().Id);
        Assert.Equal("bound-primary-source",mutation.Roots.Single().Name);Assert.Equal("Pending",obligations[0]["DeliveryState"].AsString);
        Assert.Equal(0,obligations[0]["AttemptCount"].ToInt32());
        var enrollment=new HolonReplicationEnrollment(mutation.SourceId,new[]{obligations[0]["TargetId"].AsString},identity);
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,mutation.ActorId,ProviderType.MongoDBOASIS,
            new[]{new HolonWriteCondition(id,null)},enrollment),ConditionalHolonOperation.Save,mutation.Roots,saveChildren:false);
        var plan=ConditionalHolonPlan.CreateReplicating(request,ProviderType.MongoDBOASIS,enrollment);
        Assert.Equal(plan.Fingerprint,receipts[0]["Fingerprint"].AsString);
        Assert.Equal(id,Assert.Single(ConditionalHolonReceipt.Read(receipts[0]["ResultJson"].AsString,plan,out _)).Id);
        var primary=client.GetDatabase("cosmic_primary_outbox_fixture");
        using(var names=await primary.ListCollectionNamesAsync())
        {
            var all=await names.ToListAsync();Assert.True(new System.Collections.Generic.HashSet<string>(all).SetEquals(new[]{"CosmicOwnedFixture","CosmicHolonOutbox","Avatar","AvatarDetail","Holon"}));
            foreach(var collection in all)Assert.Equal(0L,await primary.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        var evidence=Path.Combine(root.FullName,"TestResults/OwnedRoutingCurrent/source-classification-reconciliation.json");
        await File.WriteAllTextAsync(evidence,new BsonDocument{{"receipt",receipts[0]},{"obligation",obligations[0]},{"primaryFixtureEmpty",true}}.ToJson());
        var owned=new[]{"cosmic_storage_identity_fixture_a","cosmic_storage_identity_fixture_b","cosmic_primary_outbox_fixture"};
        foreach(var name in owned)await client.DropDatabaseAsync(name);
        using(var names=await client.ListDatabaseNamesAsync())Assert.Empty((await names.ToListAsync()).Intersect(owned));
        File.Delete(physical);File.Delete(Path.Combine(state,"mongo-storage-identity.pending.json"));
    }

}
