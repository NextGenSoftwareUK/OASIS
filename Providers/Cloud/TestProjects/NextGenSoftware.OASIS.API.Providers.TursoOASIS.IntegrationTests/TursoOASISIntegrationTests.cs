using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.TursoOASIS;

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
