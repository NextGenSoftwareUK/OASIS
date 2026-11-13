using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.UnitTests;

[TestClass]
public class MeilisearchNativeTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_,_) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }

    private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("failed")]
    [DataRow("canceled")]
    [DataRow("foreign")]
    [DataRow("index")]
    [DataRow("type")]
    [DataRow("error")]
    [DataRow("count")]
    [DataRow("http")]
    [DataRow("oversized")]
    [DataRow("disconnect")]
    public async Task SavePublishesIdentityOnlyAfterMatchingTerminalTaskSuccess(string mode)
    {
        var holon = new Holon { Name = "Original" };
        var transport = new Transport();
        var writes = 0;
        var polls = 0;
        transport.OnSend = async (request, token) => {
            // Identity must remain local until a terminal acknowledgement arrives.
            Assert.AreEqual(Guid.Empty, holon.Id);
            Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.MeilisearchOASIS) == true);
            if (request.Method == HttpMethod.Post)
            {
                writes++;
                Assert.AreEqual("/indexes/oasis_holons/documents", request.RequestUri!.AbsolutePath);
                using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                Assert.AreNotEqual(Guid.Empty, payload.RootElement[0].GetProperty("id").GetGuid());
                return Json(new { taskUid = 0, indexUid = "oasis_holons", type = "documentAdditionOrUpdate", status = "enqueued" }, HttpStatusCode.Accepted);
            }
            polls++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("/tasks/0", request.RequestUri!.AbsolutePath);
            if (mode == "disconnect") throw new HttpRequestException("Lost observation");
            if (mode == "http") return Json(new { }, HttpStatusCode.ServiceUnavailable);
            if (mode == "oversized") return Json(new { padding = new string('x',65537) });
            return Json(new { uid = mode == "foreign" ? 1 : 0,
                indexUid = mode == "index" ? "other" : "oasis_holons",
                type = mode == "type" ? "indexCreation" : "documentAdditionOrUpdate",
                status = mode is "failed" or "canceled" ? mode : polls == 1 ? "processing" : "succeeded",
                error = mode == "error" ? new { code = "rejected" } : null,
                details = new { receivedDocuments = 1, indexedDocuments = mode == "count" ? 0 : 1 } });
        };
        using var http = new HttpClient(transport);
        var provider = new MeilisearchOASIS(http,"http://native.invalid");
        var result = await provider.SaveHolonAsync(holon, saveChildren:false);
        Assert.AreEqual(1,writes,"Never resubmit a queued or failed operation.");
        Assert.AreEqual(mode != "success",result.IsError,result.Message);
        Assert.AreEqual(mode == "success",result.IsSaved);
        Assert.AreEqual(mode == "success" ? 1 : 0,result.SavedCount);
        if(mode == "success") { Assert.AreNotEqual(Guid.Empty,holon.Id); Assert.AreEqual(holon.Id.ToString("D"),holon.ProviderUniqueStorageKey[ProviderType.MeilisearchOASIS]); }
        else { Assert.AreEqual(Guid.Empty,holon.Id); Assert.IsNull(result.Result); StringAssert.Contains(result.Message,"task 0"); }
    }

    [DataTestMethod]
    [DataRow("existing")]
    [DataRow("create")]
    [DataRow("key")]
    [DataRow("http")]
    [DataRow("failed")]
    public async Task ActivationRequiresVerifiedSchemaAndCompletedCreation(string mode)
    {
        var transport = new Transport();
        var created = new HashSet<string>();
        var posts = 0;
        transport.OnSend = async (request, token) => {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post)
            {
                posts++;
                using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                var uid = payload.RootElement.GetProperty("uid").GetString()!;
                created.Add(uid);
                return Json(new { taskUid=0,indexUid=uid,type="indexCreation",status="enqueued" },HttpStatusCode.Accepted);
            }
            if(path == "/tasks/0") return Json(new { uid=0,indexUid=created.Last(),type="indexCreation",status=mode=="failed"?"failed":"succeeded",error=(object?)null });
            var index=path.Split('/').Last();
            if(mode=="http") return Json(new {},HttpStatusCode.Unauthorized);
            if(mode is "create" or "failed" && !created.Contains(index)) return Json(new {code="index_not_found"},HttpStatusCode.NotFound);
            return Json(new {uid=index,primaryKey=mode=="key"?"wrong":"id"});
        };
        using var http = new HttpClient(transport);
        var provider = new MeilisearchOASIS(http,"http://native.invalid") {IsProviderActivated=true};
        var result=await provider.ActivateProviderAsync();
        Assert.AreEqual(mode is "existing" or "create",!result.IsError,result.Message);
        Assert.AreEqual(!result.IsError,provider.IsProviderActivated);
        Assert.AreEqual(mode=="create"?3:mode=="failed"?1:0,posts);
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("foreign")]
    [DataRow("duplicate")]
    [DataRow("total")]
    [DataRow("budget")]
    [DataRow("http")]
    public async Task NativeKeyFetchValidatesWholeResponseBeforePublishing(string mode)
    {
        var actor=Guid.NewGuid(); var ids=new[]{Guid.NewGuid(),Guid.NewGuid()}.Order().ToArray();
        var transport=new Transport {OnSend=async (request,token)=>{
            Assert.AreEqual(HttpMethod.Post,request.Method);
            Assert.AreEqual("/indexes/oasis_holons/documents/fetch",request.RequestUri!.AbsolutePath);
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            CollectionAssert.AreEquivalent(ids.Select(i=>i.ToString("D")).ToArray(),body.RootElement.GetProperty("ids").EnumerateArray().Select(e=>e.GetString()).ToArray());
            if(mode=="budget") return Json(new {padding=new string('x',4*1024*1024+1)});
            var rows=new[]{new Holon{Id=ids[1],Name="Second",CreatedByAvatarId=actor},new Holon{Id=mode=="foreign"?Guid.NewGuid():mode=="duplicate"?ids[1]:ids[0],Name="First",CreatedByAvatarId=actor}};
            var json=JsonSerializer.Serialize(new {results=rows,offset=0,limit=3,total=mode=="total"?3:2},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
            return new HttpResponseMessage(mode=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)};
        }};
        using var http=new HttpClient(transport);
        var provider=new MeilisearchOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MeilisearchOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.MeilisearchOASIS,ids).Result).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.MeilisearchOASIS,new[]{new CosmicQueryTarget(ProviderType.MeilisearchOASIS,provider.QueryCapabilities)});
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual(mode!="success",result.IsError,result.Message);
        if(mode=="success") CollectionAssert.AreEqual(ids,result.Result.Select(h=>h.Id).ToArray());
        else {Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
    }
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchHonoursStopContinueAndAcknowledgedCounts(bool keepGoing)
    {
        var submissions=0;
        var transport=new Transport {OnSend=(request,token)=>{
            if(request.Method==HttpMethod.Post) { submissions++; return Task.FromResult(Json(new {taskUid=submissions,indexUid="oasis_holons",type="documentAdditionOrUpdate",status="enqueued"},HttpStatusCode.Accepted)); }
            return Task.FromResult(Json(new {uid=submissions,indexUid="oasis_holons",type="documentAdditionOrUpdate",status=submissions==1?"failed":"succeeded",error=(object?)null,details=new {receivedDocuments=1,indexedDocuments=1}}));
        }};
        using var http=new HttpClient(transport);
        var provider=new MeilisearchOASIS(http,"http://native.invalid");
        var records=new[]{new Holon(),new Holon()};
        var result=await provider.SaveHolonsAsync(records,saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError); Assert.AreEqual(1,result.ErrorCount);
        Assert.AreEqual(keepGoing?2:1,submissions); Assert.AreEqual(keepGoing?1:0,result.SavedCount);
        Assert.AreEqual(keepGoing,result.IsSaved); Assert.AreEqual(Guid.Empty,records[0].Id);
        if(keepGoing) Assert.AreNotEqual(Guid.Empty,records[1].Id);
    }

    [TestMethod]
    public async Task UnsupportedHistoryRefusesBeforeNativeWrite()
    {
        var transport=new Transport(); using var http=new HttpClient(transport);
        var provider=new MeilisearchOASIS(http,"http://native.invalid"){IsVersionControlEnabled=true};
        var holon=new Holon(); var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(Guid.Empty,holon.Id); Assert.AreEqual(0,transport.Calls);
    }

}
