using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests
{
    [TestClass]
    public class DynamoDBOASISProviderTests
    {
        [TestMethod]
        public void StorageSerializerRetainsOwnedInventoryAndProgress()
        {
            var detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=Guid.NewGuid(),XP=321,Karma=29};
            var item=new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="owned seed"};
            detail.Inventory.Add(item);
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var json=(string)typeof(DynamoDBOASIS).GetMethod("Ser",flags)!.Invoke(null,new object[] {detail})!;
            var loaded=(NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail)typeof(DynamoDBOASIS).GetMethod("Des",flags)!
                .MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail)).Invoke(null,new object[] {json})!;
            Assert.AreEqual(item.Id,loaded.Inventory.Single().Id);
            Assert.AreEqual(item.Name,loaded.Inventory.Single().Name);
            Assert.AreEqual(321,loaded.XP); Assert.AreEqual(29L,loaded.Karma);
            Assert.IsNotNull(loaded.Stats); Assert.IsNotNull(loaded.Skills);
        }

        private DynamoDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new DynamoDBOASIS("test-key", "test-secret", "us-east-1");
        }

        [TestMethod]
        public void ProviderType_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual(ProviderType.DynamoDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual("DynamoDBOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.ProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }

        [TestMethod]
        public void ProviderImplementsStorageInterface()
        {
            Assert.IsInstanceOfType(_provider, typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
