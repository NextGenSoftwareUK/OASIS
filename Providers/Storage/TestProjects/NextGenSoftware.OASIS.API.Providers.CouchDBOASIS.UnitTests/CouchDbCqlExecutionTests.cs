using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.UnitTests;

[TestClass]
public class CouchDbCqlExecutionTests
{
    private sealed class Handler:HttpMessageHandler
    {
        public string Mode="valid"; public List<Guid> Reads=new();
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get,request.Method,"CQL cannot write or scan");
            Assert.AreEqual("?conflicts=true",request.RequestUri!.Query);
            var id=Guid.Parse(request.RequestUri.Segments.Last());Reads.Add(id);
            if(Mode=="cancel"){Started.SetResult();await Task.Delay(Timeout.Infinite,token);}
            if(Mode=="missing")return new(HttpStatusCode.NotFound);
            if(Mode=="http" || Mode=="lateHttp" && Reads.Count==2)return new(HttpStatusCode.ServiceUnavailable);
            var row=new Holon{Id=Mode=="wrongHolon"?Guid.NewGuid():id,Name="coffee"};
            if(Mode=="deleted")row.DeletedDate=DateTime.UtcNow;
            var body=JsonNode.Parse(JsonSerializer.Serialize(row))!.AsObject();
            body["_id"]=Mode=="wrongNative"?Guid.NewGuid().ToString("D"):id.ToString("D");
            body["_rev"]=Mode=="missingRevision"?"":"1-a";
            if(Mode=="conflict")body["_conflicts"]=new JsonArray("2-other");
            if(Mode=="oversized")body["padding"]=new string('x',4*1024*1024);
            return new(HttpStatusCode.OK){Content=new StringContent(Mode=="malformed"?"!":body.ToJsonString(),Encoding.UTF8,"application/json")};
        }
    }
    private static (CouchDBOASIS P,Handler H,CosmicQuery<Holon> Q,Guid[] Ids) Setup(int count=1)
    {
        var h=new Handler();var p=new CouchDBOASIS("http://127.0.0.1:1"){IsProviderActivated=true};
        var field=typeof(CouchDBOASIS).GetField("_http",BindingFlags.NonPublic|BindingFlags.Instance)!;
        ((HttpClient)field.GetValue(p)!).Dispose();field.SetValue(p,new HttpClient(h));
        var ids=Enumerable.Range(0,count).Select(_=>Guid.NewGuid()).ToArray();
        var q=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CouchDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.CouchDBOASIS,ids).Result).Result;
        return(p,h,q,ids);
    }
    private static CosmicQueryPlan<Holon> Plan(CouchDBOASIS p,CosmicQuery<Holon> q)
        => CosmicQueryPlanner.Plan(q,ProviderType.CouchDBOASIS,new[]{new CosmicQueryTarget(ProviderType.CouchDBOASIS,p.QueryCapabilities)}).Result;
    [TestMethod]
    public async Task ThreeRecordCatalogueSelectsTwoNativeReads()
    {
        var(p,h,q,ids)=Setup(3);var chosen=ids.Take(2).ToArray();
        var result=await p.ExecuteCosmicQueryAsync(Plan(p,q.Where(r=>chosen.Contains(r.Id)).Result));
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(2,result.LoadedCount);
        CollectionAssert.AreEquivalent(chosen,h.Reads.ToArray());CollectionAssert.AreEquivalent(chosen,result.Result.Select(r=>r.Id).ToArray());
        Assert.IsTrue(result.Result.All(r=>!r.Children.Any()));
    }
    [DataTestMethod][DataRow("missing")][DataRow("deleted")]
    public async Task MissingAndDeletedNativeRecordsSkip(string mode)
    {
        var(p,h,q,_)=Setup();h.Mode=mode;var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);
    }
    [DataTestMethod][DataRow("http")][DataRow("lateHttp")][DataRow("malformed")][DataRow("wrongNative")][DataRow("wrongHolon")][DataRow("missingRevision")]
    public async Task FailedOrMismatchedAcknowledgementReturnsNoPartialRows(string mode)
    {
        var(p,h,q,_)=Setup(2);h.Mode=mode;var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.AreEqual("CQL_QUERY_FAILED",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task ConflictingRevisionsReject()
    {
        var(p,h,q,_)=Setup();h.Mode="conflict";var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.AreEqual("CQL_REVISION_AMBIGUOUS",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task StreamBudgetEnforced()
    {
        var(p,h,q,_)=Setup();h.Mode="oversized";var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task ResultBudgetEnforced()
    {
        var(p,h,q,_)=Setup(2);var result=await p.ExecuteCosmicQueryAsync(Plan(p,q),maximumResults:1);
        Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task InFlightHttpCancellationPublishesNoRows()
    {
        var(p,h,q,_)=Setup();h.Mode="cancel";using var token=new CancellationTokenSource();
        var pending=p.ExecuteCosmicQueryAsync(Plan(p,q),cancellationToken:token.Token);
        await h.Started.Task;token.Cancel();var result=await pending;
        Assert.AreEqual("CQL_QUERY_CANCELLED",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task UnsupportedVersionModeRejectsBeforeHttp()
    {
        var(p,h,q,_)=Setup();p.IsVersionControlEnabled=true;var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.AreEqual(0,h.Reads.Count);
    }
    [TestMethod]
    public async Task PagingAppliesAfterCompleteNativeSelection()
    {
        var(p,h,q,_)=Setup(3);var result=await p.ExecuteCosmicQueryAsync(Plan(p,q.Skip(1).Take(1).Result));
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(1,result.Result.Count);Assert.AreEqual(3,h.Reads.Count);
    }
}
