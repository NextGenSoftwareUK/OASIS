using System.Reflection;
using Google.Cloud.BigQuery.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryParameterTests
{
    private sealed class Wire:HttpMessageHandler
    {
        public string? Payload;public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("bigquery.googleapis.com",request.RequestUri!.Host);
            Assert.AreEqual("Bearer",request.Headers.Authorization!.Scheme);Assert.AreEqual("synthetic-bq-test-token",request.Headers.Authorization.Parameter);
            CollectionAssert.AreEqual(new[]{"gzip"},request.Content!.Headers.ContentEncoding.ToArray());
            using var compressed=await request.Content.ReadAsStreamAsync(token);
            using var decoded=new System.IO.Compression.GZipStream(compressed,System.IO.Compression.CompressionMode.Decompress);
            using var reader=new StreamReader(decoded,new System.Text.UTF8Encoding(false,true));Payload=await reader.ReadToEndAsync(token);
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest){Content=new StringContent("{\"error\":{\"code\":400,\"message\":\"Controlled rejection before cloud dispatch.\",\"errors\":[{\"reason\":\"invalidQuery\",\"message\":\"controlled\"}]}}")};
        }
    }
    private sealed class Factory(Wire wire):Google.Apis.Http.IHttpClientFactory
    {
        public Google.Apis.Http.ConfigurableHttpClient CreateHttpClient(Google.Apis.Http.CreateHttpClientArgs args)
        {
            var client=new Google.Apis.Http.ConfigurableHttpClient(new Google.Apis.Http.ConfigurableMessageHandler(wire));
            foreach(var initializer in args.Initializers)initializer.Initialize(client);
            return client;
        }
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OfficialSdkSerializesNamedParametersSeparatelyFromQueryText(bool save)
    {
        const string value="wire'; SELECT 'not-sql\\🌳";var wire=new Wire();
        using var service=new Google.Apis.Bigquery.v2.BigqueryService(new Google.Apis.Services.BaseClientService.Initializer{
            ApplicationName="oasis-controlled-test",HttpClientFactory=new Factory(wire),HttpClientInitializer=Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("synthetic-bq-test-token")});
        using var client=new BigQueryClientImpl("my-project",service);var provider=new BigQueryOASIS("my-project");
        typeof(BigQueryOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,client);
        if(save)await provider.SaveAvatarAsync(new Avatar{Id=Guid.NewGuid(),Username=value});else await provider.LoadAvatarByUsernameAsync(value);
        Assert.AreEqual(1,wire.Calls);Assert.IsNotNull(wire.Payload);
        using var json=System.Text.Json.JsonDocument.Parse(wire.Payload);var query=json.RootElement.GetProperty("configuration").GetProperty("query");
        if(save)StringAssert.StartsWith(json.RootElement.GetProperty("jobReference").GetProperty("jobId").GetString(),"oasis-record-");
        Assert.IsFalse(query.GetProperty("useLegacySql").GetBoolean());Assert.AreEqual("named",query.GetProperty("parameterMode").GetString());
        Assert.IsFalse(query.GetProperty("query").GetString()!.Contains(value,StringComparison.Ordinal));
        var name=save?"username":"value";var parameter=query.GetProperty("queryParameters").EnumerateArray().Single(row=>row.GetProperty("name").GetString()==name);
        Assert.AreEqual("STRING",parameter.GetProperty("parameterType").GetProperty("type").GetString());Assert.AreEqual(value,parameter.GetProperty("parameterValue").GetProperty("value").GetString());
    }
    private sealed class Client:BigQueryClient
    {
        public string? Sql;public BigQueryParameter[] Parameters=Array.Empty<BigQueryParameter>();public int Calls;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {Calls++;Sql=sql;Parameters=parameters?.ToArray()??Array.Empty<BigQueryParameter>();Assert.IsNotNull(queryOptions);Assert.AreEqual(false,queryOptions.UseLegacySql);throw new IOException("Controlled stop before native dispatch.");}
    }
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    [DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)][DataRow(5,false)][DataRow(5,true)]
    [DataRow(6,false)][DataRow(6,true)][DataRow(7,false)][DataRow(7,true)]
    public async Task PublicRecordValuesReachSdkAsParametersNotSqlText(int route,bool sync)
    {
        const string value="canary'; SELECT 'injected\\payload🌳";var id=Guid.NewGuid();
        var provider=new BigQueryOASIS("my-project","oasis");var client=new Client();
        typeof(BigQueryOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,client);
        if(route==0){var row=new Avatar{Id=id,Username=value,Email=value};if(sync)provider.SaveAvatar(row);else await provider.SaveAvatarAsync(row);}
        else if(route==1){var row=new AvatarDetail{Id=id,Username=value,Email=value};if(sync)provider.SaveAvatarDetail(row);else await provider.SaveAvatarDetailAsync(row);}
        else if(route==2){var row=new Holon{Id=id,Name=value};if(sync)provider.SaveHolon(row,saveChildren:false);else await provider.SaveHolonAsync(row,saveChildren:false);}
        else if(route==3){if(sync)provider.LoadAvatarByUsername(value);else await provider.LoadAvatarByUsernameAsync(value);}
        else if(route==4){if(sync)provider.LoadAvatarDetailByEmail(value);else await provider.LoadAvatarDetailByEmailAsync(value);}
        else if(route==5){if(sync)provider.LoadAvatar(id);else await provider.LoadAvatarAsync(id);}
        else if(route==6){if(sync)provider.LoadHolonsForParent(id,loadChildren:false);else await provider.LoadHolonsForParentAsync(id,loadChildren:false);}
        else{if(sync)provider.DeleteAvatar(id,false);else await provider.DeleteAvatarAsync(id,false);}
        Assert.AreEqual(1,client.Calls);Assert.IsNotNull(client.Sql);Assert.IsFalse(client.Sql.Contains(value,StringComparison.Ordinal));Assert.IsFalse(client.Sql.Contains(id.ToString(),StringComparison.Ordinal));
        Assert.IsTrue(client.Parameters.Length>0,"Actual SDK call must carry named parameters.");
        Assert.IsTrue(client.Parameters.All(parameter=>parameter.Type==BigQueryDbType.String));
        var expected=route is 3 or 4 or 6?"value":"id";
        Assert.AreEqual(route is 3 or 4?value:id.ToString(),client.Parameters.Single(parameter=>parameter.Name==expected).Value);
        if(route<3){var doc=(string)client.Parameters.Single(parameter=>parameter.Name=="doc").Value;using var json=System.Text.Json.JsonDocument.Parse(doc);Assert.AreEqual(value,json.RootElement.GetProperty(route==2?"Name":"Username").GetString());}
    }
    [DataTestMethod][DataRow(false,"bad`name")][DataRow(true,"bad`name")][DataRow(false,"bad\\name")][DataRow(true,"bad\\name")][DataRow(false,"bad\nname")][DataRow(true,"bad\nname")][DataRow(false,"")][DataRow(true,"")]
    public void ConfiguredIdentifiersCannotEscapeTheQuotedNativeTable(bool dataset,string value)
        =>Assert.ThrowsException<ArgumentException>(()=>new BigQueryOASIS(dataset?"my-project":value,dataset?value:"oasis"));
}
