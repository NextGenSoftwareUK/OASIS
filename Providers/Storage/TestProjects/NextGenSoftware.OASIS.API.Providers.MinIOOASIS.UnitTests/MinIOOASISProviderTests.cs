using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MinIOOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.UnitTests
{
    [TestClass]
    public class MinIOOASISProviderTests
    {
        private MinIOOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new MinIOOASIS("http://localhost:9000", "minioadmin", "minioadmin");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMinIOOASIS()
        {
            Assert.AreEqual(ProviderType.MinIOOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMinIOOASIS()
        {
            Assert.AreEqual("MinIOOASIS", _provider.ProviderName);
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
