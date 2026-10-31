using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.UnitTests
{
    /// <summary>Offline provider metadata checks. Activation needs a live EOSIO node and is covered by the integration tests.</summary>
    [TestClass]
    public class EOSIOOASISProviderTests
    {
        private EOSIOOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new EOSIOOASIS("http://127.0.0.1:1", "oasis",
            "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", "5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3");

        [TestMethod]
        public void ProviderType_ShouldBeEOSIOOASIS() => Assert.AreEqual(ProviderType.EOSIOOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially() => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeEOSIOOASIS() => Assert.AreEqual("EOSIOOASIS", _provider.ProviderName);

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty() => Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
    }
}
