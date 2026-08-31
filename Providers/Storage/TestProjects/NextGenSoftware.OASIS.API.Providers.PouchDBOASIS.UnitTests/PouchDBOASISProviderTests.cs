using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PouchDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.UnitTests
{
    [TestClass]
    public class PouchDBOASISProviderTests
    {
        private PouchDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new PouchDBOASIS("http://localhost:5984");
        }

        [TestMethod]
        public void ProviderType_ShouldBePouchDBOASIS()
        {
            Assert.AreEqual(ProviderType.PouchDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBePouchDBOASIS()
        {
            Assert.AreEqual("PouchDBOASIS", _provider.ProviderName);
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
