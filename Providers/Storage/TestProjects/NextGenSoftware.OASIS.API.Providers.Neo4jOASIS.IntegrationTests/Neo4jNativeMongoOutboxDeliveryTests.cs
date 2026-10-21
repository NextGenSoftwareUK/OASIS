using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using MongoStore=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoHolonReplicationOutbox;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class Neo4jNativeMongoOutboxDeliveryTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeCosmicMongoNeoOutboxWorkerPreservesGraphLineage(bool mongoSource,bool synchronous)
    {
        const string neoUri="bolt://127.0.0.1:17687";
        Assert.AreEqual(neoUri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var mongoUri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        Assert.AreEqual("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000",mongoUri);
        const string databaseName="cosmic_mongo_neo4j_outbox_fixture";
        var client=new MongoClient(mongoUri);using var names=await client.ListDatabaseNamesAsync();
        Assert.IsFalse((await names.ToListAsync()).Contains(databaseName),"Retained fixture requires inspection, not reset.");
        var database=client.GetDatabase(databaseName);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var mongo=new MongoProvider(mongoUri,databaseName);var neo=new NeoProvider(neoUri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        IOASISStorageProvider source=mongoSource ? mongo:neo;IOASISStorageProvider target=mongoSource ? neo:mongo;
        var sourceType=mongoSource ? ProviderType.MongoDBOASIS:ProviderType.Neo4jOASIS;var targetType=mongoSource ? ProviderType.Neo4jOASIS:ProviderType.MongoDBOASIS;
        var origin="owned-mongo-neo-"+Guid.NewGuid().ToString("N");var targetId=origin+"-target";var actor=Guid.NewGuid();
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var operations=new List<string>();var complete=false;
        IHolonReplicationRecoveryOutbox? store=null;HolonOutboxDispatcher? worker=null;
        IEnumerable<IHolon> Flatten(IHolon root){yield return root;foreach(var child in root.Children)foreach(var item in Flatten(child))yield return item;}
        async Task<IHolon> Commit(IHolon root,bool create,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var conditions=Flatten(root).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,sourceType)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,sourceType,conditions),kind,new[]{root});
            var enrollment=new HolonReplicationEnrollment(origin,new[]{targetId});
            var result=synchronous ? ((IAtomicHolonReplicationStorageProvider)source).ExecuteReplicatingHolonWrite(request,enrollment)
                :await ((IAtomicHolonReplicationStorageProvider)source).ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task<(long Sequence,string Payload)> Queue()
        {
            if(mongoSource)
            {
                var row=await database.GetCollection<BsonDocument>("CosmicHolonOutbox").Find(new BsonDocument("OperationId",operations.Last())).SingleAsync();
                return (row["Sequence"].AsInt64,row["PayloadJson"].AsString);
            }
            var native=await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.Sequence AS sequence,o.PayloadJson AS payload",new{op=operations.Last()})).SingleAsync();
            return (native["sequence"].As<long>(),native["payload"].As<string>());
        }
        async Task Deliver()
        {
            var result=await worker!.DispatchOnceAsync(targetId,1);Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.AreEqual(1,result.Result.CompletedCount);
            var row=await Queue();var state=await store!.InspectAsync(row.Sequence);Assert.IsFalse(state.IsError,state.Message+state.Exception);
            Assert.AreEqual(HolonDeliveryState.Completed,state.Result.State);Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);
        }
        async Task<IHolon?> Fresh()
        {
            IOASISStorageProvider reader=mongoSource ? new NeoProvider(neoUri,"neo4j","oasis-local-test-only"):new MongoProvider(mongoUri,databaseName);
            try
            {
                var activated=await reader.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message+activated.Exception);
                var result=synchronous ? reader.LoadHolon(ids[0]):await reader.LoadHolonAsync(ids[0]);
                if(result.IsError){Assert.IsNull(result.Result);return null;}return result.Result;
            }
            finally{await reader.DeActivateProviderAsync();}
        }
        void Verify(IHolon committed,IHolon read)
        {
            Assert.AreEqual(committed.Id,read.Id);Assert.AreEqual(committed.Name,read.Name);Assert.AreEqual(committed.Version,read.Version);
            Assert.AreEqual(committed.VersionId,read.VersionId);Assert.AreEqual(committed.PreviousVersionId,read.PreviousVersionId);
            Assert.AreEqual(committed.CreatedDate,read.CreatedDate);Assert.AreEqual(actor,read.CreatedByAvatarId);
            Assert.AreEqual(committed.ModifiedDate,read.ModifiedDate);Assert.AreEqual(committed.ModifiedByAvatarId,read.ModifiedByAvatarId);
            Assert.AreEqual(committed.MetaData["utc"],read.MetaData["utc"]);
            CollectionAssert.AreEqual((byte[])committed.MetaData["binary"],(byte[])read.MetaData["binary"]);
            Assert.AreNotEqual(CosmicConcurrency.GetProviderRevisionId(committed,sourceType),CosmicConcurrency.GetProviderRevisionId(read,targetType));
            Assert.AreEqual(committed.Children.Single().VersionId,read.Children.Single().VersionId);
        }
        try
        {
            var mongoActive=await mongo.ActivateProviderAsync();Assert.IsFalse(mongoActive.IsError,mongoActive.Message+mongoActive.Exception);
            var neoActive=await neo.ActivateProviderAsync();Assert.IsFalse(neoActive.IsError,neoActive.Message+neoActive.Exception);
            store=mongoSource ? new MongoStore(mongo.Database):new NeoHolonReplicationOutbox(driver);
            var adapter=new CommittedHolonReplicaTarget(targetId,(ICommittedHolonReplicaStorageProvider)target,targetType,origin,sourceType);
            worker=new HolonOutboxDispatcher(store,new[]{adapter},"owned-mongo-neo-worker");
            var input=new Holon{Id=ids[0],Name="owned mongo neo initial",Children=new List<IHolon>{new Holon{Id=ids[1],Name="owned mongo neo child"}}};
            input.MetaData["utc"]=new DateTime(2026,10,19,10,11,12,DateTimeKind.Utc).AddTicks(1234);input.MetaData["binary"]=new byte[]{0,1,127,255};
            var first=await Commit(input,true);await Deliver();Verify(first,(await Fresh())!);
            var edit=OasisJson.Project<Holon>(first);edit.Children=first.Children.Select(h=>(IHolon)OasisJson.Project<Holon>(h)).ToList();edit.Name="owned mongo neo second";
            var second=await Commit(edit,false);await Deliver();Verify(second,(await Fresh())!);
            var soft=await Commit(second,false,ConditionalHolonOperation.SoftDelete);await Deliver();
            // Verify the stored tombstone independently of provider-specific public deleted-row filtering.
            if(mongoSource)
            {
                var softRead=await Fresh();Assert.IsNotNull(softRead);Assert.AreEqual(soft.DeletedDate,softRead.DeletedDate);Assert.AreEqual(soft.VersionId,softRead.VersionId);
            }
            else
            {
                var native=await mongo.Database.Holon.Find(h=>h.HolonId==ids[0]).SingleAsync();
                var softRead=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper.ConvertMongoEntityToOASISHolon(native,false);
                Assert.AreEqual(soft.DeletedDate,softRead.DeletedDate);Assert.AreEqual(soft.VersionId,softRead.VersionId);
                var publicSoft=await Fresh();Assert.IsNotNull(publicSoft);Assert.AreEqual(soft.VersionId,publicSoft.VersionId);Assert.AreEqual(soft.DeletedDate,publicSoft.DeletedDate);
            }
            await Commit(soft,false,ConditionalHolonOperation.HardDelete);await Deliver();Assert.IsNull(await Fresh());
            if(mongoSource)
            {
                var count=(await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids=ids.Select(id=>id.ToString()).ToArray()})).SingleAsync())["count"].As<long>();Assert.AreEqual(0L,count);
            }
            else Assert.AreEqual(0L,await mongo.Database.Holon.CountDocumentsAsync(MongoDB.Driver.Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>.Filter.In(h=>h.HolonId,ids)));
            var queue=await Queue();var replay=await adapter.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.ReadItem(queue.Payload),CancellationToken.None);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.IsTrue(replay.Result);
            Assert.IsNull((await store.ClaimNextAsync(targetId,"another",TimeSpan.FromSeconds(1))).Result);
            complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{databaseName,ids,operations,complete,mongoSource,synchronous}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await neo.DeActivateProviderAsync();await mongo.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(databaseName);
        }
    }
    [TestMethod]
    [TestCategory("OwnedFixtureReconciliation")]
    public async Task NativeCosmicMongoNeoReviewedSoftFixtureIsArchivedBeforeRemoval()
    {
        const string neoUri="bolt://127.0.0.1:17687";Assert.AreEqual(neoUri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");Assert.AreEqual("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000",uri);
        const string name="cosmic_mongo_neo4j_outbox_fixture";var client=new MongoClient(uri);var db=client.GetDatabase(name);
        var ids=new[]{"3c5a7833-2901-4165-8bcf-1b5924fc6613","dfdc00b5-0fbd-4bcc-a957-2cc0d9ec4770"};
        var operations=new[]{"b9194f1d-1ce0-4a85-8364-c38d9fe377ec","21fda7e0-f966-4b49-a058-802590de9520","14a41c7a-5210-4c23-948c-2ddf50df5ce9"};
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=driver.AsyncSession();
        var collections=await(await db.ListCollectionNamesAsync()).ToListAsync();CollectionAssert.Contains(collections,"CosmicOwnedFixture");
        var mongoRows=new Dictionary<string,string[]>();
        foreach(var collection in collections)
        {
            var rows=await db.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            mongoRows[collection]=rows.Select(row=>row.ToJson()).ToArray();
            if(collection=="Holon")
            {
                Assert.AreEqual(2,rows.Count);CollectionAssert.AreEquivalent(ids,rows.Select(row=>row["HolonId"].AsString).ToArray());
                Assert.IsTrue(rows.All(row=>row["Version"].AsInt32==3));
            }
            if(collection=="CosmicWriteReceipts")Assert.AreEqual(3,rows.Count);
        }
        var neoRows=await(await session.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id IN $ids) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) RETURN labels(n) AS labels,properties(n) AS properties",new{ids,operations})).ToListAsync();
        Assert.AreEqual(8,neoRows.Count);
        var obligations=neoRows.Where(row=>row["labels"].As<List<string>>().Contains("CosmicHolonOutbox")).Select(row=>row["properties"].As<Dictionary<string,object>>()).ToArray();
        Assert.AreEqual(3,obligations.Length);Assert.IsTrue(obligations.All(row=>(string)row["DeliveryState"]=="Completed"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var archive=Path.Combine(work,"mongo-neo-reviewed-soft-fixture.json");Assert.IsFalse(File.Exists(archive),"Do not overwrite reviewed evidence.");
        var text=System.Text.Json.JsonSerializer.Serialize(new{reason="Public Mongo default load excludes soft-deleted rows; worker and both native tombstones committed",ids,operations,mongoRows,neoRows=neoRows.Select(row=>new{labels=row["labels"].As<List<string>>(),properties=row["properties"].As<Dictionary<string,object>>()})});
        await File.WriteAllTextAsync(archive,text);Assert.AreEqual(text,await File.ReadAllTextAsync(archive));
        await(await session.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id IN $ids) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) DETACH DELETE n",new{ids,operations})).ConsumeAsync();
        await client.DropDatabaseAsync(name);
    }

}
