using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.DatabricksOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DatabricksOASIS.IntegrationTests
{
    [TestClass]
    public class DatabricksOASISIntegrationTests
    {
        private DatabricksOASIS _provider = null!;
        private static readonly string connStr = Environment.GetEnvironmentVariable("DATABRICKS_HOST") ?? "https://dbc-abc.cloud.databricks.com";

        [TestInitialize]
        public void Setup()
        {
            _provider = new DatabricksOASIS(connStr, Environment.GetEnvironmentVariable("DATABRICKS_TOKEN") ?? "TOKEN", Environment.GetEnvironmentVariable("DATABRICKS_WAREHOUSE_ID") ?? "warehouse-id");
            _provider.ActivateProvider();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "databricks-test-user", Email = "databricks@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnResult()
            => Assert.IsNotNull(await _provider.LoadAvatarAsync(Guid.NewGuid()));

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Databricks Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnResult()
            => Assert.IsNotNull(await _provider.LoadHolonAsync(Guid.NewGuid()));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
