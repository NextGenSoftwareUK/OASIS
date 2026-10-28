using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
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
            var protocolRecords=result.Rows.Where(row=>row.Selected.Provider==protocol.QueryProviderType)
                .ToDictionary(row=>(string)row.Selected.Row.MetaData[metadataKey],row=>row.Selected.Row.Id);
            var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(nativeProvider,dna,registry);
            await using(var app=CosmicCqlDemo.ProtocolPointApiHost.BuildKeySet(manager,protocol,actor,protocolRecords,
                "http://127.0.0.1:0",mongo,records.Take(2).Select(row=>row.Id).ToArray()))
            {
                await app.StartAsync();
                try
                {
                    var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http=new HttpClient{BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(30)};
                    Assert.Contains("Run Mongo + protocol query",await http.GetStringAsync("/"));
                    using var foreign=await http.PostAsJsonAsync("/api/cql/federation",new{nativeKeys=new[]{"unowned"}});
                    Assert.Equal(HttpStatusCode.BadRequest,foreign.StatusCode);
                    using var forged=await http.PostAsJsonAsync("/api/cql/federation",new{nativeKeys=keys,actorId=Guid.NewGuid()});
                    Assert.Equal(HttpStatusCode.BadRequest,forged.StatusCode);
                    foreach(var selection in new[]{keys,keys.Take(2).ToArray()})
                    {
                        using var response=await http.PostAsJsonAsync("/api/cql/federation",new{nativeKeys=selection});
                        var text=await response.Content.ReadAsStringAsync();Assert.True(response.IsSuccessStatusCode,text);
                        using var json=JsonDocument.Parse(text);
                        Assert.Equal(selection.Length+2,json.RootElement.GetProperty("count").GetInt32());
                        var rows=json.RootElement.GetProperty("rows").EnumerateArray().ToArray();
                        Assert.Equal(2,rows.Count(row=>row.GetProperty("provider").GetString()=="MongoDBOASIS"));
                        Assert.Equal(selection.Length,rows.Count(row=>row.GetProperty("provider").GetString()==protocolName));
                    }
                }
                finally{await app.StopAsync();}
            }
            if(Environment.GetEnvironmentVariable("OASIS_CQL_FEDERATION_SERVE")=="1")
            {
                await using var interactive=CosmicCqlDemo.ProtocolPointApiHost.BuildKeySet(manager,protocol,actor,protocolRecords,
                    mongo:mongo,mongoRecords:records.Take(2).Select(row=>row.Id).ToArray());
                Console.WriteLine("Federated demo: http://127.0.0.1:5088/ ; POST /demo/shutdown to clean both fixtures.");
                await interactive.RunAsync();
            }

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
