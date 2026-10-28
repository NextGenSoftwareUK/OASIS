using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Shared journaled native Mongo fixture for protocol federation acceptance.</summary>
internal static class NativeProtocolMongoAcceptance
{
    internal static async Task RunAsync(string root,IOASISStorageProvider nativeProvider,string[] keys,Guid actor,
        NextGenSoftware.OASIS.API.DNA.OASISDNA dna,NextGenSoftware.OASIS.API.Core.Managers.ProviderManager registry)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var protocol=(ICosmicQueryStorageProvider)nativeProvider;
        var protocolName=protocol.QueryProviderType.ToString();
        var database=protocol.QueryProviderType==ProviderType.ActivityPubOASIS?"cosmic_cql_protocol_federation_v1":"cosmic_cql_federation_"+protocolName.ToLowerInvariant();
        var metadataKey=protocol.QueryCapabilities.StringKeySetLookup.MetadataKey;
        var state=Path.Combine(root,"LocalState/COSMIC/ProtocolMongoCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,protocol.QueryProviderType==ProviderType.ActivityPubOASIS?"fixture.pending.json":protocolName+".pending.json");
        var reconcile=Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_RECONCILE")=="1";
        Assert.False(File.Exists(journal+".next"),"Preserve interrupted journal publication.");
        Assert.Equal(reconcile,File.Exists(journal));
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())
            Assert.Equal(reconcile,(await names.ToListAsync()).Contains(database));
        var records=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,
            IsPublic=true,Name="Mongo protocol federation "+index,
            MetaData=new(){[metadataKey]=index<2?keys[index]:"unselected-fixture-key"}}).ToArray();
        if(reconcile)
        {
            using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.Equal(database,retained.RootElement.GetProperty("database").GetString());
            Assert.Equal(keys,retained.RootElement.GetProperty("keys").EnumerateArray().Select(key=>key.GetString()));
            actor=retained.RootElement.GetProperty("actor").GetGuid();
            var ids=retained.RootElement.GetProperty("ids").EnumerateArray().Select(id=>id.GetGuid()).ToArray();
            Assert.Equal(3,ids.Length);Assert.Equal(3,ids.Distinct().Count());
            for(var index=0;index<records.Length;index++){records[index].Id=ids[index];records[index].CreatedByAvatarId=actor;}
        }
        else
        {
        await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{database,actor,ids=records.Select(row=>row.Id).ToArray(),keys,phase="before-create"});file.Flush(true);
        }
        }
        var mongo=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri,database){IsVersionControlEnabled=false};
        var active=false;
        try
        {
            var activation=await mongo.ActivateProviderAsync();Assert.False(activation.IsError,activation.Message);active=true;
            if(!reconcile)foreach(var record in records){var saved=await mongo.SaveHolonAsync(record,saveChildren:false);Assert.False(saved.IsError,saved.Message);}
            using var output=new StringWriter();
            var result=await CosmicCqlDemo.NativeKeySetWalkthrough.RunFederatedAsync(
                new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(nativeProvider,dna,registry),mongo,protocol,keys,actor,output);
            Assert.Equal(5,result.ObservedInputRows);Assert.Equal(5,result.Rows.Count);
            Assert.Equal(3,result.Rows.Count(row=>row.Selected.Provider==protocol.QueryProviderType));
            Assert.Equal(records.Take(2).Select(row=>row.Id).Order(),result.Rows.Where(row=>row.Selected.Provider==ProviderType.MongoDBOASIS).Select(row=>row.Selected.Row.Id).Order());
            Assert.DoesNotContain(result.Rows,row=>row.Selected.Row.Id==records[2].Id);
            foreach(var record in records){var read=await mongo.LoadHolonAsync(record.Id,loadChildren:false);Assert.False(read.IsError);Assert.Equal(actor,read.Result.CreatedByAvatarId);Assert.Equal(record.Name,read.Result.Name);}
            // This database was absent before the journaled writes. Verify its entire
            // physical record set and collection ownership before exact fixture teardown.
            var native=client.GetDatabase(database);
            using(var collections=await native.ListCollectionNamesAsync())Assert.Equal(new[]{"Avatar","AvatarDetail","CosmicHolonOutbox","Holon"},(await collections.ToListAsync()).Order());
            foreach(var empty in new[]{"Avatar","AvatarDetail","CosmicHolonOutbox"})
                Assert.Equal(0,await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(3,await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.False((await mongo.DeActivateProviderAsync()).IsError);active=false;
            await client.DropDatabaseAsync(database);
            using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
            var report=Path.Combine(root,"TestResults/DemoBreadthCurrent/native-mongo-"+protocolName+"-federation.json");
            await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new{observedRows=5,mongoRows=2,protocol=protocolName,protocolRows=3,unselectedMongoExcluded=true,exactCleanupVerified=true}));
            File.Delete(journal);
        }
        finally{if(active)Assert.False((await mongo.DeActivateProviderAsync()).IsError);}
    }

}
