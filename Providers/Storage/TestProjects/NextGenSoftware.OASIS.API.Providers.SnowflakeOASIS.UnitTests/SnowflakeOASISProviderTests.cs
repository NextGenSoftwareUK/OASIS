using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SnowflakeOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SnowflakeOASIS.UnitTests
{
    [TestClass]
    public class SnowflakeOASISProviderTests
    {
        private SnowflakeOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new SnowflakeOASIS("account=xy12345;user=oasis;password=pw;db=OASIS;schema=PUBLIC");

        [TestMethod]
        public void ProviderType_ShouldBeSnowflakeOASIS()
            => Assert.AreEqual(ProviderType.SnowflakeOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeSnowflakeOASIS()
            => Assert.AreEqual("SnowflakeOASIS", _provider.ProviderName);

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        private const string TeachingReader =
            "account=teaching;db=demo;schema=public;user=reader;warehouse=demo;role=demo_reader;password=synthetic";

        [TestMethod]
        public void QuerySourceStartsInactive()
        {
            var source = new SnowflakeQuerySource(TeachingReader, System.Guid.NewGuid());
            Assert.AreEqual(ProviderType.SnowflakeOASIS, source.QueryProviderType);
            Assert.IsFalse(source.IsProviderActivated);
        }

        [DataTestMethod]
        [DataRow(";scheme=http")]
        [DataRow(";insecuremode=true")]
        [DataRow(";role=")]
        public void QuerySourceRejectsTransportOverridesAndMissingRole(string extra)
        {
            Assert.ThrowsException<System.ArgumentException>(
                () => new SnowflakeQuerySource(TeachingReader + extra, System.Guid.NewGuid()));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
