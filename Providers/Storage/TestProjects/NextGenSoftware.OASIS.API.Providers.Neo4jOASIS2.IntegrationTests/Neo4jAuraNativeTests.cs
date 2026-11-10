using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using AuraProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2.IntegrationTests;

[TestClass]
public class Neo4jAuraNativeTests
{
    [TestMethod]
    public async Task AvatarLifecycleUsesPersistedIdentityAndDeletionMode()
    {
        if (Environment.GetEnvironmentVariable("OASIS_NEO4J_AURA_LIFECYCLE") != "1")
            Assert.Inconclusive("Enable only for a disposable Neo4j Aura database.");

        var endpoint = Environment.GetEnvironmentVariable("OASIS_NEO4J_AURA_URI");
        var username = Environment.GetEnvironmentVariable("OASIS_NEO4J_AURA_USERNAME");
        var password = Environment.GetEnvironmentVariable("OASIS_NEO4J_AURA_PASSWORD");
        Assert.IsTrue(Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            (uri.Scheme is "neo4j+s" or "bolt+s") && string.IsNullOrEmpty(uri.UserInfo));
        Assert.IsFalse(string.IsNullOrWhiteSpace(username));
        Assert.IsFalse(string.IsNullOrWhiteSpace(password));

        var provider = new AuraProvider(endpoint!, username!, password!);
        var id = Guid.NewGuid();
        var avatar = new Avatar
        {
            AvatarId = id,
            Username = $"aura-{id:N}",
            Email = $"aura-{id:N}@example.invalid",
            Title = "Aura lifecycle fixture",
            FirstName = "Before",
            LastName = "Update"
        };

        var active = await provider.ActivateProviderAsync();
        Assert.IsTrue(active.Result, active.Exception?.GetType().FullName);
        try
        {
            var saved = await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(saved.IsSaved, saved.Exception?.GetType().FullName);
            var key = saved.Result.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS];

            avatar.FirstName = "After";
            var updated = provider.SaveAvatar(avatar);
            Assert.IsTrue(updated.IsSaved, updated.Exception?.GetType().FullName);
            Assert.AreEqual(id, (await provider.LoadAvatarByProviderKeyAsync(key)).Result.AvatarId);
            Assert.AreEqual("After", provider.LoadAvatar(id).Result.FirstName);
            Assert.AreEqual(id, (await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.AvatarId);
            Assert.AreEqual(id, provider.LoadAvatarByUsername(avatar.Username).Result.AvatarId);

            var soft = await provider.DeleteAvatarAsync(id);
            Assert.IsTrue(soft.IsDeleted, soft.Exception?.GetType().FullName);
            var hidden = await provider.LoadAvatarAsync(id);
            Assert.IsFalse(hidden.IsError, hidden.Exception?.GetType().FullName);
            Assert.IsFalse(hidden.IsLoaded);
            Assert.IsNull(hidden.Result);

            var hard = provider.DeleteAvatar(id, softDelete: false);
            Assert.IsTrue(hard.IsDeleted, hard.Exception?.GetType().FullName);
            var missing = await provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsFalse(missing.IsError, missing.Exception?.GetType().FullName);
            Assert.IsFalse(missing.Result);
        }
        finally
        {
            try
            {
                var cleanup = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsFalse(cleanup.IsError, cleanup.Exception?.GetType().FullName);
            }
            finally
            {
                var inactive = await provider.DeActivateProviderAsync();
                Assert.IsTrue(inactive.Result, inactive.Exception?.GetType().FullName);
            }
        }
    }
}
