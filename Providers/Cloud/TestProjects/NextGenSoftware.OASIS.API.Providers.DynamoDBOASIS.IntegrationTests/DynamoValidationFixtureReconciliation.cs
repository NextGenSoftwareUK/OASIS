using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    // Exact consumed-run recovery; never a general pending-journal bypass or mutation replay.
    [TestMethod]
    public async Task ReconcileKnownValidationRejectionFixtures()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        const string reportHash="339A00426E80869F82FA8EBE1273A490BA40D94B6829791A35AA04AEABB95E53";
        static string FileHash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        static string DataHash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        var report=Path.Combine(work,"dynamo-history-native-current.trx");Assert.AreEqual(reportHash,FileHash(report));
        var preserved=Path.Combine(work,"dynamo-validation-native-failed.trx");
        if(File.Exists(preserved))Assert.AreEqual(reportHash,FileHash(preserved));else File.Copy(report,preserved);
        var hashes=new Dictionary<string,string>{
            ["fault-history-capacity.pending.json"]="F6FFFE745C7485AD848ECF2C49F9618B764829D39A8500D1E2546B0E2C3884E3",
            ["public-capacity-0.pending.json"]="AA5B867A4B823EF0D81DD983324E0723B6F1472EECC7AD9E48B6E1F16BB071E9",
            ["public-capacity-1.pending.json"]="A6316262A6F3C7F84C613E4751641121E1D21C2F4FF40E0AFF5AF24FC5A60922",
            ["public-capacity-2.pending.json"]="DBA2C656247A600297E11B231BAD913A86CBA9C4D5E783ECDC010D864627063A",
            ["public-capacity-3.pending.json"]="C1E7B2C4DA598A28067C1FACBC06BE668C0B0CAF6760A50A11B6D25EEDFBBD74",
            ["public-capacity-4.pending.json"]="76B76B41D7BD18F095FB29907F7325229D328B38F2DD754C062C5D1E30B90659",
            ["public-capacity-5.pending.json"]="51A6EADAB0326F0789D1D0171EB3852164E491CF80B90FB7F833F3CFDB5EA13C"};
        CollectionAssert.AreEquivalent(hashes.Keys.ToArray(),Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        var pending=Path.Combine(work,"validation-cleanup.pending.json");var inventory=Path.Combine(work,"validation-reconciliation.json");Assert.IsFalse(File.Exists(inventory));
        using var verifier=Client();var observed=new List<(string Journal,string Table,Guid Id,string Json,string JournalHash)>();
        foreach(var pair in hashes){
            var path=Path.Combine(work,pair.Key);Assert.AreEqual(pair.Value,FileHash(path));using var intent=JsonDocument.Parse(await File.ReadAllBytesAsync(path));var root=intent.RootElement;
            var id=root.GetProperty("id").GetGuid();var table=root.GetProperty("table").GetString()!;var isPublic=root.TryGetProperty("variant",out var variant);
            if(isPublic){Assert.AreEqual("before-native-public-capacity",root.GetProperty("state").GetString());Assert.AreEqual(new[]{"OasisAvatars","OasisAvatarDetails","OasisHolons"}[variant.GetInt32()%3],table);}
            else{Assert.AreEqual("capacity",root.GetProperty("mode").GetString());Assert.AreEqual("before-native-fault-fixture",root.GetProperty("state").GetString());Assert.AreEqual("OasisHolons",table);}
            var read=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,read.HttpStatusCode);Assert.AreEqual(id.ToString(),read.Item["id"].S);
            var json=read.Item["data_json"].S;var document=JObject.Parse(json);Assert.AreEqual(id,Guid.Parse(document["Id"]!.Value<string>()!));Assert.AreEqual(isPublic?2:1,document["Version"]!.Value<int>());
            var metadata=(JObject)document["MetaData"]!;Assert.AreEqual(1,metadata.Count);Assert.AreEqual(isPublic?"second":"first",metadata["owned_history_fixture"]!.Value<string>());
            var history=(JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(isPublic?1:0,history.Count);
            var expectedName=isPublic?"capacity-"+id.ToString("N"):"owned-fault-fixture";
            Assert.AreEqual(expectedName,document[table=="OasisHolons"?"Name":"Username"]!.Value<string>());
            if(isPublic){Assert.AreEqual(1,history[0]!["Version"]!.Value<int>());Assert.AreEqual("first",history[0]!["MetaData"]!["owned_history_fixture"]!.Value<string>());Assert.AreEqual(history[0]!["VersionId"]!.Value<string>(),document["PreviousVersionId"]!.Value<string>());}
            observed.Add((path,table,id,json,pair.Value));
        }
        Assert.AreEqual(7,observed.Select(r=>r.Id).Distinct().Count());
        // Verify ALL originals before deleting any; persist exact identities/content digests first.
        using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{reportHash,records=observed.Select(r=>new{r.Table,r.Id,DataHash=DataHash(r.Json),r.JournalHash})}));file.Flush(true);
        }
        foreach(var row in observed)await RemoveOwned(verifier,row.Table,row.Id,row.Json);
        foreach(var row in observed)File.Delete(row.Journal);
        File.Move(pending,inventory);
    }
}
