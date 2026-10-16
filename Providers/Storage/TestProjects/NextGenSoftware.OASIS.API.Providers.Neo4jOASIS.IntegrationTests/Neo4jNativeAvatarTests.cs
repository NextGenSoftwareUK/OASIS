using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class Neo4jNativeAvatarTests
{
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
