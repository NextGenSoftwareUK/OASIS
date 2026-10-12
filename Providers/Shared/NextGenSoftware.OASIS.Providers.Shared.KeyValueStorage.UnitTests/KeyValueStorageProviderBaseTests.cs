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
        public bool ThrowReads;

        public Task<string> GetAsync(string key, CancellationToken ct = default) => ThrowReads
            ? throw new InvalidOperationException("store read unavailable") : Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);
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
        [DataTestMethod]
        [DataRow("verification")]
        [DataRow("reset")]
        [DataRow("refresh")]
        public async Task Token_lookup_uses_real_records_and_rejects_ambiguous_or_missing_identity(string kind)
        {
            var avatar = NewAvatar(); avatar.VerificationToken = "CaseSensitiveToken"; avatar.ResetToken = "CaseSensitiveToken";
            avatar.RefreshTokens = new() { new NextGenSoftware.OASIS.API.Core.Objects.RefreshToken { Token = "CaseSensitiveToken" } };
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>> Lookup(string token) => kind switch {
                "verification" => _provider.LoadAvatarByVerificationTokenAsync(token),
                "reset" => _provider.LoadAvatarByResetTokenAsync(token),
                _ => _provider.LoadAvatarByRefreshTokenAsync(token) };
            var found = await Lookup("CaseSensitiveToken"); Assert.IsFalse(found.IsError); Assert.AreEqual(avatar.Id, found.Result.Id);
            var sync = kind switch { "verification" => _provider.LoadAvatarByVerificationToken("CaseSensitiveToken"),
                "reset" => _provider.LoadAvatarByResetToken("CaseSensitiveToken"), _ => _provider.LoadAvatarByRefreshToken("CaseSensitiveToken") };
            Assert.IsFalse(sync.IsError); Assert.AreEqual(avatar.Id, sync.Result.Id);
            var missing = await Lookup("casesensitivetoken"); Assert.IsTrue(missing.IsError);
            Assert.AreEqual("OASIS_RECORD_NOT_FOUND", missing.ErrorCode);
            Assert.IsTrue((await Lookup(" ")).IsError);
            var duplicate = NewAvatar("other", "other@example.invalid");
            duplicate.VerificationToken = avatar.VerificationToken; duplicate.ResetToken = avatar.ResetToken; duplicate.RefreshTokens = avatar.RefreshTokens;
            Assert.IsFalse((await _provider.SaveAvatarAsync(duplicate)).IsError);
            var ambiguous = await Lookup("CaseSensitiveToken"); Assert.IsTrue(ambiguous.IsError); Assert.IsNull(ambiguous.Result);
            Assert.AreNotEqual("OASIS_RECORD_NOT_FOUND", ambiguous.ErrorCode);
            _backend.ThrowReads = true;
            var failure = await Lookup("CaseSensitiveToken"); Assert.IsTrue(failure.IsError); Assert.IsNotNull(failure.Exception);
            Assert.AreNotEqual("OASIS_RECORD_NOT_FOUND", failure.ErrorCode);
        }

        [TestMethod]
        public async Task Holon_lists_children_metadata_and_exports_read_requested_history()
        {
            _provider.IsVersionControlEnabled = true;
            var owner = Guid.NewGuid(); var parent = Guid.NewGuid();
            var holon = new Holon { Name = "first", ParentHolonId = parent, CreatedByAvatarId = owner,
                MetaData = new Dictionary<string, object> { ["phase"] = "first" } };
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            holon.Name = "second"; holon.MetaData["phase"] = "second";
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            foreach (var result in new[] {
                await _provider.LoadAllHolonsAsync(loadChildren: false, version: 1),
                _provider.LoadAllHolons(loadChildren: false, version: 1),
                await _provider.LoadHolonsForParentAsync(parent, loadChildren: false, version: 1),
                _provider.LoadHolonsForParent(parent, loadChildren: false, version: 1),
                await _provider.LoadHolonsByMetaDataAsync("phase", "first", loadChildren: false, version: 1),
                _provider.LoadHolonsByMetaData("phase", "first", loadChildren: false, version: 1),
                await _provider.LoadHolonsByMetaDataAsync(new() { ["phase"] = "first" }, MetaKeyValuePairMatchMode.All, version: 1),
                await _provider.ExportAllAsync(1), await _provider.ExportAllDataForAvatarByIdAsync(owner, 1) })
            {
                Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual("first", result.Result.Single().Name);
                Assert.AreEqual(1, result.Result.Single().Version);
            }
            Assert.AreEqual(0, (await _provider.LoadAllHolonsAsync(version: 3)).Result.Count());
            Assert.AreEqual(0, (await _provider.LoadHolonsByMetaDataAsync("phase", "second", version: 1)).Result.Count());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Avatar_and_detail_lists_honor_explicit_versions_without_returning_latest_as_history(bool keepHistory)
        {
            _provider.IsVersionControlEnabled = keepHistory;
            var avatar = NewAvatar();
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            var detail = new AvatarDetail { Id = avatar.Id, Karma = 10 };
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            avatar.FirstName = "updated"; detail.Karma = 20;
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            foreach (var latest in new[] { _provider.LoadAllAvatars(), await _provider.LoadAllAvatarsAsync() })
            {
                Assert.IsFalse(latest.IsError); Assert.AreEqual("updated", latest.Result.Single().FirstName);
                Assert.AreEqual(2, latest.Result.Single().Version);
            }
            foreach (var latest in new[] { _provider.LoadAllAvatarDetails(), await _provider.LoadAllAvatarDetailsAsync() })
            {
                Assert.IsFalse(latest.IsError); Assert.AreEqual(20, latest.Result.Single().Karma);
                Assert.AreEqual(2, latest.Result.Single().Version);
            }
            foreach (var history in new[] { _provider.LoadAllAvatars(1), await _provider.LoadAllAvatarsAsync(1) })
            {
                Assert.IsFalse(history.IsError); Assert.AreEqual(keepHistory ? 1 : 0, history.Result.Count());
                if (keepHistory) { Assert.AreEqual("Thomas", history.Result.Single().FirstName); Assert.AreEqual(1, history.Result.Single().Version); }
            }
            foreach (var history in new[] { _provider.LoadAllAvatarDetails(1), await _provider.LoadAllAvatarDetailsAsync(1) })
            {
                Assert.IsFalse(history.IsError); Assert.AreEqual(keepHistory ? 1 : 0, history.Result.Count());
                if (keepHistory) { Assert.AreEqual(10, history.Result.Single().Karma); Assert.AreEqual(1, history.Result.Single().Version); }
            }
            Assert.AreEqual(0, (await _provider.LoadAllAvatarsAsync(3)).Result.Count());
            Assert.AreEqual(0, (await _provider.LoadAllAvatarDetailsAsync(3)).Result.Count());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Rejected_avatar_rename_preserves_all_existing_records_and_indexes(bool conflictingEmail)
        {
            var original = NewAvatar("original", "original@example.invalid");
            var other = NewAvatar("occupied", "occupied@example.invalid");
            Assert.IsFalse((await _provider.SaveAvatarAsync(original)).IsError);
            Assert.IsFalse((await _provider.SaveAvatarAsync(other)).IsError);
            var snapshot = _backend.Items.ToDictionary(item => item.Key, item => item.Value);
            var update = NewAvatar(conflictingEmail ? "new-name" : other.Username,
                conflictingEmail ? other.Email : "new-email@example.invalid");
            update.Id = original.Id;
            Assert.IsTrue((await _provider.SaveAvatarAsync(update)).IsError);
            CollectionAssert.AreEquivalent(snapshot.Keys.ToList(), _backend.Items.Keys.ToList());
            foreach (var entry in snapshot) Assert.AreEqual(entry.Value, _backend.Items[entry.Key]);
            Assert.AreEqual(original.Id, (await _provider.LoadAvatarByUsernameAsync(original.Username)).Result.Id);
            Assert.AreEqual(original.Id, (await _provider.LoadAvatarByEmailAsync(original.Email)).Result.Id);
            Assert.AreEqual(other.Id, (await _provider.LoadAvatarByUsernameAsync(other.Username)).Result.Id);
            Assert.AreEqual(other.Id, (await _provider.LoadAvatarByEmailAsync(other.Email)).Result.Id);
        }

        [TestMethod]
        public async Task Transport_failure_cannot_be_classified_as_authoritative_absence()
        {
            _backend.ThrowReads = true;
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsTrue(result.IsError); Assert.IsNotNull(result.Exception);
            Assert.AreNotEqual("OASIS_RECORD_NOT_FOUND", result.ErrorCode);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Present_corrupt_record_or_index_cannot_be_classified_as_absent(bool index)
        {
            var avatar = NewAvatar(); avatar.Id = Guid.NewGuid();
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            var key = _backend.Items.Keys.Single(value => index ? value.Contains("avatar-username/") : value.EndsWith("avatar/" + avatar.Id.ToString("N")));
            _backend.Items[key] = index ? "invalid-id" : "null";
            var result = index ? await _provider.LoadAvatarByUsernameAsync(avatar.Username) : await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsTrue(result.IsError); Assert.IsNotNull(result.Exception);
            Assert.AreNotEqual("OASIS_RECORD_NOT_FOUND", result.ErrorCode);
        }

        [TestMethod]
        public async Task Missing_detail_index_preserves_authoritative_absence_code()
        {
            var result = await _provider.LoadAvatarDetailByUsernameAsync("missing-user");
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.IsNull(result.Exception);
            Assert.AreEqual("OASIS_RECORD_NOT_FOUND", result.ErrorCode);
        }

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
            Assert.AreEqual("OASIS_RECORD_NOT_FOUND", result.ErrorCode);
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
