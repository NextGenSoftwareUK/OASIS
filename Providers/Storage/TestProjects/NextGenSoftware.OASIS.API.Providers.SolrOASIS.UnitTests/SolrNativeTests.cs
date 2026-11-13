using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS.UnitTests;

[TestClass]
public class SolrNativeTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> OnSend=(_,_)=>throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Calls++;return OnSend(request,token);}
    }
    private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)
        =>new(status){Content=new StringContent(JsonSerializer.Serialize(value),Encoding.UTF8,"application/json")};

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("foreign")]
    [DataRow("version")]
    [DataRow("status")]
    [DataRow("partial")]
    [DataRow("http")]
    [DataRow("oversized")]
    [DataRow("disconnect")]
    public async Task OneHardCommittedWriteRequiresExactNativeAcknowledgementBeforeCallerPublication(string mode)
    {
        var holon=new Holon{Name="Coffee",MetaData=new(){["precise"]=1234567890.123456789012345678m,["nested"]=new[]{"one","two"}}};
        var transport=new Transport{OnSend=async(request,token)=>{
            Assert.AreEqual(Guid.Empty,holon.Id);Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.SolrOASIS)==true);
            Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("/solr/oasis_holons/update/json/docs",request.RequestUri!.AbsolutePath);
            StringAssert.Contains(request.RequestUri.Query,"commit=true&waitSearcher=true&versions=true");
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var document=body.RootElement[0];var id=document.GetProperty("id").GetGuid();
            Assert.IsFalse(document.TryGetProperty("metaData",out _),"Nested holon data must not be schema-inferred.");
            using var payload=JsonDocument.Parse(document.GetProperty("data_json").GetString()!);
            Assert.AreEqual(id,payload.RootElement.GetProperty("id").GetGuid());
            Assert.AreEqual(1234567890.123456789012345678m,payload.RootElement.GetProperty("metaData").GetProperty("precise").GetDecimal());
            if(mode=="disconnect")throw new HttpRequestException("Unknown write outcome");
            if(mode=="oversized")return Json(new{padding=new string('x',65537)});
            return Json(new{responseHeader=new{status=mode=="status"?1:0,partialResults=mode=="partial"},adds=new object[]{mode=="foreign"?Guid.NewGuid().ToString():id.ToString("D"),mode=="version"?0:long.MaxValue}},mode=="http"?HttpStatusCode.BadRequest:HttpStatusCode.OK);
        }};
        using var http=new HttpClient(transport);var provider=new SolrOASIS(http,"http://native.invalid");
        var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.AreEqual(mode=="success",!result.IsError,result.Message);Assert.AreEqual(mode=="success",result.IsSaved);
        Assert.AreEqual(mode=="success"?1:0,result.SavedCount);Assert.AreEqual(1,transport.Calls);
        if(mode=="success")Assert.AreEqual(holon.Id.ToString("D"),holon.ProviderUniqueStorageKey[ProviderType.SolrOASIS]);
        else {Assert.AreEqual(Guid.Empty,holon.Id);Assert.IsNull(result.Result);}
    }

    private static object Schema(string mode)
    {
        var fields=new List<object>();
        void Add(string name,string type,bool indexed,bool stored,bool dv)
            =>fields.Add(new{name,type,indexed,stored,docValues=dv,multiValued=mode=="multi"&&name=="data_json"});
        foreach(var name in new[]{"id","name","username","email","parentHolonId"})Add(name,"string",true,true,false);
        Add("data_json","string",mode=="indexed",true,false);Add("holonType","pint",true,true,false);
        Add("isDeleted","boolean",true,true,false);Add("_version_","plong",false,false,true);
        return new{responseHeader=new{status=0},schema=new{uniqueKey=mode=="key"?"foreign":"id",fields,
            fieldTypes=new[]{new{name="string",@class=mode=="type"?"solr.TextField":"solr.StrField"},new{name="pint",@class="solr.IntPointField"},new{name="plong",@class="solr.LongPointField"},new{name="boolean",@class="solr.BoolField"}}}};
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("key")]
    [DataRow("indexed")]
    [DataRow("multi")]
    [DataRow("type")]
    [DataRow("missing")]
    [DataRow("http")]
    public async Task ActivationChecksExplicitSchemaWithoutGuessingProvisioningMode(string mode)
    {
        var transport=new Transport{OnSend=(request,token)=>{
            Assert.AreEqual(HttpMethod.Get,request.Method);StringAssert.EndsWith(request.RequestUri!.AbsolutePath,"/schema");
            StringAssert.Contains(request.RequestUri.Query,"showDefaults=true");
            return Task.FromResult(Json(Schema(mode),mode=="missing"?HttpStatusCode.NotFound:mode=="http"?HttpStatusCode.Unauthorized:HttpStatusCode.OK));
        }};
        using var http=new HttpClient(transport);var provider=new SolrOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var result=await provider.ActivateProviderAsync();Assert.AreEqual(mode=="success",!result.IsError,result.Message);
        Assert.AreEqual(!result.IsError,provider.IsProviderActivated);Assert.AreEqual(mode=="success"?3:1,transport.Calls);
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("foreign")]
    [DataRow("payload")]
    [DataRow("duplicate")]
    [DataRow("partial")]
    [DataRow("omitted")]
    [DataRow("truncated")]
    [DataRow("start")]
    [DataRow("version")]
    [DataRow("http")]
    [DataRow("budget")]
    public async Task RealTimeKeyGetRejectsIncompleteOrInconsistentNativeRows(string mode)
    {
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()}.Order().ToArray();
        var transport=new Transport{OnSend=(request,token)=>{
            Assert.AreEqual(HttpMethod.Get,request.Method);Assert.AreEqual("/solr/oasis_holons/get",request.RequestUri!.AbsolutePath);
            var query=Uri.UnescapeDataString(request.RequestUri.Query);StringAssert.Contains(query,"ids="+string.Join(',',ids.Select(i=>i.ToString("D"))));
            StringAssert.Contains(query,"fl=id,data_json,_version_");StringAssert.Contains(query,"shards.tolerant=false");
            if(mode=="budget")return Task.FromResult(Json(new{padding=new string('x',4*1024*1024+1)}));
            object Wrap(Guid id,bool first)=>new{id=id.ToString("D"),_version_=mode=="version"?0:long.MaxValue,
                data_json=JsonSerializer.Serialize(new Holon{Id=mode=="payload"&&first?Guid.NewGuid():id,CreatedByAvatarId=actor},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase})};
            var docs=new[]{Wrap(ids[1],false),Wrap(mode=="foreign"?Guid.NewGuid():mode=="duplicate"?ids[1]:ids[0],true)};
            return Task.FromResult(Json(new{responseHeader=new{status=0,partialResults=(object)(mode=="omitted"?"omitted":mode=="partial")},
                response=new{start=mode=="start"?1:0,numFound=mode=="truncated"?3:2,docs}},mode=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK));
        }};
        using var http=new HttpClient(transport);var provider=new SolrOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolrOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.SolrOASIS,ids).Result).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.SolrOASIS,new[]{new CosmicQueryTarget(ProviderType.SolrOASIS,provider.QueryCapabilities)});
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);Assert.AreEqual(mode=="success",!result.IsError,result.Message);
        if(mode=="success")CollectionAssert.AreEqual(ids,result.Result.Select(h=>h.Id).ToArray());
        else {Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchStopContinueReportsOnlyAcknowledgedSaves(bool keepGoing)
    {
        var writes=0;var transport=new Transport{OnSend=async(request,token)=>{
            writes++;using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            return Json(new{responseHeader=new{status=0},adds=new object[]{body.RootElement[0].GetProperty("id").GetString()!,long.MaxValue}},writes==1?HttpStatusCode.BadRequest:HttpStatusCode.OK);
        }};
        using var http=new HttpClient(transport);var provider=new SolrOASIS(http,"http://native.invalid");
        var records=new[]{new Holon(),new Holon()};var result=await provider.SaveHolonsAsync(records,saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.AreEqual(1,result.ErrorCount);Assert.AreEqual(keepGoing?2:1,writes);Assert.AreEqual(keepGoing?1:0,result.SavedCount);Assert.AreEqual(Guid.Empty,records[0].Id);
    }
    [DataTestMethod]
    [DataRow("success")]
    [DataRow("foreign")]
    [DataRow("payload")]
    [DataRow("missing")]
    [DataRow("http")]
    public async Task GenericPointReadUsesTheSameCanonicalPayloadWithoutRawSchemaFallback(string mode)
    {
        var id=Guid.NewGuid();var transport=new Transport{OnSend=(request,token)=>{
            Assert.AreEqual(HttpMethod.Get,request.Method);StringAssert.Contains(request.RequestUri!.Query,"id="+id.ToString("D"));
            var json=JsonSerializer.Serialize(new Holon{Id=mode=="payload"?Guid.NewGuid():id,Name="Coffee",MetaData=new(){["nested"]=new[]{"one","two"}}},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
            return Task.FromResult(Json(new{responseHeader=new{status=0},doc=mode=="missing"?null:new{id=mode=="foreign"?Guid.NewGuid().ToString("D"):id.ToString("D"),data_json=json}},mode=="http"?HttpStatusCode.Unauthorized:HttpStatusCode.OK));
        }};
        using var http=new HttpClient(transport);var provider=new SolrOASIS(http,"http://native.invalid");
        var result=await provider.LoadHolonAsync(id,loadChildren:false);
        Assert.AreEqual(mode=="success",!result.IsError,result.Message);
        if(mode=="success"){Assert.AreEqual(id,result.Result.Id);Assert.AreEqual("Coffee",result.Result.Name);Assert.IsTrue(result.Result.MetaData.ContainsKey("nested"));}
        else Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task EmptyCatalogueAndUnsupportedHistoryDoNotPerformNativeIO()
    {
        var transport=new Transport();using var http=new HttpClient(transport);
        var provider=new SolrOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolrOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.SolrOASIS,Array.Empty<Guid>()).Result).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.SolrOASIS,new[]{new CosmicQueryTarget(ProviderType.SolrOASIS,provider.QueryCapabilities)});
        var empty=await provider.ExecuteCosmicQueryAsync(plan.Result);Assert.IsFalse(empty.IsError,empty.Message);Assert.AreEqual(0,empty.Result.Count);
        provider.IsVersionControlEnabled=true;var holon=new Holon();var write=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.IsTrue(write.IsError);Assert.IsFalse(write.IsSaved);Assert.AreEqual(Guid.Empty,holon.Id);Assert.AreEqual(0,transport.Calls);
    }

}
