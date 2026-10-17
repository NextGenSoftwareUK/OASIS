using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TypesenseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.UnitTests
{
    [TestClass]
    public class TypesenseOASISProviderTests
    {
        private TypesenseOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new TypesenseOASIS("http://localhost:8108", "test-api-key");
        }

        [TestMethod]
        public void ProviderType_ShouldBeTypesenseOASIS()
        {
            Assert.AreEqual(ProviderType.TypesenseOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeTypesenseOASIS()
        {
            Assert.AreEqual("TypesenseOASIS", _provider.ProviderName);
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
