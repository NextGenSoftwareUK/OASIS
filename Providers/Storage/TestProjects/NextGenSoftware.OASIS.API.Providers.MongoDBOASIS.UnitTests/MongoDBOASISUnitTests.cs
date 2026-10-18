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

        [DataTestMethod]
        [DataRow("Stats")][DataRow("SuperPowers")][DataRow("Aura")][DataRow("HumanDesign")]
        public void StructuredProfileAcceptsIndependentInterfaceAndDetaches(string field)
        {
            object independent=field switch{"Stats"=>new IndependentAvatarStats(),"SuperPowers"=>new IndependentAvatarPowers(),"Aura"=>new IndependentAvatarAura(),_=>new IndependentHumanDesign()};
            var slot=typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail).GetProperty(field)!;
            var properties=slot.PropertyType.GetProperties();var number=10;
            foreach(var property in properties){
                object value=property.PropertyType==typeof(int)?number++:property.PropertyType==typeof(string)?"independent-human-type":new AvatarStat{Current=number++,Max=number+100};
                property.SetValue(independent,value);
            }
            var source=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=Guid.NewGuid()};slot.SetValue(source,independent);
            var stored=DataHelper.ConvertOASISAvatarDetailToMongoEntity(source);
            var native=typeof(MongoAvatarDetail).GetProperty(field)!.GetValue(stored)!;
            var loaded=DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<MongoAvatarDetail>(stored));
            Assert.IsFalse(loaded.IsError);Assert.IsNotNull(loaded.Result);var read=slot.GetValue(loaded.Result)!;
            Assert.AreNotSame(independent,native);Assert.AreNotSame(native,read);
            foreach(var property in properties){
                if(property.GetValue(independent) is AvatarStat original){
                    var saved=(AvatarStat)property.GetValue(native)!;var restored=(AvatarStat)property.GetValue(read)!;
                    Assert.AreEqual(original.Current,saved.Current);Assert.AreEqual(original.Max,saved.Max);
                    Assert.AreEqual(original.Current,restored.Current);Assert.AreEqual(original.Max,restored.Max);
                    Assert.AreNotSame(original,saved);Assert.AreNotSame(saved,restored);
                }else{
                    Assert.AreEqual(property.GetValue(independent),property.GetValue(native));
                    Assert.AreEqual(property.GetValue(independent),property.GetValue(read));
                }
            }
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

        [DataTestMethod]
        [DataRow("Spells")][DataRow("GeneKeys")][DataRow("Gifts")][DataRow("Achievements")][DataRow("KarmaAkashicRecords")]
        public void ProfileCollectionsAcceptIndependentItemsAndDetach(string field)
        {
            var entry=IndependentAvatarCollections.Create(field,1);var slot=IndependentAvatarCollections.Slot(field);
            var source=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=Guid.NewGuid()};
            IndependentAvatarCollections.Assign(source,field,entry);
            var stored=DataHelper.ConvertOASISAvatarDetailToMongoEntity(source);
            var nativeList=(System.Collections.IList)typeof(MongoAvatarDetail).GetProperty(field)!.GetValue(stored)!;
            var loaded=DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<MongoAvatarDetail>(stored));
            Assert.IsFalse(loaded.IsError);Assert.IsNotNull(loaded.Result);
            var readList=(System.Collections.IList)slot.GetValue(loaded.Result)!;
            Assert.AreEqual(1,nativeList.Count);Assert.AreEqual(1,readList.Count);
            Assert.AreNotSame(entry,nativeList[0]);Assert.AreNotSame(nativeList[0],readList[0]);
            foreach(var property in slot.PropertyType.GetGenericArguments()[0].GetProperties()){
                Assert.AreEqual(IndependentAvatarCollections.Scalar(property,entry),IndependentAvatarCollections.Scalar(property,nativeList[0]!));
                Assert.AreEqual(IndependentAvatarCollections.Scalar(property,entry),IndependentAvatarCollections.Scalar(property,readList[0]!));
                if(property.PropertyType.IsGenericType){Assert.AreNotSame(property.GetValue(entry),property.GetValue(nativeList[0]));Assert.AreNotSame(property.GetValue(nativeList[0]),property.GetValue(readList[0]));}
            }
        }

        [TestMethod]
        public void ChakraInterfaceAndNestedValuesDetachBothDirections()
        {
            var chakras=IndependentAvatarChakras.Create(1);
            var source=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{Id=Guid.NewGuid(),Chakras=chakras};
            var stored=DataHelper.ConvertOASISAvatarDetailToMongoEntity(source);
            var loaded=DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<MongoAvatarDetail>(stored));
            Assert.IsFalse(loaded.IsError);Assert.IsNotNull(loaded.Result);
            foreach(var slot in typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarChakras).GetProperties()){
                var original=(Chakra)slot.GetValue(chakras)!;
                var native=(Chakra)slot.GetValue(stored.Chakras)!;
                var read=(Chakra)slot.GetValue(loaded.Result.Chakras)!;
                Assert.AreEqual(Newtonsoft.Json.JsonConvert.SerializeObject(original),Newtonsoft.Json.JsonConvert.SerializeObject(native));
                Assert.AreEqual(Newtonsoft.Json.JsonConvert.SerializeObject(original),Newtonsoft.Json.JsonConvert.SerializeObject(read));
                Assert.AreNotSame(original,native);Assert.AreNotSame(native,read);
                Assert.AreNotSame(original.Crystal,native.Crystal);Assert.AreNotSame(native.Crystal,read.Crystal);
                Assert.AreNotSame(original.Element,native.Element);Assert.AreNotSame(native.Element,read.Element);
                Assert.AreNotSame(original.Crystal.Name,native.Crystal.Name);Assert.AreNotSame(native.Crystal.Name,read.Crystal.Name);
                Assert.AreNotSame(original.GiftsUnlocked,native.GiftsUnlocked);Assert.AreNotSame(native.GiftsUnlocked,read.GiftsUnlocked);
                Assert.AreNotSame(original.GiftsUnlocked[0],native.GiftsUnlocked[0]);Assert.AreNotSame(native.GiftsUnlocked[0],read.GiftsUnlocked[0]);
            }
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
