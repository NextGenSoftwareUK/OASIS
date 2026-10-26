using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Data.Sqlite;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public Task ReviewedAccessSeedPreservesReadFailureBeforeExactCleanup() => ReviewAccessSeed(false);

    [TestMethod]
    public Task ReviewedHostedAccessSeedPreservesSettledReadsBeforeExactCleanup() => ReviewAccessSeed(true);

    [TestMethod]
    public Task ReviewedHostedAccessPrefixPreservesAllOriginalReceiptsBeforeExactCleanup() => ReviewAccessSeed(true,true);

    private static async Task ReviewAccessSeed(bool hosted,bool prefix=false)
    {
        var hash = prefix ? "098E12FFEE8298BCC7EFC18443B34B9D78B2DE16964FF508F6AD44EC0892405D"
            : hosted ? "452E85CA73F3EEAB8C489D4A7CE4AE7143A7F7439114C6498D9C339A1EF89AFE"
            : "195CA1DDE290A24150E3554C50033C013F8375B43FB034EEB32DBE2FCCF9A1FB";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var marker = Path.Combine(work, "cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(marker);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(bytes)));
        var failure = await File.ReadAllBytesAsync(Path.Combine(work, "neo4j-cql-v1-demo-current.trx"));
        Assert.AreEqual(prefix ? "E6CCC84E1A702371AA08D83862BFFD2FA80E9EDAD59B4D2A81DE3F4462DEF2E1"
            : hosted ? "6ABD11F5975544FEBB75C04896ED6C5BF20A3FB661C3A06C60FA5E7FCB961CD8"
            : "437D2B709AB1A928BD56320980F9E60339D85CED0712EC0A05B105457BF7F7E7", Convert.ToHexString(SHA256.HashData(failure)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        Assert.AreEqual(prefix?"before-http-request":hosted ? "before-hosted-read-policy" : "before-create", fixture.GetProperty("stage").GetString());
        Assert.AreEqual(prefix?85:hosted ? 3 : 0, fixture.GetProperty("requests").GetArrayLength());
        Assert.AreEqual(0,fixture.GetProperty("scalarWrites").GetArrayLength());
        Assert.AreEqual(prefix?48:0,fixture.GetProperty("sharingOperations").GetArrayLength());
        const string database = "cosmic_cql_v1_web4_storage";
        Assert.AreEqual(database, fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_usage", fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length);
        var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned", "public child", "private child", "owned child", "public behind private" };
        var snapshots = new List<object>();
        foreach (IOASISStorageProvider provider in new IOASISStorageProvider[]
        {
            new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database),
            new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS("Data Source=" + sqlite),
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only")
        })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var read = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(read.IsError, read.Message);
                var rows = read.Result.ToArray();
                CollectionAssert.AreEquivalent(ids, rows.Select(row => row.Id).ToArray());
                foreach (var row in rows)
                {
                    var index = Array.IndexOf(ids, row.Id);
                    Assert.AreEqual(names[index], row.Name);
                    Assert.AreEqual(fixture.GetProperty(index is 2 or 4 or 7 ? "alice" : "bob").GetGuid(), row.CreatedByAvatarId);
                    Assert.AreEqual(index is 3 or 5 or 8, row.IsPublic);
                    // The existing manager save contract defaults an empty
                    // parent to its creator; only these two roots were saved.
                    var expectedParent=prefix && index<2 ? fixture.GetProperty("bob").GetGuid()
                        : index<5 ? Guid.Empty : index==8 ? ids[6] : ids[3];
                    Assert.AreEqual(expectedParent,row.ParentHolonId);
                    if (index < 5) Assert.AreEqual(fixture.GetProperty("tag").GetString(), row.MetaData["fixture"].ToString());
                    if(prefix && index==0)
                    {
                        Assert.IsTrue(JsonSerializer.SerializeToElement(row.MetaData["owned-write-control"]).GetBoolean());
                        Assert.AreEqual("committed",row.MetaData["hosted-write-control"].ToString());
                    }
                    if(prefix && index<2)Assert.AreEqual("committed",row.MetaData["owned-batch-control"].ToString());
                    snapshots.Add(new { authority = provider.ProviderType.Value.ToString(), row.Id,
                        sha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(StoredRecordJson.Serialize(row)))) });
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        var mongo = new MongoClient(uri);
        var usageProof = new List<object>();
        if(hosted)
        {
            var operations = await mongo.GetDatabase("cosmic_cql_v1_web4_usage").GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
                .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
            Assert.AreEqual(prefix?27:3, operations.Count);
            Assert.AreEqual(prefix?24:2, operations.Sum(value => value.ProviderAttempts.Count));
            Assert.AreEqual(prefix?24:2, operations.Sum(value => value.Receipts.Count));
            foreach(var request in fixture.GetProperty("requests").EnumerateArray().Where(value=>value.TryGetProperty("key",out _)))
            {
                if(!prefix)Assert.AreEqual("MongoDBOASIS", request.GetProperty("provider").GetString());
                Assert.AreEqual(fixture.GetProperty("outsider").GetGuid(), request.GetProperty("actor").GetGuid());
                var identity=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] {
                    "WEB4",request.GetProperty("actor").GetGuid().ToString("D"),request.GetProperty("key").GetString() })));
                var operation=operations.Single(value=>value.Id==new Guid(identity.AsSpan(0,16)).ToString("D"));
                Assert.AreEqual("settled",operation.State);
                Assert.AreEqual("POST "+request.GetProperty("path").GetString(),operation.Endpoint);
                var rejected=prefix?request.GetProperty("Mode").GetInt32()==1:operation.Endpoint.EndsWith("/projection",StringComparison.Ordinal);
                Assert.AreEqual(rejected?0:1,operation.ProviderAttempts.Count);
                Assert.AreEqual(rejected?"failed":"succeeded",operation.ObservedOutcome);
                Assert.AreEqual(rejected?0m:0.002m,operation.Settlement.ActualCostUsd);
                if(rejected)Assert.IsFalse(string.IsNullOrWhiteSpace(operation.NoProviderExecutionReason));
                foreach(var receipt in operation.Receipts)Assert.AreEqual(request.GetProperty("provider").GetString(),receipt.Provider);
                usageProof.Add(new{operation.Id,operation.Endpoint,operation.State,operation.ObservedOutcome,
                    attempts=operation.ProviderAttempts.Count,receipts=operation.Receipts.Count,cost=operation.Settlement.ActualCostUsd});
            }
        }
        else using (var databases = await mongo.ListDatabaseNamesAsync())
            Assert.IsFalse((await databases.ToListAsync()).Contains("cosmic_cql_v1_web4_usage"));
        using (var collections = await mongo.GetDatabase(database).ListCollectionNamesAsync())
            foreach (var name in await collections.ToListAsync())
                if (name != "Holon") Assert.AreEqual(prefix && (name=="CosmicWriteReceipt" || name=="CosmicOperationFence")?7L:0L,
                    await mongo.GetDatabase(database).GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
                    "Unexpected collection count: "+name);
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession();
        var receipts = await (await session.RunAsync("MATCH (r:CosmicWriteReceipt) RETURN count(r) AS n")).SingleAsync();
        Assert.AreEqual(prefix?7L:0L, receipts["n"].As<long>());
        var conditionalProof=new List<object>();
        if(prefix)
        {
            var ops=fixture.GetProperty("sharingOperations").EnumerateArray().Select(value=>value.GetString()!).ToArray();
            var committedIndexes=new[]{0,1,3,6,8,11,14};
            void CheckReceipt(string authority,string operation,IEnumerable<IHolon> graph,int offset)
            {
                CollectionAssert.Contains(committedIndexes.Select(index=>ops[offset+index]).ToArray(),operation);
                var rows=graph.ToArray();
                CollectionAssert.AreEquivalent(operation==ops[offset+11]?new[]{ids[0],ids[1]}:new[]{ids[0]},rows.Select(row=>row.Id).ToArray());
                Assert.IsTrue(rows.All(row=>row.CreatedByAvatarId==fixture.GetProperty("bob").GetGuid()));
                conditionalProof.Add(new{authority,operation,ids=rows.Select(row=>row.Id).ToArray()});
            }
            var mongoReceipts=await mongo.GetDatabase(database).GetCollection<BsonDocument>("CosmicWriteReceipt").Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
            var fences=await mongo.GetDatabase(database).GetCollection<BsonDocument>("CosmicOperationFence").Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
            CollectionAssert.AreEquivalent(mongoReceipts.Select(row=>row["_id"].AsString).ToArray(),fences.Select(row=>row["_id"].AsString).ToArray());
            Assert.IsTrue(fences.All(row=>row["Generation"].ToInt64()==1L));
            foreach(var receipt in mongoReceipts)CheckReceipt("MongoDBOASIS",receipt["_id"].AsString,ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString),0);
            using(var connection=new SqliteConnection("Data Source="+sqlite+";Mode=ReadOnly"))
            {
                connection.Open();using var command=connection.CreateCommand();command.CommandText="SELECT OperationId,ResultJson FROM CosmicWriteReceipt";
                using var reader=command.ExecuteReader();var count=0;
                while(reader.Read())
                {
                    using var receipt=JsonDocument.Parse(reader.GetString(1));
                    Assert.AreEqual(fixture.GetProperty("bob").GetGuid(),receipt.RootElement.GetProperty("actor").GetGuid());
                    CheckReceipt("SQLLiteDBOASIS",reader.GetString(0),ConditionalHolonReceipt.Read(receipt.RootElement.GetProperty("graph").GetString()!),16);count++;
                }
                Assert.AreEqual(7,count);
            }
            var neo=await(await session.RunAsync("MATCH(r:CosmicWriteReceipt) RETURN r.OperationId AS operation,r.ResultJson AS graph")).ToListAsync();
            foreach(var receipt in neo)CheckReceipt("Neo4jOASIS",receipt["operation"].As<string>(),ConditionalHolonReceipt.Read(receipt["graph"].As<string>()),32);
            Assert.AreEqual(21,conditionalProof.Count);
        }
        var proofPrefix=prefix?"cql-v1-hosted-access-prefix":hosted?"cql-v1-hosted-access-seed":"cql-v1-access-seed";
        await File.WriteAllTextAsync(Path.Combine(work, proofPrefix+"-reconciliation.json"), JsonSerializer.Serialize(new { hash, snapshots, conditionalProof, usageProof }));
        await File.WriteAllBytesAsync(Path.Combine(work, proofPrefix+"-original.json"), bytes);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(marker))));
        await (await session.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ids.Select(value => value.ToString("D")).ToArray() })).ConsumeAsync();
        await (await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = ids.Select(value => value.ToString("D")).ToArray() })).ConsumeAsync();
        if(prefix)await(await session.RunAsync("MATCH(r:CosmicWriteReceipt) WHERE r.OperationId IN $ops DETACH DELETE r",
            new{ops=fixture.GetProperty("sharingOperations").EnumerateArray().Select(value=>value.GetString()!).ToArray()})).ConsumeAsync();
        await mongo.DropDatabaseAsync(database);
        if(hosted)await mongo.DropDatabaseAsync("cosmic_cql_v1_web4_usage");
        await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
