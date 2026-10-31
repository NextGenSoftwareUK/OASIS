using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [TestMethod]
    public async Task ReconcileKnownReflectionSignatureFixtures()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string reportHash="802B54BCF845A7131349072CA49E0B7AB8ABEAC0837D5345F1B55B4518478A3B";
        Assert.AreEqual(reportHash,Hash(Path.Combine(work,"dynamo-history-native-current.trx")));
        var hashes=new Dictionary<string,string>{
            ["fault-history-capacity.pending.json"]="6279206BD512DB59470BF093C4531254CDF12C54D3932AE72B6C2D1AD1DBE4FE",
            ["fault-history-lost.pending.json"]="D16007317BFE4FDA96D6F94BF09D712875EB843BDA8C300347668580BAD70AA8",
            ["fault-history-missing-ack.pending.json"]="EA6252C22E19A1652B831A508CB05E43B8640CA61E7A811FAF28FD8ABBD3CD95",
            ["fault-history-race.pending.json"]="C86F8CF59A1D9937D6E909A69F264D38067DD84D7867C80294FF97EB2462C5B1",
            ["lifecycle-history.pending.json"]="D268F1898357B5483DE8757ED4CF7C6D143B85E141ECE660B0188871872EDFD6",
            ["native-lifecycle-token-False.pending.json"]="DAA28EB043309FC19859E1A44AA95C8A96E71EF39564677A175F86741F5D3E94",
            ["native-lifecycle-token-True.pending.json"]="A05268498CBE4C102CFE97E4D8947E9B0EFFAEA462D1564E2703A9B5906FCCC1",
            ["record-history-2.pending.json"]="F9861988D2F42D49BEF99D003A5DB8AE71A27255892BE9E24EA3D73153BE05A2",
            ["record-history-5.pending.json"]="934364EB219D67144D43D5730B179E34FEEB28FE56F3AAAD268427F40AF7E3C9"};
        CollectionAssert.AreEquivalent(hashes.Keys.ToArray(),Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        using var verifier=Client();
        var observed=new List<(Guid Id,string Json)>();
        foreach(var pair in hashes){
            var path=Path.Combine(work,pair.Key);Assert.AreEqual(pair.Value,Hash(path));
            using var intent=JsonDocument.Parse(await File.ReadAllBytesAsync(path));var root=intent.RootElement;var id=root.GetProperty("id").GetGuid();
            foreach(var table in new[]{"OasisHolons","OasisAvatars","OasisAvatarDetails"}){
                var read=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});
                Assert.AreEqual(HttpStatusCode.OK,read.HttpStatusCode);
                if(pair.Key.StartsWith("record-history-") && table=="OasisHolons"){
                    var json=read.Item["data_json"].S;var doc=JObject.Parse(json);
                    Assert.AreEqual(id,doc["Id"]!.ToObject<Guid>());Assert.AreEqual("history-"+id.ToString("N"),doc["Name"]!.Value<string>());
                    Assert.AreEqual(2,doc["Version"]!.Value<int>());Assert.AreEqual("second",doc["MetaData"]!["owned_history_fixture"]!.Value<string>());
                    var history=(JArray)doc[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,history.Count);
                    Assert.AreEqual(1,history[0]!["Version"]!.Value<int>());Assert.AreEqual("first",history[0]!["MetaData"]!["owned_history_fixture"]!.Value<string>());
                    observed.Add((id,json));
                }else Assert.AreEqual(0,read.Item.Count,"Reflection failed before native dispatch for this identity.");
            }
            if(root.TryGetProperty("childId",out var child)){
                var read=await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=child.GetGuid().ToString()}}},ConsistentRead=true});
                Assert.AreEqual(HttpStatusCode.OK,read.HttpStatusCode);Assert.AreEqual(0,read.Item.Count);
            }
        }
        Assert.AreEqual(2,observed.Count);
        // All originals validated before any cleanup; exact-data CAS, never replay the failed write.
        foreach(var row in observed)await RemoveOwned(verifier,"OasisHolons",row.Id,row.Json);
        foreach(var pair in hashes){
            var path=Path.Combine(work,pair.Key);Assert.AreEqual(pair.Value,Hash(path));
            File.Move(path,Path.Combine(work,Path.GetFileNameWithoutExtension(path)+".reconciled.json"));
        }
    }
}
