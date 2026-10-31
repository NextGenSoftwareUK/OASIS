using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public sealed partial class AWSNativeRunLifecycle
{
    [TestMethod]
    public async Task ReadOnlyInspectSqliteKarmaHistoryFailure()
    {
        await InspectHistoryOriginal("79CA2FF579FA41B593A16BA200350776DDC469360363AA4FBD81B80FE4063288",
            "8DE63ADEDB312A112A5E3F3106C265496D91C37C6667E513C34E76CE753EB8F5","2026-10-21T03:21:23.0794497Z",
            "23b3fe4a-d424-46c4-b94b-b57f3e9cc16c","1c7f6742-ec69-421b-ad0e-9527f5c866b5",0,"aws-sqlite-karma-history-inspection.json");
    }

    [TestMethod]
    public async Task ReadOnlyInspectCanonicalHistoryAssertionFailure()
    {
        await InspectHistoryOriginal("D8A61675424C62F07554634E969BF225B92F9806138113838D4A389383CB33EA",
            "7A174ABBC33CACE6EF6355E65F035328489945D9A3689F504EEDDB7F74B2ADF6","2026-10-21T03:41:15.6015493Z",
            "9962ecf1-17d7-43f3-a1ae-a5a9251b7e22","4d516ebe-49a3-4e7e-85d2-fda2bcb5e522",2,"aws-canonical-history-inspection.json");
    }

    private static async Task InspectHistoryOriginal(string reportSha,string sqliteSha,string started,
        string initialId,string selectedId,long expectedHistoryCount,string proofName)
    {
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        var sqlite=Path.GetFullPath(Path.Combine(Work(),"aws-karma-affinity.db"));
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sqlite))));
        foreach(var suffix in new[] {"-wal","-shm","-journal"})Assert.IsFalse(File.Exists(sqlite+suffix));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual(started,intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(x=>x.GetString()!).ToArray());
        var details=new List<object>();
        var expectedDetails=new Dictionary<string,(long Karma,long Xp)> {
            [initialId]=(10,111),[selectedId]=(500,222)};
        using(var connection=new SqliteConnection($"Data Source={sqlite};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync();
            using var count=connection.CreateCommand();count.CommandText="SELECT COUNT(*) FROM KarmaAkashicRecord";
            Assert.AreEqual(expectedHistoryCount,(long)(await count.ExecuteScalarAsync())!);
            using var query=connection.CreateCommand();query.CommandText="SELECT Id,Karma,XP FROM AvatarDetail ORDER BY Id";
            using var reader=await query.ExecuteReaderAsync();
            while(await reader.ReadAsync())
            {
                var id=reader.GetString(0);Assert.IsTrue(expectedDetails.TryGetValue(id,out var expected));
                Assert.AreEqual(expected.Karma,reader.GetInt64(1));Assert.AreEqual(expected.Xp,reader.GetInt64(2));
                details.Add(new {id,karma=reader.GetInt64(1),xp=reader.GetInt64(2)});
            }
            Assert.AreEqual(2,details.Count);
        }
        using var client=Client();var inventory=new List<object>();
        foreach(var table in Tables)foreach(var row in await Rows(client,table))
        {
            var id=Guid.Parse(row["id"].S);var json=row["data_json"].S;
            using var body=JsonDocument.Parse(json);Assert.AreEqual(id,body.RootElement.GetProperty("Id").GetGuid());
            inventory.Add(new {table,id,sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))});
        }
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sqlite))));
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        await DurableJson(Path.Combine(Work(),proofName),new {
            readOnly=true,reportSha,sqliteSha,sqliteHistoryCount=expectedHistoryCount,details,inventory});
    }

    [TestMethod]
    public async Task ReadOnlyInspectKarmaMutationFailure()
    {
        const string reportSha="28578E643210B4227D64001BA28C0737DF3476E1F0CBD72B095F27469C6B6E15";
        const string sqliteSha="EDA1358AD2D7850719AB15014983AD5C3838392841925C075C20CBF6CDBE9CD3";
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-native-current.trx")))));
        var sqlite=Path.Combine(Work(),"aws-karma-affinity.db");
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sqlite))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("2026-10-21T02:34:15.6306224Z",intent.RootElement.GetProperty("startedUtc").GetString());
        var target=Guid.Parse("a48c8342-5843-4bd3-8c90-5b465f39e223");
        var ledger=new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"settings:{target}:karma")));
        using var client=Client();var inventory=new List<object>();var selected=new List<object>();
        foreach(var table in Tables)foreach(var row in await Rows(client,table))
        {
            var id=Guid.Parse(row["id"].S);var json=row["data_json"].S;
            using var body=JsonDocument.Parse(json);Assert.AreEqual(id,body.RootElement.GetProperty("Id").GetGuid());
            inventory.Add(new {table,id,sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))});
            if(id==target||id==ledger)
            {
                var fields=new Dictionary<string,string>();
                foreach(var field in new[] {"Name","Version","Karma","XP","MetaData","KarmaAkashicRecords"})
                    if(body.RootElement.TryGetProperty(field,out var value))fields[field]=value.GetRawText();
                selected.Add(new {table,id,fields});
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new {target,ledger,selected}));
        await DurableJson(Path.Combine(Work(),"aws-karma-mutation-inspection.json"),new {readOnly=true,reportSha,sqliteSha,target,ledger,selected,inventory});
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sqlite))));
    }

    private const string KarmaFailedReportSha="877C3C35C54029656E598ECACBAB66E2DC4F39E1ED573DF01AE1313E5B6BCCE2";
    private const string KarmaFailedSqliteSha="7C7DBDEBD66683EBC59315E80044E2D1519C7341A371C91F80ECBF08C712F6D0";
    private const string KarmaFailedFixtureId="aef6fe26-bafe-47d7-8801-efdb402574d9";
    private static async Task<string> VerifyKnownKarmaSeedOriginals()
    {
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        Assert.AreEqual(KarmaFailedReportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("2026-10-21T02:12:53.4989939Z",intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(value=>value.GetString()!).ToArray());
        var original=Path.Combine(AppContext.BaseDirectory,"TestResults","AWSRoutingFiles","karma-affinity.db");
        Assert.AreEqual(KarmaFailedSqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        foreach(var suffix in new[] {"-wal","-shm","-journal"})Assert.IsFalse(File.Exists(original+suffix),"Original must be closed and checkpointed.");
        using(var connection=new SqliteConnection($"Data Source={original};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync();
            foreach(var table in new[] {"Avatar","Holon","KarmaAkashicRecord","CosmicWritePreparation","CosmicWriteReceipt","CosmicHolonRevision","CosmicReplicaHead","CosmicHolonOutbox"})
            {
                using var empty=connection.CreateCommand();empty.CommandText=$"SELECT COUNT(*) FROM \"{table}\"";
                Assert.AreEqual(0L,(long)(await empty.ExecuteScalarAsync())!);
            }
            using var detail=connection.CreateCommand();detail.CommandText="SELECT Id, Username, Karma, XP, Version FROM AvatarDetail";
            using var reader=await detail.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(KarmaFailedFixtureId,reader.GetString(0));
            Assert.AreEqual("owned-karma-aef6fe26bafe47d78801efdb402574d9",reader.GetString(1));
            Assert.AreEqual(37L,reader.GetInt64(2));Assert.AreEqual(111L,reader.GetInt64(3));Assert.AreEqual(0L,reader.GetInt64(4));
            Assert.IsFalse(await reader.ReadAsync());
        }
        Assert.AreEqual(KarmaFailedSqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        return original;
    }

    [TestMethod]
    public async Task ReadOnlyReconcileKnownKarmaSeedFailure()
    {
        await VerifyKnownKarmaSeedOriginals();
        using var client=Client();var inventory=new List<object>();
        foreach(var table in Tables)foreach(var row in await Rows(client,table))
        {
            Assert.IsTrue(Guid.TryParse(row["id"].S,out var id));
            using var body=JsonDocument.Parse(row["data_json"].S);
            Assert.AreEqual(id,body.RootElement.GetProperty("Id").GetGuid());
            inventory.Add(new {table,id,sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row["data_json"].S)))});
        }
        await DurableJson(Path.Combine(Work(),"aws-karma-seed-reconciliation.json"),new {
            readOnly=true,failedReportSha256=KarmaFailedReportSha,sqliteSha256=KarmaFailedSqliteSha,
            sqliteDetailId=KarmaFailedFixtureId,noKarmaLedgerInSqlite=true,inventory});
    }

    [TestMethod]
    public async Task CleanupOnlyReviewedKarmaSeedFailure()
    {
        Assert.AreEqual("known-karma-seed-reconciled",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        var original=await VerifyKnownKarmaSeedOriginals();
        await CleanupReviewedInventory(original,"aws-karma-seed-reconciliation.json",
            "140EAE72B16A22179A38F172608A429A679728C7078EAD924317820904D647F0",
            KarmaFailedReportSha,KarmaFailedSqliteSha,"failedReportSha256",42,"aws-karma-seed-cleanup.json");
    }

    [TestMethod]
    public async Task CleanupOnlyReviewedKarmaMutationFailure()
    {
        Assert.AreEqual("known-karma-mutation-reconciled",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        const string reportSha="28578E643210B4227D64001BA28C0737DF3476E1F0CBD72B095F27469C6B6E15";
        const string sqliteSha="EDA1358AD2D7850719AB15014983AD5C3838392841925C075C20CBF6CDBE9CD3";
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-native-current.trx")))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("2026-10-21T02:34:15.6306224Z",intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(x=>x.GetString()!).ToArray());
        var original=Path.GetFullPath(Path.Combine(Work(),"aws-karma-affinity.db"));
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        foreach(var suffix in new[] {"-wal","-shm","-journal"})Assert.IsFalse(File.Exists(original+suffix));
        await CleanupReviewedInventory(original,"aws-karma-mutation-inspection.json",
            "2F092998FD82312446001E74743E006F6F7B889E10AA6D3074CEF925BD7356CB",
            reportSha,sqliteSha,"reportSha",46,"aws-karma-mutation-cleanup.json");
    }

    [TestMethod]
    public async Task CleanupOnlyReviewedSqliteKarmaHistoryFailure()
    {
        Assert.AreEqual("known-sqlite-karma-history-reconciled",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        const string reportSha="79CA2FF579FA41B593A16BA200350776DDC469360363AA4FBD81B80FE4063288";
        const string sqliteSha="8DE63ADEDB312A112A5E3F3106C265496D91C37C6667E513C34E76CE753EB8F5";
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-native-current.trx")))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("2026-10-21T03:21:23.0794497Z",intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(x=>x.GetString()!).ToArray());
        var original=Path.GetFullPath(Path.Combine(Work(),"aws-karma-affinity.db"));
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        foreach(var suffix in new[] {"-wal","-shm","-journal"})Assert.IsFalse(File.Exists(original+suffix));
        await CleanupReviewedInventory(original,"aws-sqlite-karma-history-inspection.json",
            "4B77447725DA054EFC9342B836E191B0F46D87F18AA8E0767A66E485199BD85D",
            reportSha,sqliteSha,"reportSha",54,"aws-sqlite-karma-history-cleanup.json");
    }

    [TestMethod]
    public async Task CleanupOnlyReviewedCanonicalHistoryAssertionFailure()
    {
        Assert.AreEqual("known-canonical-history-reconciled",Environment.GetEnvironmentVariable("OASIS_AWS_CLEANUP_AUTHORITY"));
        const string reportSha="D8A61675424C62F07554634E969BF225B92F9806138113838D4A389383CB33EA";
        const string sqliteSha="7A174ABBC33CACE6EF6355E65F035328489945D9A3689F504EEDDB7F74B2ADF6";
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-native-current.trx")))));
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Work(),"aws-full-suite.pending.json")));
        Assert.AreEqual("2026-10-21T03:41:15.6015493Z",intent.RootElement.GetProperty("startedUtc").GetString());
        Assert.AreEqual("verified-empty-before-exclusive-native-suite",intent.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("http://127.0.0.1:18000",intent.RootElement.GetProperty("endpoint").GetString());
        CollectionAssert.AreEqual(Tables,intent.RootElement.GetProperty("tables").EnumerateArray().Select(x=>x.GetString()!).ToArray());
        var original=Path.GetFullPath(Path.Combine(Work(),"aws-karma-affinity.db"));
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        foreach(var suffix in new[] {"-wal","-shm","-journal"})Assert.IsFalse(File.Exists(original+suffix));
        await CleanupReviewedInventory(original,"aws-canonical-history-inspection.json",
            "A7A85C272E3A41936B50809300494FC807DFEB61419E3A980772A25ABEAEC0CE",
            reportSha,sqliteSha,"reportSha",54,"aws-canonical-history-cleanup.json");
    }

    private static async Task CleanupReviewedInventory(string original,string proofName,string proofSha,
        string reportSha,string sqliteSha,string reportProperty,int expectedCount,string cleanupName)
    {
        var proofPath=Path.Combine(Work(),proofName);
        Assert.AreEqual(proofSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(proofPath))));
        using var proof=JsonDocument.Parse(await File.ReadAllTextAsync(proofPath));
        Assert.IsTrue(proof.RootElement.GetProperty("readOnly").GetBoolean());
        Assert.AreEqual(reportSha,proof.RootElement.GetProperty(reportProperty).GetString());
        var expected=proof.RootElement.GetProperty("inventory").EnumerateArray().ToDictionary(
            row=>row.GetProperty("table").GetString()+"/"+row.GetProperty("id").GetGuid(),row=>row.GetProperty("sha256").GetString());
        Assert.AreEqual(expectedCount,expected.Count);
        using var client=Client();var observed=new List<(string Table,string Id,string Json)>();
        foreach(var table in Tables)foreach(var row in await Rows(client,table))
        {
            var id=row["id"].S;var json=row["data_json"].S;
            Assert.IsTrue(expected.TryGetValue(table+"/"+id,out var expectedHash),"Unreviewed row; no deletion.");
            Assert.AreEqual(expectedHash,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            observed.Add((table,id,json));
        }
        Assert.AreEqual(expected.Count,observed.Count,"Reviewed membership changed; no deletion.");
        await DurableJson(Path.Combine(Work(),cleanupName),new {
            sourceReconciliationSha256=proofSha,failedReportSha256=reportSha,sqliteSha256=sqliteSha,
            rows=observed.Select(row=>new {table=row.Table,id=row.Id,sha256=expected[row.Table+"/"+row.Id]})});
        foreach(var row in observed)
        {
            var key=new Dictionary<string,AttributeValue> {{"id",new(){S=row.Id}}};
            var deleted=await client.DeleteItemAsync(new DeleteItemRequest {TableName=row.Table,Key=key,
                ConditionExpression="data_json = :expected",ExpressionAttributeValues=new() {{":expected",new(){S=row.Json}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,deleted.HttpStatusCode);Assert.AreEqual(row.Json,deleted.Attributes["data_json"].S);
            Assert.AreEqual(0,(await client.GetItemAsync(new GetItemRequest {TableName=row.Table,Key=key,ConsistentRead=true})).Item.Count);
        }
        foreach(var table in Tables)Assert.AreEqual(0,(await Rows(client,table)).Count);
        Assert.AreEqual(sqliteSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        File.Delete(original);Assert.IsFalse(File.Exists(original));
        File.Delete(Path.Combine(Work(),"aws-full-suite.pending.json"));
        Assert.AreEqual(reportSha,Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Work(),"aws-full-native-current.trx")))));
    }
}
