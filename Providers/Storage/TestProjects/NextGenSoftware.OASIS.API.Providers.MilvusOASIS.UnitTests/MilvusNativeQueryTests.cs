using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests;
[TestClass]
public class MilvusNativeQueryTests
{
    private sealed class Transport:HttpMessageHandler
    {
        public Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> OnSend=(_,_)=>throw new InvalidOperationException();public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;return OnSend(request,token);}
    }
    private static CosmicQueryPlan<Holon> Plan(MilvusOASIS provider,Guid actor,Guid[] ids,bool paging=false)
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MilvusOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.MilvusOASIS,ids).Result).Result;
        if(paging)query=query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query,ProviderType.MilvusOASIS,new[]{new CosmicQueryTarget(ProviderType.MilvusOASIS,provider.QueryCapabilities)}).Result;
    }
    [DataTestMethod]
    [DataRow("success")][DataRow("paging")][DataRow("code")][DataRow("foreign")][DataRow("payload")][DataRow("duplicate")][DataRow("shape")][DataRow("http")][DataRow("envelope")][DataRow("docsize")][DataRow("empty")][DataRow("budget")]
    public async Task OneStrongNativeKeyQueryValidatesAllRowsBeforePublishing(string mode)
    {
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()}.OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var transport=new Transport{OnSend=async(request,token)=>{
            Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("/v2/vectordb/entities/query",request.RequestUri!.AbsolutePath);
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));var root=body.RootElement;
            Assert.AreEqual("oasis_holons",root.GetProperty("collectionName").GetString());Assert.AreEqual("Strong",root.GetProperty("consistencyLevel").GetString());
            Assert.AreEqual("id in "+JsonSerializer.Serialize(ids.Select(id=>id.ToString("D")).ToArray()),root.GetProperty("filter").GetString());
            Assert.AreEqual(3,root.GetProperty("limit").GetInt32());Assert.AreEqual(0,root.GetProperty("offset").GetInt32());
            CollectionAssert.AreEqual(new[]{"id","doc"},root.GetProperty("outputFields").EnumerateArray().Select(e=>e.GetString()).ToArray());
            object Row(Guid id,bool first)=>new{id=id.ToString("D"),doc=mode=="docsize"?new string('x',65536):JsonSerializer.Serialize(new Holon{Id=mode=="payload"&&first?Guid.NewGuid():id,CreatedByAvatarId=actor,Name=first?"first":"second"})};
            var rows=new[]{Row(ids[1],false),Row(mode=="foreign"?Guid.NewGuid():mode=="duplicate"?ids[1]:ids[0],true)};
            var json=mode=="envelope"?JsonSerializer.Serialize(new{padding=new string('x',4*1024*1024+1)}):JsonSerializer.Serialize(new{code=mode=="code"?1:0,data=mode=="shape"?(object)new{}:mode=="empty"?Array.Empty<object>():rows});
            return new HttpResponseMessage(mode=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)};
        }};
        using var http=new HttpClient(transport);var provider=new MilvusOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,actor,ids,mode=="paging"),maximumResults:mode=="budget"?1:100);
        var success=mode is "success" or "paging" or "empty";Assert.AreEqual(success,!result.IsError,result.Message);Assert.AreEqual(1,transport.Calls);
        if(mode=="success")CollectionAssert.AreEqual(ids,result.Result.Select(h=>h.Id).ToArray());
        else if(mode=="paging")Assert.AreEqual(ids[1],result.Result.Single().Id);
        else if(mode=="empty")Assert.AreEqual(0,result.Result.Count);
        else {Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
    }
    [TestMethod]
    public async Task EmptyCatalogueAndPreCancellationDoNotQueryNativeStorage()
    {
        var transport=new Transport();using var http=new HttpClient(transport);var provider=new MilvusOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var empty=await provider.ExecuteCosmicQueryAsync(Plan(provider,Guid.NewGuid(),Array.Empty<Guid>()));Assert.IsFalse(empty.IsError,empty.Message);Assert.AreEqual(0,empty.Result.Count);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var stopped=await provider.ExecuteCosmicQueryAsync(Plan(provider,Guid.NewGuid(),new[]{Guid.NewGuid()}),cancellationToken:cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED",stopped.ErrorCode);Assert.AreEqual(0,transport.Calls);
    }
}
