using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleCqlNativeTests
{
    [TestMethod]
    public async Task ReconcileOriginalManagerConfigurationFailureWithoutReplayingWrites()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OracleCqlCurrent");
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var failed = Path.Combine(root.FullName, "TestResults/OracleCurrent/Archive/oracle-mongo-manager-failed-B0390120.trx");
        Assert.AreEqual("B03901203069577829EDB9F3AB5D4595BECEB9866E573587D006AC2E05E9E623",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(failed))));
        var mongoPath = Path.Combine(state, "mongo.pending.json");
        using var mongoIntent = JsonDocument.Parse(await File.ReadAllTextAsync(mongoPath));
        const string database = "cosmic_cql_mongo_oracle_demo";
        Assert.AreEqual(database, mongoIntent.RootElement.GetProperty("database").GetString());
        var actor = mongoIntent.RootElement.GetProperty("actor").GetGuid();
        var mongoIds = mongoIntent.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var client = new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
        var native = client.GetDatabase(database);
        using (var names = await native.ListCollectionNamesAsync())
            CollectionAssert.AreEquivalent(new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox", "Holon" }, (await names.ToListAsync()).ToArray());
        foreach (var empty in new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox" })
            Assert.AreEqual(0L, await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
            "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000", database);
        Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
        try
        {
            Assert.AreEqual(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            for (var index = 0; index < mongoIds.Length; index++)
            {
                var row = await mongo.LoadHolonAsync(mongoIds[index], loadChildren: false);
                Assert.IsFalse(row.IsError);
                Assert.AreEqual(actor, row.Result.CreatedByAvatarId);
                Assert.AreEqual("COSMIC owned Mongo Oracle copy " + index, row.Result.Name);
            }
        }
        finally { await mongo.DeActivateProviderAsync(); }
        await client.DropDatabaseAsync(database);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        File.Delete(mongoPath);

        var oraclePath = Path.Combine(state, "fixture.pending.json");
        using var intent = JsonDocument.Parse(await File.ReadAllTextAsync(oraclePath));
        var ids = intent.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var expectedNames = intent.RootElement.GetProperty("names").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.AreEqual(4, ids.Length);
        await using var connection = new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));
        await connection.OpenAsync();
        for (var index = 0; index < ids.Length; index++)
        {
            var json = await OracleNativeAcceptanceTests.Read(connection, "OASIS_HOLONS", ids[index]);
            Assert.IsNotNull(json);
            using var body = JsonDocument.Parse(json);
            Assert.AreEqual(ids[index], body.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual(expectedNames[index], body.RootElement.GetProperty("Name").GetString());
            await using var command = new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:id AND DBMS_LOB.COMPARE(DATA_JSON,:expected)=0", connection) { BindByName = true };
            command.Parameters.Add("id", OracleDbType.Raw).Value = ids[index].ToByteArray();
            command.Parameters.Add("expected", OracleDbType.Clob).Value = json;
            Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
            Assert.IsNull(await OracleNativeAcceptanceTests.Read(connection, "OASIS_HOLONS", ids[index]));
        }
        File.Delete(oraclePath);
    }
}
