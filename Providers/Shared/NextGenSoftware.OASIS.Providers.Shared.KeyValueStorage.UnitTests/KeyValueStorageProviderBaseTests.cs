using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests
{
    internal sealed class InMemoryBackend : IKeyValueBackend
    {
        public readonly ConcurrentDictionary<string, string> Items = new();
        public bool Reachable = true;

        public Task<string> GetAsync(string key, CancellationToken ct = default) => Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);
        public Task PutAsync(string key, string value, CancellationToken ct = default) { Items[key] = value; return Task.CompletedTask; }
        public Task DeleteAsync(string key, CancellationToken ct = default) { Items.TryRemove(key, out _); return Task.CompletedTask; }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(Items.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k).ToList());
        public Task VerifyAsync(CancellationToken ct = default) => Reachable ? Task.CompletedTask : throw new InvalidOperationException("store unreachable");
    }

    internal sealed class TestProvider : KeyValueStorageProviderBase
    {
        public TestProvider(IKeyValueBackend backend) : base(backend)
        {
            ProviderName = "TestKVOASIS";
            ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.VercelKVOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(API.Core.Enums.ProviderCategory.Storage);
        }
    }

    [TestClass]
    public class KeyValueStorageProviderBaseTests
    {
        private InMemoryBackend _backend;
        private TestProvider _provider;

        [TestInitialize]
        public void Init()
        {
            _backend = new InMemoryBackend();
            _provider = new TestProvider(_backend);
        }

        private static Avatar NewAvatar(string username = "neo", string email = "neo@matrix.io")
            => new() { Username = username, Email = email, FirstName = "Thomas", LastName = "Anderson" };

        [TestMethod]
        public async Task Activation_fails_when_store_is_unreachable()
        {
            _backend.Reachable = false;
            var result = await _provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task Avatar_round_trips_by_id_username_email_and_provider_key()
        {
            var saved = await _provider.SaveAvatarAsync(NewAvatar());
            Assert.IsFalse(saved.IsError, saved.Message);
            var id = saved.Result.Id;
            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(id.ToString(), saved.Result.ProviderUniqueStorageKey[ProviderType.VercelKVOASIS]);

            Assert.AreEqual("neo", (await _provider.LoadAvatarAsync(id)).Result.Username);
            Assert.AreEqual(id, (await _provider.LoadAvatarByUsernameAsync("NEO")).Result.Id);
            Assert.AreEqual(id, (await _provider.LoadAvatarByEmailAsync("Neo@Matrix.io")).Result.Id);
            Assert.AreEqual(id, (await _provider.LoadAvatarByProviderKeyAsync(id.ToString())).Result.Id);
        }

        [TestMethod]
        public async Task Missing_avatar_is_an_error_not_an_empty_success()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
        }

        [TestMethod]
        public async Task Duplicate_username_is_rejected()
        {
            Assert.IsFalse((await _provider.SaveAvatarAsync(NewAvatar("trinity", "t@m.io"))).IsError);
            var clash = await _provider.SaveAvatarAsync(NewAvatar("trinity", "other@m.io"));
            Assert.IsTrue(clash.IsError);
        }

        [TestMethod]
        public async Task Renaming_an_avatar_moves_its_username_index()
        {
            var avatar = (await _provider.SaveAvatarAsync(NewAvatar("morpheus", "m@m.io"))).Result;
            avatar.Username = "morpheus2";
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);

            Assert.IsTrue((await _provider.LoadAvatarByUsernameAsync("morpheus")).IsError);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByUsernameAsync("morpheus2")).Result.Id);
        }

        [TestMethod]
        public async Task Soft_delete_keeps_the_record_and_hard_delete_removes_it_and_its_indexes()
        {
            var id = (await _provider.SaveAvatarAsync(NewAvatar())).Result.Id;

            Assert.IsFalse((await _provider.DeleteAvatarAsync(id, softDelete: true)).IsError);
            var soft = await _provider.LoadAvatarAsync(id);
            Assert.IsFalse(soft.IsError);
            Assert.AreNotEqual(default, soft.Result.DeletedDate);

            Assert.IsFalse((await _provider.DeleteAvatarByUsernameAsync("neo", softDelete: false)).IsError);
            Assert.IsTrue((await _provider.LoadAvatarAsync(id)).IsError);
            Assert.IsTrue((await _provider.LoadAvatarByEmailAsync("neo@matrix.io")).IsError);
            Assert.AreEqual(0, _backend.Items.Count);
        }

        [TestMethod]
        public async Task Avatar_detail_loads_through_the_avatar_indexes()
        {
            var id = (await _provider.SaveAvatarAsync(NewAvatar())).Result.Id;
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = "neo", Karma = 42 })).IsError);

            var detail = await _provider.LoadAvatarDetailByEmailAsync("neo@matrix.io");
            Assert.IsFalse(detail.IsError, detail.Message);
            Assert.AreEqual(42, detail.Result.Karma);
            Assert.AreEqual(1, (await _provider.LoadAllAvatarDetailsAsync()).Result.Count());
        }

        [TestMethod]
        public async Task Holon_tree_saves_and_loads_children_recursively()
        {
            var root = new Holon { Name = "root", HolonType = HolonType.Holon };
            var child = new Holon { Name = "child", HolonType = HolonType.Holon };
            child.Children = new List<API.Core.Interfaces.IHolon> { new Holon { Name = "grandchild" } };
            root.Children = new List<API.Core.Interfaces.IHolon> { child };

            var saved = await _provider.SaveHolonAsync(root);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(root.Id, loadChildren: true, recursive: true);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("child", loaded.Result.Children.Single().Name);
            Assert.AreEqual("grandchild", loaded.Result.Children.Single().Children.Single().Name);

            var forParent = await _provider.LoadHolonsForParentAsync(root.Id.ToString(), loadChildren: false);
            Assert.AreEqual(child.Id, forParent.Result.Single().Id);
        }

        [TestMethod]
        public async Task Deleting_a_holon_removes_it_and_its_parent_link()
        {
            var parent = new Holon { Name = "p" };
            var child = new Holon { Name = "c" };
            parent.Children = new List<API.Core.Interfaces.IHolon> { child };
            await _provider.SaveHolonAsync(parent);

            Assert.IsFalse((await _provider.DeleteHolonAsync(child.Id)).IsError);
            Assert.IsTrue((await _provider.LoadHolonAsync(child.Id)).IsError);
            Assert.AreEqual(0, (await _provider.LoadHolonsForParentAsync(parent.Id)).Result.Count());
        }

        [TestMethod]
        public async Task Metadata_queries_match_all_or_any()
        {
            var a = new Holon { Name = "a", MetaData = new Dictionary<string, object> { ["colour"] = "red", ["size"] = "L" } };
            var b = new Holon { Name = "b", MetaData = new Dictionary<string, object> { ["colour"] = "red", ["size"] = "S" } };
            await _provider.SaveHolonsAsync(new[] { a, b });

            var all = await _provider.LoadHolonsByMetaDataAsync(new Dictionary<string, string> { ["colour"] = "red", ["size"] = "L" }, MetaKeyValuePairMatchMode.All);
            var any = await _provider.LoadHolonsByMetaDataAsync(new Dictionary<string, string> { ["size"] = "L", ["size2"] = "S" }, MetaKeyValuePairMatchMode.Any);
            Assert.AreEqual(a.Id, all.Result.Single().Id);
            Assert.AreEqual(a.Id, any.Result.Single().Id);
            Assert.AreEqual(2, (await _provider.LoadHolonsByMetaDataAsync("colour", "red")).Result.Count());
        }

        [TestMethod]
        public async Task Search_finds_avatars_and_holons_by_text()
        {
            await _provider.SaveAvatarAsync(NewAvatar());
            await _provider.SaveHolonAsync(new Holon { Name = "Nebuchadnezzar", Description = "hovercraft" });

            var search = new SearchParams { SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = "ne" } } };
            var result = await _provider.SearchAsync(search);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(1, result.Result.SearchResultAvatars.Count);
            Assert.AreEqual(1, result.Result.SearchResultHolons.Count);
            Assert.AreEqual(2, result.Result.NumberOfResults);
        }

        [TestMethod]
        public async Task Export_returns_only_holons_created_by_the_avatar()
        {
            var id = (await _provider.SaveAvatarAsync(NewAvatar())).Result.Id;
            await _provider.SaveHolonsAsync(new[] { new Holon { Name = "mine", CreatedByAvatarId = id }, new Holon { Name = "theirs", CreatedByAvatarId = Guid.NewGuid() } });

            var export = await _provider.ExportAllDataForAvatarByUsernameAsync("neo");
            Assert.AreEqual("mine", export.Result.Single().Name);
        }

        [TestMethod]
        public async Task Version_control_keeps_prior_versions_without_polluting_listings()
        {
            _provider.IsVersionControlEnabled = true;
            var holon = new Holon { Name = "v1" };
            await _provider.SaveHolonAsync(holon);
            holon.Name = "v2";
            await _provider.SaveHolonAsync(holon);

            var current = await _provider.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.AreEqual("v2", current.Result.Name);
            Assert.AreEqual(2, current.Result.Version);

            var first = await _provider.LoadHolonAsync(holon.Id, loadChildren: false, version: 1);
            Assert.IsFalse(first.IsError, first.Message);
            Assert.AreEqual("v1", first.Result.Name);
            Assert.AreEqual(1, (await _provider.LoadAllHolonsAsync()).Result.Count());

            Assert.IsFalse((await _provider.DeleteHolonAsync(holon.Id)).IsError);
            Assert.AreEqual(0, _backend.Items.Count);
        }

        [TestMethod]
        public async Task Without_version_control_old_versions_are_not_kept()
        {
            var holon = new Holon { Name = "v1" };
            await _provider.SaveHolonAsync(holon);
            holon.Name = "v2";
            await _provider.SaveHolonAsync(holon);

            Assert.IsTrue((await _provider.LoadHolonAsync(holon.Id, loadChildren: false, version: 1)).IsError);
        }

        [TestMethod]
        public async Task Stored_json_cannot_instantiate_non_OASIS_types()
        {
            var id = Guid.NewGuid();
            _backend.Items[$"oasis/holon/{id:N}"] = "{\"$type\":\"System.IO.FileInfo, System.IO.FileSystem\",\"fileName\":\"x\"}";
            var result = await _provider.LoadHolonAsync(id, loadChildren: false);
            Assert.IsTrue(result.IsError);
        }
    }
}
