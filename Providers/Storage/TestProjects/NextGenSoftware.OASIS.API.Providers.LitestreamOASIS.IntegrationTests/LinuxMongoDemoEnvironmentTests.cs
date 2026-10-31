using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class LinuxMongoDemoEnvironmentTests
{
    [TestMethod]
    public async Task AdmitOrVerifyOwnedLinuxReplicaSet()
    {
        Assert.IsTrue(OperatingSystem.IsLinux());
        var state = Environment.GetEnvironmentVariable("OASIS_COSMIC_LINUX_MONGO_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-cosmic-mongo", state);
        Assert.IsTrue(File.Exists(Path.Combine(state, "store.json")));
        var pending = Path.Combine(state, "replica-init.pending.json");
        var receipt = Path.Combine(state, "replica-init.json");
        Assert.IsFalse(File.Exists(pending), "Original replica-init outcome must be reconciled, never replayed.");
        var direct = new MongoClient("mongodb://127.0.0.1:27039/?directConnection=true&serverSelectionTimeoutMS=3000").GetDatabase("admin");
        if (!File.Exists(receipt))
        {
            try
            {
                await direct.RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetConfig", 1));
                Assert.Fail("Do not adopt an already configured unowned replica set.");
            }
            catch (MongoCommandException error) when (error.Code == 94) { /* Native NotYetInitialized admission. */ }
            using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(file, new { replica = "cosmicLinuxDemoRs", host = "127.0.0.1:27039" }); file.Flush(true); }
            await direct.RunCommandAsync<BsonDocument>(new BsonDocument("replSetInitiate", new BsonDocument {
                { "_id", "cosmicLinuxDemoRs" }, { "members", new BsonArray { new BsonDocument { { "_id", 0 }, { "host", "127.0.0.1:27039" } } } }
            }));
        }
        var config = (await direct.RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetConfig", 1)))["config"].AsBsonDocument;
        Assert.AreEqual("cosmicLinuxDemoRs", config["_id"].AsString);
        Assert.AreEqual(1, config["members"].AsBsonArray.Count);
        Assert.AreEqual("127.0.0.1:27039", config["members"][0]["host"].AsString);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var primary = false;
        do
        {
            var hello = await direct.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
            primary = hello["isWritablePrimary"].AsBoolean;
            if (!primary) await Task.Delay(200);
        } while (!primary && DateTime.UtcNow < deadline);
        Assert.IsTrue(primary, "Owned replica set must reach primary before fixture writes.");
        if (File.Exists(pending)) File.Move(pending, receipt);
    }
}
