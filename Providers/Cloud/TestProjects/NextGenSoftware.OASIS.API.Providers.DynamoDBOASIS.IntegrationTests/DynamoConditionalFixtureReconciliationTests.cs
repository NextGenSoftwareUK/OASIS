using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoConditionalFixtureReconciliationTests
{
    [TestMethod]
    public async Task ReconcileOriginalFourInitialGraphAssertionFixtures()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var report=Path.Combine(work,"dynamo-conditional-native-current.trx");
        Assert.AreEqual("C3F05D24795A1A0A0A14973110802E0A5D218837BA60EF77C475DE68A5C85EE0",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var journals=Directory.GetFiles(work,"*.pending.json").OrderBy(path=>path).ToArray();
        CollectionAssert.AreEquivalent(new[]{"atomic-graph-False-False.pending.json","atomic-graph-False-True.pending.json","atomic-graph-True-False.pending.json","atomic-graph-True-True.pending.json"},
            journals.Select(Path.GetFileName).ToArray());
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        using var client=new AmazonDynamoDBClient(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),new AmazonDynamoDBConfig {
            ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0
        });
        var originals=new List<(string Table,string Id,string Field,string Json)>();
        async Task<Dictionary<string,AttributeValue>> Read(string table,string id) {
            var result=await client.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result.Item;
        }
        foreach(var journal in journals) {
            var expected=Path.GetFileName(journal) switch {
                "atomic-graph-False-False.pending.json"=>"3111888ECF3A0D3E50FB7FF2FE8943907D3FD1EC5F207DBB6AE728C7CE55033E",
                "atomic-graph-False-True.pending.json"=>"17F8665C369C93627F83ED748473BA0CCF8C66F8F6714401DE5EA8474FBD7712",
                "atomic-graph-True-False.pending.json"=>"BC89177147DF143806640CCE301D561C834C209C2328A6D133AF6A23D983350E",
                "atomic-graph-True-True.pending.json"=>"A84AAB4EC3157956136CAFA49BF7B73C3918B0D6C32CB652958D818848E6614B",
                _=>throw new InvalidDataException("Unexpected original journal.")
            };
            Assert.AreEqual(expected,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(journal))));
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-atomic-native-write",intent.RootElement.GetProperty("state").GetString());
            var ids=intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            var ops=intent.RootElement.GetProperty("operations").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            var authority=intent.RootElement.GetProperty("authority").GetString()!;
            Assert.IsTrue(authority is "DynamoDBOASIS" or "AWSOASIS");Assert.AreEqual(2,ids.Length);Assert.AreEqual(4,ops.Length);
            foreach(var op in ops.Skip(1))Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",authority+":"+op.ToString("N"))).Count);
            var key=authority+":"+ops[0].ToString("N");var receipt=await Read("OasisHolonWriteReceipts",key);
            Assert.AreEqual(key,receipt["id"].S);Assert.AreEqual(64,receipt["request_hash"].S.Length);
            var roots=ConditionalHolonReceipt.Read(receipt["result_json"].S);
            Assert.AreEqual(ids[0],roots.Single().Id);Assert.AreEqual(ids[1],roots.Single().Children.Single().Id);
            originals.Add(("OasisHolonWriteReceipts",key,"result_json",receipt["result_json"].S));
            for(var index=0;index<ids.Length;index++) {
                var item=await Read("OasisHolons",ids[index].ToString());var json=item["data_json"].S;
                using var document=JsonDocument.Parse(json);
                Assert.AreEqual(ids[index],document.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(index==0?"owned atomic root":"owned atomic child",document.RootElement.GetProperty("Name").GetString());
                Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());
                if(index==0)Assert.AreEqual(0.1234567890123456789012345678m,document.RootElement.GetProperty("MetaData").GetProperty("exact").GetDecimal());
                originals.Add(("OasisHolons",ids[index].ToString(),"data_json",json));
            }
        }
        var cleanupIntent=Path.Combine(work,"conditional-original-cleanup-intent.json");
        Assert.IsFalse(File.Exists(cleanupIntent));
        using(var file=new FileStream(cleanupIntent,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(originals.Select(row=>new {
                row.Table,row.Id,row.Field,sha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(row.Json)))
            })));file.Flush(true);
        }
        foreach(var row in originals) {
            var response=await client.DeleteItemAsync(new DeleteItemRequest{TableName=row.Table,Key=new(){{"id",new(){S=row.Id}}},
                ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json",row.Field}},
                ExpressionAttributeValues=new(){{":expected",new(){S=row.Json}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);Assert.AreEqual(row.Json,response.Attributes[row.Field].S);
            Assert.AreEqual(0,(await Read(row.Table,row.Id)).Count);
        }
        var proof=Path.Combine(work,"conditional-original-cleanup-verified.json");
        using(var file=new FileStream(proof,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{removed=originals.Count,verifiedUtc=DateTime.UtcNow,
                intentSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(cleanupIntent)))}));file.Flush(true);
        }
        foreach(var journal in journals)File.Delete(journal);
    }
}
