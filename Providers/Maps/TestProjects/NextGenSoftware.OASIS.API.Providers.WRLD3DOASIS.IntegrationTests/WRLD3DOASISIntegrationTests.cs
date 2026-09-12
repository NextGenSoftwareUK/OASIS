using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS;

namespace NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.IntegrationTests
{
    [TestClass]
    public class WRLD3DOASISIntegrationTests
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
            Assert.AreEqual(MapProviderType.WRLD3D, _provider.MapProviderType);
        }

        [TestMethod]
        public void MapProviderName_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.MapProviderName);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.MapProviderName));
        }

        [TestMethod]
        public void MapProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.MapProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.MapProviderDescription));
        }
    }
}
