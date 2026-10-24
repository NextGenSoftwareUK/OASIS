using System.Net;
using System.Reflection;
using System.Text.Json;
using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryCollectionTests
{
    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(0,false,true)][DataRow(0,true,false)][DataRow(0,true,true)]
    [DataRow(1,false,false)][DataRow(1,false,true)][DataRow(1,true,false)][DataRow(1,true,true)]
    [DataRow(2,false,false)][DataRow(2,false,true)][DataRow(2,true,false)][DataRow(2,true,true)]
    [DataRow(3,false,false)][DataRow(3,false,true)][DataRow(3,true,false)][DataRow(3,true,true)]
    public async Task DeletedDetailsAreExcludedFromOrdinaryReads(int route,bool sync,bool deleted)
    {
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-tree",Email="owned@example.invalid",DeletedDate=deleted?new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc):DateTime.MinValue};
        var stored=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(detail);
        var client=new Rows{Stored=stored};var provider=new BigQueryOASIS("my-project");
        typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        if(route==3){
            var result=sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(deleted?0:1,result.LoadedCount);Assert.AreEqual(deleted?0:1,result.Result.Count());
        }else{
            var result=route==0?(sync?provider.LoadAvatarDetail(detail.Id):await provider.LoadAvatarDetailAsync(detail.Id)):
                route==1?(sync?provider.LoadAvatarDetailByUsername(detail.Username):await provider.LoadAvatarDetailByUsernameAsync(detail.Username)):
                (sync?provider.LoadAvatarDetailByEmail(detail.Email):await provider.LoadAvatarDetailByEmailAsync(detail.Email));
            Assert.AreEqual(deleted,result.IsError);Assert.AreEqual(!deleted,result.IsLoaded);Assert.AreEqual(deleted?0:1,result.LoadedCount);
            if(deleted)Assert.IsNull(result.Result);else Assert.AreEqual(detail.Id,result.Result.Id);
        }
        Assert.AreEqual(stored,client.Stored,"Read visibility must not rewrite the stored record.");
    }

    private sealed class Rows(string? bad=null):BigQueryClient
    {
        public string? Stored;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            var job=new JobReference{ProjectId="my-project",JobId=queryOptions?.JobId??"owned-read-job"};
            if(sql.TrimStart().StartsWith("MERGE",StringComparison.Ordinal)){
                Stored=(string)parameters.Single(parameter=>parameter.Name=="doc").Value;
                return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,NumDmlAffectedRows=1},null,new GetQueryResultsOptions()));
            }
            var values=Stored!=null?new[]{Stored}:new[]{"{\"Id\":\""+Guid.NewGuid()+"\"}",bad};
            var rows=values.Select(value=>new TableRow{F=new List<TableCell>{new(){V=value}}}).ToList();
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,TotalRows=(ulong)rows.Count,Schema=new(){Fields=new List<TableFieldSchema>{new(){Name="doc",Type="STRING"}}},Rows=rows},null,new GetQueryResultsOptions()));
        }
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SavedDetailUsesExistingCanonicalCodecForOwnedInterfaces(bool sync)
    {
        var client=new Rows();var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var item=Guid.NewGuid();var source=new AvatarDetail{Inventory=new List<IInventoryItem>{new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem{Id=item,Name="Oak seed",Quantity=3}},MetaData=new(){{"large",9007199254740993L}}};
        var saved=sync?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        var loaded=sync?provider.LoadAvatarDetail(source.Id):await provider.LoadAvatarDetailAsync(source.Id);Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
        Assert.IsTrue(loaded.IsLoaded);Assert.AreEqual(1,loaded.LoadedCount);Assert.IsNotNull(loaded.Result.Chakras);
        var inventory=loaded.Result.Inventory.Single();Assert.AreEqual(item,inventory.Id);Assert.AreEqual("Oak seed",inventory.Name);Assert.AreEqual(3,inventory.Quantity);
        Assert.AreEqual("9007199254740993",loaded.Result.MetaData["large"].ToString());
    }
    public static IEnumerable<object?[]> MalformedCases(){foreach(var kind in Enumerable.Range(0,4))foreach(var sync in new[]{false,true})foreach(var bad in new string?[]{null,"","null"})yield return new object?[]{kind,sync,bad};}
    [DataTestMethod][DynamicData(nameof(MalformedCases),DynamicDataSourceType.Method)]
    public async Task MalformedStoredRowsCannotBeSilentlyDropped(int kind,bool sync,string? bad)
    {
        var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,new Rows(bad));
        bool error,loaded;int count;object? value;Exception? cause;
        if(kind==0){var result=sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();error=result.IsError;loaded=result.IsLoaded;count=result.LoadedCount;value=result.Result;cause=result.Exception;}
        else if(kind==1){var result=sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();error=result.IsError;loaded=result.IsLoaded;count=result.LoadedCount;value=result.Result;cause=result.Exception;}
        else{var result=kind==2?(sync?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false)):(sync?provider.LoadHolonsForParent(Guid.NewGuid(),loadChildren:false):await provider.LoadHolonsForParentAsync(Guid.NewGuid(),loadChildren:false));error=result.IsError;loaded=result.IsLoaded;count=result.LoadedCount;value=result.Result;cause=result.Exception;}
        Assert.IsTrue(error);Assert.IsFalse(loaded);Assert.AreEqual(0,count);Assert.IsNull(value);Assert.IsInstanceOfType<InvalidDataException>(cause);
    }
    private sealed class Transport(int kind,bool empty=false,bool failSecond=false):HttpMessageHandler
    {
        public readonly Guid Parent=Guid.NewGuid();public readonly Guid[] Ids=Enumerable.Range(0,1002).Select(_=>Guid.NewGuid()).ToArray();
        public string Sql="",Job="";public int Pages;
        private object JobResponse()=>new{jobReference=new{projectId="my-project",jobId=Job},status=new{state="DONE"},configuration=new{query=new{query=Sql,useLegacySql=false}}};
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.AreEqual("bigquery.googleapis.com",request.RequestUri!.Host);
            if(request.Method==HttpMethod.Post){
                CollectionAssert.AreEqual(new[]{"gzip"},request.Content!.Headers.ContentEncoding.ToArray());
                using var stream=await request.Content.ReadAsStreamAsync(token);using var decoded=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionMode.Decompress);
                using var reader=new StreamReader(decoded);using var document=JsonDocument.Parse(await reader.ReadToEndAsync(token));
                Sql=document.RootElement.GetProperty("configuration").GetProperty("query").GetProperty("query").GetString()!;Job=document.RootElement.GetProperty("jobReference").GetProperty("jobId").GetString()!;
                return Reply(JobResponse());
            }
            if(request.RequestUri.AbsolutePath.Contains("/queries/")){
                Pages++;var next=request.RequestUri.Query.Contains("pageToken=owned-next");var total=empty?0:Sql.Contains("LIMIT 1000",StringComparison.Ordinal)?1000:1002;
                if(next&&failSecond)return new HttpResponseMessage(HttpStatusCode.BadRequest){Content=new StringContent("{\"error\":{\"code\":400,\"message\":\"Controlled later query page rejection.\",\"errors\":[{\"reason\":\"invalidQuery\",\"message\":\"controlled\"}]}}")};
                var rows=Enumerable.Range(next?1000:0,next?total-1000:Math.Min(1000,total)).Select(index=>{
                    IHolonBase record=kind==0?new Avatar{Id=Ids[index],Description="record-"+index}:kind==1?new AvatarDetail{Id=Ids[index],Description="record-"+index}:new Holon{Id=Ids[index],ParentHolonId=Parent,Description="record-"+index};
                    return new{f=new[]{new{v=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(record)}}};
                }).ToArray();
                return Reply(new{jobReference=new{projectId="my-project",jobId=Job},jobComplete=true,totalRows=total.ToString(),schema=new{fields=new[]{new{name="doc",type="STRING"}}},rows,pageToken=!next&&total>1000?"owned-next":null});
            }
            if(request.RequestUri.AbsolutePath.Contains("/jobs/"))return Reply(JobResponse());
            throw new InvalidOperationException("Unexpected controlled BigQuery request: "+request.RequestUri.AbsolutePath);
        }
        private static HttpResponseMessage Reply(object body)=>new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(body),System.Text.Encoding.UTF8,"application/json")};
    }
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task CollectionsExhaustOfficialSdkPagesBeyondOneThousand(int kind,bool sync)
        =>await VerifyCollections(kind,sync,false);

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task EmptyNativeCollectionsRemainLoadedWithZeroCount(int kind,bool sync)
        =>await VerifyCollections(kind,sync,true);

    private static async Task VerifyCollections(int kind,bool sync,bool empty)
    {
        var wire=new Transport(kind,empty:empty);
        using var service=new Google.Apis.Bigquery.v2.BigqueryService(new Google.Apis.Services.BaseClientService.Initializer{ApplicationName="oasis-controlled-test",HttpClientFactory=new BigQueryParameterTests.Factory(wire),HttpClientInitializer=Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("synthetic-bq-test-token")});
        using var client=new BigQueryClientImpl("my-project",service);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        IEnumerable<IHolonBase> rows;bool loaded,error;int count;string message;
        if(kind==0){var result=sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();rows=result.Result;loaded=result.IsLoaded;count=result.LoadedCount;error=result.IsError;message=result.Message;}
        else if(kind==1){var result=sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();rows=result.Result;loaded=result.IsLoaded;count=result.LoadedCount;error=result.IsError;message=result.Message;}
        else{var result=kind==2?(sync?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false)):(sync?provider.LoadHolonsForParent(wire.Parent,loadChildren:false):await provider.LoadHolonsForParentAsync(wire.Parent,loadChildren:false));rows=result.Result;loaded=result.IsLoaded;count=result.LoadedCount;error=result.IsError;message=result.Message;}
        Assert.IsFalse(error,message);Assert.AreEqual(empty?0:1002,rows.Count());Assert.AreEqual(empty?0:1002,count);Assert.IsTrue(loaded);Assert.AreEqual(empty?1:2,wire.Pages);
        if(!empty){Assert.AreEqual(wire.Ids[^1],rows.Last().Id);Assert.AreEqual("record-1001",rows.Last().Description);}Assert.IsFalse(wire.Sql.Contains("LIMIT 1000",StringComparison.Ordinal));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task LaterNativePageFailureCannotPublishTheFirstThousandRows(bool sync)
    {
        var wire=new Transport(2,failSecond:true);
        using var service=new Google.Apis.Bigquery.v2.BigqueryService(new Google.Apis.Services.BaseClientService.Initializer{ApplicationName="oasis-controlled-test",HttpClientFactory=new BigQueryParameterTests.Factory(wire),HttpClientInitializer=Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("synthetic-bq-test-token")});
        using var client=new BigQueryClientImpl("my-project",service);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var result=sync?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsNull(result.Result);Assert.AreEqual(2,wire.Pages);
        Assert.IsInstanceOfType<Google.GoogleApiException>(result.Exception);Assert.AreEqual(HttpStatusCode.BadRequest,((Google.GoogleApiException)result.Exception).HttpStatusCode);
    }
}
