using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.UnitTests
{
    [TestClass]
    public class WRLD3DOASISProviderTests
    {
        private WRDLD3DOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new WRDLD3DOASIS();
        }

        [TestMethod]
        public void MapProviderType_ShouldBeWRLD3D()
        {
            var providerType = _provider.MapProviderType;
            Assert.AreEqual(MapProviderType.WRLD3D, providerType);
        }

        [TestMethod]
        public void MapProviderName_ShouldNotBeEmpty()
        {
            var name = _provider.MapProviderName;
            Assert.IsNotNull(name);
            Assert.IsFalse(string.IsNullOrEmpty(name));
        }

        [TestMethod]
        public void MapProviderDescription_ShouldNotBeEmpty()
        {
            var description = _provider.MapProviderDescription;
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }
    }
}
