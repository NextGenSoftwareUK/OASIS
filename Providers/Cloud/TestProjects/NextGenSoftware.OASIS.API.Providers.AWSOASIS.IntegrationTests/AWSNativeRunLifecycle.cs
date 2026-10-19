using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

// Explicit runner phases, excluded from the provider suite itself. A failed suite retains its marker/data.
[TestClass][DoNotParallelize]
public sealed class AWSNativeRunLifecycle
{
    private static readonly string[] Tables={"OasisAvatars","OasisAvatarDetails","OasisHolons"};
    private static string Work()=>Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")??throw new InvalidOperationException("Retained runner required.");
    private static AmazonDynamoDBClient Client()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        return new(new BasicAWSCredentials("OwnedFullSuite","owned-synthetic-secret"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0});
    }
    private static async Task<List<Dictionary<string,AttributeValue>>> Rows(AmazonDynamoDBClient client,string table)
    {
        var rows=new List<Dictionary<string,AttributeValue>>();Dictionary<string,AttributeValue>? cursor=null;
        do{
            var response=await client.ScanAsync(new ScanRequest{TableName=table,ConsistentRead=true,ExclusiveStartKey=cursor});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);rows.AddRange(response.Items);cursor=response.LastEvaluatedKey;
        }while(cursor?.Count>0);
        return rows;
    }
    private static async Task DurableJson(string path,object document)
    {
        using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(document));stream.Flush(true);
    }
    [TestMethod]
    public async Task ReadOnlyReconcileKnownPreReadFailover()
    {
        var reportPath=Path.Combine(Work(),"aws-known-failed-native.trx");
        Assert.AreEqual("51C9C22ABD431CB36E9B1FF369047E418D0D221F3F23ACCB22F1CDDB9D1D7829",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(reportPath))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("2026-10-19T08:39:41.124211Z",intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        var id=Guid.Parse("5a0ca78a-7950-4208-b031-1bd747fa51ec");
        var original=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","Holons",id+".json");
        var bytes=await File.ReadAllBytesAsync(original);
        Assert.AreEqual("D099DD32795CA5DC7E5039DB3CBF2B1B3084B884588A5050864E25313E9A9A21",Convert.ToHexString(SHA256.HashData(bytes)));
        using var secondary=JsonDocument.Parse(bytes);Assert.AreEqual(id,secondary.RootElement.GetProperty("Id").GetGuid());
        Assert.AreEqual("must-not-replay",secondary.RootElement.GetProperty("Name").GetString());
        Assert.AreEqual(id.ToString(),secondary.RootElement.GetProperty("ProviderUniqueStorageKey").GetProperty("LocalFileOASIS").GetString());
        using var client=Client();
        var native=await client.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});
        Assert.AreEqual(HttpStatusCode.OK,native.HttpStatusCode);Assert.AreEqual(0,native.Item.Count);
        var inventory=new List<object>();
        foreach(var table in Tables){var rows=await Rows(client,table);inventory.Add(new{table,count=rows.Count});}
        await DurableJson(Path.Combine(Work(),"aws-known-pre-read-reconciliation.json"),new{id,primaryAbsent=true,secondarySha256=Convert.ToHexString(SHA256.HashData(bytes)),readOnly=true,inventory});
    }
    [TestMethod]
    public async Task BeginFromVerifiedEmptyRetainedTables()
    {
        var marker=Path.Combine(Work(),"aws-full-suite.pending.json");Assert.IsFalse(File.Exists(marker),"Reconcile prior full-suite evidence before another run.");
        var provider=new AWSOASIS("us-east-1","OwnedFullSuite","owned-synthetic-secret","http://127.0.0.1:18000");
        try{Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);}finally{await provider.DeActivateProviderAsync();}
        using var client=Client();foreach(var table in Tables)Assert.AreEqual(0,(await Rows(client,table)).Count,"Nonempty retained table is not disposable: "+table);
        await DurableJson(marker,new{state="verified-empty-before-exclusive-native-suite",endpoint="http://127.0.0.1:18000",tables=Tables,startedUtc=DateTime.UtcNow});
    }
    [TestMethod]
    public async Task CleanupOnlyAfterVerifiedFullSuitePass()
    {
        Assert.AreEqual("verified-full-suite-pass",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        await CleanupObservedAsync("aws-full-suite-cleanup-current.json");
    }
    [TestMethod]
    public async Task CleanupKnownReadOnlyReconciledRun()
    {
        Assert.AreEqual("known-run-reconciled",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        Assert.AreEqual("51C9C22ABD431CB36E9B1FF369047E418D0D221F3F23ACCB22F1CDDB9D1D7829",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-known-failed-native.trx")))));
        using var proof=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-known-pre-read-reconciliation.json")));
        Assert.IsTrue(proof.RootElement.GetProperty("readOnly").GetBoolean());Assert.IsTrue(proof.RootElement.GetProperty("primaryAbsent").GetBoolean());
        var id=Guid.Parse("5a0ca78a-7950-4208-b031-1bd747fa51ec");Assert.AreEqual(id,proof.RootElement.GetProperty("id").GetGuid());
        var original=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","Holons",id+".json");
        Assert.AreEqual("D099DD32795CA5DC7E5039DB3CBF2B1B3084B884588A5050864E25313E9A9A21",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        using var client=Client();Assert.AreEqual(0,(await client.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true})).Item.Count);
        var expectedCounts=new[]{7,3,16};for(var index=0;index<Tables.Length;index++)Assert.AreEqual(expectedCounts[index],(await Rows(client,Tables[index])).Count);
        await CleanupObservedAsync("aws-known-run-cleanup.json",original);
    }
    private static async Task CleanupObservedAsync(string inventoryName,string? reconciledLocalOriginal=null)
    {
        var marker=Path.Combine(Work(),"aws-full-suite.pending.json");
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(marker));
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(value=>value.GetString()!).ToArray());
        using var client=Client();var observed=new List<(string Table,string Id,string Json)>();
        foreach(var table in Tables)foreach(var row in await Rows(client,table)){
            Assert.IsTrue(row.TryGetValue("id",out var id)&&Guid.TryParse(id.S,out var parsed)&&parsed!=Guid.Empty);
            Assert.IsTrue(row.TryGetValue("data_json",out var json)&&!string.IsNullOrWhiteSpace(json.S));
            observed.Add((table,id!.S,json!.S));
        }
        // Persist exact identities plus content digests before any conditional cleanup; no secrets/payload dump.
        var inventory=Path.Combine(Work(),inventoryName);Assert.IsFalse(File.Exists(inventory),"Review retained cleanup evidence before replacing it.");
        await DurableJson(inventory,observed.Select(row=>new{table=row.Table,id=row.Id,sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.Json)))}).ToArray());
        foreach(var row in observed){
            var key=new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id}}};
            var deleted=await client.DeleteItemAsync(new DeleteItemRequest{TableName=row.Table,Key=key,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=row.Json}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,deleted.HttpStatusCode);Assert.AreEqual(row.Json,deleted.Attributes["data_json"].S);
            Assert.AreEqual(0,(await client.GetItemAsync(new GetItemRequest{TableName=row.Table,Key=key,ConsistentRead=true})).Item.Count);
        }
        foreach(var table in Tables)Assert.AreEqual(0,(await Rows(client,table)).Count);
        if(reconciledLocalOriginal!=null){
            Assert.AreEqual("D099DD32795CA5DC7E5039DB3CBF2B1B3084B884588A5050864E25313E9A9A21",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(reconciledLocalOriginal))));
            File.Delete(reconciledLocalOriginal);Assert.IsFalse(File.Exists(reconciledLocalOriginal));
        }
        File.Delete(marker);
    }
}
