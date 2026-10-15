using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests
{
    [TestClass]
    public class GoogleCloudOASISProviderTests
    {
        private GoogleCloudOASIS _provider = null!;

        [DataTestMethod]
        [DataRow(System.DateTimeKind.Local)]
        [DataRow(System.DateTimeKind.Utc)]
        [DataRow(System.DateTimeKind.Unspecified)]
        public void FirestoreDateConversionUsesUtcForExistingDetailInputs(System.DateTimeKind kind)
        {
            var input=new System.DateTime(2026,10,15,1,2,3,kind);
            var method=typeof(GoogleCloudOASIS).GetMethod("ToFirestoreTimestamp",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var timestamp=(Google.Cloud.Firestore.Timestamp)method.Invoke(null,new object[] {input})!;
            var expected=kind==System.DateTimeKind.Local ? input.ToUniversalTime():System.DateTime.SpecifyKind(input,System.DateTimeKind.Utc);
            Assert.AreEqual(expected,timestamp.ToDateTime());
            Assert.AreEqual(System.DateTimeKind.Utc,timestamp.ToDateTime().Kind);
            var empty=(Google.Cloud.Firestore.Timestamp)method.Invoke(null,new object[] {default(System.DateTime)})!;
            Assert.AreEqual(System.DateTime.UnixEpoch,empty.ToDateTime());
        }

        [TestInitialize]
        public void Setup()
        {
            _provider = new GoogleCloudOASIS(
                enableStorage: false,
                enableFirestore: false,
                enableBigQuery: false);
        }

        [TestMethod]
        public void ProviderType_ShouldBeGoogleCloudOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.GoogleCloudOASIS, providerType.Value);
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
        public void ProviderName_ShouldBeGoogleCloudOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("GoogleCloudOASIS", providerName);
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
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            // Arrange
            _provider.ActivateProvider();
            Assert.IsTrue(_provider.IsProviderActivated);

            // Act
            var result = _provider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            // Arrange & Act
            var version = _provider.GetProviderVersion();

            // Assert
            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
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
