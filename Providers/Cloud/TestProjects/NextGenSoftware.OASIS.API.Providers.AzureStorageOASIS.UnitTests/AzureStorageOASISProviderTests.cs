using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class AzureStorageOASISProviderTests
    {
        private AzureStorageOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new AzureStorageOASIS("UseDevelopmentStorage=true");

        [TestMethod]
        public void StoredAvatarRetainsFullProfileAndMetadata()
        {
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=System.Guid.NewGuid(),Username="owned-user",Email="owned@example.invalid",
                FirstName="Actual",LastName="Profile",Description="stored profile",Version=3,
                MetaData=new System.Collections.Generic.Dictionary<string,object> {{"owned-field","owned-value"}}};
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var json=(string)typeof(AzureStorageOASIS).GetMethod("Serialise",flags)!.MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar)).Invoke(null,new object[] {avatar})!;
            var loaded=(NextGenSoftware.OASIS.API.Core.Holons.Avatar)typeof(AzureStorageOASIS).GetMethod("Deserialise",flags)!.MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Holons.Avatar)).Invoke(null,new object[] {json})!;
            Assert.AreEqual(avatar.Id,loaded.Id);Assert.AreEqual(avatar.FirstName,loaded.FirstName);Assert.AreEqual(avatar.LastName,loaded.LastName);
            Assert.AreEqual(avatar.Description,loaded.Description);Assert.AreEqual(3,loaded.Version);
            Assert.AreEqual("owned-value",loaded.MetaData["owned-field"]);
        }

        [TestMethod]
        public void ProviderType_ShouldBeSet()
            => Assert.AreNotEqual(ProviderType.None, _provider.ProviderType.Value);

        [TestMethod]
        public void ProviderName_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderName));

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderCategory_ShouldBeSet()
            => Assert.IsNotNull(_provider.ProviderCategory);

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
