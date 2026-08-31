using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests
{
    [TestClass]
    public class RedisOASISProviderTests
    {
        private RedisOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new RedisOASIS("localhost:6379");
        }

        [TestMethod]
        public void ProviderType_ShouldBeRedisOASIS()
        {
            Assert.AreEqual(ProviderType.RedisOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeRedisOASIS()
        {
            Assert.AreEqual("RedisOASIS", _provider.ProviderName);
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
