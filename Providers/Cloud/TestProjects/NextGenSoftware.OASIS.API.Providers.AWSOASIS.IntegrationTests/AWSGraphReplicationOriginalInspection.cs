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
    public async Task ReadOnlyInspectCurrentCaptureFailureDynamo()
    {
        static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        var intent=Path.Combine(Work(),"aws-full-suite.pending.json");
        var db=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","delete-routing.db");
        var sqliteProof=Path.Combine(Work(),"aws-current-capture-sqlite-inspection.json");
        void Originals()
        {
            Assert.AreEqual("4828E3BDD2A815802DF0584FFDDF7B8923FE73C928A1B44D6E2553FC9CB80B1B",Hash(report));
            Assert.AreEqual("4318BC3C95825DB4C68329819B6B0CB7DF4E5A03EFBEB51F344A1E9233975C42",Hash(intent));
            Assert.AreEqual("91CDEEECC747759105DA3D0426A72E80DADBA91DFA0C1F2215AB54B85BC95692",Hash(db));
            Assert.AreEqual("399A9D55D67DB13C57F3D2342E92EB259D986566C09DB6DBD690526FF90AAEBE",Hash(sqliteProof));
            Assert.IsFalse(File.Exists(db+"-wal"));Assert.IsFalse(File.Exists(db+"-shm"));
        }
        Originals();
        using var prior=JsonDocument.Parse(File.ReadAllBytes(sqliteProof));
        var sqliteIds=prior.RootElement.GetProperty("rows").EnumerateArray().Select(row=>row.GetProperty("Id").GetGuid()).ToHashSet();
        Assert.AreEqual(3,sqliteIds.Count);
        using var client=Client();var inventory=new List<object>();var matching=new List<Guid>();var nativeGraphs=new List<Guid>();
        foreach(var table in Tables)
            foreach(var row in await Rows(client,table))
            {
                var id=Guid.Parse(row["id"].S);var json=row["data_json"].S;
                using var body=JsonDocument.Parse(json);Assert.AreEqual(id,body.RootElement.GetProperty("Id").GetGuid());
                if(table=="OasisHolons")
                {
                    if(sqliteIds.Contains(id))matching.Add(id);
                    if(body.RootElement.TryGetProperty("Name",out var name)&&name.GetString() is "owned-v2-graph-root" or "owned-v2-graph-child" or "owned-v2-graph-leaf")nativeGraphs.Add(id);
                }
                inventory.Add(new{table,id,sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))});
            }
        Originals();
        await DurableJson(Path.Combine(Work(),"aws-current-capture-dynamo-inspection.json"),new {
            state="current-native-read-only-inspected",readOnly=true,sourceReportSha256=Hash(report),sourceIntentSha256=Hash(intent),
            sqliteSha256=Hash(db),sqliteInspectionSha256=Hash(sqliteProof),allRows=inventory,
            matchingSqliteIds=matching,nativeGraphIds=nativeGraphs,replayedWrites=0,originalInnerExceptionRetained=false });
        Originals();
    }

    [TestMethod]
    public async Task ReadOnlyInspectCurrentCaptureFailureSqlite()
    {
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        var intent=Path.Combine(Work(),"aws-full-suite.pending.json");
        var db=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","delete-routing.db");
        static string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        void Original()
        {
            Assert.AreEqual("4828E3BDD2A815802DF0584FFDDF7B8923FE73C928A1B44D6E2553FC9CB80B1B",Hash(report));
            Assert.AreEqual("4318BC3C95825DB4C68329819B6B0CB7DF4E5A03EFBEB51F344A1E9233975C42",Hash(intent));
            Assert.AreEqual("91CDEEECC747759105DA3D0426A72E80DADBA91DFA0C1F2215AB54B85BC95692",Hash(db));
            Assert.IsFalse(File.Exists(db+"-wal"));Assert.IsFalse(File.Exists(db+"-shm"));
        }
        Original();
        using var native=new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False");
        var rows=native.Holons.AsNoTracking().ToArray();
        Assert.AreEqual(0,native.Avatars.AsNoTracking().Count());
        Assert.AreEqual(0,native.AvatarDetails.AsNoTracking().Count());
        Assert.IsTrue(rows.All(row=>row.Name is "owned-v2-graph-root" or "owned-v2-graph-child" or "owned-v2-graph-leaf"));
        // Report observed identities/phase evidence, never reconstruct the omitted
        // original exception or mutate/query through the provider save surface.
        Original();
        await DurableJson(Path.Combine(Work(),"aws-current-capture-sqlite-inspection.json"),new {
            state="original-sqlite-read-only-inspected",readOnly=true,sourceReportSha256=Hash(report),
            sourceIntentSha256=Hash(intent),sqliteSha256=Hash(db),holonCount=rows.Length,
            rows=rows.Select(row=>new{row.Id,row.ParentHolonId,row.Name,row.Version,row.DeletedDate,
                payloadSha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.PersistentValueJson)))}),
            nativeDynamoInspected=false,replayedWrites=0,originalInnerExceptionRetained=false });
        Original();
    }

    [TestMethod]
    public async Task CleanupOnlyHashVerifiedGeneratedGraphFailure()
        => await CleanupReviewedGraphFailure(false);

    [TestMethod]
    public async Task CleanupOnlyHashVerifiedCurrentCaptureFailure()
        => await CleanupReviewedGraphFailure(true);

    private async Task CleanupReviewedGraphFailure(bool current)
    {
        Assert.AreEqual(current ? "reviewed-current-capture-original" : "reviewed-generated-graph-original", Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        var report = Path.Combine(Work(), "aws-full-native-current.trx");
        var intent = Path.Combine(Work(), "aws-full-suite.pending.json");
        var proofPath = Path.Combine(Work(), current ? "aws-current-capture-dynamo-inspection.json" : "aws-generated-graph-original-inspection.json");
        var prefix = current ? "aws-current-capture-reviewed-cleanup" : "aws-generated-graph-reviewed-cleanup";
        var db = Path.Combine(AppContext.BaseDirectory, "TestResults", "AWSRoutingFiles", "delete-routing.db");
        void ValidateOriginals()
        {
            Assert.AreEqual(current ? "4828E3BDD2A815802DF0584FFDDF7B8923FE73C928A1B44D6E2553FC9CB80B1B" : "45C6CCE6C2A3790191AC14AEE4DB3EC3AA36BE50DC2A5CA166131B3E43BEA342", Hash(report));
            Assert.AreEqual(current ? "4318BC3C95825DB4C68329819B6B0CB7DF4E5A03EFBEB51F344A1E9233975C42" : "4811A1E4A6862AFAE74043EE5F1FDBC09BA9F6C775E83C1DC8CFB3E44F58133D", Hash(intent));
            Assert.AreEqual(current ? "A72C4A949CB9D4DD8111231525DC7DC4688B740CC0ACEE2AEC39FBCDC0992E99" : "579A9B90435B848CFF14DB1311D509CEE4273088CD8D8A187139D4D8A880BF1C", Hash(proofPath));
            Assert.AreEqual(current ? "91CDEEECC747759105DA3D0426A72E80DADBA91DFA0C1F2215AB54B85BC95692" : "7CC5887614B5ACE36E6D79F5D0A2A0C96DE45D41B14CDCC2B87A4E7AB0B5DA9D", Hash(db));
            Assert.IsFalse(File.Exists(db + "-wal")); Assert.IsFalse(File.Exists(db + "-shm"));
        }
        ValidateOriginals();
        using (var native = new DataContext($"Data Source={db};Mode=ReadOnly;Pooling=False"))
        {
            Assert.AreEqual(current ? 3 : 0, native.Holons.AsNoTracking().Count());
            Assert.AreEqual(0, native.Avatars.AsNoTracking().Count());
            Assert.AreEqual(0, native.AvatarDetails.AsNoTracking().Count());
        }
        using var proof = JsonDocument.Parse(File.ReadAllBytes(proofPath));
        Assert.IsTrue(proof.RootElement.GetProperty("readOnly").GetBoolean());
        var expected = proof.RootElement.GetProperty("allRows").EnumerateArray().ToDictionary(
            row => (row.GetProperty("table").GetString()!, row.GetProperty("id").GetGuid().ToString()),
            row => row.GetProperty("sha256").GetString()!);
        Assert.AreEqual(current ? 80 : 83, expected.Count);
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
        await DurableJson(Path.Combine(Work(), prefix + ".json"), new {
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
        await DurableJson(Path.Combine(Work(), prefix + "-complete.json"), new {
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
