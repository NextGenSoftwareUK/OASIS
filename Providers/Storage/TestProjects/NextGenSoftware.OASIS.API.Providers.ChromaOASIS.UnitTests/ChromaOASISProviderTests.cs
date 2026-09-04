using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ChromaOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests
{
    [TestClass]
    public class ChromaOASISProviderTests
    {
        private ChromaOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ChromaOASIS("http://localhost:8000");
        }

        [TestMethod]
        public void ProviderType_ShouldBeChromaOASIS()
        {
            Assert.AreEqual(ProviderType.ChromaOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeChromaOASIS()
        {
            Assert.AreEqual("ChromaOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.GetProviderVersion()));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
