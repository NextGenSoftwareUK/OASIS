using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosBlockChainOASISIntegrationTests
{
    private CosmosBlockChainOASIS _provider = null!;

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required real-chain test setting {name} is missing. No mock or skipped evidence is allowed.");

    [TestInitialize]
    public async Task Setup()
    {
        _provider = new CosmosBlockChainOASIS(Required("OASIS_COSMOS_TEST_RPC"),
            Required("OASIS_COSMOS_TEST_CHAIN"), Required("OASIS_COSMOS_TEST_KEY"),
            Required("OASIS_COSMOS_TEST_CONTRACT"), "wasm", "0.025stake")
        { IsVersionControlEnabled = true };
        var activation = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider != null)
        {
            await _provider.DeActivateProviderAsync();
            _provider.Dispose();
        }
    }

    [TestMethod]
    public async Task AvatarAndDetailPersistWithIndexesAndVersions()
    {
        var id = Guid.NewGuid();
        var avatar = new Avatar { Id = id, Username = "cosmos-" + id.ToString("N"),
            Email = id.ToString("N") + "@example.test", FirstName = "Before" };
        try
        {
            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            var firstVersion = saved.Result.Version;
            var loaded = await _provider.LoadAvatarAsync(id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("Before", loaded.Result.FirstName);
            Assert.AreEqual(id, (await _provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(id, (await _provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            avatar.FirstName = "After";
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.AreEqual("After", (await _provider.LoadAvatarAsync(id)).Result.FirstName);
            Assert.AreEqual("Before", (await _provider.LoadAvatarAsync(id, firstVersion)).Result.FirstName);
            var detail = new AvatarDetail { Id = id, Username = avatar.Username, Email = avatar.Email };
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            Assert.AreEqual(id, (await _provider.LoadAvatarDetailAsync(id)).Result.Id);
            Assert.AreEqual(id, (await _provider.LoadAvatarDetailByEmailAsync(avatar.Email)).Result.Id);
        }
        finally
        {
            var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }
        Assert.IsTrue((await _provider.LoadAvatarAsync(id)).IsError);
    }

    [TestMethod]
    public async Task HolonsPersistMetadataParentIndexesAndDeletion()
    {
        var parentId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var holon = new Holon { Id = id, ParentHolonId = parentId, Name = "Cosmos roundtrip", Description = "real contract storage" };
        holon.MetaData["evidence"] = id.ToString("N");
        try
        {
            var saved = await _provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var loaded = await _provider.LoadHolonAsync(id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Description, loaded.Result.Description);
            Assert.AreEqual(id.ToString("N"), loaded.Result.MetaData["evidence"].ToString());
            var children = await _provider.LoadHolonsForParentAsync(parentId, loadChildren: false);
            Assert.IsFalse(children.IsError, children.Message);
            Assert.IsTrue(children.Result.Any(x => x.Id == id));
            var filtered = await _provider.LoadHolonsByMetaDataAsync("evidence", id.ToString("N"), loadChildren: false);
            Assert.IsFalse(filtered.IsError, filtered.Message);
            Assert.IsTrue(filtered.Result.Any(x => x.Id == id));
        }
        finally
        {
            var deleted = await _provider.DeleteHolonAsync(id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }
        Assert.IsTrue((await _provider.LoadHolonAsync(id)).IsError);
        Assert.IsFalse((await _provider.LoadHolonsForParentAsync(parentId)).Result.Any(x => x.Id == id));
    }
}
