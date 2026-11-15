using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class MongoSqliteKarmaReadTests
{
    [Fact]
    public async Task ColdKarmaReadDoesNotPersistSettings()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The karma check requires the owned MongoDB runner.");

        var database = "hyperdrive_karma_" + Guid.NewGuid().ToString("N");
        var directory = Directory.CreateTempSubdirectory("oasis-routing-karma-");
        var client = new MongoClient(uri);
        var mongo = new MongoDBOASIS(uri, database);
        try
        {
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "karma.db")};Pooling=False");
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var activation = await provider.ActivateProviderAsync();
                Assert.False(activation.IsError, activation.Message);
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                    { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
                var providers = new ProviderManager(provider, dna)
                    { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
                providers.RegisterProvider(provider);
                var avatarId = Guid.NewGuid();
                var settingsId = new Guid(System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes($"settings:{avatarId}:karma")));
                var karma = new KarmaManager(provider, dna, providers);
                var balance = karma.GetKarma(avatarId);
                Assert.False(balance.IsError, balance.Message);
                Assert.Equal(0L, balance.Result);
                var stats = await karma.GetKarmaStatsAsync(avatarId);
                Assert.False(stats.IsError, stats.Message);
                Assert.Equal(0L, Assert.IsType<long>(stats.Result["totalKarma"]));

                var persisted = await new HolonManager(provider, dna, providers)
                    .LoadHolonAsync(settingsId, loadChildren: false, allowMissing: true);
                Assert.False(persisted.IsError, persisted.Message);
                Assert.Equal(OASISResultErrorCodes.RecordNotFound, persisted.ErrorCode);
                Assert.Null(persisted.Result);
            }
        }
        finally
        {
            try { await client.DropDatabaseAsync(database); }
            finally { directory.Delete(recursive: true); }
        }
    }
}
