using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileCredentialNativeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RestoringStoredPasswordPreservesPendingProfileAndSessionChanges(bool hyperDriveV2, bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var database = "profile_credentials_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-profile-credentials-");
        try
        {
            var mongo = new MongoDBOASIS(uri, database);
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "credentials.db")};Pooling=False");
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var activated = await provider.ActivateProviderAsync();
                Assert.False(activated.IsError, activated.Message);
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = hyperDriveV2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
                    StorageProviders = new() { ProviderMethodCallTimeOutSeconds = 3 },
                    Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
                    SubscriptionConfig = new() { PlanType = "Gold" }
                } };
                var providers = new ProviderManager(null, dna)
                {
                    IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
                };
                providers.RegisterProvider(provider);
                var selected = await providers.SetAndActivateCurrentStorageProviderAsync(provider.ProviderType.Value);
                Assert.False(selected.IsError, selected.Message);
                var id = Guid.NewGuid();
                var username = "credential-" + id.ToString("N");
                var hash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
                var originalToken = Guid.NewGuid().ToString("N");
                var pendingToken = Guid.NewGuid().ToString("N");
                var now = DateTime.UtcNow;
                var seeded = await provider.SaveAvatarAsync(new Avatar
                {
                    Id = id, Username = username, Email = username + "@example.test", FirstName = "Original",
                    Title = "Original", Password = hash, IsActive = true,
                    AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                    RefreshTokens = new List<RefreshToken> { new() { Token = originalToken, Created = now, Expires = now.AddDays(1) } }
                });
                Assert.False(seeded.IsError, seeded.Message);
                var seededDetail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = username, Email = username + "@example.test" });
                Assert.False(seededDetail.IsError, seededDetail.Message);
                var loaded = await provider.LoadAvatarAsync(id);
                Assert.False(loaded.IsError, loaded.Message);
                Assert.Single(loaded.Result.RefreshTokens);
                loaded.Result.Password = null;
                loaded.Result.FirstName = "Edited";
                loaded.Result.Title = "Edited";
                loaded.Result.RefreshTokens.Add(new RefreshToken { Token = pendingToken, Created = now, Expires = now.AddDays(1) });
                var accounts = new AvatarManager(provider, dna, providers);
                var saved = synchronous
                    ? accounts.SaveAvatar(loaded.Result, providerType: provider.ProviderType.Value)
                    : await accounts.SaveAvatarAsync(loaded.Result, providerType: provider.ProviderType.Value);
                Assert.False(saved.IsError, saved.Message);
                Assert.True(saved.IsSaved);
                var persisted = await provider.LoadAvatarAsync(id);
                Assert.False(persisted.IsError, persisted.Message);
                Assert.Equal("Edited", persisted.Result.FirstName);
                Assert.Equal("Edited", persisted.Result.Title);
                Assert.Equal(hash, persisted.Result.Password);
                Assert.Equal(2, persisted.Result.RefreshTokens.Count);
                Assert.Contains(persisted.Result.RefreshTokens, token => token.Token == originalToken);
                Assert.Contains(persisted.Result.RefreshTokens, token => token.Token == pendingToken);
            }
        }
        finally
        {
            try
            {
                await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            }
            finally
            {
                var path = Path.GetFullPath(directory.FullName);
                var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.True(path.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase));
                Assert.StartsWith("oasis-profile-credentials-", directory.Name);
                directory.Delete(recursive: true);
                Assert.False(Directory.Exists(path));
            }
        }
    }
}
