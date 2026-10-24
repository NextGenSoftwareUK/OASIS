using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryGraphWriteTests
{
    private sealed class Wire:HttpMessageHandler
    {
        private string _sql="",_job="";public int Writes;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.AreEqual("bigquery.googleapis.com",request.RequestUri!.Host);
            object Job()=>new{jobReference=new{projectId="my-project",jobId=_job},status=new{state="DONE"},configuration=new{query=new{query=_sql,useLegacySql=false}}};
            if(request.Method==HttpMethod.Post){
                CollectionAssert.AreEqual(new[]{"gzip"},request.Content!.Headers.ContentEncoding.ToArray());using var stream=await request.Content.ReadAsStreamAsync(token);using var decoded=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionMode.Decompress);using var reader=new StreamReader(decoded);using var json=System.Text.Json.JsonDocument.Parse(await reader.ReadToEndAsync(token));
                var query=json.RootElement.GetProperty("configuration").GetProperty("query");_sql=query.GetProperty("query").GetString()!;_job=json.RootElement.GetProperty("jobReference").GetProperty("jobId").GetString()!;
                if(_sql.TrimStart().StartsWith("MERGE",StringComparison.Ordinal)){
                    Writes++;StringAssert.StartsWith(_job,"oasis-record-");StringAssert.Contains(_sql,"t.doc = @expected_doc");StringAssert.Contains(_sql,"WHEN NOT MATCHED AND NOT @expected_exists");
                    var parameters=query.GetProperty("queryParameters").EnumerateArray().ToArray();var exists=parameters.Single(row=>row.GetProperty("name").GetString()=="expected_exists");Assert.AreEqual("BOOL",exists.GetProperty("parameterType").GetProperty("type").GetString());Assert.AreEqual("false",exists.GetProperty("parameterValue").GetProperty("value").GetString());
                    var original=parameters.Single(row=>row.GetProperty("name").GetString()=="expected_doc");Assert.AreEqual("STRING",original.GetProperty("parameterType").GetProperty("type").GetString());
                    Assert.IsTrue(!original.GetProperty("parameterValue").TryGetProperty("value",out var value)||value.ValueKind==System.Text.Json.JsonValueKind.Null);
                    return new(System.Net.HttpStatusCode.BadRequest){Content=new StringContent("{\"error\":{\"code\":400,\"message\":\"Controlled no-cloud rejection.\",\"errors\":[{\"reason\":\"invalidQuery\",\"message\":\"controlled\"}]}}")};
                }
                return Reply(Job());
            }
            if(request.RequestUri.AbsolutePath.Contains("/queries/",StringComparison.Ordinal))return Reply(new{jobReference=new{projectId="my-project",jobId=_job},jobComplete=true,totalRows="0",schema=new{fields=new[]{new{name="doc",type="STRING"}}},rows=Array.Empty<object>()});
            if(request.RequestUri.AbsolutePath.Contains("/jobs/",StringComparison.Ordinal))return Reply(Job());
            throw new InvalidOperationException("Unexpected controlled SDK graph request.");
        }
        private static HttpResponseMessage Reply(object value)=>new(System.Net.HttpStatusCode.OK){Content=new StringContent(System.Text.Json.JsonSerializer.Serialize(value),System.Text.Encoding.UTF8,"application/json")};
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OfficialSdkSerializesNativeConditionalGraphWrite(bool sync)
    {
        var wire=new Wire();using var service=new Google.Apis.Bigquery.v2.BigqueryService(new Google.Apis.Services.BaseClientService.Initializer{ApplicationName="oasis-controlled-test",HttpClientFactory=new BigQueryParameterTests.Factory(wire),HttpClientInitializer=Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("synthetic-bq-test-token")});using var client=new BigQueryClientImpl("my-project",service);
        var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,client);var source=new Holon();
        var result=sync?provider.SaveHolon(source,saveChildren:false):await provider.SaveHolonAsync(source,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreEqual(1,wire.Writes);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(Guid.Empty,source.Id);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)][DataRow(false,5)][DataRow(true,5)]
    public async Task InvalidWholeGraphRejectsBeforeAnyNativeIo(bool sync,int kind)
    {
        var child=new Holon();var root=new Holon{Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{child}};
        if(kind==0)root.Children.Add(null!);if(kind==1)child.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{root};if(kind==2){root.Id=Guid.NewGuid();child.Id=root.Id;}if(kind==3)child.ParentHolonId=Guid.NewGuid();
        var client=new Ledger();var provider=Provider(client);bool error;
        if(kind==5){var r=sync?provider.SaveHolons(new[]{root,root}):await provider.SaveHolonsAsync(new[]{root,root});error=r.IsError;Assert.AreEqual(0,r.SavedCount);}
        else{var r=sync?provider.SaveHolon(root,maxChildDepth:kind==4?-1:0):await provider.SaveHolonAsync(root,maxChildDepth:kind==4?-1:0);error=r.IsError;Assert.AreEqual(0,r.SavedCount);}
        Assert.IsTrue(error);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);Assert.AreEqual(0,client.Records.Count);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task UnselectedMembershipPreservesNativeRoleManifest(bool sync)
    {
        var id=Guid.NewGuid();var manifest=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        var stored=new Holon{Id=id,ChildRoleManifestJson=manifest,Description="old"};var source=new Holon{Id=id,Description="updated"};var client=new Ledger();client.Records[id.ToString()]=StoredRecordJson.Serialize(stored);
        var provider=Provider(client);var result=sync?provider.SaveHolon(source,saveChildren:false):await provider.SaveHolonAsync(source,saveChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);var actual=StoredRecordJson.DeserializeStored<Holon>(client.Records[id.ToString()]);Assert.AreEqual(manifest,actual.ChildRoleManifestJson);Assert.AreEqual(manifest,source.ChildRoleManifestJson);Assert.AreEqual("updated",actual.Description);Assert.AreEqual(0,source.Children.Count);
    }
    internal sealed class Ledger:BigQueryClient
    {
        public readonly Dictionary<string,string> Records=new();public int Writes,Reads;public int FailAt;public Action? FirstWrite;public Action<string>? AfterRead;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            var args=parameters?.ToDictionary(row=>row.Name,row=>row.Value)??new();var job=new JobReference{ProjectId="my-project",JobId=queryOptions?.JobId??"controlled-read"};
            if(sql.TrimStart().StartsWith("SELECT",StringComparison.Ordinal)){
                Reads++;
                var values=args.TryGetValue("id",out var id)?Records.TryGetValue(id.ToString()!,out var value)?new[]{value}:Array.Empty<string>():Records.Values.ToArray();
                if(id!=null)AfterRead?.Invoke(id.ToString()!);
                return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,TotalRows=(ulong)values.Length,Schema=new(){Fields=new List<TableFieldSchema>{new(){Name="doc",Type="STRING"}}},Rows=values.Select(value=>new TableRow{F=new List<TableCell>{new(){V=value}}}).ToList()},null,new GetQueryResultsOptions()));
            }
            Assert.IsTrue(sql.TrimStart().StartsWith("MERGE",StringComparison.Ordinal));Writes++;if(Writes==FailAt)throw new IOException("Controlled uncertain graph member.");
            var key=(string)args["id"];var admitted=true;
            if(args.TryGetValue("expected_exists",out var exists)){
                StringAssert.Contains(sql,"t.doc = @expected_doc");StringAssert.Contains(sql,"WHEN NOT MATCHED AND NOT @expected_exists");
                var present=Records.TryGetValue(key,out var original);admitted=present==(bool)exists&&(!present||original==(string?)args["expected_doc"]);
            }
            if(admitted)Records[key]=(string)args["doc"];
            if(Writes==1)FirstWrite?.Invoke();
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,NumDmlAffectedRows=admitted?1:0},null,new GetQueryResultsOptions()));
        }
    }
    private static BigQueryOASIS Provider(Ledger client){var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,client);return provider;}
    public static IEnumerable<object[]> Cases(){foreach(var sync in new[]{false,true})foreach(var recursive in new[]{false,true})foreach(var depth in new[]{0,1,2})yield return new object[]{sync,recursive,depth};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SelectedGraphWritesPersistBoundParentsRolesAndRoundtrip(bool sync,bool recursive,int depth)
    {
        var grand=new Holon{Name="grand"};var child=new Holon{Name="child",Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{grand}};var root=new Holon{Name="root",Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{child}};
        var client=new Ledger();var provider=Provider(client);var result=sync?provider.SaveHolon(root,recursive:recursive,maxChildDepth:depth,saveChildrenOnProvider:true):await provider.SaveHolonAsync(root,recursive:recursive,maxChildDepth:depth,saveChildrenOnProvider:true);
        var expected=recursive&&(depth==0||depth>1)?3:2;Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(expected,result.SavedCount);Assert.AreEqual(expected,client.Writes);Assert.AreEqual(expected,client.Records.Count);
        Assert.AreNotEqual(Guid.Empty,root.Id);Assert.AreNotEqual(Guid.Empty,child.Id);Assert.AreEqual(root.Id,child.ParentHolonId);
        if(expected==3){Assert.AreNotEqual(Guid.Empty,grand.Id);Assert.AreEqual(child.Id,grand.ParentHolonId);}else Assert.AreEqual(Guid.Empty,grand.Id);
        var stored=StoredRecordJson.DeserializeStored<Holon>(client.Records[root.Id.ToString()]);Assert.IsNotNull(stored.ChildRoleManifestJson);Assert.AreEqual(child.Id,HolonChildRoleManifestJson.Deserialize(stored.ChildRoleManifestJson).Roles.SelectMany(role=>role.ChildIds).Single());
        var loaded=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);Assert.AreEqual(expected==3?1:0,loaded.Result.Children.Single().Children.Count);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task FailedOrEditedLaterNodeRetainsPartialFenceAndStopsDispatch(bool sync,bool edit)
    {
        var child=new Holon{Description="original"};var root=new Holon{Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{child,new Holon()}};
        var client=new Ledger{FailAt=edit?0:2};if(edit)client.FirstWrite=()=>child.Description="newer caller edit";var provider=Provider(client);
        var result=sync?provider.SaveHolon(root,saveChildrenOnProvider:true):await provider.SaveHolonAsync(root,saveChildrenOnProvider:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(edit?1:2,client.Writes);Assert.AreEqual(1,client.Records.Count);Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(edit?"newer caller edit":"original",child.Description);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConcurrentExistingChildChangeCannotBeOverwrittenByDepthLimitedGraph(bool sync)
    {
        var root=new Holon();var child=new Holon{Id=Guid.NewGuid(),Description="original"};root.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{child};
        var client=new Ledger();client.Records[child.Id.ToString()]=StoredRecordJson.Serialize(child);string? concurrent=null;
        client.AfterRead=key=>{if(key==child.Id.ToString()){var changed=StoredRecordJson.Project<Holon>(child);changed.Description="concurrent native edit";concurrent=StoredRecordJson.Serialize(changed);client.Records[key]=concurrent;}};
        var provider=Provider(client);var result=sync?provider.SaveHolon(root,maxChildDepth:1,saveChildrenOnProvider:true):await provider.SaveHolonAsync(root,maxChildDepth:1,saveChildrenOnProvider:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.IsNotNull(concurrent);Assert.AreEqual(concurrent,client.Records[child.Id.ToString()]);Assert.AreEqual("original",child.Description);
    }
}
