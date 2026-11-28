using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeRecursiveSetHttpUsesServerSqlPermissionsAndOneAttempt(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyRecursiveSetHttp,
            nativeJourneySuffix:"-recursive-set-http-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyRecursiveSetHttp(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var type=provider.ProviderType.Value;var actor=Guid.NewGuid();var outsider=Guid.NewGuid();
        var row=new Holon{Id=Guid.NewGuid(),Name="native-tree-http",Description="fixture",CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        owned.Add(row.Id);Assert.IsNull((await provider.LoadHolonAsync(row.Id)).Result);
        var saved=await provider.SaveHolonAsync(row);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);Assert.IsNotNull(saved.Result);var persistedVersion=saved.Result.Version;
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var mongo=new MongoClient(uri);var database="cosmic_recursive_http_usage_"+Guid.NewGuid().ToString("N");
        using(var names=await mongo.ListDatabaseNamesAsync())Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var outbox=new MongoUsageOutboxStore(uri,database);var authority=new Authority();var initialized=false;
        try
        {
            await outbox.InitializeAsync(CancellationToken.None);initialized=true;
            var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var key=new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var ownerToken=Token(actor,key);var foreignToken=Token(outsider,key);authority.Tokens.Add(ownerToken,actor);authority.Tokens.Add(foreignToken,outsider);
            await using var app=Host(new Dictionary<ProviderType,IOASISStorageProvider>{{type,provider}},outbox,authority,secret);
            await app.StartAsync();
            try
            {
                var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client=new HttpClient{BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(20)};
                var compiler=new CqlTextCompiler();var id=row.Id.ToString("D");
                var entity="(SELECT * FROM "+type+" WHERE Id = '"+id+"' LIMIT 1)";
                var entitySql="("+entity+" UNION ALL "+entity+" LIMIT 1) UNION ALL "+entity;
                var selected="(SELECT Name AS label, Description AS detail FROM "+type+" WHERE Id = '"+id+"' LIMIT 1)";
                var child="WITH q AS ("+selected+" UNION ALL "+selected+") SELECT label AS kept FROM q LIMIT 1";
                var right="(SELECT Name AS kept FROM "+type+" WHERE Id = '"+id+"' LIMIT 1)";
                var selectedSql="("+child+") UNION ALL "+right;
                var scalarSql="WITH counted AS ("+selectedSql+") SELECT COUNT(*) AS total FROM counted";
                var numericLeaf="(SELECT Version AS amount FROM "+type+" WHERE Id = '"+id+"' LIMIT 1)";
                var numericSet=numericLeaf+" UNION ALL "+numericLeaf;
                var sumSql="WITH amounts AS ("+numericSet+") SELECT SUM(amount) AS total FROM amounts";
                var averageSql="WITH amounts AS ("+numericSet+") SELECT AVG(amount) AS mean FROM amounts";
                var minSql="WITH labels AS ("+selectedSql+") SELECT MIN(kept) AS first FROM labels";
                var maxSql="WITH amounts AS ("+numericSet+") SELECT MAX(amount) AS highest FROM amounts";
                async Task Send(string sql,string mode,string? bearer,int expected)
                {
                    var query=compiler.Compile(sql);Assert.AreEqual("data/query-cql/set",query.Path);
                    var body=JsonNode.Parse(query.Body.GetRawText())!.AsObject();if(mode is "count" or "sum" or "average" or "min" or "max")body["maximumResults"]=1;
                    using var request=new HttpRequestMessage(HttpMethod.Post,"/api/"+query.Path){Content=new StringContent(body.ToJsonString(),Encoding.UTF8,"application/json")};
                    if(bearer!=null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
                    request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString("D"));
                    var watch=System.Diagnostics.Stopwatch.StartNew();using var response=await client.SendAsync(request);
                    timings.Add(mode+"-"+(bearer==ownerToken?"owner":bearer==foreignToken?"foreign":"anonymous"),watch.ElapsedMilliseconds);
                    var text=await response.Content.ReadAsStringAsync();
                    if(bearer==null){Assert.AreEqual(HttpStatusCode.Unauthorized,response.StatusCode,text);return;}
                    Assert.AreEqual(HttpStatusCode.OK,response.StatusCode,text);using var json=JsonDocument.Parse(text);var result=json.RootElement.GetProperty("result");
                    if(mode is "min" or "max")
                    {
                        Assert.AreEqual("cql-scalar-v1",result.GetProperty("version").GetString());
                        Assert.AreEqual(mode=="min"?"Min":"Max",result.GetProperty("operation").GetString());
                        Assert.AreEqual(mode=="min"?"String":"Int32",result.GetProperty("valueType").GetString());
                        if(mode=="min")Assert.AreEqual(expected==0?null:row.Name,result.GetProperty("value").GetString());
                        else Assert.AreEqual(persistedVersion,result.GetProperty("value").GetInt32());
                    }
                    else if(mode is "sum" or "average")
                    {
                        Assert.AreEqual("cql-scalar-v1",result.GetProperty("version").GetString());
                        Assert.AreEqual(mode=="sum"?"Sum":"Average",result.GetProperty("operation").GetString());
                        Assert.AreEqual(mode=="sum"?"Int32":"Decimal",result.GetProperty("valueType").GetString());
                        if(mode=="sum")Assert.AreEqual(expected*persistedVersion,result.GetProperty("value").GetInt32());
                        else Assert.AreEqual((decimal)persistedVersion,decimal.Parse(result.GetProperty("value").GetString()!,System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if(mode=="count")
                    {
                        Assert.AreEqual("cql-scalar-v1",result.GetProperty("version").GetString());
                        Assert.AreEqual("LongCount",result.GetProperty("operation").GetString());
                        Assert.AreEqual(expected.ToString(System.Globalization.CultureInfo.InvariantCulture),result.GetProperty("value").GetString());
                    }
                    else
                    {
                        var rows=result.GetProperty("rows");Assert.AreEqual(expected,rows.GetArrayLength());
                        if(mode=="selected")
                        {
                            Assert.AreEqual("cql-projected-set-v1",result.GetProperty("version").GetString());
                            Assert.AreEqual(1,result.GetProperty("columns").GetArrayLength());Assert.AreEqual("kept",result.GetProperty("columns")[0].GetProperty("alias").GetString());
                            foreach(var item in rows.EnumerateArray()){Assert.AreEqual(1,item.GetArrayLength());Assert.AreEqual(row.Name,item[0].GetString());}
                        }
                        else foreach(var item in rows.EnumerateArray())Assert.AreEqual(row.Id,item.GetProperty("id").GetGuid());
                    }
                    var operation=await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(),CancellationToken.None);
                    Assert.AreEqual("settled",operation.State);Assert.AreEqual(1,operation.ProviderAttempts.Count);Assert.AreEqual(1,operation.Receipts.Count);
                    Assert.AreEqual(0.002m,authority.Settlements[operation.Id].ActualCostUsd);
                }
                foreach(var sample in new[]{(entitySql,"entity"),(selectedSql,"selected"),(scalarSql,"count"),(sumSql,"sum")})
                {await Send(sample.Item1,sample.Item2,ownerToken,2);await Send(sample.Item1,sample.Item2,foreignToken,0);await Send(sample.Item1,sample.Item2,null,0);}
                await Send(averageSql,"average",ownerToken,2);await Send(averageSql,"average",null,0);
                await Send(minSql,"min",ownerToken,1);await Send(minSql,"min",foreignToken,0);await Send(minSql,"min",null,0);
                await Send(maxSql,"max",ownerToken,1);await Send(maxSql,"max",null,0);
                Assert.AreEqual(12,authority.Started.Count);Assert.AreEqual(12,authority.Settlements.Count);
            }
            finally{await app.StopAsync();}
        }
        finally
        {
            if(initialized)
            {
                foreach(var id in authority.Started.Keys)Assert.AreEqual("settled",(await outbox.GetAsync(id,CancellationToken.None)).State,"Preserve uncertain owned usage evidence.");
                await mongo.DropDatabaseAsync(database);using var names=await mongo.ListDatabaseNamesAsync();Assert.IsFalse((await names.ToListAsync()).Contains(database));
            }
        }
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{["serverSqlSetExtrema"]=true,["serverSqlNumericSetAggregates"]=true,["serverSqlCompilation"]=true,["nestedEntityWindows"]=true,
            ["selectedChildBindings"]=true,["nativeScalarCountBeyondPublication"]=true,["ownerForeignAnonymousBoundaries"]=true,["oneSettledNativeAttempt"]=true});
    }
}
