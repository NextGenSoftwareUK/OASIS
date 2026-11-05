using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS.IntegrationTests;

[TestClass]
public class AlgoliaCqlNativeTests
{
    public sealed class Source
    {
        public string AppId {get;set;}="";
        public Guid Actor {get;set;}
        public Guid[] LiveIds {get;set;}=Array.Empty<Guid>();
        public Guid DeletedId {get;set;}
        public Guid MissingId {get;set;}
    }

    [TestMethod]
    [TestCategory("AlgoliaCqlNative")]
    public async Task ExistingOwnedRecordsSupportThreeTwoZeroPagingWithoutMutation()
    {
        var path=Environment.GetEnvironmentVariable("COSMIC_ALGOLIA_CQL_SOURCE");
        var key=Environment.GetEnvironmentVariable("COSMIC_ALGOLIA_CQL_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path),"Explicit owned Algolia source required; native acceptance is open.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key),"Provide the source-scoped search credential outside the source file.");
        var raw=await File.ReadAllBytesAsync(path!);
        Assert.IsTrue(raw.Length<=16384,"Bounded owned source descriptor required.");
        using(var descriptor=JsonDocument.Parse(raw,new JsonDocumentOptions {MaxDepth=4})) {
            var fields=new HashSet<string>(new[] {"AppId","Actor","LiveIds","DeletedId","MissingId"},StringComparer.Ordinal);
            foreach(var property in descriptor.RootElement.EnumerateObject())
                Assert.IsTrue(fields.Remove(property.Name),"Exact source fields without duplicate properties or credentials are required.");
            Assert.AreEqual(0,fields.Count,"All source fields are required.");
        }
        var source=JsonSerializer.Deserialize<Source>(raw)!;
        Assert.AreNotEqual(Guid.Empty,source.Actor);Assert.AreEqual(3,source.LiveIds.Length);
        var ids=source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        Assert.AreEqual(5,ids.Distinct().Count());Assert.IsFalse(ids.Contains(Guid.Empty));
        using var http=new HttpClient(new HttpClientHandler {AllowAutoRedirect=false}) {Timeout=TimeSpan.FromSeconds(15)};
        var provider=new AlgoliaOASIS(source.AppId,key!,source.LiveIds[0],http);
        async Task<JsonNode[]> Observe()
        {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request=new HttpRequestMessage(HttpMethod.Post,$"https://{source.AppId}-dsn.algolia.net/1/indexes/*/objects");
            request.Content=JsonContent.Create(new {requests=ids.Select(id=>new {indexName="oasis_holons",objectID=id.ToString("D")})});
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
            response.EnsureSuccessStatusCode();
            using var stream=await response.Content.ReadAsStreamAsync(deadline.Token);
            using var output=new MemoryStream();var buffer=new byte[8192];int count;
            while((count=await stream.ReadAsync(buffer,deadline.Token))!=0) {
                Assert.IsTrue(output.Length+count<=4*1024*1024,"Native fixture response exceeds budget.");output.Write(buffer,0,count);
            }
            var node=JsonNode.Parse(output.ToArray())!;
            Assert.IsNull(node["message"],"Native index status must be resolved before qualification.");
            Assert.IsNull(node["error"]);Assert.AreEqual(5,node["results"]!.AsArray().Count);
            var observed=new List<JsonNode>();
            for(int index=0;index<ids.Length;index++) {
                var row=node["results"]![index];
                if(ids[index]==source.MissingId) {Assert.IsNull(row);continue;}
                Assert.IsNotNull(row);Assert.AreEqual(ids[index].ToString("D"),row["objectID"]!.GetValue<string>());
                Assert.AreEqual(ids[index].ToString("D"),row["id"]!.GetValue<string>());
                Assert.AreEqual(source.Actor.ToString("D"),row["createdByAvatarId"]!.GetValue<string>());
                var deleted=DateTime.Parse(row["deletedDate"]!.GetValue<string>(),System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind)!=DateTime.MinValue;
                Assert.AreEqual(ids[index]==source.DeletedId,deleted);
                observed.Add(row.DeepClone());
            }
            return observed.ToArray();
        }
        var before=await Observe();
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        try
        {
            CosmicQuery<Holon> Query()=>CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AlgoliaOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor,ProviderType.AlgoliaOASIS,ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var planned=CosmicQueryPlanner.Plan(query,ProviderType.AlgoliaOASIS,
                    new[] {new CosmicQueryTarget(ProviderType.AlgoliaOASIS,provider.QueryCapabilities)});
                Assert.IsFalse(planned.IsError,planned.Message);
                var result=await provider.ExecuteCosmicQueryAsync(planned.Result);
                Assert.IsFalse(result.IsError,result.Message);return result.Result.Select(row=>row.Id).ToArray();
            }
            var ordered=source.LiveIds.OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered,await Read(Query()));
            var subset=new[] {ordered[0],ordered[2]};
            CollectionAssert.AreEqual(subset,await Read(Query().Where(row=>subset.Contains(row.Id)).Result));
            var excluded=new[] {source.DeletedId,source.MissingId};
            Assert.AreEqual(0,(await Read(Query().Where(row=>excluded.Contains(row.Id)).Result)).Length);
            CollectionAssert.AreEqual(new[] {ordered[1]},await Read(Query().Skip(1).Result.Take(1).Result));
            var after=await Observe();Assert.AreEqual(before.Length,after.Length);
            for(int index=0;index<before.Length;index++) Assert.IsTrue(JsonNode.DeepEquals(before[index],after[index]),"Native record changed during read-only qualification.");
            CollectionAssert.AreEqual(raw,await File.ReadAllBytesAsync(path!));
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
