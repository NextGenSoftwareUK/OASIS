using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class ArangoDbNativeCqlTests
{
    private sealed record Seed(Guid Actor,Guid[] Ids);
    private static void Durable(string path,object value)
    {
        using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        JsonSerializer.Serialize(file,value);file.Flush(true);
    }
    [TestMethod]
    public async Task OriginalNativeRecordsFilterAndReloadWithoutNewWrites()
    {
        var state=Environment.GetEnvironmentVariable("COSMIC_ARANGODB_ROOT")??throw new InvalidOperationException("Use retained native runner.");
        var manifest=Path.Combine(state,"identity.json");var pending=Path.Combine(state,"seed.pending.json");
        var baseline=Path.Combine(state,"native-baseline.json");
        Assert.IsFalse(File.Exists(pending),"Original write outcome unresolved; reconcile, never replay/reseed.");
        var created=!File.Exists(manifest);
        if(!created)Assert.IsTrue(File.Exists(baseline),"Original baseline missing; never bless replacement state.");
        using var config=JsonDocument.Parse(File.ReadAllText(Path.Combine(state,"credentials.json")));
        var user=config.RootElement.GetProperty("username").GetString()!;var password=config.RootElement.GetProperty("password").GetString()!;
        const string endpoint="http://127.0.0.1:18529";
        using var http=new HttpClient{BaseAddress=new Uri(endpoint)};
        http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(user+":"+password)));
        var seed=created?new Seed(Guid.NewGuid(),Enumerable.Range(0,3).Select(_=>Guid.NewGuid()).ToArray()):JsonSerializer.Deserialize<Seed>(File.ReadAllText(manifest))!;
        if(created)Durable(pending,seed);
        var provider=new ArangoDBOASIS(endpoint,user,password);
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
        try
        {
            async Task<string[]> Catalogue()
            {
                // Fixture verification only; native CQL reads individual document keys.
                using var response=await http.PostAsync("/_db/oasis/_api/cursor",new StringContent(
                    JsonSerializer.Serialize(new{query="FOR d IN holons RETURN d._key",batchSize=10}),Encoding.UTF8,"application/json"));
                response.EnsureSuccessStatusCode();
                using var listing=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.IsFalse(listing.RootElement.GetProperty("hasMore").GetBoolean());
                return listing.RootElement.GetProperty("result").EnumerateArray().Select(v=>v.GetString()!).ToArray();
            }
            async Task VerifyCatalogue()
                => CollectionAssert.AreEquivalent(seed.Ids.Select(id=>id.ToString("D")).ToArray(),await Catalogue());
            if(created)
            {
                Assert.AreEqual(0,(await Catalogue()).Length,"Unexpected existing source; no reseed.");
                var names=new[]{"Espresso","Latte","Tea"};
                for(var i=0;i<3;i++)
                {
                    var row=new Holon{Id=seed.Ids[i],Name=names[i],CreatedByAvatarId=seed.Actor,IsNewHolon=true};
                    var saved=await provider.SaveHolonAsync(row,saveChildren:false);
                    Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
                    Assert.AreEqual(row.Id.ToString("D"),row.ProviderUniqueStorageKey[ProviderType.ArangoDBOASIS]);
                    Assert.IsTrue(row.IsNewHolon,"Manager owns clean-state publication.");
                    Durable(Path.Combine(state,$"write-{i}.ack.json"),new{id=row.Id,revision=saved.MetaData["ArangoDB.Revision"]});
                }
            }
            await VerifyCatalogue();
            async Task<Dictionary<string,string>> Hashes()
            {
                var hashes=new Dictionary<string,string>();
                for(var i=0;i<3;i++)
                {
                    var id=seed.Ids[i];var bytes=await http.GetByteArrayAsync("/_db/oasis/_api/document/holons/"+id.ToString("D"));
                    using var native=JsonDocument.Parse(bytes);
                    Assert.AreEqual(id.ToString("D"),native.RootElement.GetProperty("_key").GetString());
                    using var ack=JsonDocument.Parse(File.ReadAllText(Path.Combine(state,$"write-{i}.ack.json")));
                    Assert.AreEqual(ack.RootElement.GetProperty("revision").GetString(),native.RootElement.GetProperty("_rev").GetString());
                    hashes[id.ToString("D")]=Convert.ToHexString(SHA256.HashData(bytes));
                }
                return hashes;
            }
            var before=await Hashes();
            if(created){Durable(baseline,before);Durable(manifest,seed);File.Delete(pending);}
            else CollectionAssert.AreEquivalent(JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(baseline))!.ToArray(),before.ToArray());
            async Task<string> Sequence()
            {
                using var info=JsonDocument.Parse(await http.GetStringAsync("/_db/oasis/_api/collection/holons/revision"));return info.RootElement.GetProperty("revision").GetRawText();
            }
            var sequence=await Sequence();
            var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArangoDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor,ProviderType.ArangoDBOASIS,seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan=CosmicQueryPlanner.Plan(selection,ProviderType.ArangoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.ArangoDBOASIS,provider.QueryCapabilities)});
                Assert.IsFalse(plan.IsError,plan.Message);var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);return result.Result;
            }
            CollectionAssert.AreEquivalent(new[]{"Espresso","Latte","Tea"},(await Read(query)).Select(r=>r.Name).ToArray());
            var coffee=seed.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(new[]{"Espresso","Latte"},(await Read(query.Where(r=>coffee.Contains(r.Id)).Result)).Select(r=>r.Name).ToArray());
            Assert.AreEqual(1,(await Read(query.Skip(1).Take(1).Result)).Count);
            CollectionAssert.AreEquivalent(before.ToArray(),(await Hashes()).ToArray());
            Assert.AreEqual(sequence,await Sequence(),"CQL must not modify native collection revision.");await VerifyCatalogue();
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
