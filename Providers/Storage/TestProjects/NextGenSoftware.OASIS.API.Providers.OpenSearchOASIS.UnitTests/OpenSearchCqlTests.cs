using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.UnitTests;

[TestClass]
public class OpenSearchCqlTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed class Handler(Func<Guid,HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Guid> Selected = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get,request.Method);
            Assert.IsTrue(request.RequestUri!.AbsolutePath.StartsWith("/oasis_holons/_doc/"));
            Assert.AreEqual("?realtime=true&_source=true",request.RequestUri.Query);
            Assert.AreEqual("Basic",request.Headers.Authorization!.Scheme);
            var id = Guid.ParseExact(request.RequestUri.AbsolutePath.Split('/').Last(),"D");
            Selected.Add(id); return Task.FromResult(respond(id));
        }
    }
    private static HttpResponseMessage Response(string json,HttpStatusCode code=HttpStatusCode.OK)
        => new(code) { Content = new StringContent(json) };
    private static string Native(Guid id,Guid? storedId=null,Guid? nativeId=null,string index="oasis_holons",bool deleted=false)
        => JsonSerializer.Serialize(new { _index=index,_id=(nativeId??id).ToString("D"),found=true,
            _source=new Holon { Id=storedId??id,Name="OpenSearch teaching record",DeletedDate=deleted?DateTime.UtcNow:DateTime.MinValue } },Options);
    private static string Missing(Guid id) => JsonSerializer.Serialize(new { _index="oasis_holons",_id=id.ToString("D"),found=false });
    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.OpenSearchOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.OpenSearchOASIS,ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(OpenSearchOASIS provider,CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query,ProviderType.OpenSearchOASIS,new[] { new CosmicQueryTarget(ProviderType.OpenSearchOASIS,provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task ExactNativeKeysReturnThreeTwoAndPageAfterDeletedAndMissing()
    {
        var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var handler=new Handler(id=>id==ids[4]?Response(Missing(id),HttpStatusCode.NotFound):Response(Native(id,deleted:id==ids[3])));
        using var http=new HttpClient(handler);
        var provider=new OpenSearchOASIS("http://127.0.0.1:1","root","controlled",ids[0],http);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        var full=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids)));
        Assert.IsFalse(full.IsError,full.Message);
        CollectionAssert.AreEqual(ids.Take(3).ToArray(),full.Result.Select(row=>row.Id).ToArray());
        var subset=new[] {ids[0],ids[2]};
        var filtered=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids).Where(row=>subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError,filtered.Message);
        CollectionAssert.AreEqual(subset,filtered.Result.Select(row=>row.Id).ToArray());
        var page=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids).Skip(1).Result.Take(1).Result));
        Assert.IsFalse(page.IsError,page.Message);
        CollectionAssert.AreEqual(new[] {ids[1]},page.Result.Select(row=>row.Id).ToArray());
    }

    [DataTestMethod]
    [DataRow("unauthorized")]
    [DataRow("forbidden")]
    [DataRow("server_error")]
    [DataRow("missing_index")]
    [DataRow("wrong_index")]
    [DataRow("wrong_native_id")]
    [DataRow("wrong_source_id")]
    [DataRow("duplicate_native_id")]
    [DataRow("duplicate_source_id")]
    [DataRow("missing_source")]
    [DataRow("false_on_success")]
    [DataRow("true_on_missing")]
    [DataRow("source_on_missing")]
    [DataRow("malformed")]
    [DataRow("oversized")]
    public async Task NativeFailuresNeverPublishPartialRows(string fault)
    {
        var ids=new[] {Guid.NewGuid(),Guid.NewGuid()}.OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var handler=new Handler(id=> {
            if(id==ids[0]) return Response(Native(id));
            var identity="\"_index\":\"oasis_holons\",\"_id\":\""+id+"\"";
            return fault switch {
                "unauthorized"=>Response("{}",HttpStatusCode.Unauthorized),
                "forbidden"=>Response("{}",HttpStatusCode.Forbidden),
                "server_error"=>Response("{}",HttpStatusCode.InternalServerError),
                "missing_index"=>Response("{\"error\":{\"type\":\"index_not_found_exception\"}}",HttpStatusCode.NotFound),
                "wrong_index"=>Response(Native(id,index:"other_index")),
                "wrong_native_id"=>Response(Native(id,nativeId:Guid.NewGuid())),
                "wrong_source_id"=>Response(Native(id,storedId:Guid.NewGuid())),
                "duplicate_native_id"=>Response("{"+identity+",\"_id\":\""+id+"\",\"found\":true,\"_source\":{\"id\":\""+id+"\"}}"),
                "duplicate_source_id"=>Response("{"+identity+",\"found\":true,\"_source\":{\"id\":\""+id+"\",\"id\":\""+id+"\"}}"),
                "missing_source"=>Response("{"+identity+",\"found\":true}"),
                "false_on_success"=>Response(Missing(id)),
                "true_on_missing"=>Response(Native(id),HttpStatusCode.NotFound),
                "source_on_missing"=>Response("{"+identity+",\"found\":false,\"_source\":{}}",HttpStatusCode.NotFound),
                "malformed"=>Response("{oops"),
                _=>Response(new string('x',1024*1024+1))
            };
        });
        using var http=new HttpClient(handler);
        var provider=new OpenSearchOASIS("http://127.0.0.1:1","root","controlled",http) {IsProviderActivated=true};
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(ids)));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(fault=="oversized"?"CQL_QUERY_LIMIT":"CQL_QUERY_FAILED",result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("absent")]
    [DataRow("deleted")]
    [DataRow("denied")]
    public async Task ActivationVerifiesExistingLiveSourceWithoutProvisioning(string fault)
    {
        var id=Guid.NewGuid();
        var handler=new Handler(key=>fault switch {
            "absent"=>Response(Missing(key),HttpStatusCode.NotFound),
            "deleted"=>Response(Native(key,deleted:true)),_=>Response("{}",HttpStatusCode.Forbidden) });
        using var http=new HttpClient(handler);
        var provider=new OpenSearchOASIS("http://127.0.0.1:1","root","controlled",id,http) {IsProviderActivated=true};
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError); Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] {id},handler.Selected);
    }

    [TestMethod]
    public async Task MissingProbeInactiveHistoricalAndCancelledQueriesDoNotDispatch()
    {
        var handler=new Handler(_=>throw new AssertFailedException("Native request must not dispatch."));
        using var http=new HttpClient(handler);
        var provider=new OpenSearchOASIS("http://127.0.0.1:1","root","controlled",http);
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        var plan=Plan(provider,Query(new[] {Guid.NewGuid()}));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE",(await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        provider.IsProviderActivated=true;
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicQueryAsync(plan,cancellationToken:cancelled.Token)).ErrorCode);
        provider.IsVersionControlEnabled=true;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",(await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        Assert.AreEqual(0,handler.Selected.Count);
    }

    [TestMethod]
    public async Task AggregateTransportBudgetFailsWithoutPartialRows()
    {
        var handler=new Handler(id=>Response(Native(id)[..^1]+",\"padding\":\""+new string('x',950000)+"\"}"));
        using var http=new HttpClient(handler);
        var provider=new OpenSearchOASIS("http://127.0.0.1:1","root","controlled",http) {IsProviderActivated=true};
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,Query(Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray())));
        Assert.IsTrue(result.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);Assert.IsNull(result.Result);
    }
}
