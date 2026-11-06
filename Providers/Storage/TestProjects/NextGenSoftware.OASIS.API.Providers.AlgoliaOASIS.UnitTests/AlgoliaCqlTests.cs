using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS.UnitTests;

[TestClass]
public class AlgoliaCqlTests
{
    private sealed class Handler(Func<Guid[],HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Guid[]> Requests = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Post,request.Method);
            Assert.AreEqual("https://appid-dsn.algolia.net/1/indexes/*/objects",request.RequestUri!.AbsoluteUri);
            Assert.AreEqual("APPID",request.Headers.GetValues("X-Algolia-Application-Id").Single());
            Assert.AreEqual("CONTROLLED",request.Headers.GetValues("X-Algolia-API-Key").Single());
            using var body=JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var rows=body.RootElement.GetProperty("requests").EnumerateArray().ToArray();
            Assert.IsTrue(rows.All(row=>row.GetProperty("indexName").GetString()=="oasis_holons"));
            var ids=rows.Select(row=>row.GetProperty("objectID").GetGuid()).ToArray();
            Requests.Add(ids);return respond(ids);
        }
    }
    private static HttpResponseMessage Response(string json,HttpStatusCode status=HttpStatusCode.OK)
        =>new(status) {Content=new StringContent(json)};
    private static JsonNode Native(Guid id,bool deleted=false,bool includeChild=false)
    {
        var node=JsonSerializer.SerializeToNode(new Holon {Id=id,Name="Algolia teaching record",
            DeletedDate=deleted?DateTime.UtcNow:DateTime.MinValue,
            Children=includeChild?new[] {new Holon {Id=Guid.NewGuid(),Name="Child excluded from query"}}:Array.Empty<Holon>()},
            new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase})!;
        node["objectID"]=id.ToString("D");return node;
    }
    private static string Batch(IEnumerable<JsonNode?> rows)=>JsonSerializer.Serialize(new {results=rows});
    private static CosmicQuery<Holon> Query(Guid[] ids)=>CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AlgoliaOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.AlgoliaOASIS,ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(AlgoliaOASIS provider,CosmicQuery<Holon> query)
        =>CosmicQueryPlanner.Plan(query,ProviderType.AlgoliaOASIS,new[] {new CosmicQueryTarget(ProviderType.AlgoliaOASIS,provider.QueryCapabilities)}).Result;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HostedSourceRejectsPrivateRecordsBeforeReturningRows(bool publicProbe)
    {
        var probeId = Guid.NewGuid();
        var privateId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var handler = new Handler(ids => Response(Batch(ids.Select(id =>
        {
            var record = Native(id, deleted: id == deletedId);
            record["isPublic"] = id == probeId && publicProbe;
            return record;
        }))));
        using var client = new HttpClient(handler);
        var source = new AlgoliaQuerySource("APPID", "CONTROLLED", probeId, client);

        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.AreEqual(publicProbe, !activation.IsError);
        if (publicProbe)
        {
            var publicResult = await source.ExecuteCosmicQueryAsync(Plan(source, Query(new[] { probeId, deletedId })));
            Assert.IsFalse(publicResult.IsError, publicResult.Message);
            Assert.AreEqual(probeId, publicResult.Result.Single().Id);

            var result = await source.ExecuteCosmicQueryAsync(Plan(source, Query(new[] { probeId, privateId })));
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }
    }

    [TestMethod]
    public async Task NativeBatchReturnsThreeTwoZeroPagingAndExcludesChildren()
    {
        var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var handler=new Handler(selected=>Response(Batch(selected.Select(id=>id==ids[4]?null:Native(id,id==ids[3],includeChild:true)))));
        using var http=new HttpClient(handler);
        var provider=new AlgoliaOASIS("APPID","CONTROLLED",ids[0],http);
        var activation=await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError,activation.Message+" "+activation.Exception);
        var full=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids)));
        Assert.IsFalse(full.IsError,full.Message);CollectionAssert.AreEqual(ids.Take(3).ToArray(),full.Result.Select(row=>row.Id).ToArray());
        Assert.IsTrue(full.Result.All(row=>!row.Children.Any()));
        CollectionAssert.AreEqual(ids,handler.Requests.Last());
        var subset=new[] {ids[0],ids[2]};
        var filtered=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids).Where(row=>subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError,filtered.Message);CollectionAssert.AreEqual(subset,filtered.Result.Select(row=>row.Id).ToArray());
        CollectionAssert.AreEqual(subset,handler.Requests.Last());
        var missing=new[] {ids[3],ids[4]};
        Assert.AreEqual(0,(await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids).Where(row=>missing.Contains(row.Id)).Result))).Result.Count);
        var page=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids).Skip(1).Result.Take(1).Result));
        CollectionAssert.AreEqual(new[] {ids[1]},page.Result.Select(row=>row.Id).ToArray());
    }

    [DataTestMethod]
    [DataRow("denied")][DataRow("rate_limited")][DataRow("server_error")][DataRow("missing_index")]
    [DataRow("short_batch")][DataRow("extra_batch")][DataRow("wrong_object_id")][DataRow("wrong_holon_id")]
    [DataRow("reordered")][DataRow("duplicate_object_id")][DataRow("duplicate_results")]
    [DataRow("invalid_slot")][DataRow("malformed")][DataRow("aggregate_budget")][DataRow("record_budget")]
    public async Task NativeFailuresReturnNoPartialRows(string fault)
    {
        var ids=new[] {Guid.NewGuid(),Guid.NewGuid()}.OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var handler=new Handler(selected=> {
            var a=Native(selected[0]);var b=Native(selected[1]);
            if(fault=="wrong_object_id") b["objectID"]=Guid.NewGuid().ToString("D");
            if(fault=="wrong_holon_id") b["id"]=Guid.NewGuid().ToString("D");
            if(fault=="record_budget") b["padding"]=new string('x',1024*1024);
            return fault switch {
                "denied"=>Response("{}",HttpStatusCode.Forbidden),
                "rate_limited"=>Response("{}",HttpStatusCode.TooManyRequests),
                "server_error"=>Response("{}",HttpStatusCode.ServiceUnavailable),
                "missing_index"=>Response("{\"results\":[null,null],\"message\":\"Index oasis_holons does not exist.\"}"),
                "short_batch"=>Response(Batch(new[] {a})),
                "extra_batch"=>Response(Batch(new[] {a,b,Native(Guid.NewGuid())})),
                "reordered"=>Response(Batch(new[] {b,a})),
                "duplicate_object_id"=>Response("{\"results\":["+a.ToJsonString()+","+b.ToJsonString()[..^1]+",\"objectID\":\""+selected[1]+"\"}]}"),
                "duplicate_results"=>Response("{\"results\":[],\"results\":["+a.ToJsonString()+","+b.ToJsonString()+"]}"),
                "invalid_slot"=>Response("{\"results\":["+a.ToJsonString()+",42]}"),
                "malformed"=>Response("{broken"),
                "aggregate_budget"=>Response(new string('x',4*1024*1024+1)),
                _=>Response(Batch(new[] {a,b}))
            };
        });
        using var http=new HttpClient(handler);
        var provider=new AlgoliaOASIS("APPID","CONTROLLED",http) {IsProviderActivated=true};
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids)));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual(fault.EndsWith("budget")?"CQL_QUERY_LIMIT":"CQL_QUERY_FAILED",result.ErrorCode);
        Assert.AreEqual(1,handler.Requests.Count);
        if(fault is "denied" or "rate_limited" or "server_error") Assert.IsInstanceOfType(result.Exception,typeof(HttpRequestException));
        else if(fault=="malformed") Assert.IsInstanceOfType(result.Exception,typeof(JsonException));
        else if(!fault.EndsWith("budget")) Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("absent")][DataRow("deleted")][DataRow("denied")]
    public async Task QueryActivationVerifiesExistingLiveRecordWithoutWrites(string fault)
    {
        var id=Guid.NewGuid();
        var handler=new Handler(ids=>fault=="denied"?Response("{}",HttpStatusCode.Unauthorized):Response(Batch(new[] {fault=="absent"?null:Native(ids[0],true)})));
        using var http=new HttpClient(handler);
        var provider=new AlgoliaOASIS("APPID","CONTROLLED",id,http) {IsProviderActivated=true};
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] {id},handler.Requests.Single());
    }

    [TestMethod]
    public async Task MissingProbeInactiveCancelledAndHistoricalQueriesDoNotDispatch()
    {
        var handler=new Handler(_=>throw new AssertFailedException("Native query must not dispatch."));
        using var http=new HttpClient(handler);var provider=new AlgoliaOASIS("APPID","CONTROLLED",http);
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        var plan=Plan(provider,Query(new[] {Guid.NewGuid()}));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE",(await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        provider.IsProviderActivated=true;using var cancel=new CancellationTokenSource();cancel.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicQueryAsync(plan,cancellationToken:cancel.Token)).ErrorCode);
        provider.IsVersionControlEnabled=true;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",(await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        Assert.AreEqual(0,handler.Requests.Count);
    }

    [DataTestMethod]
    [DataRow("APPID/evil")][DataRow("APPID.example")][DataRow("APPID@evil")][DataRow("")]
    public void ApplicationIdCannotChangeCredentialDestination(string appId)
        =>Assert.ThrowsException<ArgumentException>(()=>new AlgoliaOASIS(appId,"CONTROLLED"));

    [TestMethod]
    public void InvalidCredentialIsRejectedWithoutEchoingItsValue()
    {
        var error=Assert.ThrowsException<ArgumentException>(()=>new AlgoliaOASIS("APPID","PRIVATE\r\nInjected"));
        Assert.IsFalse(error.Message.Contains("PRIVATE",StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReusedClientCannotCombineCredentialsFromDifferentApplications()
    {
        using var http=new HttpClient(new Handler(_=>throw new AssertFailedException("No native request expected.")));
        _=new AlgoliaOASIS("APPID","CONTROLLED",http);
        Assert.ThrowsException<ArgumentException>(()=>new AlgoliaOASIS("OTHERAPP","OTHERCONTROLLED",http));
        CollectionAssert.AreEqual(new[] {"APPID"},http.DefaultRequestHeaders.GetValues("X-Algolia-Application-Id").ToArray());
        CollectionAssert.AreEqual(new[] {"CONTROLLED"},http.DefaultRequestHeaders.GetValues("X-Algolia-API-Key").ToArray());
    }
}
