using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.RedshiftOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RedshiftOASIS.UnitTests
{
    [TestClass]
    public class RedshiftOASISProviderTests
    {
        private RedshiftOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new RedshiftOASIS("Host=cluster.redshift.amazonaws.com;Port=5439;Database=oasis;Username=oasis;Password=pw");

        [TestMethod]
        public void ProviderType_ShouldBeRedshiftOASIS()
            => Assert.AreEqual(ProviderType.RedshiftOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeRedshiftOASIS()
            => Assert.AreEqual("RedshiftOASIS", _provider.ProviderName);

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
