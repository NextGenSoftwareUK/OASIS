using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using AuraProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2.IntegrationTests;

[TestClass]
public class Neo4jAuraNativeTests
{
    [TestMethod]
    public async Task AvatarLifecycleUsesPersistedIdentityAndDeletionMode()
    {
        var provider = CreateProvider();
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
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, hidden.ErrorCode);
            Assert.IsFalse(hidden.IsLoaded);
            Assert.IsNull(hidden.Result);

            var hard = provider.DeleteAvatar(id, softDelete: false);
            Assert.IsTrue(hard.IsDeleted, hard.Exception?.GetType().FullName);
            var missing = await provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode);
            Assert.IsFalse(missing.Result);
        }
        finally
        {
            try
            {
                var cleanup = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(cleanup.IsDeleted || cleanup.ErrorCode == OASISResultErrorCodes.RecordNotFound, cleanup.Exception?.GetType().FullName);
            }
            finally
            {
                var inactive = await provider.DeActivateProviderAsync();
                Assert.IsTrue(inactive.Result, inactive.Exception?.GetType().FullName);
            }
        }
    }

    [TestMethod]
    public async Task HolonLifecycleUsesPersistedParentAndProviderKey()
    {
        var provider = CreateProvider();
        var parent = new Holon { Id = Guid.NewGuid(), Name = "Aura parent" };
        var child = new Holon { Id = Guid.NewGuid(), Name = $"Aura child {parent.Id:N}", ParentHolonId = parent.Id };
        var active = await provider.ActivateProviderAsync();
        Assert.IsTrue(active.Result, active.Exception?.GetType().FullName);
        try
        {
            var saved = await provider.SaveHolonsAsync(new[] { parent, child }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Exception?.GetType().FullName);
            Assert.AreEqual(2, saved.SavedCount);

            var parentKey = parent.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS];
            var childKey = child.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS];
            var loaded = await provider.LoadHolonAsync(childKey, loadChildren: false);
            Assert.IsTrue(loaded.IsLoaded, loaded.Exception?.GetType().FullName);
            Assert.AreEqual(child.Id, loaded.Result.Id);
            Assert.AreEqual(parent.Id, loaded.Result.ParentHolonId);

            var children = provider.LoadHolonsForParent(parentKey, loadChildren: false);
            Assert.IsTrue(children.IsLoaded, children.Exception?.GetType().FullName);
            CollectionAssert.AreEqual(new[] { child.Id }, children.Result.Select(item => item.Id).ToArray());

            var matches = await provider.SearchAsync(new SearchParams
            {
                SearchOnlyForCurrentAvatar = false,
                ParentId = parent.Id,
                SearchGroups = new()
                {
                    new SearchTextGroup { SearchHolons = true, HolonType = HolonType.All, SearchQuery = child.Name }
                }
            }, loadChildren: false);
            Assert.IsTrue(matches.IsLoaded, matches.Exception?.GetType().FullName);
            CollectionAssert.AreEqual(new[] { child.Id }, matches.Result.SearchResultHolons.Select(item => item.Id).ToArray());

            var deleted = await provider.DeleteHolonAsync(childKey);
            Assert.IsTrue(deleted.IsDeleted, deleted.Exception?.GetType().FullName);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, provider.LoadHolon(child.Id).ErrorCode);
            Assert.IsTrue(provider.LoadHolon(parent.Id, loadChildren: false).IsLoaded);
        }
        finally
        {
            try
            {
                var childCleanup = await provider.DeleteHolonAsync(child.Id);
                var parentCleanup = await provider.DeleteHolonAsync(parent.Id);
                Assert.IsTrue(childCleanup.IsDeleted || childCleanup.ErrorCode == OASISResultErrorCodes.RecordNotFound);
                Assert.IsTrue(parentCleanup.IsDeleted || parentCleanup.ErrorCode == OASISResultErrorCodes.RecordNotFound);
            }
            finally
            {
                var inactive = await provider.DeActivateProviderAsync();
                Assert.IsTrue(inactive.Result, inactive.Exception?.GetType().FullName);
            }
        }
    }

    private static AuraProvider CreateProvider()
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

        return new AuraProvider(endpoint!, username!, password!);
    }
}
