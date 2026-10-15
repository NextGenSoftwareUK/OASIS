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
        public void StoredHolonRetainsIdentityRelationshipsAndMetadata()
        {
            var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=System.Guid.NewGuid(),Name="owned root",Description="stored description",
                ParentHolonId=System.Guid.NewGuid(),CreatedByAvatarId=System.Guid.NewGuid(),Version=3,
                MetaData=new System.Collections.Generic.Dictionary<string,object> {{"owned-field","owned-value"}}};
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var json=(string)typeof(AzureStorageOASIS).GetMethod("Serialise",flags)!.MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IHolon)).Invoke(null,new object[] {holon})!;
            var loaded=(NextGenSoftware.OASIS.API.Core.Holons.Holon)typeof(AzureStorageOASIS).GetMethod("Deserialise",flags)!.MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Holons.Holon)).Invoke(null,new object[] {json})!;
            Assert.AreEqual(holon.Id,loaded.Id);Assert.AreEqual(holon.Name,loaded.Name);Assert.AreEqual(holon.Description,loaded.Description);
            Assert.AreEqual(holon.ParentHolonId,loaded.ParentHolonId);Assert.AreEqual(holon.CreatedByAvatarId,loaded.CreatedByAvatarId);
            Assert.AreEqual(3,loaded.Version);Assert.AreEqual("owned-value",loaded.MetaData["owned-field"]);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task DetailValidationFailsBeforeNetwork()
        {
            Assert.IsInstanceOfType((await _provider.LoadAvatarDetailAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await _provider.LoadAllAvatarDetailsAsync(version:1)).Exception,typeof(System.NotSupportedException));
            Assert.IsInstanceOfType((await _provider.LoadAvatarDetailByUsernameAsync(" ")).Exception,typeof(System.ArgumentException));
            Assert.ThrowsException<System.ArgumentException>(()=>new AzureStorageOASIS("UseDevelopmentStorage=true",avatarDetailContainer:"oasis-avatars"));
        }

        [TestMethod]
        public void DetailCodecPreservesInventoryAndRejectsWrongBlobIdentity()
        {
            var detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=System.Guid.NewGuid(),XP=321,Karma=29};
            detail.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=System.Guid.NewGuid(),Name="owned seed"});
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var json=(string)typeof(AzureStorageOASIS).GetMethod("Serialise",flags)!.MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail)).Invoke(null,new object[] {detail})!;
            var decoder=typeof(AzureStorageOASIS).GetMethod("DecodeDetail",flags)!;
            var loaded=(NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail)decoder.Invoke(null,new object[] {json,detail.Id.ToString()})!;
            Assert.AreEqual(detail.Inventory[0].Id,loaded.Inventory[0].Id);Assert.AreEqual("owned seed",loaded.Inventory[0].Name);
            Assert.AreEqual(321,loaded.XP);Assert.AreEqual(29L,loaded.Karma);
            var error=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>decoder.Invoke(null,new object[] {json,System.Guid.NewGuid().ToString()}));
            Assert.IsInstanceOfType(error.InnerException,typeof(System.IO.InvalidDataException));
        }

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
