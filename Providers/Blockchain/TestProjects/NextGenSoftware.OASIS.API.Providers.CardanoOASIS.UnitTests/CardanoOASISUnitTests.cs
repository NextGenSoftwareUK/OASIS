using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS.UnitTests
{
    /// <summary>Offline provider metadata checks. Activation needs a Blockfrost project and is covered by the integration tests.</summary>
    [TestClass]
    public class CardanoOASISProviderTests
    {
        private CardanoOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new CardanoOASIS();

        [TestMethod]
        public void ProviderType_ShouldBeCardanoOASIS() => Assert.AreEqual(ProviderType.CardanoOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially() => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldNotBeEmpty() => Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderName));

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty() => Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
    }
}
