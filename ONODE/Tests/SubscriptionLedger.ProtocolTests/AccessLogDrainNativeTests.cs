using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class AccessLogDrainNativeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AcceptedEventDrainsAfterGlobalOrAppOptOut(bool appOptIn, bool shutdownDrain)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var databaseName = "access_drain_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(databaseName, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var dna = JsonSerializer.Deserialize<OASISDNA>(JsonSerializer.Serialize(new
        {
            OASIS = new { StorageProviders = new { MongoDBOASIS = new { ConnectionString = uri, DBName = databaseName } } }
        }));
        var settings = new AccessLogSettings { Enabled = !appOptIn, FlushIntervalSeconds = shutdownDrain ? 60 : 1 };
        var store = new MongoAccessLogStore(() => dna);
        var service = new AccessLogService(() => settings, store);
        using var worker = new AccessLogFlushHostedService(service, () => settings, NullLogger<AccessLogFlushHostedService>.Instance);
        var avatar = Guid.NewGuid();
        var accepted = Guid.NewGuid();
        var ignored = Guid.NewGuid();
        try
        {
            if (appOptIn) service.Overrides.Replace(new (string, bool?)[] { ("app", true) });
            service.Record(avatar, "app", "load", new[] { (accepted, "Holon") });
            settings.Enabled = false;
            service.Overrides.Replace(Array.Empty<(string, bool?)>());
            Assert.False(service.IsEnabled);
            service.Record(avatar, "app", "load", new[] { (ignored, "Holon") });
            await worker.StartAsync(default);
            if (!shutdownDrain)
            {
                // Bounded observation of the actual timer/insert on the owned backend, before shutdown can flush it.
                var observed = false;
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    if ((await store.QueryByAvatarAsync(avatar, 10, 0, default)).Count == 1) { observed = true; break; }
                    await Task.Delay(250);
                }
                Assert.True(observed, "The periodic worker did not drain the accepted event while logging was off.");
            }
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await worker.StopAsync(stop.Token);
            var recorded = Assert.Single(await store.QueryByAvatarAsync(avatar, 10, 0, default));
            Assert.Equal(accepted, Assert.Single(recorded.HolonIds));
            Assert.Equal(0, service.DroppedEvents);
            Assert.Equal(0, service.FailedBatches);
        }
        finally
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await worker.StopAsync(stop.Token); }
            finally
            {
                await client.DropDatabaseAsync(databaseName);
                Assert.DoesNotContain(databaseName, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            }
        }
    }
}
