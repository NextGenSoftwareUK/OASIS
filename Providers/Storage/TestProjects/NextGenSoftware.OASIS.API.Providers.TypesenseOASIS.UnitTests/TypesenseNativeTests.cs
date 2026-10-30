using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.UnitTests;

[TestClass]
public class TypesenseNativeTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> OnSend=(_,_)=>throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Calls++;return OnSend(request,token);}
    }
    private static HttpResponseMessage Json(object body,HttpStatusCode status=HttpStatusCode.OK)
        =>new(status){Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json")};

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("created")]
    [DataRow("http")]
    [DataRow("foreign")]
    [DataRow("oversized")]
    [DataRow("disconnect")]
    public async Task SaveRequiresNativeAcknowledgementBeforePublishingCallerIdentity(string mode)
    {
        var holon=new Holon{Name="Coffee"};
        var transport=new Transport{OnSend=async(request,token)=>{
            Assert.AreEqual(Guid.Empty,holon.Id);
            Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.TypesenseOASIS)==true);
            Assert.AreEqual(HttpMethod.Post,request.Method);
            Assert.AreEqual("/collections/oasis_holons/documents",request.RequestUri!.AbsolutePath);
            StringAssert.Contains(request.RequestUri.Query,"action=upsert&dirty_values=reject");
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var id=body.RootElement.GetProperty("id").GetGuid();Assert.AreNotEqual(Guid.Empty,id);
            if(mode=="disconnect")throw new HttpRequestException("Unknown outcome");
            if(mode=="oversized")return Json(new {padding=new string('x',4*1024*1024+1)});
            return Json(new{id=mode=="foreign"?Guid.NewGuid():id},mode=="http"?HttpStatusCode.BadRequest:mode=="created"?HttpStatusCode.Created:HttpStatusCode.OK);
        }};
        using var http=new HttpClient(transport);var provider=new TypesenseOASIS(http,"http://native.invalid","key");
        var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        var success=mode is "success" or "created";
        Assert.AreEqual(success,!result.IsError,result.Message);Assert.AreEqual(success,result.IsSaved);
        Assert.AreEqual(success?1:0,result.SavedCount);Assert.AreEqual(1,transport.Calls);
        if(success)Assert.AreEqual(holon.Id.ToString("D"),holon.ProviderUniqueStorageKey[ProviderType.TypesenseOASIS]);
        else {Assert.AreEqual(Guid.Empty,holon.Id);Assert.IsNull(result.Result);}
    }

    [DataTestMethod]
    [DataRow("existing")]
    [DataRow("create")]
    [DataRow("http")]
    [DataRow("schema")]
    [DataRow("failed-create")]
    [DataRow("wrong-create")]
    public async Task OnlyConfirmedAbsencePermitsCreationAndActiveStateNeedsValidSchema(string mode)
    {
        var posts=0;
        var transport=new Transport{OnSend=async(request,token)=>{
            if(request.Method==HttpMethod.Get){
                var name=request.RequestUri!.AbsolutePath.Split('/').Last();
                if(mode is "create" or "failed-create" or "wrong-create")return Json(new{},HttpStatusCode.NotFound);
                return Json(new {name,fields=new[]{new{name=".*",type=mode=="schema"?"string":"auto"}}},mode=="http"?HttpStatusCode.Unauthorized:HttpStatusCode.OK);
            }
            posts++;using var payload=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var expected=payload.RootElement.GetProperty("name").GetString();
            return Json(new {name=mode=="wrong-create"?"foreign":expected,fields=new[]{new{name=".*",type="auto"}}},mode=="failed-create"?HttpStatusCode.InternalServerError:HttpStatusCode.Created);
        }};
        using var http=new HttpClient(transport);var provider=new TypesenseOASIS(http,"http://native.invalid","key"){IsProviderActivated=true};
        var result=await provider.ActivateProviderAsync();
        Assert.AreEqual(mode is "existing" or "create",!result.IsError,result.Message);
        Assert.AreEqual(!result.IsError,provider.IsProviderActivated);
        Assert.AreEqual(mode=="create"?3:mode is "failed-create" or "wrong-create"?1:0,posts);
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("foreign")]
    [DataRow("duplicate")]
    [DataRow("cutoff")]
    [DataRow("truncated")]
    [DataRow("page")]
    [DataRow("http")]
    [DataRow("budget")]
    public async Task BoundedKeyQueryRejectsTruncatedOrForeignNativeRowsBeforePublishing(string mode)
    {
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()}.Order().ToArray();
        var transport=new Transport{OnSend=(request,token)=>{
            Assert.AreEqual(HttpMethod.Get,request.Method);Assert.IsNull(request.Content);
            Assert.AreEqual("/collections/oasis_holons/documents/search",request.RequestUri!.AbsolutePath);
            var query=Uri.UnescapeDataString(request.RequestUri.Query);
            StringAssert.Contains(query,"filter_by=id:=[`"+ids[0].ToString("D")+"`,`"+ids[1].ToString("D")+"`]");
            StringAssert.Contains(query,"per_page=3&page=1&exhaustive_search=true&enable_analytics=false");
            if(mode=="budget")return Task.FromResult(Json(new {padding=new string('x',4*1024*1024+1)}));
            var rows=new[]{new Holon{Id=ids[1],CreatedByAvatarId=actor},new Holon{Id=mode=="foreign"?Guid.NewGuid():mode=="duplicate"?ids[1]:ids[0],CreatedByAvatarId=actor}};
            var json=JsonSerializer.Serialize(new {found=mode=="truncated"?3:2,page=mode=="page"?2:1,search_cutoff=mode=="cutoff",hits=rows.Select(row=>new {document=row})},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
            return Task.FromResult(new HttpResponseMessage(mode=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)});
        }};
        using var http=new HttpClient(transport);var provider=new TypesenseOASIS(http,"http://native.invalid","key"){IsProviderActivated=true};
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TypesenseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.TypesenseOASIS,ids).Result).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.TypesenseOASIS,new[]{new CosmicQueryTarget(ProviderType.TypesenseOASIS,provider.QueryCapabilities)});
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual(mode=="success",!result.IsError,result.Message);
        if(mode=="success")CollectionAssert.AreEqual(ids,result.Result.Select(h=>h.Id).ToArray());
        else {Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchCountsOnlyAcknowledgedWritesAndHonoursStopContinue(bool keepGoing)
    {
        var writes=0;var transport=new Transport{OnSend=async(request,token)=>{
            writes++;using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            return Json(new{id=body.RootElement.GetProperty("id").GetGuid()},writes==1?HttpStatusCode.BadRequest:HttpStatusCode.OK);
        }};
        using var http=new HttpClient(transport);var provider=new TypesenseOASIS(http,"http://native.invalid","key");
        var records=new[]{new Holon(),new Holon()};var result=await provider.SaveHolonsAsync(records,saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.AreEqual(1,result.ErrorCount);Assert.AreEqual(keepGoing?2:1,writes);
        Assert.AreEqual(keepGoing?1:0,result.SavedCount);Assert.AreEqual(keepGoing,result.IsSaved);Assert.AreEqual(Guid.Empty,records[0].Id);
    }
    [TestMethod]
    public async Task EmptyCatalogueReturnsWithoutNativeSearchOrAccidentalFullScan()
    {
        var transport=new Transport();using var http=new HttpClient(transport);
        var provider=new TypesenseOASIS(http,"http://native.invalid","key"){IsProviderActivated=true};
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TypesenseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.TypesenseOASIS,Array.Empty<Guid>()).Result).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.TypesenseOASIS,new[]{new CosmicQueryTarget(ProviderType.TypesenseOASIS,provider.QueryCapabilities)});
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);Assert.AreEqual(0,transport.Calls);
    }

    [TestMethod]
    public async Task UnsupportedHistoryCannotMutateCallerOrSubmitNativeWrite()
    {
        var transport=new Transport();using var http=new HttpClient(transport);
        var provider=new TypesenseOASIS(http,"http://native.invalid","key"){IsVersionControlEnabled=true};
        var holon=new Holon();var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(Guid.Empty,holon.Id);Assert.AreEqual(0,transport.Calls);
    }

}
