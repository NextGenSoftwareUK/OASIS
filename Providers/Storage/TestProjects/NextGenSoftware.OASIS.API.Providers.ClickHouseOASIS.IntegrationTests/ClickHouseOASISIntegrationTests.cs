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
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONN") ?? "Host=localhost;Port=8123;Database=oasis;Username=default;Password=";

        [TestInitialize]
        public void Setup()
        {
            _provider = new ClickHouseOASIS(ConnectionString);
            _provider.ActivateProvider();
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
