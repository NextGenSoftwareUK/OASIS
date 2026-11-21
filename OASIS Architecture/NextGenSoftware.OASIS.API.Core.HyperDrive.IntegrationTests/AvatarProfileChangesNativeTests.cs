using System.Text.Json;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileChangesNativeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileEditsAndClearsPreserveManagedStateOnMongoAndSqlite(bool clearOptionalFields)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var database = "profile_fields_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-profile-fields-");
        var mongo = new MongoDBOASIS(uri, database);
        try
        {
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "profile.db")};Pooling=False");
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var activated = await provider.ActivateProviderAsync();
                Assert.False(activated.IsError, activated.Message);
                var id = Guid.NewGuid();
                var username = "profile-" + id.ToString("N");
                var password = Guid.NewGuid().ToString("N");
                var verification = Guid.NewGuid().ToString("N");
                var quest = Guid.NewGuid();
                var objective = Guid.NewGuid();
                var avatar = new Avatar { Id = id, Username = username, Email = username + "@example.test",
                    Title = "Original", FirstName = "Original", LastName = "Original", Password = password,
                    VerificationToken = verification, IsActive = true,
                    AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
                var detail = new AvatarDetail { Id = id, Username = username, Email = avatar.Email, Address = "Original",
                    ActiveQuestId = quest, ActiveObjectiveId = objective, Karma = 37, XP = 42 };
                var seedAvatar = await provider.SaveAvatarAsync(avatar);
                var seedDetail = await provider.SaveAvatarDetailAsync(detail);
                Assert.False(seedAvatar.IsError, seedAvatar.Message);
                Assert.False(seedDetail.IsError, seedDetail.Message);
                var loadedAvatar = await provider.LoadAvatarAsync(id);
                var loadedDetail = await provider.LoadAvatarDetailAsync(id);
                Assert.False(loadedAvatar.IsError, loadedAvatar.Message);
                Assert.False(loadedDetail.IsError, loadedDetail.Message);
                var expectedName = clearOptionalFields ? "" : "Changed";
                var patch = clearOptionalFields
                    ? JsonSerializer.Deserialize<AvatarProfilePatch>("{\"title\":null,\"firstName\":\"\",\"lastName\":null,\"address\":null}")!
                    : new AvatarProfilePatch { Title = expectedName, FirstName = expectedName, LastName = expectedName,
                        Address = expectedName, Username = username + "-edited", Email = username + "-edited@example.test" };
                var applied = AvatarProfileChanges.Apply(patch, loadedAvatar.Result, loadedDetail.Result);
                Assert.False(applied.IsError, applied.Message);
                Assert.True(applied.Result.AvatarChanged);
                Assert.True(applied.Result.DetailChanged);
                Assert.False(applied.IsSaved);
                var savedDetail = await provider.SaveAvatarDetailAsync(loadedDetail.Result);
                var savedAvatar = await provider.SaveAvatarAsync(loadedAvatar.Result);
                Assert.False(savedDetail.IsError, savedDetail.Message);
                Assert.False(savedAvatar.IsError, savedAvatar.Message);
                var persistedAvatar = await provider.LoadAvatarAsync(id);
                var persistedDetail = await provider.LoadAvatarDetailAsync(id);
                Assert.False(persistedAvatar.IsError, persistedAvatar.Message);
                Assert.False(persistedDetail.IsError, persistedDetail.Message);
                Assert.Equal(expectedName, persistedAvatar.Result.Title);
                Assert.Equal(expectedName, persistedAvatar.Result.FirstName);
                Assert.Equal(expectedName, persistedAvatar.Result.LastName);
                Assert.Equal(expectedName, persistedDetail.Result.Address);
                Assert.Equal(password, persistedAvatar.Result.Password);
                Assert.Equal(verification, persistedAvatar.Result.VerificationToken);
                Assert.True(persistedAvatar.Result.IsActive);
                Assert.Equal(quest, persistedDetail.Result.ActiveQuestId);
                Assert.Equal(objective, persistedDetail.Result.ActiveObjectiveId);
                Assert.Equal(37, persistedDetail.Result.Karma);
                Assert.Equal(42, persistedDetail.Result.XP);
                Assert.Equal(persistedAvatar.Result.Username, persistedDetail.Result.Username);
                Assert.Equal(persistedAvatar.Result.Email, persistedDetail.Result.Email);
                var unchanged = AvatarProfileChanges.Apply(patch, persistedAvatar.Result, persistedDetail.Result);
                Assert.False(unchanged.IsError, unchanged.Message);
                Assert.False(unchanged.Result.AvatarChanged);
                Assert.False(unchanged.Result.DetailChanged);
            }
        }
        finally
        {
            try
            {
                await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            }
            finally { directory.Delete(recursive: true); Assert.False(Directory.Exists(directory.FullName)); }
        }
    }

    [Fact]
    public void InvalidIdentityInputDoesNotPartiallyApplyOtherFields()
    {
        var id = Guid.NewGuid();
        var avatar = new Avatar { Id = id, Title = "Original" };
        var detail = new AvatarDetail { Id = id, Address = "Original" };
        var result = AvatarProfileChanges.Apply(new AvatarProfilePatch { Title = "Changed", Address = "Changed", Email = null }, avatar, detail);
        Assert.True(result.IsError);
        Assert.Equal("AVATAR_PROFILE_INVALID", result.ErrorCode);
        Assert.Equal("Original", avatar.Title);
        Assert.Equal("Original", detail.Address);
    }

    [Fact]
    public void RecordsFromDifferentAvatarsAreRefusedWithoutMutation()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Title = "Original" };
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Address = "Original" };
        var result = AvatarProfileChanges.Apply(new AvatarProfilePatch { Title = "Changed", Address = "Changed" }, avatar, detail);
        Assert.True(result.IsError);
        Assert.Equal("AVATAR_PROFILE_ID_MISMATCH", result.ErrorCode);
        Assert.Equal("Original", avatar.Title);
        Assert.Equal("Original", detail.Address);
    }
}
