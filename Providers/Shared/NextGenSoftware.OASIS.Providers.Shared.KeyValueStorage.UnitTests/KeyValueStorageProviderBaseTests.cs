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
        public string FailPutKey;

        public Task<string> GetAsync(string key, CancellationToken ct = default) => ThrowReads
            ? throw new InvalidOperationException("store read unavailable") : Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);
        public Task PutAsync(string key, string value, CancellationToken ct = default) { if (key == FailPutKey) throw new InvalidOperationException("controlled write failure"); Items[key] = value; return Task.CompletedTask; }
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
        [TestMethod]
        public async Task Failed_reparent_record_write_preserves_previous_parent_index()
        {
            var originalParent = Guid.NewGuid();
            var holon = new Holon { Id = Guid.NewGuid(), ParentHolonId = originalParent, Name = "before" };
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            var snapshot = _backend.Items.OrderBy(value => value.Key).ToArray();
            _backend.FailPutKey = $"oasis/holon/{holon.Id:N}";
            var replacement = new Holon { Id = holon.Id, ParentHolonId = Guid.NewGuid(), Name = "after" };
            var failed = await _provider.SaveHolonAsync(replacement);
            Assert.IsTrue(failed.IsError); StringAssert.Contains(failed.Message, "controlled write failure");
            CollectionAssert.AreEqual(snapshot, _backend.Items.OrderBy(value => value.Key).ToArray());
            var retained = await _provider.LoadHolonsForParentAsync(originalParent);
            Assert.IsFalse(retained.IsError); Assert.AreEqual("before", retained.Result.Single().Name);
        }

        [TestMethod]
        public async Task Metadata_queries_expand_only_matching_trees_not_unrelated_cycles()
        {
            var cycle = new Holon { Id = Guid.NewGuid(), Name = "unrelated-cycle" }; cycle.ParentHolonId = cycle.Id;
            var selected = new Holon { Id = Guid.NewGuid(), Name = "selected", MetaData = new Dictionary<string, object> { ["selected"] = "yes" } };
            foreach (var holon in new[] { cycle, selected }) Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            var single = await _provider.LoadHolonsByMetaDataAsync("selected", "yes");
            Assert.IsFalse(single.IsError, single.Message); Assert.AreEqual(selected.Id, single.Result.Single().Id);
            var pairs = _provider.LoadHolonsByMetaData(new Dictionary<string, string> { ["selected"] = "yes" }, MetaKeyValuePairMatchMode.All);
            Assert.IsFalse(pairs.IsError, pairs.Message); Assert.AreEqual(selected.Id, pairs.Result.Single().Id);
            var all = await _provider.LoadAllHolonsAsync(); Assert.IsTrue(all.IsError);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Recursive_save_rejects_object_and_identity_cycles_before_writes(bool duplicateIdentity)
        {
            var parent = new Holon { Id = Guid.NewGuid(), Name = "cycle-save" };
            var child = duplicateIdentity ? new Holon { Id = parent.Id } : parent;
            parent.Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child };
            var saved = await _provider.SaveHolonAsync(parent);
            Assert.IsTrue(saved.IsError); StringAssert.Contains(saved.Message, "Cyclic holon child graph");
            Assert.AreEqual(0, _backend.Items.Count);
        }

        [TestMethod]
        public async Task Historical_child_membership_uses_requested_version_after_reparenting()
        {
            _provider.IsVersionControlEnabled = true;
            var oldParent = Guid.NewGuid(); var newParent = Guid.NewGuid();
            var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = oldParent, Name = "before" };
            Assert.IsFalse((await _provider.SaveHolonAsync(child)).IsError);
            child.ParentHolonId = newParent; child.Name = "after";
            Assert.IsFalse((await _provider.SaveHolonAsync(child)).IsError);
            var currentOld = await _provider.LoadHolonsForParentAsync(oldParent);
            Assert.IsFalse(currentOld.IsError); Assert.AreEqual(0, currentOld.Result.Count());
            var historicalOld = await _provider.LoadHolonsForParentAsync(oldParent, version: 1);
            Assert.IsFalse(historicalOld.IsError, historicalOld.Message); Assert.AreEqual("before", historicalOld.Result.Single().Name);
            var historicalNew = _provider.LoadHolonsForParent(newParent, version: 1);
            Assert.IsFalse(historicalNew.IsError); Assert.AreEqual(0, historicalNew.Result.Count());
            var currentNew = _provider.LoadHolonsForParent(newParent);
            Assert.IsFalse(currentNew.IsError); Assert.AreEqual("after", currentNew.Result.Single().Name);
        }

        [TestMethod]
        public async Task Collection_and_metadata_reads_honor_child_loading_and_depth()
        {
            var parent = new Holon { Id = Guid.NewGuid(), Name = "parent", MetaData = new Dictionary<string, object> { ["owned"] = "tree" } };
            var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, Name = "child" };
            var grandchild = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id, Name = "grandchild" };
            foreach (var item in new[] { parent, child, grandchild }) Assert.IsFalse((await _provider.SaveHolonAsync(item)).IsError);
            var flat = await _provider.LoadAllHolonsAsync(loadChildren: false);
            Assert.IsFalse(flat.IsError); Assert.AreEqual(0, flat.Result.Single(value => value.Id == parent.Id).Children.Count());
            var recursive = await _provider.LoadAllHolonsAsync();
            Assert.IsFalse(recursive.IsError, recursive.Message);
            Assert.AreEqual(grandchild.Id, recursive.Result.Single(value => value.Id == parent.Id).Children.Single().Children.Single().Id);
            var shallow = _provider.LoadAllHolons(recursive: false);
            Assert.IsFalse(shallow.IsError); Assert.AreEqual(0, shallow.Result.Single(value => value.Id == parent.Id).Children.Single().Children.Count());
            var bounded = await _provider.LoadHolonsByMetaDataAsync("owned", "tree", maxChildDepth: 1);
            Assert.IsFalse(bounded.IsError); Assert.AreEqual(0, bounded.Result.Single().Children.Single().Children.Count());
            var metadata = _provider.LoadHolonsByMetaData("owned", "tree");
            Assert.IsFalse(metadata.IsError); Assert.AreEqual(grandchild.Id, metadata.Result.Single().Children.Single().Children.Single().Id);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Recursive_parent_cycles_fail_explicitly_but_bounded_reads_terminate(bool selfParent)
        {
            var first = new Holon { Id = Guid.NewGuid(), Name = "cycle-first" };
            var second = new Holon { Id = Guid.NewGuid(), Name = "cycle-second", ParentHolonId = first.Id };
            first.ParentHolonId = selfParent ? first.Id : second.Id;
            Assert.IsFalse((await _provider.SaveHolonAsync(first)).IsError);
            if (!selfParent) Assert.IsFalse((await _provider.SaveHolonAsync(second)).IsError);
            var cyclic = await _provider.LoadHolonAsync(first.Id, continueOnError: false);
            Assert.IsTrue(cyclic.IsError); Assert.IsNotNull(cyclic.Exception);
            StringAssert.Contains(cyclic.Message, "Cyclic holon parent relationship");
            var parent = _provider.LoadHolonsForParent(first.Id);
            Assert.IsTrue(parent.IsError);
            StringAssert.Contains(parent.Message, "Cyclic holon parent relationship");
            var bounded = await _provider.LoadHolonAsync(first.Id, maxChildDepth: 1);
            Assert.IsFalse(bounded.IsError, bounded.Message);
            Assert.AreEqual(1, bounded.Result.Children.Count());
        }

        [TestMethod]
        public async Task Object_metadata_still_rejects_nested_non_OASIS_runtime_types()
        {
            var holon = new Holon { MetaData = new Dictionary<string, object>() };
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            var json = Newtonsoft.Json.Linq.JObject.Parse(OasisJson.Serialize<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(holon));
            json["MetaData"]["unsafe"] = new Newtonsoft.Json.Linq.JObject {
                ["$type"] = "System.IO.FileInfo, System.Private.CoreLib", ["OriginalPath"] = "never-open-this-path" };
            _backend.Items[$"oasis/holon/{holon.Id:N}"] = json.ToString();
            var loaded = await _provider.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsTrue(loaded.IsError); Assert.IsNotNull(loaded.Exception);
            Assert.AreNotEqual("OASIS_RECORD_NOT_FOUND", loaded.ErrorCode);
        }

        [TestMethod]
        public async Task Structured_karma_history_metadata_round_trips_without_allowing_arbitrary_types()
        {
            var avatarId = Guid.NewGuid();
            var holon = new Holon { MetaData = new Dictionary<string, object> {
                ["history"] = new List<Dictionary<string, object>> { new() { ["avatarId"] = avatarId, ["amount"] = 25L } } } };
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            var loaded = await _provider.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            var history = (List<Dictionary<string, object>>)loaded.Result.MetaData["history"];
            Assert.AreEqual(avatarId.ToString(), history.Single()["avatarId"]); Assert.AreEqual(25L, history.Single()["amount"]);
        }

        [DataTestMethod]
        [DataRow("id", false)] [DataRow("id", true)]
        [DataRow("username", false)] [DataRow("username", true)]
        [DataRow("email", false)] [DataRow("email", true)]
        public async Task Explicit_detail_deletion_preserves_identity_and_removes_only_owned_detail_history(string selector, bool asynchronous)
        {
            _provider.IsVersionControlEnabled = true;
            var avatar = NewAvatar(); Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Karma = 42, IsActive = true })).IsError);
            NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider storage = _provider;
            async Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> Delete(bool soft) => (selector, asynchronous) switch {
                ("id", true) => await storage.DeleteAvatarDetailAsync(avatar.Id, soft),
                ("id", false) => storage.DeleteAvatarDetail(avatar.Id, soft),
                ("username", true) => await storage.DeleteAvatarDetailByUsernameAsync(avatar.Username, soft),
                ("username", false) => storage.DeleteAvatarDetailByUsername(avatar.Username, soft),
                ("email", true) => await storage.DeleteAvatarDetailByEmailAsync(avatar.Email, soft),
                _ => storage.DeleteAvatarDetailByEmail(avatar.Email, soft) };
            var soft = await Delete(true); Assert.IsFalse(soft.IsError, soft.Message); Assert.IsTrue(soft.Result);
            var retained = (await storage.LoadAvatarDetailAsync(avatar.Id)).Result;
            Assert.IsFalse(retained.IsActive); Assert.AreNotEqual(DateTime.MinValue, retained.DeletedDate); Assert.AreEqual(42, retained.Karma);
            Assert.IsTrue((await storage.LoadAvatarDetailAsync(avatar.Id, 1)).Result.IsActive);
            var hard = await Delete(false); Assert.IsFalse(hard.IsError, hard.Message); Assert.IsTrue(hard.Result);
            Assert.IsNull((await storage.LoadAvatarDetailAsync(avatar.Id)).Result);
            Assert.IsNull((await storage.LoadAvatarDetailAsync(avatar.Id, 1)).Result);
            Assert.IsFalse(_backend.Items.Keys.Any(key => key.Contains("avatardetail/")));
            Assert.AreEqual(avatar.Id, (await storage.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(avatar.Id, (await storage.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            Assert.IsTrue((await Delete(false)).IsError);
        }

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
