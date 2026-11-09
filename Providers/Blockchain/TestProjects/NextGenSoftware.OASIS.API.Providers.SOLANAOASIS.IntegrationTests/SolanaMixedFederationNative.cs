using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public partial class SolanaIndexedQueryNative
{
    private static async Task<object> VerifyNativeMongoFederationAsync(string root,HolonManager manager,
        ICosmicChainQueryIndexReader index,ICosmicChainQuerySourceObserver observer,
        CosmicChainQueryReadRequirement requirement,Seed seed)
    {
        const string database="cosmic_cql_mongo_solana_demo";
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string coffee="COSMIC native chain coffee";
        var state=Path.Combine(root,"LocalState/COSMIC/MongoSolanaCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        Assert.IsFalse(File.Exists(journal)||File.Exists(journal+".next"),"Preserve original Mongo fixture; never replay writes.");
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())Assert.IsFalse((await names.ToListAsync()).Contains(database));

        // The same synthetic logical ID has one Mongo copy and one retained chain copy.
        // PreserveCopies does not assert causal/content agreement between these copies.
        var records=new[]{new Holon{Id=seed.Id,CreatedByAvatarId=seed.Actor,Name=coffee},
            new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=seed.Actor,Name="Unselected Mongo control"}};
        await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{database,seed.Actor,ids=records.Select(row=>row.Id).ToArray(),
                seed.Id,seed.IndexId,seed.Genesis,seed.Signature,phase="before-mongo-create"});file.Flush(true);
        }
        var mongo=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri,database){IsVersionControlEnabled=false};
        var active=false;
        try
        {
            var activation=await mongo.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);active=true;
            foreach(var record in records){var saved=await mongo.SaveHolonAsync(record,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);}
            // Run the same readable C# sample that the teaching host will call.
            var result = await CosmicCqlDemo.MixedFederationWalkthrough.RunAsync(
                manager, mongo, index, observer, requirement, seed.Actor, seed.Id, coffee);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.AreEqual(2,result.Result.ObservedInputRows);
            Assert.AreEqual(2,result.Result.Rows.Count);
            var chain=result.Result.Rows.Single(row=>row.Selected.Provider==ProviderType.SolanaOASIS).Selected;
            var native=result.Result.Rows.Single(row=>row.Selected.Provider==ProviderType.MongoDBOASIS).Selected;
            Assert.AreEqual(seed.Id,chain.Row.Id);Assert.AreEqual(seed.Id,native.Row.Id);
            Assert.IsNull(native.IndexedEvidence);Assert.IsNotNull(chain.IndexedEvidence);
            Assert.AreEqual(seed.Signature,chain.IndexedEvidence.Receipt.CommitReference);
            Assert.AreEqual(seed.VersionId,chain.IndexedEvidence.Receipt.Record.VersionId);
            Assert.AreEqual(seed.IndexId,chain.IndexedEvidence.Read.Checkpoint.Source.IndexId);
            Assert.AreEqual(ProviderType.SQLLiteDBOASIS,chain.IndexedEvidence.Read.Checkpoint.Source.ExecutionProvider);
            Assert.AreEqual(seed.Genesis,chain.IndexedEvidence.Read.Head.NetworkIdentity);
            Assert.AreEqual(1,result.Result.IndexedReads.Count);
            Assert.IsTrue(chain.IndexedEvidence.Read.Head.Position.Slot>=chain.IndexedEvidence.Read.Checkpoint.Position.Slot);
            Assert.IsFalse(result.Result.Rows.Any(row=>row.Selected.Row.Id==records[1].Id));

            await VerifyMixedHttpAsync(manager, mongo, index, observer, requirement, seed,
                chain.IndexedEvidence.Read.Checkpoint.Position);

            // Exact synthetic Mongo ownership only; no chain deletion or transaction.
            foreach(var record in records){var read=await mongo.LoadHolonAsync(record.Id,loadChildren:false);
                Assert.IsFalse(read.IsError);Assert.AreEqual(seed.Actor,read.Result.CreatedByAvatarId);Assert.AreEqual(record.Name,read.Result.Name);}
            var db=client.GetDatabase(database);
            using(var collections=await db.ListCollectionNamesAsync())CollectionAssert.AreEqual(
                new[]{"Avatar","AvatarDetail","CosmicHolonOutbox","Holon"},(await collections.ToListAsync()).Order().ToArray());
            foreach(var empty in new[]{"Avatar","AvatarDetail","CosmicHolonOutbox"})
                Assert.AreEqual(0L,await db.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.AreEqual(2L,await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);active=false;
            await client.DropDatabaseAsync(database);
            using(var names=await client.ListDatabaseNamesAsync())Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
            return new{observedPhysicalRows=2,mongoRows=1,solanaRows=1,retainedEvidence=true,seed.Id,seed.IndexId,
                sourceWrites=0,sourceReseeded=false,unselectedMongoExcluded=true,exactMongoCleanupVerified=true,
                checkpoint=chain.IndexedEvidence.Read.Checkpoint.Position.Slot.ToString(),finalizedHead=chain.IndexedEvidence.Read.Head.Position.Slot.ToString()};
        }
        finally{if(active)Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);}
    }
}
