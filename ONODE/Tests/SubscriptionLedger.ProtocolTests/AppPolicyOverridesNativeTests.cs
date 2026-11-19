using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class AppPolicyOverridesNativeTests
{
    [Fact]
    public async Task CompletePolicyProjectionIncludesAppsBeyondAdminPageWithoutAccountSecrets()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var databaseName = "app_policy_" + Guid.NewGuid().ToString("N");
        var finds = new ConcurrentQueue<BsonDocument>();
        var settings = MongoClientSettings.FromConnectionString(uri);
        settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
        {
            if (command.CommandName == "find" && command.DatabaseNamespace.DatabaseName == databaseName &&
                command.Command["find"] == "subscription_app_accounts")
                finds.Enqueue(command.Command.DeepClone().AsBsonDocument);
        });
        var client = new MongoClient(settings);
        Assert.DoesNotContain(databaseName, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        try
        {
            var repository = new MongoSubscriptionUsageRepository(client, databaseName, TimeProvider.System);
            var records = Enumerable.Range(0, 501).Select(index => new AppAccountRecord
            {
                AppId = "app-" + index.ToString("D4"),
                KeyHashSha256 = Guid.NewGuid().ToString("N"),
                Name = new string('x', 1024),
                Overrides = index == 0 ? null : new AppUsageOverrides
                {
                    AccessLogEnabled = index == 500,
                    SealedHistoryEnabled = index == 500
                }
            }).ToArray();
            await client.GetDatabase(databaseName).GetCollection<AppAccountRecord>("subscription_app_accounts").InsertManyAsync(records);
            var policies = await repository.ListAppPolicyOverridesAsync();
            Assert.Equal(501, policies.Count);
            var last = Assert.Single(policies.Where(policy => policy.AppId == "app-0500"));
            Assert.True(last.AccessLogEnabled);
            Assert.True(last.SealedHistoryEnabled);
            var inherited = Assert.Single(policies.Where(policy => policy.AppId == "app-0000"));
            Assert.Null(inherited.AccessLogEnabled);
            Assert.Null(inherited.SealedHistoryEnabled);
            var command = Assert.Single(finds);
            Assert.True(!command.Contains("limit") || command["limit"].ToInt64() == 0);
            var projection = command["projection"].AsBsonDocument;
            Assert.All(projection.Names, field => Assert.Contains(field,
                new[] { "AppId", "AccessLogEnabled", "SealedHistoryEnabled", "_id" }));
            Assert.DoesNotContain("KeyHashSha256", projection.ToJson());
            Assert.DoesNotContain("Name", projection.ToJson());
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        }
    }
}
