using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoIdlePollingTests
{
    [Fact]
    public async Task EmptyQueuesDoNotAcquireOrRenewGlobalWriteLeases()
    {
        // Deliberately loopback-only: this regression must never exercise the hosted Atlas database.
        const string connection = "mongodb://127.0.0.1:27017/?directConnection=true&serverSelectionTimeoutMS=3000";
        string name = "hd_idle_" + Guid.NewGuid().ToString("N")[..20];
        var client = new MongoClient(connection);
        var database = client.GetDatabase(name);
        try
        {
            var provider = new MongoDBOASIS.MongoDBOASIS(connection, name);
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            // First call initializes durable sync collections; subsequent empty polls must not write leases.
            var first = await provider.ClaimCommandsAsync("idle-worker", 1, DateTime.UtcNow.AddMinutes(5), default);
            Assert.False(first.IsError, first.Message);
            async Task<string> Lease(string collection) => (await database.GetCollection<BsonDocument>(collection)
                .Find(new BsonDocument()).FirstAsync()).ToJson();
            var commandsBefore = await Lease("HyperDriveSyncCommandLeases");
            var fanOutBefore = await Lease("HyperDriveSyncFanOutLeases");
            for (int i = 0; i < 3; i++)
            {
                var commands = await provider.ClaimCommandsAsync("idle-worker", 1, DateTime.UtcNow.AddMinutes(5), default);
                var fanOut = await provider.ClaimFanOutAsync("idle-worker", 25, DateTime.UtcNow.AddMinutes(2), default);
                Assert.False(commands.IsError, commands.Message);
                Assert.False(fanOut.IsError, fanOut.Message);
                Assert.False(commands.Result.LeaseAcquired);
                Assert.False(fanOut.Result.LeaseAcquired);
                Assert.Empty(commands.Result.Items);
                Assert.Empty(fanOut.Result.Items);
            }
            Assert.Equal(commandsBefore, await Lease("HyperDriveSyncCommandLeases"));
            Assert.Equal(fanOutBefore, await Lease("HyperDriveSyncFanOutLeases"));
        }
        finally { await client.DropDatabaseAsync(name); }
    }
}
