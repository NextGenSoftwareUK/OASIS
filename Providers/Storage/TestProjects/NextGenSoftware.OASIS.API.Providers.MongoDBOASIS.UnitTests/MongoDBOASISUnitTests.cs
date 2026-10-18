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

        [TestMethod]
        public void AvatarSkillsAcceptIndependentInterfaceAndDetachBothDirections()
        {
            var skills=new IndependentAvatarSkills();var value=1;
            foreach(var property in typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarSkills).GetProperties())property.SetValue(skills,value++);
            var source=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=Guid.NewGuid(),Skills=skills};
            var stored=DataHelper.ConvertOASISAvatarDetailToMongoEntity(source);
            foreach(var property in typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarSkills).GetProperties())Assert.AreEqual(property.GetValue(skills),property.GetValue(stored.Skills));
            var computers=stored.Skills.Computers;skills.Computers=999;Assert.AreEqual(computers,stored.Skills.Computers);
            var loaded=DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<MongoAvatarDetail>(stored));
            Assert.IsFalse(loaded.IsError);Assert.IsNotNull(loaded.Result);
            Assert.AreNotSame(stored.Skills,loaded.Result.Skills);
            stored.Skills.Computers=888;Assert.AreEqual(computers,loaded.Result.Skills.Computers);
        }

        [TestMethod]
        public void AvatarDetailConversionPreservesDescriptionAndModel()
        {
            var stored=new MongoAvatarDetail{HolonId=Guid.NewGuid(),Description="stored-description",Model3D="stored-model"};
            var converted=DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<MongoAvatarDetail>(stored));
            Assert.IsFalse(converted.IsError);Assert.IsNotNull(converted.Result);
            Assert.AreEqual(stored.Description,converted.Result.Description);Assert.AreEqual(stored.Model3D,converted.Result.Model3D);
            var roundtrip=DataHelper.ConvertOASISAvatarDetailToMongoEntity(converted.Result);
            Assert.AreEqual(stored.Description,roundtrip.Description);Assert.AreEqual(stored.Model3D,roundtrip.Model3D);
        }

        [TestMethod]
        public void NativeCodeGenesis_WithoutGenerator_DoesNotAcknowledgeSuccessOrActivateStorage()
        {
            NextGenSoftware.OASIS.API.Core.Interfaces.IOASISSuperStar generator = _provider;
            Assert.IsFalse(generator.NativeCodeGenesis(null!, null!, null!));
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async System.Threading.Tasks.Task InvalidBulkGraph_DoesNotPublishEarlierIdentitiesOrParents(bool synchronous)
        {
            var child = new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            var first = new NextGenSoftware.OASIS.API.Core.Holons.Holon
            {
                Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { child }
            };
            var conflicting = new NextGenSoftware.OASIS.API.Core.Holons.Holon
            {
                Children = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>
                {
                    new NextGenSoftware.OASIS.API.Core.Holons.Holon { ParentHolonId = Guid.NewGuid() }
                }
            };
            var roots = new[] { first, conflicting };
            var result = synchronous
                ? _provider.SaveHolons(roots, saveChildrenOnProvider: true)
                : await _provider.SaveHolonsAsync(roots, saveChildrenOnProvider: true);
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", result.ErrorCode);
            Assert.AreEqual(Guid.Empty, first.Id);
            Assert.AreEqual(Guid.Empty, child.Id);
            Assert.AreEqual(Guid.Empty, child.ParentHolonId);
            Assert.AreEqual(Guid.Empty, conflicting.Id);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

    }
}
