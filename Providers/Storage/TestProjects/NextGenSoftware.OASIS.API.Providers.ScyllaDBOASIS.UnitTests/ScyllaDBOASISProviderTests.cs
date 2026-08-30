using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS.UnitTests
{
    [TestClass]
    public class ScyllaDBOASISProviderTests
    {
        private ScyllaDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ScyllaDBOASIS("127.0.0.1");
        }

        [TestMethod]
        public void ProviderType_ShouldBeScyllaDBOASIS()
        {
            Assert.AreEqual(ProviderType.ScyllaDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeScyllaDBOASIS()
        {
            Assert.AreEqual("ScyllaDBOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            var version = _provider.GetProviderVersion();
            Assert.IsFalse(string.IsNullOrWhiteSpace(version));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
