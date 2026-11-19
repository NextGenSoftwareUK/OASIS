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
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
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

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
