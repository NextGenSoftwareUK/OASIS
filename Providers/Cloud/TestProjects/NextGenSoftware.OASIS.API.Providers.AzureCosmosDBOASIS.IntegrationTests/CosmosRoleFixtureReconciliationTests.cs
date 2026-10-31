using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class CosmosNativeStorageTests
{
    [TestMethod][TestCategory("RoleReconciliation")]
    public async Task ReconcileExactOwnedNullRoleDeleteFixtureWithoutReplay()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_COSMOSDB_NATIVE"));
        var report="C:/Source/OASIS/TestResults/CosmosDBCurrent/cosmos-native-current.trx";
        Assert.AreEqual("19099E1CA870481802446E3545C45E43167F255983B30CEFC7D725A6053D0547",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var path=Path.Combine(AppContext.BaseDirectory,"TestResults","CosmosDBRouting","delete-backup.db");
        Assert.IsFalse(File.Exists(path+"-wal"));Assert.IsFalse(File.Exists(path+"-shm"));
        string Hash()=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string sqliteHash="5C0F67FEE3F4FDEABEEC778328D4AA486496FB90B8B05B27F429B0DCC816D816";Assert.AreEqual(sqliteHash,Hash());
        const string id="2666b81f-18dc-4c45-adad-6982403f2630";
        using(var sqlite=new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=path,Mode=Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))
        {
            await sqlite.OpenAsync();using var command=sqlite.CreateCommand();command.CommandText="SELECT Id, Name FROM Holon";
            using var reader=await command.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());Assert.AreEqual(Guid.Parse(id),Guid.Parse(reader.GetString(0)));Assert.AreEqual("owned delete",reader.GetString(1));Assert.IsFalse(await reader.ReadAsync());
        }
        using var client=new CosmosClient("https://127.0.0.1:8081",EmulatorKey,new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,
            MaxRetryAttemptsOnRateLimitedRequests=0,HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
        var container=client.GetContainer("oasis-cosmos-crud","holonItems");
        using var response=await container.ReadItemStreamAsync(id,new PartitionKey(id));response.EnsureSuccessStatusCode();
        using var body=new StreamReader(response.Content);var document=JObject.Parse(await body.ReadToEndAsync());
        using var feed=container.GetItemQueryIterator<JObject>(new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id",id));
        var feedRows=new List<JObject>();while(feed.HasMoreResults)feedRows.AddRange(await feed.ReadNextAsync());Assert.AreEqual(1,feedRows.Count);
        Assert.AreEqual("2D31DB206BA372F8F2E3CBDF36E9DB5F7BBBCC12B3B28642126D711870E9B023",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(feedRows[0].ToString(Newtonsoft.Json.Formatting.None)))));
        Assert.AreEqual("DF4D6DCF4DBF2EC89CD008B0EE9F42CDF59F2613D24EC2B5FD3E139F0067416C",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.ToString(Newtonsoft.Json.Formatting.None)))));
        // Read-only inspector E84F644C... proved this EXACT original point response has
        // one extra service-owned _self field. Both full hashes above still pin its value.
        // Do not replace them or normalize any canonical/history field to permit cleanup.
        Assert.IsNull(feedRows[0].Property("_self"));
        Assert.AreEqual(JTokenType.String,document["_self"]?.Type);
        Assert.IsFalse(string.IsNullOrWhiteSpace(document["_self"]!.Value<string>()));
        var comparablePoint=(JObject)document.DeepClone();comparablePoint.Remove("_self");
        Assert.IsTrue(JToken.DeepEquals(feedRows[0],comparablePoint),"Every field other than the independently verified point-only _self metadata must match exactly.");
        TestContext.WriteLine("Both original full feed/point digests verified; all fields identical except inspected point-only _self service metadata.");
        Assert.AreEqual(id,document["Id"]!.Value<string>());Assert.AreEqual("owned delete",document["Name"]!.Value<string>());Assert.AreEqual(JTokenType.Null,document["ChildRoleManifestJson"]!.Type);
        Assert.AreEqual("3a752f11-a038-4a5f-9b16-fd26274ada25",response.Headers.ETag);
        using var deleted=await container.DeleteItemStreamAsync(id,new PartitionKey(id),new ItemRequestOptions{IfMatchEtag=response.Headers.ETag});Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);
        using var absent=await container.ReadItemStreamAsync(id,new PartitionKey(id));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
        Assert.AreEqual(sqliteHash,Hash());File.Delete(path);
        TestContext.WriteLine("Exact owned null-role fixture removed with original ETag; secondary exact hash+identity verified before disposable file removal; no routed mutation replay.");
    }
}
