using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.Common;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using MongoAvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests
{
    [TestClass]
    public class MongoDBOASISProviderTests
    {
        private MongoProvider _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            // Construction is deliberately backend-free. Activation belongs to the
            // disposable integration profile because it opens a real MongoDB connection.
            _provider = new MongoProvider("mongodb://127.0.0.1:27017", "oasis-unit-tests");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMongoDBOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.MongoDBOASIS, providerType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Arrange & Act
            var isActivated = _provider.IsProviderActivated;

            // Assert
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMongoDBOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("MongoDBOASIS", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            // Arrange & Act
            var description = _provider.ProviderDescription;

            // Assert
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestMethod]
        public void ConvertAvatarDetail_WithKarmaHistory_InitializesAndCopiesHistory()
        {
            var stored = new MongoAvatarDetail
            {
                HolonId = Guid.NewGuid(),
                Karma = 333,
                KarmaAkashicRecords = new List<KarmaAkashicRecord>
                {
                    new KarmaAkashicRecord { Karma = 7, TotalKarma = 333 }
                }
            };

            var converted = DataHelper.ConvertMongoEntityToOASISAvatarDetail(
                new OASISResult<MongoAvatarDetail>(stored));

            Assert.IsFalse(converted.IsError);
            Assert.IsNotNull(converted.Result);
            Assert.IsNotNull(converted.Result.KarmaAkashicRecords);
            Assert.AreEqual(1, converted.Result.KarmaAkashicRecords.Count);
            Assert.AreEqual(333, converted.Result.Karma);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
