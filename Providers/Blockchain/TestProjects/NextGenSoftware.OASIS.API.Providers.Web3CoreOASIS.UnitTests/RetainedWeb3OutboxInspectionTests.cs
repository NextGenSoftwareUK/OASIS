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
        Assert.AreEqual("420b0d49-f99b-6172-2cb9-0d967044c8a9",graph[0]["_id"].ToString(),"Never inspect a replacement run as the original.");
        var attempts=graph[0]["ProviderAttempts"].AsBsonArray.Select(value=>value.AsBsonDocument).ToArray();
        Assert.IsTrue(attempts.Any(row=>row["Provider"].AsString=="SQLLiteDBOASIS"));Assert.IsTrue(attempts.Any(row=>row["Provider"].AsString=="PolygonOASIS"));
        var dispatch=attempts.Select(row=>{
            var preparation=row.GetValue("NativePreparation",BsonNull.Value);
            string signedBytesKeccak256=null;
            if(!preparation.IsBsonNull){
                Assert.AreEqual("31337",preparation["ChainId"].AsString,"Only the original local-chain preparation may be examined.");
                var bytes=Convert.FromBase64String(preparation["SignedTransactionBase64"].AsString);
                signedBytesKeccak256="0x"+Convert.ToHexString(new Nethereum.Util.Sha3Keccack().CalculateHash(bytes)).ToLowerInvariant();
            }
            // Public attempt identifiers/hashes only. Never emit signed bytes, signatures or authority envelopes.
            return new{provider=row["Provider"].AsString,clientRequestId=row.GetValue("ClientRequestId",BsonNull.Value).ToString(),providerRequestId=row.GetValue("ProviderRequestId",BsonNull.Value).ToString(),acknowledgedAt=row.GetValue("AcknowledgedAtUtc",BsonNull.Value).ToString(),hasNativePreparation=!preparation.IsBsonNull,signedBytesKeccak256};
        }).ToArray();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{readOnly=true,database,operations=original.Length,graphOperation=graph[0]["_id"].ToString(),state=graph[0]["State"].AsString,outcome=graph[0].GetValue("ObservedOutcome",BsonNull.Value).ToString(),providers=attempts.Select(row=>row["Provider"].AsString).ToArray(),dispatch,originalSha256=Digest(original)}));
        Assert.AreEqual(Digest(original),Digest(await Read()),"Original evidence changed during inspection.");
    }
}
