using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiStorageTests
{
    private SuiOASIS _provider = null!;

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required real-chain test setting {name} is missing. No mock or skipped evidence is allowed.");

    [TestInitialize]
    public async Task Setup()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback, "Evidence cannot spend public-chain assets.");
        using var deployment = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var root = deployment.RootElement;
        _provider = new SuiOASIS(rpc, "localnet", root.GetProperty("chainId").GetString()!,
            root.GetProperty("packageAddress").GetString()!, root.GetProperty("privateKey").GetString()!,
            root.GetProperty("storageObjectId").GetString()!) { IsVersionControlEnabled = true };
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
        var avatar = new Avatar { Id = id, Username = "sui-" + id.ToString("N"),
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
        var holon = new Holon { Id = id, ParentHolonId = parentId, Name = "Sui roundtrip", Description = "real contract storage" };
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

    [TestMethod]
    public async Task ImportSearchExportAndProximityUsePersistedRecords()
    {
        var name = "sui-query-" + Guid.NewGuid().ToString("N");
        var root = new Holon { Id = Guid.NewGuid(), Name = name };
        var child = new Holon { Id = Guid.NewGuid(), Name = name + "-child", ParentHolonId = root.Id };
        root.MetaData["Latitude"] = 51d;
        root.MetaData["Longitude"] = 0d;
        child.MetaData["Latitude"] = 52d;
        child.MetaData["Longitude"] = 0d;
        try
        {
            var imported = await _provider.ImportAsync(new[] { root, child });
            Assert.IsFalse(imported.IsError, imported.Message);
            var providerKey = root.ProviderUniqueStorageKey[ProviderType.SuiOASIS];
            Assert.AreEqual(root.Id, (await _provider.LoadHolonAsync(providerKey, loadChildren: false)).Result.Id);
            var tree = await _provider.LoadHolonAsync(root.Id);
            Assert.IsFalse(tree.IsError, tree.Message);
            Assert.IsTrue(tree.Result.Children.Any(item => item.Id == child.Id));
            var search = await _provider.SearchAsync(new SearchParams
            {
                SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = name } }
            });
            Assert.IsFalse(search.IsError, search.Message);
            Assert.AreEqual(2, search.Result.SearchResultHolons.Count);
            var exported = await _provider.ExportAllAsync();
            Assert.IsFalse(exported.IsError, exported.Message);
            Assert.IsTrue(exported.Result.Any(item => item.Id == root.Id));
            Assert.IsTrue(exported.Result.Any(item => item.Id == child.Id));
            var nearby = ((IOASISNETProvider)_provider).GetHolonsNearMe(51000000, 0, 100, HolonType.All);
            Assert.IsFalse(nearby.IsError, nearby.Message);
            Assert.IsTrue(nearby.Result.Any(item => item.Id == root.Id));
            Assert.IsFalse(nearby.Result.Any(item => item.Id == child.Id));
        }
        finally
        {
            Assert.IsFalse((await _provider.DeleteHolonAsync(child.Id)).IsError);
            Assert.IsFalse((await _provider.DeleteHolonAsync(root.Id)).IsError);
        }
    }
}
