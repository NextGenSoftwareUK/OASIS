using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")]
    public Task NativeSetHttpPreservesOccurrencesPermissionsAndMetering(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,setHttpJourney:true);
    private static async Task<int> VerifyNativeSetHttpJourney(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings,bool selectedColumns=false,bool stagedInputs=false,bool lateSelection=false,bool entityStages=false,bool selectedStages=false)
    {
        var type=provider.ProviderType.Value;var actor=Guid.NewGuid();var outsider=Guid.NewGuid();
        var row=new Holon {Id=Guid.NewGuid(),Name="native-set-http",CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        owned.Add(row.Id);
        Assert.IsNull((await provider.LoadHolonAsync(row.Id)).Result);
        var saved=await provider.SaveHolonAsync(row);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
        Assert.IsTrue(saved.IsSaved);
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var mongo=new MongoClient(uri);var usageDb="cosmic_set_http_usage_"+Guid.NewGuid().ToString("N");
        using(var names=await mongo.ListDatabaseNamesAsync())Assert.IsFalse((await names.ToListAsync()).Contains(usageDb));
        var outbox=new MongoUsageOutboxStore(uri,usageDb);var initialized=false;var authority=new Authority();
        try
        {
            await outbox.InitializeAsync(CancellationToken.None);initialized=true;
            var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key=new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var ownerToken=Token(actor,key);var otherToken=Token(outsider,key);
            authority.Tokens.Add(ownerToken,actor);authority.Tokens.Add(otherToken,outsider);
            await using var app=Host(new Dictionary<ProviderType,IOASISStorageProvider>{{type,provider}},outbox,authority,secret);
            await app.StartAsync();
            try
            {
                var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var http=new HttpClient {BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(20)};
                var filter=new CqlV1Filter {Field="Id",Scalar=CqlV1Scalar.Guid,Value=JsonSerializer.SerializeToElement(row.Id.ToString("D"))};
                var body=new CqlSetV1Request {Provider=type.ToString(),Kind="Concat",
                    Left=new(){Filters=new[]{filter},Take=1},Right=new(){Filters=new[]{filter},Take=1}};
                if(selectedColumns)
                {
                    body.Left.Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}};
                    body.Right.Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="other"}};
                }
                if(lateSelection)
                {
                    Assert.IsFalse(selectedColumns);Assert.IsFalse(stagedInputs);
                    body.OutputColumns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}};
                }
                if(entityStages)
                {
                    Assert.IsTrue(lateSelection);Assert.IsFalse(selectedColumns);Assert.IsFalse(stagedInputs);
                    body.Take=2;
                    body.EntityStages=new[]{
                        new CqlSetV1EntityStage{Operation="Where",Where=new(){Kind="Filter",Filter=filter}},
                        new CqlSetV1EntityStage{Operation="OrderBy",Ordering=new[]{new CqlV1Ordering{Field="Name",Descending=true}}},
                        new CqlSetV1EntityStage{Operation="Skip",Count=1},
                        new CqlSetV1EntityStage{Operation="Take",Count=1}};
                }
                if(selectedStages)
                {
                    Assert.IsTrue(lateSelection);Assert.IsFalse(selectedColumns);Assert.IsFalse(stagedInputs);Assert.IsFalse(entityStages);
                    body.Take=2;
                    body.SelectedStages=new[]{
                        new CqlCompositionV1Stage{Operation="Where",Filters=new[]{new CqlGroupedV1Filter{
                            Alias="label",Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement(row.Name)}}},
                        new CqlCompositionV1Stage{Operation="OrderBy",Order=new[]{new CqlGroupedV1Ordering{Alias="label",Descending=true}}},
                        new CqlCompositionV1Stage{Operation="Skip",Count=1},
                        new CqlCompositionV1Stage{Operation="Take",Count=1},
                        new CqlCompositionV1Stage{Operation="Reverse"},
                        new CqlCompositionV1Stage{Operation="Select",Columns=new[]{new CqlCompositionV1Column{Alias="kept",SourceAlias="label"}}}};
                }
                if(stagedInputs)
                {
                    Assert.IsTrue(selectedColumns);
                    body.Left.Columns=new[]{
                        new CqlProjectionV1Column{Field="Name",Alias="name"},
                        new CqlProjectionV1Column{Field="Description",Alias="detail"}};
                    body.Left.Stages=new[]{
                        new CqlCompositionV1Stage{Operation="Where",Filters=new[]{
                            new CqlGroupedV1Filter{Alias="name",Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement(row.Name)}}},
                        new CqlCompositionV1Stage{Operation="OrderBy",Order=new[]{new CqlGroupedV1Ordering{Alias="name",Descending=true}}},
                        new CqlCompositionV1Stage{Operation="Skip",Count=0},
                        new CqlCompositionV1Stage{Operation="Take",Count=1},
                        new CqlCompositionV1Stage{Operation="Select",Columns=new[]{new CqlCompositionV1Column{Alias="label",SourceAlias="name"}}}};
                    body.Right.Stages=new[]{
                        new CqlCompositionV1Stage{Operation="Where",Filters=new[]{
                            new CqlGroupedV1Filter{Alias="other",Operator=CqlV1Operator.NotEqual,Value=JsonSerializer.SerializeToElement(row.Name)}}}};
                }
                async Task Send(string? bearer,int expected)
                {
                    using var request=new HttpRequestMessage(HttpMethod.Post,"/api/data/query-cql/set"){
                        Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json")};
                    if(bearer!=null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
                    request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString("D"));
                    var watch=System.Diagnostics.Stopwatch.StartNew();
                    using var response=await http.SendAsync(request);
                    timings.Add(bearer==ownerToken?"owner-http":bearer==otherToken?"foreign-http":"anonymous-http",watch.ElapsedMilliseconds);
                    var text=await response.Content.ReadAsStringAsync();
                    if(bearer==null){Assert.AreEqual(HttpStatusCode.Unauthorized,response.StatusCode,text);return;}
                    Assert.AreEqual(HttpStatusCode.OK,response.StatusCode,text);
                    using var json=JsonDocument.Parse(text);var result=json.RootElement.GetProperty("result");
                    var rows=result.GetProperty("rows");Assert.AreEqual(expected,rows.GetArrayLength());
                    if(selectedColumns || lateSelection)
                    {
                        Assert.AreEqual(lateSelection?"cql-set-projection-v1":"cql-projected-set-v1",result.GetProperty("version").GetString());
                        var columns=result.GetProperty("columns");Assert.AreEqual(1,columns.GetArrayLength());
                        Assert.AreEqual(selectedStages?"kept":"label",columns[0].GetProperty("alias").GetString());
                        foreach(var item in rows.EnumerateArray())
                        {
                            Assert.AreEqual(1,item.GetArrayLength());Assert.AreEqual(row.Name,item[0].GetString());
                        }
                    }
                    else foreach(var item in rows.EnumerateArray())Assert.AreEqual(row.Id,item.GetProperty("id").GetGuid());
                    var operation=await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(),CancellationToken.None);
                    Assert.AreEqual("settled",operation.State);
                    Assert.AreEqual(1,operation.ProviderAttempts.Count);Assert.AreEqual(1,operation.Receipts.Count);
                    Assert.AreEqual(0.002m,authority.Settlements[operation.Id].ActualCostUsd);
                }
                await Send(ownerToken,stagedInputs||entityStages||selectedStages?1:2);await Send(otherToken,0);await Send(null,0);
                Assert.AreEqual(2,authority.Started.Count);Assert.AreEqual(2,authority.Settlements.Count);
            }
            finally{await app.StopAsync();}
        }
        finally
        {
            if(initialized)
            {
                foreach(var id in authority.Started.Keys)
                    Assert.AreEqual("settled",(await outbox.GetAsync(id,CancellationToken.None)).State,
                        "Do not erase uncertain owned operation evidence.");
                await mongo.DropDatabaseAsync(usageDb);
                using var names=await mongo.ListDatabaseNamesAsync();Assert.IsFalse((await names.ToListAsync()).Contains(usageDb));
            }
        }
        return 3;
    }
}
