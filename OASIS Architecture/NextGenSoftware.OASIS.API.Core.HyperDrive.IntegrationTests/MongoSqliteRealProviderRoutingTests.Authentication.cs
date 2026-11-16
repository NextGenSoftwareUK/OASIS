using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    [Fact]
    public async Task AvatarLoginAndRefreshUseSQLiteWhenMongoRejectsSessionWrites()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("Owned reusable Mongo runner required.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1")))
            root = root.Parent;
        if (root == null) throw new InvalidOperationException("Existing checkout required.");

        var directory = Path.Combine(root.FullName, "TestResults", "HyperDriveMongoSqliteCurrent");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var database = "hyperdrive_login_" + suffix;
        var app = "hyperdrive-login-" + suffix;
        var sqlitePath = Path.Combine(directory, app + ".db");
        var client = new MongoClient(uri);
        var admin = client.GetDatabase("admin");
        var mongo = new MongoDBOASIS(uri + "&appName=" + app, database);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={sqlitePath};Pooling=False");
        const string password = "Fixture-Login-9!";
        const string ip = "127.0.0.1";
        var failpointEnabled = false;

        try
        {
            (await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
            var dna = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = HyperDriveModes.V2,
                    StorageProviders = new() { ProviderMethodCallTimeOutSeconds = 3 },
                    Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
                    SubscriptionConfig = new() { PlanType = "Gold" }
                }
            };
            var providers = new[]
            {
                new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
            };
            var manager = new ProviderManager(null, dna)
            {
                IsAutoFailOverEnabled = true,
                IsAutoReplicationEnabled = true,
                IsAutoLoadBalanceEnabled = false
            };
            manager.RegisterProvider(mongo);
            manager.RegisterProvider(sqlite);
            (await manager.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS)).IsError.Should().BeFalse();
            manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
            manager.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(providers).IsError.Should().BeFalse();
            manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
            var router = new OASISHyperDrive(manager);
            var fixture = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = app,
                Email = app + "@example.invalid",
                IsActive = true,
                Verified = DateTime.UtcNow,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                AvatarType = new EnumValue<AvatarType>(AvatarType.User)
            };
            var seed = await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest
            {
                Operation = "SaveAvatar", Payload = fixture, PreferredProvider = ProviderType.MongoDBOASIS
            });
            seed.IsSaved.Should().BeTrue(seed.Message);
            seed.IsWarning.Should().BeFalse(seed.Message);
            (await sqlite.LoadAvatarAsync(fixture.Id)).Result.Password.Should().Be(fixture.Password);

            await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                { "data", new BsonDocument
                    {
                        { "failCommands", new BsonArray { "update", "insert", "findAndModify" } },
                        { "appName", app }, { "errorCode", 2 }
                    } }
            });
            failpointEnabled = true;

            var avatarManager = new AvatarManager(mongo, dna, manager);
            var invalid = await avatarManager.AuthenticateAsync(fixture.Username, "wrong-password", ip);
            invalid.IsError.Should().BeTrue();
            var login = await avatarManager.AuthenticateAsync(fixture.Username, password, ip);
            login.IsError.Should().BeFalse(login.Message);
            login.IsSaved.Should().BeTrue();
            login.Result.JwtToken.Should().NotBeNullOrEmpty();
            var token = login.Result.RefreshToken;
            token.Should().NotBeNullOrEmpty();
            var firstSession = (await sqlite.LoadAvatarAsync(fixture.Id)).Result.RefreshTokens.Single();
            var copyStillOnMongo = (await mongo.LoadAvatarAsync(fixture.Id)).Result;
            (copyStillOnMongo.RefreshTokens?.Any(refresh => refresh.Token == firstSession.Token) ?? false)
                .Should().BeFalse();
            token.Should().StartWith("oasis-r1.SQLLiteDBOASIS.");
            avatarManager.RefreshToken(firstSession.Token, ip).IsError.Should().BeTrue();
            avatarManager.RefreshToken(token.Replace("SQLLiteDBOASIS", "MongoDBOASIS"), ip).IsError.Should().BeTrue();

            var rotated = avatarManager.RefreshToken(token, ip);
            rotated.IsError.Should().BeFalse(rotated.Message);
            rotated.Result.RefreshToken.Should().NotBe(token).And.StartWith("oasis-r1.SQLLiteDBOASIS.");
            var stored = (await sqlite.LoadAvatarAsync(fixture.Id)).Result;
            stored.RefreshTokens.Single(refresh => refresh.Token == firstSession.Token).IsActive.Should().BeFalse();
            stored.RefreshTokens.Should().ContainSingle(refresh => refresh.IsActive);
            avatarManager.RefreshToken(token, ip).IsError.Should().BeTrue("rotation revokes the old token");

            var revoked = avatarManager.RevokeToken(rotated.Result.RefreshToken, ip);
            revoked.IsSaved.Should().BeTrue(revoked.Message);
            avatarManager.RefreshToken(rotated.Result.RefreshToken, ip).IsError.Should().BeTrue();
            (await sqlite.LoadAvatarAsync(fixture.Id)).Result.RefreshTokens.Should()
                .OnlyContain(refresh => !refresh.IsActive);
        }
        finally
        {
            if (failpointEnabled)
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            await mongo.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
            await client.DropDatabaseAsync(database);
            if (File.Exists(sqlitePath)) File.Delete(sqlitePath);
        }
    }
}
