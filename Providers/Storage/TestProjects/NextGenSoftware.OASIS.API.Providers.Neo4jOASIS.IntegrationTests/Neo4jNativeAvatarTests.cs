using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ConcurrentProfileUpdateCannotBeLostToStaleDelete(bool softDelete)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var heldSession = raw.AsyncSession();
        var count = await (await heldSession.RunAsync("MATCH (a:Avatar) RETURN count(a) AS count")).SingleAsync();
        Assert.AreEqual(0L, count["count"].As<long>(), "Retained evidence exists; no overwrite allowed.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var avatar = new Avatar { Id = Guid.Parse("00000000-0000-4000-8000-000000000100"),
            Username = "owned-concurrent", FirstName = "before", IsActive = true };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        using var transaction = await heldSession.BeginTransactionAsync();
        Task<NextGenSoftware.OASIS.Common.OASISResult<bool>>? deletion = null;
        var committed = false;
        try
        {
            await (await transaction.RunAsync("MATCH (a:Avatar {Id:$id}) SET a.__heldTestLock = true REMOVE a.__heldTestLock RETURN a.Id", new { id = avatar.Id.ToString() })).ConsumeAsync();
            deletion = provider.DeleteAvatarAsync(avatar.Id, softDelete);
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var blocked = false;
            do
            {
                using var inspection = raw.AsyncSession();
                var rows = await (await inspection.RunAsync("SHOW TRANSACTIONS YIELD currentQuery, status WHERE currentQuery CONTAINS '__oasisAvatarLock' RETURN status")).ToListAsync();
                blocked = rows.Any(row => row["status"].As<string>().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase));
                if (!blocked) await Task.Delay(50);
            } while (!blocked && !deletion.IsCompleted && DateTime.UtcNow < deadline);
            Assert.IsTrue(blocked, "Delete must be observed waiting on the actual held node lock before update commits.");
            avatar.FirstName = "concurrent committed profile";
            var snapshot = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(avatar);
            await (await transaction.RunAsync("MATCH (a:Avatar {Id:$id}) SET a.SnapshotJson=$snapshot RETURN a.Id", new { id = avatar.Id.ToString(), snapshot })).ConsumeAsync();
            await transaction.CommitAsync(); committed = true;
            var rejected = await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(rejected.IsError); Assert.IsFalse(rejected.Result); Assert.IsFalse(rejected.IsSaved);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode, rejected.ErrorCode);
            var current = await provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(current.IsError, current.Message);
            Assert.IsTrue(current.Result.IsActive); Assert.IsFalse(current.Result.IsDeleted);
            Assert.AreEqual("concurrent committed profile", current.Result.FirstName);
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id, false)).IsError);
        }
        finally
        {
            if (!committed) await transaction.RollbackAsync();
            if (deletion != null) await deletion.WaitAsync(TimeSpan.FromSeconds(10));
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task OwnedNativeAvatarSnapshotsSelectorsDeletionAndReactivation()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        const string password = "oasis-local-test-only"; // Public credential for the loopback-only disposable test service.
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", password));
        using (var preflight = raw.AsyncSession())
        {
            var row = await (await preflight.RunAsync("MATCH (a:Avatar) RETURN count(a) AS count")).SingleAsync();
            Assert.AreEqual(0L, row["count"].As<long>(), "Retained avatar evidence exists; refuse overwrite or deletion.");
        }
        var provider = new Provider(uri, "neo4j", password);
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            for (var index = 0; index < 8; index++)
            {
                var id = Guid.Parse("00000000-0000-4000-8000-" + (index + 1).ToString("D12"));
                var avatar = new Avatar { Id = id, Username = "owned-native-" + index,
                    Email = "owned-native-" + index + "@example.invalid", FirstName = "Full", LastName = "Snapshot",
                    Description = "native complete profile", IsActive = true, Version = 3, VersionId = Guid.NewGuid() };
                avatar.MetaData["biome"] = "woodland";
                var saved = await provider.SaveAvatarAsync(avatar);
                Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved);
                foreach (var read in new[] { await provider.LoadAvatarAsync(id), await provider.LoadAvatarByProviderKeyAsync(id.ToString()),
                    await provider.LoadAvatarByUsernameAsync(avatar.Username), await provider.LoadAvatarByEmailAsync(avatar.Email) })
                {
                    Assert.IsFalse(read.IsError, read.Message); Assert.IsTrue(read.IsLoaded);
                    Assert.AreEqual("Full", read.Result.FirstName); Assert.AreEqual(avatar.VersionId, read.Result.VersionId);
                    Assert.AreEqual("woodland", read.Result.MetaData["biome"]);
                }
                if (index == 0)
                {
                    Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                    Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
                    Assert.AreEqual("Snapshot", (await provider.LoadAvatarAsync(id)).Result.LastName);
                    avatar.FirstName = null;
                    Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
                    Assert.IsNull((await provider.LoadAvatarAsync(id)).Result.FirstName);
                }
                var collection = await provider.LoadAllAvatarsAsync();
                Assert.IsFalse(collection.IsError, collection.Message); Assert.IsTrue(collection.IsLoaded);
                Assert.AreEqual(id, collection.Result.Single().Id);
                var soft = index switch {
                    0 => await provider.DeleteAvatarAsync(id),
                    1 => await provider.DeleteAvatarAsync(id.ToString()),
                    2 => await provider.DeleteAvatarByEmailAsync(avatar.Email),
                    3 => await provider.DeleteAvatarByUsernameAsync(avatar.Username),
                    4 => provider.DeleteAvatar(id),
                    5 => provider.DeleteAvatar(id.ToString()),
                    6 => provider.DeleteAvatarByEmail(avatar.Email),
                    _ => provider.DeleteAvatarByUsername(avatar.Username)
                };
                Assert.IsFalse(soft.IsError, soft.Message); Assert.IsTrue(soft.Result); Assert.IsTrue(soft.IsSaved);
                var deleted = await provider.LoadAvatarAsync(id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsFalse(deleted.Result.IsActive); Assert.IsTrue(deleted.Result.IsDeleted);
                Assert.AreEqual("Snapshot", deleted.Result.LastName); Assert.AreEqual("woodland", deleted.Result.MetaData["biome"]);
                var hard = await provider.DeleteAvatarAsync(id, false);
                Assert.IsFalse(hard.IsError, hard.Message); Assert.IsTrue(hard.Result);
                using var check = raw.AsyncSession();
                var remaining = await (await check.RunAsync("MATCH (a:Avatar {Id:$id}) RETURN count(a) AS count", new { id = id.ToString() })).SingleAsync();
                Assert.AreEqual(0L, remaining["count"].As<long>());
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
        // Failure leaves owned records intact for inspection; never sweep a database or unknown fixtures.
    }
}
