using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class OpenSearchCqlBootstrapTests
{
    [TestMethod]
    [TestCategory("OpenSearchCqlBootstrap")]
    public async Task OwnedAbsentIndexGetsOneCreateOnlyCanonicalHolonBootstrap()
    {
        var path=Environment.GetEnvironmentVariable("COSMIC_OPENSEARCH_CQL_SOURCE")!;
        var raw=await File.ReadAllBytesAsync(path);
        var source=JsonSerializer.Deserialize<OpenSearchCqlNativeTests.Source>(raw)!;
        Assert.AreEqual("http://127.0.0.1:19200",source.Endpoint);
        Assert.AreEqual(3,source.LiveIds.Length); Assert.AreNotEqual(Guid.Empty,source.Actor);
        var ids=source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        Assert.AreEqual(5,ids.Distinct().Count()); Assert.IsFalse(ids.Contains(Guid.Empty));
        var state=Path.GetDirectoryName(path)!;
        using var lease=new FileStream(Path.Combine(state,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        using var http=new HttpClient(new HttpClientHandler {AllowAutoRedirect=false}) {BaseAddress=new Uri(source.Endpoint),Timeout=TimeSpan.FromSeconds(15)};
        using(var exists=await http.GetAsync("/oasis_holons"))
        {
            Assert.AreEqual(HttpStatusCode.NotFound,exists.StatusCode,"Existing index requires reconciliation, never reseeding.");
            using var body=JsonDocument.Parse(await exists.Content.ReadAsStringAsync());
            Assert.AreEqual("index_not_found_exception",body.RootElement.GetProperty("error").GetProperty("type").GetString());
        }
        var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(raw));
        async Task WriteOnce(string name,object value)
        {
            using var file=new FileStream(Path.Combine(state,name),FileMode.CreateNew,FileAccess.Write,FileShare.None);
            await JsonSerializer.SerializeAsync(file,value);file.Flush(true);
        }
        await WriteOnce("bootstrap.intent.json",new {sourceSha256=hash,index="oasis_holons",maximumCreates=4});
        using(var created=await http.PutAsJsonAsync("/oasis_holons",new {settings=new {number_of_shards=1,number_of_replicas=0}}))
        {
            created.EnsureSuccessStatusCode();
            using var body=JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Assert.IsTrue(body.RootElement.GetProperty("acknowledged").GetBoolean());
            Assert.IsTrue(body.RootElement.GetProperty("shards_acknowledged").GetBoolean());
            Assert.AreEqual("oasis_holons",body.RootElement.GetProperty("index").GetString());
            await WriteOnce("bootstrap-index.ack.json",new {index="oasis_holons",sourceSha256=hash});
        }
        var options=new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase,DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull};
        foreach(var id in source.LiveIds.Append(source.DeletedId))
        {
            var row=new Holon {Id=id,Name="OpenSearch CQL teaching " + id.ToString("N"),CreatedByAvatarId=source.Actor,
                ParentHolonId=source.Actor,IsActive=true,IsPublic=false,
                DeletedDate=id==source.DeletedId?new DateTime(2026,11,5,0,0,0,DateTimeKind.Utc):DateTime.MinValue};
            var body=JsonSerializer.SerializeToUtf8Bytes(row,options);
            await WriteOnce("bootstrap-" + id.ToString("D") + ".intent.json",new {id,sourceSha256=hash,payloadSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(body))});
            using var payload=new ByteArrayContent(body);payload.Headers.ContentType=new("application/json");
            using var response=await http.PutAsync("/oasis_holons/_create/" + id.ToString("D"),payload);
            Assert.AreEqual(HttpStatusCode.Created,response.StatusCode,await response.Content.ReadAsStringAsync());
            using var ack=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreEqual("oasis_holons",ack.RootElement.GetProperty("_index").GetString());
            Assert.AreEqual(id.ToString("D"),ack.RootElement.GetProperty("_id").GetString());
            Assert.AreEqual("created",ack.RootElement.GetProperty("result").GetString());
            Assert.AreEqual(1,ack.RootElement.GetProperty("_version").GetInt32());
            Assert.AreEqual(0,ack.RootElement.GetProperty("_shards").GetProperty("failed").GetInt32());
            Assert.IsTrue(ack.RootElement.GetProperty("_shards").GetProperty("successful").GetInt32()>=1);
            await WriteOnce("bootstrap-" + id.ToString("D") + ".ack.json",new {id,sourceSha256=hash,version=1});
        }
        await WriteOnce("bootstrap.ack.json",new {sourceSha256=hash,createdRecords=4});
    }
}
