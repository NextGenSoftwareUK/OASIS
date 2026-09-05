using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DuckDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.UnitTests
{
    [TestClass]
    public class DuckDBOASISProviderTests
    {
        private DuckDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new DuckDBOASIS("Data Source=oasis.duckdb");

        [TestMethod]
        public void ProviderType_ShouldBeDuckDBOASIS()
            => Assert.AreEqual(ProviderType.DuckDBOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeDuckDBOASIS()
            => Assert.AreEqual("DuckDBOASIS", _provider.ProviderName);

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
