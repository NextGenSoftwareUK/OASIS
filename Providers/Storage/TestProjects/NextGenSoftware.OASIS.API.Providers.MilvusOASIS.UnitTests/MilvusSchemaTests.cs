using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests;
[TestClass]
public class MilvusSchemaTests
{
    private sealed class Transport(string mode):HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("/v2/vectordb/collections/describe",request.RequestUri!.AbsolutePath);
            using var payload=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            object Field(string name,string type,string key,string value)=>new{name,type,primaryKey=name=="id",autoId=false,partitionKey=false,@params=new[]{new{key,value}}};
            var fields=new[]{Field("id","VarChar","max_length","64"),Field("doc","VarChar","max_length",mode=="bound"?"1":"65535"),Field("username","VarChar","max_length","512"),Field("email","VarChar","max_length","512"),Field("parent_holon_id","VarChar","max_length","64"),Field("vector","FloatVector","dim",mode=="dimension"?"1":"2")};
            return new HttpResponseMessage(mode=="http"?HttpStatusCode.Unauthorized:HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{code=mode=="code"?1:0,data=new{collectionName=payload.RootElement.GetProperty("collectionName").GetString(),collectionID=long.MaxValue,autoId=mode=="auto",enableDynamicField=false,consistencyLevel=mode=="consistency"?"Bounded":"Strong",load=mode=="load"?"LoadStateNotLoad":"LoadStateLoaded",fields,indexes=new[]{new{fieldName="vector",indexName="vector_idx",metricType=mode=="index"?"COSINE":"L2"}}}}))};
        }
    }
    [DataTestMethod]
    [DataRow("success")][DataRow("http")][DataRow("code")][DataRow("bound")][DataRow("dimension")][DataRow("consistency")][DataRow("load")][DataRow("auto")][DataRow("index")]
    public async Task AdmissionNeverCreatesOrReplacesACollectionAfterErrors(string mode)
    {
        using var transport=new Transport(mode);using var http=new HttpClient(transport);
        var provider=new MilvusOASIS(http,"http://native.invalid"){IsProviderActivated=true};
        var result=await provider.ActivateProviderAsync();Assert.AreEqual(mode=="success",!result.IsError,result.Message);
        Assert.AreEqual(!result.IsError,provider.IsProviderActivated);Assert.AreEqual(mode=="success"?3:1,transport.Calls);
    }
}
