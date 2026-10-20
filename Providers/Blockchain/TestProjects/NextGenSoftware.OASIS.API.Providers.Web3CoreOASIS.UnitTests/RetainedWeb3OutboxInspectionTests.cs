using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

/// <summary>Explicit original-only read inspection. Never initialize, claim, replay, settle or delete evidence.</summary>
[TestClass]
public class RetainedWeb3OutboxInspectionTests
{
    [TestMethod]
    public async Task InspectOriginalSqlitePrecommitFaultWithoutMutation()
    {
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        Assert.IsTrue(uri?.StartsWith("mongodb://127.0.0.1:27029/",StringComparison.Ordinal)==true,"Retained loopback replica required.");
        var settings=MongoClientSettings.FromConnectionString(uri);settings.ReadConcern=ReadConcern.Majority;settings.ReadPreference=ReadPreference.Primary;settings.ServerSelectionTimeout=TimeSpan.FromSeconds(15);
        var client=new MongoClient(settings);const string database="oasis_web3_prepared_native";
        Assert.IsTrue((await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database));
        var collection=client.GetDatabase(database).GetCollection<BsonDocument>("subscription_usage_outbox_v1");
        async Task<BsonDocument[]> Read()=> (await collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1025).ToListAsync()).OrderBy(row=>row["_id"].ToString(),StringComparer.Ordinal).ToArray();
        var original=await Read();Assert.IsTrue(original.Length is >0 and <=1024);
        string Digest(IEnumerable<BsonDocument> rows)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",rows.Select(row=>row.ToJson())))));
        var graph=original.Where(row=>row.GetValue("Endpoint",BsonNull.Value).IsString&&row["Endpoint"].AsString=="local post-commit graph fixture").ToArray();
        Assert.AreEqual(1,graph.Length,"Inspect the exact first failed graph schedule, not arbitrary outbox records.");
        var attempts=graph[0]["ProviderAttempts"].AsBsonArray.Select(value=>value.AsBsonDocument).ToArray();
        Assert.IsTrue(attempts.Any(row=>row["Provider"].AsString=="SQLLiteDBOASIS"));Assert.IsTrue(attempts.Any(row=>row["Provider"].AsString=="PolygonOASIS"));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{readOnly=true,database,operations=original.Length,graphOperation=graph[0]["_id"].ToString(),state=graph[0]["State"].AsString,outcome=graph[0].GetValue("ObservedOutcome",BsonNull.Value).ToString(),providers=attempts.Select(row=>row["Provider"].AsString).ToArray(),originalSha256=Digest(original)}));
        Assert.AreEqual(Digest(original),Digest(await Read()),"Original evidence changed during inspection.");
    }
}
