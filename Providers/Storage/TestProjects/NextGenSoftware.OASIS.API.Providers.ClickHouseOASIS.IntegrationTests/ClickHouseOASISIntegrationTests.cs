using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.IntegrationTests
{
    [TestClass]
    public class ClickHouseOASISIntegrationTests
    {
        private ClickHouseOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            var connectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONN");
            if (Environment.GetEnvironmentVariable("OASIS_CLICKHOUSE_STORAGE_TEST_WRITES") != "1" ||
                string.IsNullOrWhiteSpace(connectionString))
                Assert.Inconclusive("Configure an isolated writable ClickHouse instance for the storage tests.");

            _provider = new ClickHouseOASIS(connectionString!);
            var activation = _provider.ActivateProvider();
            Assert.IsTrue(activation.Result && !activation.IsError, activation.Message);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "clickhouse-test-user", Email = "clickhouse@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var id = Guid.NewGuid();
            var avatar = new Avatar
            {
                Id = id,
                Username = $"clickhouse-{id:N}",
                Email = $"clickhouse-{id:N}@example.invalid"
            };
            try
            {
                var saved = await _provider.SaveAvatarAsync(avatar);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsNotNull(saved.Result, saved.Message);
                var loaded = await _provider.LoadAvatarAsync(id);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.IsNotNull(loaded.Result, loaded.Message);
                Assert.AreEqual(id, loaded.Result.Id);
                Assert.AreEqual(avatar.Username, loaded.Result.Username);
                Assert.AreEqual(avatar.Email, loaded.Result.Email);
            }
            finally
            {
                var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.Result && !deleted.IsError, deleted.Message);
            }
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "ClickHouse Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            var result = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task HardDeletedHolonIsImmediatelyAbsent()
        {
            var id = Guid.NewGuid();
            try
            {
                var saved = await _provider.SaveHolonAsync(new Holon { Id = id, Name = "ClickHouse hard-delete fixture" });
                Assert.IsTrue(saved.IsSaved && !saved.IsError, saved.Message);
                var before = await _provider.LoadHolonAsync(id);
                Assert.AreEqual(id, before.Result?.Id, before.Message);
            }
            finally
            {
                var deleted = await _provider.DeleteHolonSoftAsync(id, softDelete: false);
                Assert.IsTrue(deleted.Result && !deleted.IsError, deleted.Message);
            }

            var after = await _provider.LoadHolonAsync(id);
            Assert.IsNull(after.Result, after.Message);
            using var connection = new ClickHouse.Client.ADO.ClickHouseConnection(
                Environment.GetEnvironmentVariable("CLICKHOUSE_CONN")!);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT count() FROM oasis.holons WHERE id = '{id:D}'";
            Assert.AreEqual(0UL, Convert.ToUInt64(await command.ExecuteScalarAsync()));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
