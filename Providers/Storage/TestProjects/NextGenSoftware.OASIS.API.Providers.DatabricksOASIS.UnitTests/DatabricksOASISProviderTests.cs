using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DatabricksOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DatabricksOASIS.UnitTests
{
    [TestClass]
    public class DatabricksOASISProviderTests
    {
        private DatabricksOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new DatabricksOASIS("https://dbc-abc.cloud.databricks.com", "TOKEN", "warehouse-id");

        [TestMethod]
        public void ProviderType_ShouldBeDatabricksOASIS()
            => Assert.AreEqual(ProviderType.DatabricksOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeDatabricksOASIS()
            => Assert.AreEqual("DatabricksOASIS", _provider.ProviderName);

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.GetProviderVersion()));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
