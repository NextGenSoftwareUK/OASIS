using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class QuestDbBrowserNativeTests
{
    [TestMethod]
    [TestCategory("QuestDbBrowserNative")]
    public async Task NativeSymbolsAndPrivateMongoCopiesServeVerifiedBrowserPresets()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_questdb_browser_demo";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var state = Environment.GetEnvironmentVariable("COSMIC_QUESTDB_BROWSER_SOURCE")!;
        Assert.AreEqual("browser-source", new DirectoryInfo(state).Name);
        Assert.IsTrue(File.Exists(Path.Combine(state, "active-process.json")));
        Assert.IsTrue(File.Exists(Path.Combine(state, "bootstrap.ack.json")));
        using var source = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "source.json")));
        var actor = source.RootElement.GetProperty("actor").GetGuid();
        var keys = new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" };
        using var nativeHttp = new HttpClient(new HttpClientHandler {AllowAutoRedirect=false});
        using var quest = new QuestDbSymbolQuerySource(nativeHttp,
            new Uri("http://127.0.0.1:29000/api/v1/sql/execute"), actor, keys);
        Assert.IsFalse((await quest.ActivateQuerySourceAsync()).IsError);
        var nativeQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.QuestDBOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, QuestDbSymbolQuerySource.SymbolMetadata))).Result;
        var nativePlan = CosmicQueryPlanner.Plan(nativeQuery, ProviderType.QuestDBOASIS,
            new[] {new CosmicQueryTarget(ProviderType.QuestDBOASIS,quest.QueryCapabilities)}).Result;
        var nativeRows = await quest.ExecuteCosmicQueryAsync(nativePlan);
        Assert.IsFalse(nativeRows.IsError, nativeRows.Message);
        Assert.AreEqual(3,nativeRows.Result.Count);
        var ids = keys.Select(key => nativeRows.Result.Single(row => (string)row.MetaData[QuestDbSymbolQuerySource.SymbolMetadata] == key).Id).ToArray();
        using var sourceLease = new FileStream(Path.Combine(state,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var journal = Path.Combine(state,"mongo.pending.json");
        Assert.IsFalse(File.Exists(journal),"Preserve any unfinished Mongo outcome.");
        var client = new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var records = ids.Take(2).Append(Guid.NewGuid()).Select((id,index)=>new Holon {
            Id=id,Name="QuestDB Mongo copy " + index,CreatedByAvatarId=actor,IsPublic=false,
            MetaData=new Dictionary<string,object> {[QuestDbSymbolQuerySource.SymbolMetadata]=index<2?keys[index]:"UNENROLLED"}
        }).ToArray();
        using(var f=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
            await JsonSerializer.SerializeAsync(f,new {database,actor,ids=records.Select(row=>row.Id)});f.Flush(true);
        }
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri,database) {IsVersionControlEnabled=false};
        try
        {
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach(var row in records) {
                var saved=await mongo.SaveHolonAsync(row,saveChildren:false);
                Assert.IsFalse(saved.IsError,saved.Message);
            }
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings {LogSwitchingProviders=false}}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var registry=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            registry.RegisterProvider(mongo);registry.RegisterProvider(quest);
            var manager=new HolonManager(null,dna,registry);
            CosmicQuery<Holon> Query(string[] selected) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.QuestDBOASIS).Result)
                .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,QuestDbSymbolQuerySource.SymbolMetadata))).Result;
            var presets=new Dictionary<string,CosmicQuery<Holon>> { ["all"]=Query(keys),["subset"]=Query(keys.Take(2).ToArray()) };
            await using(var app=CosmicCqlDemo.IdentityFederationApiHost.Build(manager,mongo,quest,actor,ids,
                records.Select(row=>row.Id),ids,"http://127.0.0.1:0",contentPresets:presets))
            {
                await app.StartAsync();
                try
                {
                    var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http=new HttpClient {BaseAddress=new Uri(address)};
                    var page=await http.GetStringAsync("/");
                    StringAssert.Contains(page,"QuestDbSymbol");StringAssert.Contains(page,"separate from QuestDB holon storage");
                    foreach(var preset in new[] {"all","subset"}) {
                        using var response=await http.PostAsJsonAsync("/api/cql/federation",new {preset});
                        Assert.AreEqual(HttpStatusCode.OK,response.StatusCode,await response.Content.ReadAsStringAsync());
                        using var result=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        Assert.AreEqual(preset=="all"?5:4,result.RootElement.GetProperty("count").GetInt32());
                        var observed=result.RootElement.GetProperty("rows").EnumerateArray().ToArray();
                        Assert.AreEqual(preset=="all"?3:2,observed.Count(row=>row.GetProperty("provider").GetString()=="QuestDBOASIS"));
                        Assert.AreEqual(2,observed.Count(row=>row.GetProperty("provider").GetString()=="MongoDBOASIS"));
                        Assert.IsFalse(observed.Any(row=>row.GetProperty("id").GetGuid()==records[2].Id));
                    }
                    using(var unknown=await http.PostAsJsonAsync("/api/cql/federation",new {preset="unknown"})) Assert.AreEqual(HttpStatusCode.BadRequest,unknown.StatusCode);
                    using(var forged=await http.PostAsJsonAsync("/api/cql/federation",new {preset="all",actor=Guid.NewGuid()})) Assert.AreEqual(HttpStatusCode.BadRequest,forged.StatusCode);
                    await quest.DeActivateProviderAsync();
                    using(var inactive=await http.PostAsJsonAsync("/api/cql/federation",new {preset="all"})) Assert.AreEqual(HttpStatusCode.ServiceUnavailable,inactive.StatusCode);
                    Assert.IsFalse((await quest.ActivateQuerySourceAsync()).IsError);
                }
                finally {await app.StopAsync();}
            }
            if(Environment.GetEnvironmentVariable("COSMIC_QUESTDB_BROWSER_SERVE")=="1")
            {
                await using var app=CosmicCqlDemo.IdentityFederationApiHost.Build(manager,mongo,quest,actor,ids,
                    records.Select(row=>row.Id),ids,"http://127.0.0.1:5134",contentPresets:presets);
                await app.StartAsync();
                var ready=Path.Combine(state,"demo-ready.json");
                await File.WriteAllTextAsync(ready,JsonSerializer.Serialize(new {url="http://127.0.0.1:5134/"}));
                await app.WaitForShutdownAsync();File.Delete(ready);
            }
            var db=client.GetDatabase(database);
            Assert.AreEqual(3L,await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach(var row in records) {
                var observed=await mongo.LoadHolonAsync(row.Id,loadChildren:false);
                Assert.IsFalse(observed.IsError);Assert.AreEqual(row.Name,observed.Result.Name);Assert.AreEqual(actor,observed.Result.CreatedByAvatarId);
            }
            using(var names=await db.ListCollectionNamesAsync())
            foreach(var name in await names.ToListAsync()) {
                if(name=="Holon") continue;
                var rows=await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync();
                Assert.IsTrue(rows.Count==0 || name=="CosmicMigrationRecordGate" && rows.Count==1 && rows[0]["_id"].AsString=="cosmic-migration-record-gate-schema-v1");
            }
            await mongo.DeActivateProviderAsync();await client.DropDatabaseAsync(database);
            using(var names=await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
        }
        finally {await mongo.DeActivateProviderAsync();await quest.DeActivateProviderAsync();}
    }
}
