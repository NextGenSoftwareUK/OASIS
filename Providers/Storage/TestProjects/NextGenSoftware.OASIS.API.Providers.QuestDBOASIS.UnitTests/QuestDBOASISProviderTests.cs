using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests
{
    [TestClass]
    public class QuestDBOASISProviderTests
    {
        private QuestDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new QuestDBOASIS("Host=localhost;Port=8812;Username=admin;Password=quest;Database=qdb");
        }

        [TestMethod]
        public void ProviderType_ShouldBeQuestDBOASIS()
        {
            Assert.AreEqual(ProviderType.QuestDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeQuestDBOASIS()
        {
            Assert.AreEqual("QuestDBOASIS", _provider.ProviderName);
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
