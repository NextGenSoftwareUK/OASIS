using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ValKeyOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ValKeyOASIS.UnitTests
{
    [TestClass]
    public class ValKeyOASISProviderTests
    {
        private ValKeyOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new ValKeyOASIS("localhost:6379");

        [TestMethod]
        public void ProviderType_ShouldBeValKeyOASIS()
            => Assert.AreEqual(ProviderType.ValKeyOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeValKeyOASIS()
            => Assert.AreEqual("ValKeyOASIS", _provider.ProviderName);

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
