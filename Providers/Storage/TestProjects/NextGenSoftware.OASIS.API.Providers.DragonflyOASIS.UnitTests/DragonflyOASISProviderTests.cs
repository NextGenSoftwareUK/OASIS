using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DragonflyOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.UnitTests
{
    [TestClass]
    public class DragonflyOASISProviderTests
    {
        private DragonflyOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new DragonflyOASIS("localhost:6379");

        [TestMethod]
        public void ProviderType_ShouldBeDragonflyOASIS()
            => Assert.AreEqual(ProviderType.DragonflyOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeDragonflyOASIS()
            => Assert.AreEqual("DragonflyOASIS", _provider.ProviderName);

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
