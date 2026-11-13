using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests;

[TestClass]
public class MilvusSaveOwnershipTests
{
    private sealed class Coffee : Holon {public string Category {get;set;}="coffee";}
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
    [DataRow("code")]
    [DataRow("count")]
    [DataRow("foreign")]
    [DataRow("empty")]
    [DataRow("numeric")]
    [DataRow("http")]
    [DataRow("oversized")]
    [DataRow("disconnect")]
    public async Task NativeCodeCountAndIdMustAcknowledgeBeforeSavePublication(string mode)
    {
        var holon=new Coffee{Name="Espresso",MetaData=new(){["precise"]=1234567890.123456789012345678m}};
        var transport=new Transport{OnSend=async(request,token)=>{
            Assert.AreEqual(Guid.Empty,holon.Id);Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.MilvusOASIS)==true);
            Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("/v2/vectordb/entities/upsert",request.RequestUri!.AbsolutePath);
            Assert.AreEqual("15",request.Headers.GetValues("Request-Timeout").Single());
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("oasis_holons",body.RootElement.GetProperty("collectionName").GetString());
            var row=body.RootElement.GetProperty("data")[0];var id=row.GetProperty("id").GetGuid();
            using var payload=JsonDocument.Parse(row.GetProperty("doc").GetString()!);
            Assert.AreEqual(id,payload.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual("coffee",payload.RootElement.GetProperty("Category").GetString());
            Assert.AreEqual(1234567890.123456789012345678m,payload.RootElement.GetProperty("MetaData").GetProperty("precise").GetDecimal());
            if(mode=="disconnect")throw new HttpRequestException("Unknown outcome");
            if(mode=="oversized")return Json(new{padding=new string('x',65537)});
            var ids=mode=="empty"?Array.Empty<object>():new object[]{mode=="numeric"?1:mode=="foreign"?Guid.NewGuid().ToString("D"):id.ToString("D")};
            return Json(new{code=mode=="code"?1800:0,data=new{upsertCount=mode=="count"?0:1,upsertIds=ids}},mode=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK);
        }};
        using var http=new HttpClient(transport);var provider=new MilvusOASIS(http,"http://native.invalid");
        var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.AreEqual(mode=="success",!result.IsError,result.Message);Assert.AreEqual(mode=="success",result.IsSaved);Assert.AreEqual(mode=="success"?1:0,result.SavedCount);
        Assert.AreEqual(1,transport.Calls,"No retry after unknown outcome.");
        if(mode=="success")Assert.AreEqual(holon.Id.ToString("D"),holon.ProviderUniqueStorageKey[ProviderType.MilvusOASIS]);
        else {Assert.AreEqual(Guid.Empty,holon.Id);Assert.IsNull(result.Result);Assert.IsNotNull(result.Exception);}
    }

    [TestMethod]
    public async Task FailedExistingWritePreservesAllCallerKeysAndIdentity()
    {
        var id=Guid.NewGuid();var keys=new Dictionary<ProviderType,string>{{ProviderType.MilvusOASIS,"original"},{ProviderType.MongoDBOASIS,"other"}};
        var holon=new Holon{Id=id,ProviderUniqueStorageKey=keys};
        var transport=new Transport{OnSend=(_,_)=>Task.FromResult(Json(new{code=1}))};
        using var http=new HttpClient(transport);var result=await new MilvusOASIS(http,"http://native.invalid").SaveHolonAsync(holon,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(id,holon.Id);Assert.AreSame(keys,holon.ProviderUniqueStorageKey);
        Assert.AreEqual("original",keys[ProviderType.MilvusOASIS]);Assert.AreEqual("other",keys[ProviderType.MongoDBOASIS]);Assert.IsFalse(result.IsSaved);
    }

    [DataTestMethod]
    [DataRow("size")]
    [DataRow("history")]
    public async Task UnsupportedWritesRejectBeforeNativeIOAndCallerMutation(string mode)
    {
        var transport=new Transport();using var http=new HttpClient(transport);var provider=new MilvusOASIS(http,"http://native.invalid"){IsVersionControlEnabled=mode=="history"};
        var holon=new Holon{Name=mode=="size"?new string('x',65536):"coffee"};var result=await provider.SaveHolonAsync(holon,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,transport.Calls);Assert.AreEqual(Guid.Empty,holon.Id);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchStopContinueCountsOnlyAcknowledgedWrites(bool keepGoing)
    {
        var writes=0;var transport=new Transport{OnSend=async(request,token)=>{
            writes++;using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));var id=body.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
            return Json(new{code=writes==1?1:0,data=new{upsertCount=1,upsertIds=new[]{id}}});
        }};
        using var http=new HttpClient(transport);var provider=new MilvusOASIS(http,"http://native.invalid");var records=new[]{new Holon(),new Holon()};
        var result=await provider.SaveHolonsAsync(records,saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.AreEqual(1,result.ErrorCount);Assert.AreEqual(keepGoing?2:1,writes);Assert.AreEqual(keepGoing?1:0,result.SavedCount);Assert.AreEqual(keepGoing,result.IsSaved);Assert.AreEqual(Guid.Empty,records[0].Id);
    }
}
