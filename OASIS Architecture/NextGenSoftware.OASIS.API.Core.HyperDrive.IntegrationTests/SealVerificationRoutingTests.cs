using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using System.Security.Cryptography;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SealVerificationRoutingTests
{
    [Fact]
    public async Task SealVerificationPreservesNativeRouting()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");

        var database = "hyperdrive_seal_routing_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-seal-routing-");
        var previousPolicy = HolonSealing.Policy;
        var previousApp = HolonSealing.CurrentAppId;
        MongoDBOASIS? mongo = null;
        SQLLiteDBOASIS? sqlite = null;
        try
        {
            var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var policy = new HolonSealPolicy(() => new HolonSealSettings { Enabled = true, Key = key });
            var owner = Guid.NewGuid();
            policy.SetOwner(owner, true);
            HolonSealing.Policy = policy;
            HolonSealing.CurrentAppId = null;
            mongo = new MongoDBOASIS(uri, database);
            sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "routing.db")};Pooling=False");
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            var providers = new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            providers.RegisterProvider(mongo);
            providers.RegisterProvider(sqlite);
            var activated = await mongo.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            var selected = await providers.SetAndActivateCurrentStorageProviderAsync(ProviderType.SQLLiteDBOASIS);
            Assert.False(selected.IsError, selected.Message);

            var id = Guid.NewGuid();
            var saved = await mongo.SaveHolonAsync(new Holon { Id = id, Name = "Owned seal routing fixture",
                CreatedByAvatarId = owner, IsActive = true }, saveChildren: false);
            Assert.False(saved.IsError, saved.Message);
            var manager = new HolonManager(mongo, dna, providers);
            var current = providers.CurrentStorageProvider;
            var preferred = providers.DefaultGlobalStorageProvider;
            Assert.Same(sqlite, current);
            var verified = await Task.WhenAll(Enumerable.Range(0, 4).Select(index =>
                manager.VerifyHolonSealAsync(id, index % 2 == 0 ? ProviderType.Default : ProviderType.MongoDBOASIS)));
            foreach (var result in verified)
            {
                Assert.Equal(HolonSealStatus.Verified, result.Status);
                Assert.Equal(id, result.HolonId);
                Assert.Equal(1, result.SealedVersionCount);
            }
            Assert.Same(current, providers.CurrentStorageProvider);
            Assert.Same(preferred, providers.DefaultGlobalStorageProvider);
            Assert.Equal(ProviderType.SQLLiteDBOASIS, providers.CurrentStorageProviderType.Value);
        }
        finally
        {
            try
            {
                if (mongo != null) await mongo.DeActivateProviderAsync();
                sqlite?.Dispose();
                await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            }
            finally
            {
                HolonSealing.Policy = previousPolicy;
                HolonSealing.CurrentAppId = previousApp;
                directory.Delete(recursive: true);
            }
        }
    }
}
