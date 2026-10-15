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

        [TestMethod]
        public async Task HolonCollectionValidatesBeforeActivationAndRetainsCause()
        {
            var provider = new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadAllHolonsAsync(maxChildDepth:-1)).Exception, typeof(System.ArgumentOutOfRangeException));
            Assert.IsInstanceOfType((await provider.LoadAllHolonsAsync(curentChildDepth:-1)).Exception, typeof(System.ArgumentOutOfRangeException));
            Assert.IsInstanceOfType((await provider.LoadAllHolonsAsync(version:1)).Exception, typeof(System.NotSupportedException));
            Assert.AreEqual(0, provider.Activations);
            var failed = await provider.LoadAllHolonsAsync();
            Assert.AreSame(provider.Cause, failed.Exception);
            Assert.AreEqual("owned-code", failed.ErrorCode);
        }

        [TestMethod]
        public async Task HolonStorageKeyUsesGuidValidationAndPreservesActivationCause()
        {
            var provider = new FailedActivationProvider();
            foreach (var key in new[] { "", "not-a-guid", System.Guid.Empty.ToString() })
                Assert.IsInstanceOfType((await provider.LoadHolonAsync(key)).Exception, typeof(System.ArgumentException));
            Assert.AreEqual(0, provider.Activations);
            var failed = await provider.LoadHolonAsync(System.Guid.NewGuid().ToString());
            Assert.AreSame(provider.Cause, failed.Exception);
            Assert.AreEqual("owned-code", failed.ErrorCode);
            Assert.AreEqual(1, provider.Activations);
        }

        [TestMethod]
        public async Task HolonGuidValidationPrecedesActivationAndRetainsOriginalFailure()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadHolonAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadHolonAsync(System.Guid.NewGuid(),version:1)).Exception,typeof(System.NotSupportedException));
            Assert.IsInstanceOfType((await provider.LoadHolonAsync(System.Guid.NewGuid(),maxChildDepth:-1)).Exception,typeof(System.ArgumentOutOfRangeException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.LoadHolonAsync(System.Guid.NewGuid());
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task FirestoreWriteAttemptRetainsCauseWithoutInventingCommitIdentity()
        {
            var cause=new System.IO.IOException("owned lost reply");
            System.Func<Task<int>> operation=()=>Task.FromException<int>(cause);
            var method=typeof(GoogleCloudOASIS).GetMethod("ObserveFirestoreMutationAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(typeof(int));
            var task=(Task<int>)method.Invoke(null,new object[] {operation,"projects/owned/databases/(default)/documents/avatarDetails/owned"})!;
            var error=await Assert.ThrowsExceptionAsync<GoogleFirestoreMutationReconciliationException>(async()=>await task);
            Assert.AreSame(cause,error.InnerException);Assert.AreEqual(string.Empty,error.OriginalTransactionId);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        }

        [TestMethod]
        public async Task DetailGuidAndListRetainOriginalActivationFailure()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadAvatarDetailAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadAllAvatarDetailsAsync(version:1)).Exception,typeof(System.NotSupportedException));
            Assert.AreEqual(0,provider.Activations);
            var one=await provider.LoadAvatarDetailAsync(System.Guid.NewGuid());
            var all=await provider.LoadAllAvatarDetailsAsync();
            Assert.AreSame(provider.Cause,one.Exception);Assert.AreSame(provider.Cause,all.Exception);
            Assert.AreEqual("owned-code",one.ErrorCode);Assert.AreEqual("owned-code",all.ErrorCode);Assert.AreEqual(2,provider.Activations);
        }

        private sealed class FailedActivationProvider : GoogleCloudOASIS
        {
            public int Activations;
            public readonly System.Exception Cause=new System.IO.IOException("owned activation failure");
            public FailedActivationProvider():base(enableStorage:false,enableFirestore:true,enableBigQuery:false) {}
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()
            {
                Activations++;
                return Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<bool> {IsError=true,Exception=Cause,ErrorCode="owned-code",Message="owned activation failure"});
            }
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task DetailSelectorsValidateBeforeActivationAndRetainActivationCause(bool email)
        {
            var provider=new FailedActivationProvider();
            var invalid=email ? await provider.LoadAvatarDetailByEmailAsync(" "):await provider.LoadAvatarDetailByUsernameAsync(" ");
            Assert.IsInstanceOfType(invalid.Exception,typeof(System.ArgumentException));
            var historical=email ? await provider.LoadAvatarDetailByEmailAsync("owned",version:1):await provider.LoadAvatarDetailByUsernameAsync("owned",version:1);
            Assert.IsInstanceOfType(historical.Exception,typeof(System.NotSupportedException));Assert.AreEqual(0,provider.Activations);
            var failed=email ? await provider.LoadAvatarDetailByEmailAsync("owned"):await provider.LoadAvatarDetailByUsernameAsync("owned");
            Assert.IsTrue(failed.IsError);Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
            Assert.AreEqual(1,provider.Activations);
        }

        [TestMethod]
        public void CanonicalDetailDocumentRetainsInventoryProgressAndMetadata()
        {
            var detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=System.Guid.NewGuid(),Username="owned-user",Email="owned@example.invalid",XP=321,Karma=29,
                MetaData=new System.Collections.Generic.Dictionary<string,object> {{"owned-field","owned-value"}}};
            detail.Inventory.Add(new InventoryItem {Id=System.Guid.NewGuid(),Name="owned seed"});
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var fields=(System.Collections.Generic.Dictionary<string,object>)typeof(GoogleCloudOASIS).GetMethod("EncodeAvatarDetail",flags)!.Invoke(null,new object[] {detail})!;
            var decode=typeof(GoogleCloudOASIS).GetMethod("DecodeAvatarDetail",flags)!;
            var loaded=(NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail)decode.Invoke(null,new object[] {fields,detail.Id.ToString()})!;
            Assert.AreEqual(detail.Username,fields["username"]);Assert.AreEqual(detail.Email,fields["email"]);
            Assert.AreEqual(321,loaded.XP);Assert.AreEqual(29L,loaded.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,loaded.Inventory[0].Id);Assert.AreEqual("owned seed",loaded.Inventory[0].Name);
            Assert.AreEqual("owned-value",loaded.MetaData["owned-field"]);Assert.IsNotNull(loaded.Stats);
            var wrong=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>decode.Invoke(null,new object[] {fields,System.Guid.NewGuid().ToString()}));
            Assert.IsInstanceOfType(wrong.InnerException,typeof(System.IO.InvalidDataException));
            fields.Remove("data_json");
            var missing=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>decode.Invoke(null,new object[] {fields,detail.Id.ToString()}));
            Assert.IsInstanceOfType(missing.InnerException,typeof(System.IO.InvalidDataException));
        }

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
