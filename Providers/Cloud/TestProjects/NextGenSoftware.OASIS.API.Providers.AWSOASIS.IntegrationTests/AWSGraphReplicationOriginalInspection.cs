using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using System.Net;
using System.Text;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public sealed partial class AWSNativeRunLifecycle
{
    [TestMethod]
    public async Task CleanupOnlyHashVerifiedGeneratedGraphFailure()
    {
        Assert.AreEqual("reviewed-generated-graph-original", Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        var report = Path.Combine(Work(), "aws-full-native-current.trx");
        var intent = Path.Combine(Work(), "aws-full-suite.pending.json");
        var proofPath = Path.Combine(Work(), "aws-generated-graph-original-inspection.json");
        var db = Path.Combine(AppContext.BaseDirectory, "TestResults", "AWSRoutingFiles", "delete-routing.db");
        void ValidateOriginals()
        {
            Assert.AreEqual("45C6CCE6C2A3790191AC14AEE4DB3EC3AA36BE50DC2A5CA166131B3E43BEA342", Hash(report));
            Assert.AreEqual("4811A1E4A6862AFAE74043EE5F1FDBC09BA9F6C775E83C1DC8CFB3E44F58133D", Hash(intent));
            Assert.AreEqual("579A9B90435B848CFF14DB1311D509CEE4273088CD8D8A187139D4D8A880BF1C", Hash(proofPath));
            Assert.AreEqual("7CC5887614B5ACE36E6D79F5D0A2A0C96DE45D41B14CDCC2B87A4E7AB0B5DA9D", Hash(db));
            Assert.IsFalse(File.Exists(db + "-wal")); Assert.IsFalse(File.Exists(db + "-shm"));
        }
        ValidateOriginals();
        using (var native = new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False"))
        {
            Assert.AreEqual(0, native.Holons.AsNoTracking().Count());
            Assert.AreEqual(0, native.Avatars.AsNoTracking().Count());
            Assert.AreEqual(0, native.AvatarDetails.AsNoTracking().Count());
        }
        using var proof = JsonDocument.Parse(File.ReadAllBytes(proofPath));
        Assert.IsTrue(proof.RootElement.GetProperty("readOnly").GetBoolean());
        var expected = proof.RootElement.GetProperty("allRows").EnumerateArray().ToDictionary(
            row => (row.GetProperty("table").GetString()!, row.GetProperty("id").GetGuid().ToString()),
            row => row.GetProperty("sha256").GetString()!);
        Assert.AreEqual(83, expected.Count);
        using var client = Client();
        var observed = new List<(string Table, string Id, string Json)>();
        foreach (var table in Tables)
            foreach (var row in await Rows(client, table))
            {
                var id = row["id"].S; var json = row["data_json"].S;
                Assert.IsTrue(expected.TryGetValue((table, id), out var hash), "Unexpected original row: " + table + "/" + id);
                Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
                observed.Add((table, id, json));
            }
        Assert.AreEqual(expected.Count, observed.Count);
        ValidateOriginals();
        // Persist the reviewed exact set BEFORE conditional deletion. Failure or
        // an uncertain acknowledgement preserves this journal and forbids replay.
        await DurableJson(Path.Combine(Work(), "aws-generated-graph-reviewed-cleanup.json"), new {
            state = "reviewed-original-cleanup-started", sourceProofSha256 = Hash(proofPath),
            sqliteSha256 = Hash(db), rows = expected.Select(row => new { table = row.Key.Item1, id = row.Key.Item2, sha256 = row.Value }) });
        foreach (var row in observed)
        {
            var key = new Dictionary<string, AttributeValue> { { "id", new() { S = row.Id } } };
            var deleted = await client.DeleteItemAsync(new DeleteItemRequest { TableName = row.Table, Key = key,
                ConditionExpression = "data_json = :expected", ExpressionAttributeValues = new() { { ":expected", new() { S = row.Json } } },
                ReturnValues = ReturnValue.ALL_OLD });
            Assert.AreEqual(HttpStatusCode.OK, deleted.HttpStatusCode);
            Assert.AreEqual(row.Json, deleted.Attributes["data_json"].S);
            Assert.AreEqual(0, (await client.GetItemAsync(new GetItemRequest { TableName = row.Table, Key = key, ConsistentRead = true })).Item.Count);
        }
        foreach (var table in Tables) Assert.AreEqual(0, (await Rows(client, table)).Count);
        ValidateOriginals();
        File.Delete(db); Assert.IsFalse(File.Exists(db));
        File.Delete(intent); Assert.IsFalse(File.Exists(intent));
        await DurableJson(Path.Combine(Work(), "aws-generated-graph-reviewed-cleanup-complete.json"), new {
            state = "verified-owned-originals-absent", rowsDeleted = observed.Count, sqliteRemoved = true,
            sourceReportSha256 = Hash(report), sourceProofSha256 = Hash(proofPath), replayedWrites = 0 });
    }

    [TestMethod]
    public async Task ReadOnlyInspectOriginalGeneratedGraphReplicationFailure()
    {
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        var intent=Path.Combine(Work(),"aws-full-suite.pending.json");
        var db=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","delete-routing.db");
        static string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        void Original()
        {
            Assert.AreEqual("45C6CCE6C2A3790191AC14AEE4DB3EC3AA36BE50DC2A5CA166131B3E43BEA342",Hash(report));
            Assert.AreEqual("4811A1E4A6862AFAE74043EE5F1FDBC09BA9F6C775E83C1DC8CFB3E44F58133D",Hash(intent));
            Assert.AreEqual("7CC5887614B5ACE36E6D79F5D0A2A0C96DE45D41B14CDCC2B87A4E7AB0B5DA9D",Hash(db));
            Assert.IsFalse(File.Exists(db+"-wal"));Assert.IsFalse(File.Exists(db+"-shm"));
        }
        Original();
        using(var marker=JsonDocument.Parse(File.ReadAllBytes(intent)))
        {
            Assert.AreEqual("verified-empty-before-exclusive-native-suite",marker.RootElement.GetProperty("state").GetString());
            Assert.AreEqual("http://127.0.0.1:18000",marker.RootElement.GetProperty("endpoint").GetString());
        }
        using(var native=new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False"))
        {
            Assert.AreEqual(0,native.Holons.AsNoTracking().Count());
            Assert.AreEqual(0,native.Avatars.AsNoTracking().Count());
            Assert.AreEqual(0,native.AvatarDetails.AsNoTracking().Count());
        }
        using var client=Client();var inventory=new List<object>();var graph=new List<(Guid Id,Guid Parent,string Name,int Version)>();
        foreach(var table in Tables)
        {
            var rows=await Rows(client,table);
            foreach(var row in rows)
            {
                var id=Guid.Parse(row["id"].S);var json=row["data_json"].S;
                using var canonical=JsonDocument.Parse(json);Assert.AreEqual(id,canonical.RootElement.GetProperty("Id").GetGuid());
                var name=canonical.RootElement.TryGetProperty("Name",out var label)?label.GetString():null;
                if(table=="OasisHolons"&&name is "owned-v2-graph-root" or "owned-v2-graph-child" or "owned-v2-graph-leaf")
                    graph.Add((id,canonical.RootElement.GetProperty("ParentHolonId").GetGuid(),name!,canonical.RootElement.GetProperty("Version").GetInt32()));
                inventory.Add(new{table,id,sha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)))});
            }
        }
        Assert.AreEqual(3,graph.Count);var root=graph.Single(row=>row.Name=="owned-v2-graph-root");
        var child=graph.Single(row=>row.Name=="owned-v2-graph-child");var leaf=graph.Single(row=>row.Name=="owned-v2-graph-leaf");
        Assert.AreEqual(Guid.Empty,root.Parent);Assert.AreEqual(root.Id,child.Parent);Assert.AreEqual(child.Id,leaf.Parent);
        // This original routing test did not enable logical version history.
        // Record the observed versions; do not invent a version-one prerequisite.
        Assert.IsTrue(graph.All(row=>row.Id!=Guid.Empty&&row.Version>=0));Original();
        await DurableJson(Path.Combine(Work(),"aws-generated-graph-original-inspection.json"),new{
            state="original-read-only-inspected",readOnly=true,sourceReportSha256=Hash(report),sourceIntentSha256=Hash(intent),sqliteSha256=Hash(db),
            primaryGraph=graph.Select(row=>new{row.Id,row.Parent,row.Name,row.Version}),sqliteHolons=0,sqliteAvatars=0,sqliteDetails=0,
            originalReplicaExceptionRetained=false,allRows=inventory,
            scope="Original primary3records and empty SQLite observed; no save/replay/cleanup and no reconstruction of the missing original replica exception"});
        Original();
    }
}
