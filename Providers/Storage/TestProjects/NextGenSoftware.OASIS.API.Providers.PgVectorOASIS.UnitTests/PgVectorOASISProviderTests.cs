using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PgVectorOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PgVectorOASIS.UnitTests
{
    [TestClass]
    public class PgVectorOASISProviderTests
    {
        private PgVectorOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new PgVectorOASIS("Host=localhost;Port=5432;Database=oasis;Username=postgres;Password=postgres");
        }

        [TestMethod]
        public void ProviderType_ShouldBePgVectorOASIS()
        {
            Assert.AreEqual(ProviderType.PgVectorOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBePgVectorOASIS()
        {
            Assert.AreEqual("PgVectorOASIS", _provider.ProviderName);
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
