using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests
{
    [TestClass]
    public class MilvusOASISProviderTests
    {
        private MilvusOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new MilvusOASIS("http://localhost:19530");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMilvusOASIS()
        {
            Assert.AreEqual(ProviderType.MilvusOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMilvusOASIS()
        {
            Assert.AreEqual("MilvusOASIS", _provider.ProviderName);
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
