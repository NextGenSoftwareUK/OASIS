using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.TursoOASIS;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real CRUD surface against a live backend.
    ///
    /// Connection details come from TURSOOASIS_&lt;PARAM&gt; environment variables. When the
    /// backend is not reachable the provider fails to activate and every test is
    /// inconclusive rather than failed - so an unconfigured machine reports "skipped"
    /// instead of red.
    /// </summary>
    [TestClass]
    public class TursoOASISIntegrationTests
    {
        private const string OwnedFixture = "http://127.0.0.1:19693";
        private TursoOASIS _provider = null!;
        private bool _live;

        [TestInitialize]
        public void Setup()
        {
            _provider = TursoOASISTestFactory.Create();
            try { _live = !_provider.ActivateProvider().IsError && _provider.IsProviderActivated; }
            catch { _live = false; }
        }

        private void RequireLive()
        {
            if (!_live)
                Assert.Inconclusive(
                    "No live TursoOASIS backend configured - set the TURSOOASIS_* environment variables to run this.");
        }

        [TestMethod]
        public async Task NativeStatementRejectionCannotAcknowledgeAvatarSave()
        {
            RequireOwnedFixture();
            using var client = new HttpClient();
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "rejected-native-write" };
            await ExecuteFixtureSql(client, "CREATE TRIGGER oasis_test_reject BEFORE INSERT ON oasis_avatars BEGIN SELECT RAISE(ABORT, 'isolated native rejection'); END");
            try
            {
                var result = await _provider.SaveAvatarAsync(avatar);
                Assert.IsTrue(result.IsError);
                Assert.IsFalse(result.IsSaved);
                Assert.IsNull(result.Result);
                var readback = await ExecuteFixtureSql(client, $"SELECT count(*) AS total FROM oasis_avatars WHERE id = '{avatar.Id:D}'");
                Assert.AreEqual("0", readback.GetProperty("rows")[0][0].GetProperty("value").GetString());
            }
            finally
            {
                await ExecuteFixtureSql(client, "DROP TRIGGER oasis_test_reject");
                Assert.IsFalse((await _provider.DeActivateProviderAsync()).IsError);
                Assert.IsFalse(_provider.IsProviderActivated);
            }

        }

        [TestMethod]
        public async Task NativeBootstrapRollsBackOnSchemaError()
        {
            RequireOwnedFixture();
            using var client = new HttpClient();
            await ExecuteFixtureSql(client, "DROP TABLE oasis_avatar_details");
            await ExecuteFixtureSql(client, "DROP TABLE oasis_holons");
            await ExecuteFixtureSql(client, "DROP TABLE oasis_avatars");
            await ExecuteFixtureSql(client, "CREATE TABLE oasis_avatars (id TEXT PRIMARY KEY)");

            var rejected = await _provider.ActivateProviderAsync();
            Assert.IsTrue(rejected.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
            var rollback = await ExecuteFixtureSql(client, "SELECT name FROM sqlite_master WHERE name IN ('oasis_avatar_details', 'oasis_holons')");
            Assert.AreEqual(0, rollback.GetProperty("rows").GetArrayLength());

            await ExecuteFixtureSql(client, "DROP TABLE oasis_avatars");
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            Assert.IsTrue(_provider.IsProviderActivated);
            var schema = await ExecuteFixtureSql(client, "SELECT name FROM sqlite_master WHERE name IN ('oasis_avatars', 'oasis_avatar_details', 'oasis_holons', 'idx_avatars_username', 'idx_avatars_email', 'idx_holons_parent', 'idx_holons_type')");
            Assert.AreEqual(7, schema.GetProperty("rows").GetArrayLength());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CoreStorageSuiteAgainstNativeLibSql(bool synchronous)
        {
            RequireOwnedFixture();
            using var client = new HttpClient();
            var report = await CoreStorageConformance.RunAsync(
                _provider, nameof(TursoOASIS), "native", synchronous, _ => Task.CompletedTask,
                cleanupSoftDeletedFixture: async (id, _) =>
                {
                    var deleted = await ExecuteFixtureSql(client, $"DELETE FROM oasis_holons WHERE id = '{id:D}' AND is_deleted = 1");
                    var count = deleted.GetProperty("affected_row_count").GetInt32();
                    return new OASISResult<bool> { Result = count == 1, IsDeleted = count == 1, DeletedCount = count };
                },
                expectedDefaultDeletion: "soft",
                observeNativeTombstone: async (id, owner, name) =>
                {
                    var readback = await ExecuteFixtureSql(client, $"SELECT data_json FROM oasis_holons WHERE id = '{id:D}' AND is_deleted = 1");
                    var rows = readback.GetProperty("rows");
                    if (rows.GetArrayLength() == 0)
                        return false;
                    Assert.AreEqual(1, rows.GetArrayLength());
                    using var payload = JsonDocument.Parse(rows[0][0].GetProperty("value").GetString()!);
                    Assert.AreEqual(owner, payload.RootElement.GetProperty("createdByAvatarId").GetGuid());
                    Assert.AreEqual(name, payload.RootElement.GetProperty("name").GetString());
                    return true;
                });
            Assert.IsTrue(report.Succeeded,
                $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
            Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
            Assert.IsTrue(report.FixtureCleanupComplete);
        }

        [TestMethod]
        public async Task NativeAvatarDetailDeletionRollsBackWithAvatar()
        {
            RequireOwnedFixture();
            using var client = new HttpClient();
            var id = Guid.NewGuid();
            var marker = $"avatar-detail-delete-{id:N}";
            Assert.IsTrue((await _provider.SaveAvatarAsync(
                new Avatar { Id = id, Username = marker, Email = marker + "@example.invalid" })).IsSaved);
            Assert.IsTrue((await _provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = marker })).IsSaved);

            await ExecuteFixtureSql(client, $"CREATE TRIGGER oasis_test_protect_detail BEFORE DELETE ON oasis_avatar_details WHEN OLD.id = '{id:D}' BEGIN SELECT RAISE(ABORT, 'isolated detail deletion rejection'); END");
            try
            {
                var rejected = await _provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(rejected.IsError);
                Assert.IsFalse(rejected.IsDeleted);
                Assert.IsTrue((await _provider.LoadAvatarAsync(id)).IsLoaded);
                Assert.IsTrue((await _provider.LoadAvatarDetailAsync(id)).IsLoaded);
            }
            finally
            {
                await ExecuteFixtureSql(client, "DROP TRIGGER oasis_test_protect_detail");
            }

            var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsTrue(deleted.IsDeleted, deleted.Message);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await _provider.LoadAvatarDetailAsync(id)).ErrorCode);
        }

        [TestMethod]
        public async Task NativePointReadHonorsChildLoadingAndDepth()
        {
            RequireOwnedFixture();
            var root = new Holon { Id = Guid.NewGuid(), Name = "root" };
            var child = new Holon { Id = Guid.NewGuid(), Name = "child", HolonType = HolonType.Holon };
            var grandchild = new Holon { Id = Guid.NewGuid(), Name = "grandchild", HolonType = HolonType.Quest };
            child.Children.Add(grandchild);
            root.Children.Add(child);
            var saved = await _provider.SaveHolonAsync(root);
            Assert.IsTrue(saved.IsSaved, saved.Message);
            Assert.AreEqual(3, saved.SavedCount);

            var flat = await _provider.LoadHolonAsync(root.Id, loadChildren: false);
            Assert.IsTrue(flat.IsLoaded, flat.Message);
            Assert.AreEqual(0, flat.Result!.Children.Count);
            var direct = _provider.LoadHolon(root.Id, loadChildren: true, recursive: false);
            Assert.IsTrue(direct.IsLoaded, direct.Message);
            Assert.AreEqual(child.Id, direct.Result!.Children.Single().Id);
            Assert.AreEqual(0, direct.Result.Children.Single().Children.Count);
            var bounded = await _provider.LoadHolonAsync(root.Id, loadChildren: true, maxChildDepth: 1);
            Assert.IsTrue(bounded.IsLoaded, bounded.Message);
            Assert.AreEqual(0, bounded.Result!.Children.Single().Children.Count);
            var recursive = await _provider.LoadHolonAsync(root.Id, loadChildren: true);
            Assert.IsTrue(recursive.IsLoaded, recursive.Message);
            Assert.AreEqual(grandchild.Id, recursive.Result!.Children.Single().Children.Single().Id);

            var parentSelection = await _provider.LoadHolonsForParentAsync(root.Id, HolonType.Holon, loadChildren: true);
            Assert.IsTrue(parentSelection.IsLoaded, parentSelection.Message);
            Assert.AreEqual(1, parentSelection.LoadedCount);
            Assert.AreEqual(grandchild.Id, parentSelection.Result!.Single().Children.Single().Id);
            var boundedSelection = _provider.LoadHolonsForParent(root.Id, loadChildren: true, maxChildDepth: 1);
            Assert.IsTrue(boundedSelection.IsLoaded, boundedSelection.Message);
            Assert.AreEqual(0, boundedSelection.Result!.Single().Children.Count);
            var flatSelection = await _provider.LoadHolonsForParentAsync(root.Id, loadChildren: false);
            Assert.IsTrue(flatSelection.IsLoaded, flatSelection.Message);
            Assert.AreEqual(0, flatSelection.Result!.Single().Children.Count);

            Assert.IsTrue((await _provider.DeleteHolonAsync(child.Id)).IsDeleted);
            var afterDelete = await _provider.LoadHolonAsync(root.Id, loadChildren: true);
            Assert.IsTrue(afterDelete.IsLoaded, afterDelete.Message);
            Assert.AreEqual(0, afterDelete.Result!.Children.Count);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task NativeSearchDoesNotTreatSqlFailureAsEmptyResults()
        {
            RequireOwnedFixture();
            using var client = new HttpClient();
            var query = new SearchParams();
            var empty = await _provider.SearchAsync(query, loadChildren: false);
            Assert.IsTrue(empty.IsLoaded, empty.Message);
            Assert.AreEqual(0, empty.LoadedCount);

            await ExecuteFixtureSql(client, "ALTER TABLE oasis_holons RENAME TO oasis_test_unavailable_holons");
            try
            {
                var failed = await _provider.SearchAsync(query, loadChildren: false);
                Assert.IsTrue(failed.IsError);
                Assert.IsFalse(failed.IsLoaded);
                Assert.IsNull(failed.Result);
                Assert.IsNotNull(failed.Exception);
            }
            finally
            {
                await ExecuteFixtureSql(client, "ALTER TABLE oasis_test_unavailable_holons RENAME TO oasis_holons");
            }
        }

        private void RequireOwnedFixture()
        {
            if (Environment.GetEnvironmentVariable("TURSOOASIS_URL") != OwnedFixture ||
                Environment.GetEnvironmentVariable("OASIS_TURSO_OWNED_STORAGE_FIXTURE") != "1")
                Assert.Inconclusive("This test requires the isolated native libSQL runner.");
            RequireLive();
        }

        private static async Task<JsonElement> ExecuteFixtureSql(HttpClient client, string sql)
        {
            using var response = await client.PostAsJsonAsync(OwnedFixture + "/v2/pipeline", new
            {
                requests = new object[] { new { type = "execute", stmt = new { sql } }, new { type = "close" } }
            });
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var statement = json.RootElement.GetProperty("results")[0];
            Assert.AreEqual("ok", statement.GetProperty("type").GetString());
            return statement.GetProperty("response").GetProperty("result").Clone();
        }

        [TestMethod]
        public async Task NativeAvatarDeletionRequiresOneAffectedRow()
        {
            RequireOwnedFixture();
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "native-delete-" + Guid.NewGuid().ToString("N") };
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            try
            {
                var soft = await _provider.DeleteAvatarAsync(avatar.Id);
                Assert.IsTrue(soft.IsDeleted, soft.Message);
                Assert.AreEqual(1, soft.DeletedCount);
                var hidden = await _provider.LoadAvatarAsync(avatar.Id);
                Assert.IsNull(hidden.Result);

                var repeatedSoft = await _provider.DeleteAvatarAsync(avatar.Id);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, repeatedSoft.ErrorCode);
                Assert.IsFalse(repeatedSoft.IsDeleted);

                var hard = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
                Assert.IsTrue(hard.IsDeleted, hard.Message);
                Assert.AreEqual(1, hard.DeletedCount);
                var repeatedHard = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, repeatedHard.ErrorCode);
                Assert.IsFalse(repeatedHard.IsDeleted);
            }
            finally
            {
                await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            }
        }

        [TestMethod]
        public async Task SaveAndLoadAvatar_RoundTrips()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result!.Username);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_FindsTheSavedAvatar()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var found = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(found.IsError, found.Message);
            Assert.AreEqual(avatar.Id, found.Result!.Id);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task ConcurrentHolonDeletes_AcknowledgeOnlyTheWinningWrite()
        {
            RequireLive();
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Concurrent Turso deletion" };
            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var attempts = await Task.WhenAll(_provider.DeleteHolonAsync(holon.Id), _provider.DeleteHolonAsync(holon.Id));
            var winner = Array.Find(attempts, attempt => attempt.IsDeleted);
            var other = Array.Find(attempts, attempt => !attempt.IsDeleted);
            Assert.IsNotNull(winner);
            Assert.IsNotNull(other);
            Assert.IsFalse(winner.IsError, winner.Message);
            Assert.AreEqual(1, winner.DeletedCount);
            Assert.AreEqual(holon.Id, winner.Result!.Id);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, other.ErrorCode);
            Assert.IsNull((await _provider.LoadHolonAsync(holon.Id)).Result);
        }

        [TestMethod]
        public async Task SaveAndLoadHolon_RoundTrips()
        {
            RequireLive();

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS IT Holon {Guid.NewGuid():N}" };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result!.Name);

            await _provider.DeleteHolonAsync(holon.Id);
        }

        [TestMethod]
        public async Task DeletedAvatar_IsNotReturnedAsLive()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            await _provider.SaveAvatarAsync(avatar);
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Assert.IsFalse(deleted.IsError, deleted.Message);

            var reloaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsTrue(reloaded.IsError || reloaded.Result == null || reloaded.Result.IsDeleted,
                "A soft-deleted avatar should not come back as a live record.");
        }

        [TestMethod]
        public async Task LoadAllAvatars_ReturnsAResult()
        {
            RequireLive();
            var all = await _provider.LoadAllAvatarsAsync();
            Assert.IsNotNull(all);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
