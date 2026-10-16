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
        [DataTestMethod]
        [DataRow(false,1)] [DataRow(true,1)]
        [DataRow(false,-1)] [DataRow(true,-1)]
        public async Task ExplicitAvatarVersionsNeverSilentlyReadLatest(bool synchronous,int version)
        {
            var provider=new FailedActivationProvider();var id=System.Guid.NewGuid();
            void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,version<0 ? typeof(System.ArgumentOutOfRangeException):typeof(System.NotSupportedException));
            }
            Check(synchronous ? provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));
            Check(synchronous ? provider.LoadAvatarByProviderKey(id.ToString(),version):await provider.LoadAvatarByProviderKeyAsync(id.ToString(),version));
            Check(synchronous ? provider.LoadAvatarByUsername("owned",version):await provider.LoadAvatarByUsernameAsync("owned",version));
            Check(synchronous ? provider.LoadAvatarByEmail("owned@example.invalid",version):await provider.LoadAvatarByEmailAsync("owned@example.invalid",version));
            Check(synchronous ? provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version));
            Check(synchronous ? provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));
            Check(synchronous ? provider.LoadAvatarDetailByUsername("owned",version):await provider.LoadAvatarDetailByUsernameAsync("owned",version));
            Check(synchronous ? provider.LoadAvatarDetailByEmail("owned@example.invalid",version):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid",version));
            Check(synchronous ? provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version));
            Assert.AreEqual(0,provider.Activations);
        }

        [DataTestMethod]
        [DataRow("DecodeAvatar")]
        [DataRow("DecodeAvatarDetail")]
        [DataRow("DecodeHolon")]
        public void MalformedStoredJsonIsAnIntegrityFailureWithOriginalCause(string decoder)
        {
            var id=System.Guid.NewGuid().ToString();
            var fields=new System.Collections.Generic.Dictionary<string,object> {{"id",id},{"data_json","{ invalid json"}};
            var method=typeof(GoogleCloudOASIS).GetMethod(decoder,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method);
            var failure=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>method.Invoke(null,new object[] {fields,id}));
            Assert.IsInstanceOfType(failure.InnerException,typeof(System.IO.InvalidDataException));
            Assert.IsInstanceOfType(failure.InnerException.InnerException,typeof(Newtonsoft.Json.JsonException));
        }

        private GoogleCloudOASIS _provider = null!;

        [TestMethod]
        public void NearbyValidatesMicrodegreesBeforeActivationAndPreservesCause()
        {
            var provider=new FailedActivationProvider();
            var network=(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISNETProvider)provider;
            Assert.IsInstanceOfType(network.GetAvatarsNearMe(90000001,0,1).Exception,typeof(System.ArgumentOutOfRangeException));
            Assert.IsInstanceOfType(network.GetHolonsNearMe(0,0,-1,HolonType.All).Exception,typeof(System.ArgumentOutOfRangeException));
            Assert.AreEqual(0,provider.Activations);
            var failed=network.GetAvatarsNearMe(-33000000,151000000,100);
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task TombstoneCapabilityRejectsNullBeforeActivationAndPreservesFailure()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.ApplyAvatarTombstoneAsync(null!)).Exception,typeof(System.ArgumentNullException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.ApplyAvatarTombstoneAsync(new AvatarTombstone(System.Guid.NewGuid(),System.DateTime.UtcNow));
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task ExplicitMissingCredentialsFailLocallyAndLeaveProviderInactive(bool bigQuery)
        {
            var provider=new GoogleCloudOASIS(credentialsPath:System.IO.Path.Combine(System.IO.Path.GetTempPath(),"oasis-nonexistent-google-credentials","credentials.json"),
                enableStorage:!bigQuery,enableFirestore:false,enableBigQuery:bigQuery);
            var result=await provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task HolonDeleteRejectsInvalidIdentityBeforeActivationAndPreservesCause()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.DeleteHolonAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.DeleteHolonAsync("bad-key")).Exception,typeof(System.ArgumentException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.DeleteHolonAsync(System.Guid.NewGuid().ToString());
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task GraphPreflightRejectsCyclesWithoutPublishingIdsOrActivating()
        {
            var provider=new FailedActivationProvider();
            var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            root.Children=new System.Collections.Generic.List<IHolon> {root};
            var failed=await provider.SaveHolonAsync(root);
            Assert.IsTrue(failed.IsError);Assert.IsInstanceOfType(failed.Exception,typeof(System.ArgumentException));
            Assert.AreEqual(System.Guid.Empty,root.Id);Assert.AreEqual(0,provider.Activations);
            var child=new NextGenSoftware.OASIS.API.Core.Holons.Holon {ParentHolonId=System.Guid.NewGuid()};
            root.Children=new System.Collections.Generic.List<IHolon> {child};
            Assert.IsTrue((await provider.SaveHolonAsync(root)).IsError);
            Assert.AreEqual(System.Guid.Empty,root.Id);Assert.AreEqual(System.Guid.Empty,child.Id);
            Assert.AreEqual(0,provider.Activations);
        }

        [TestMethod]
        public async Task BatchPreflightAndUncertaintyPreserveAcknowledgedIds()
        {
            var provider=new BatchProbeProvider();
            var one=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=System.Guid.NewGuid()};
            Assert.IsTrue((await provider.SaveHolonsAsync(new[] {one,one})).IsError);
            Assert.AreEqual(0,provider.Writes);
            var two=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=System.Guid.NewGuid()};
            var three=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=System.Guid.NewGuid()};
            var failed=await provider.SaveHolonsAsync(new[] {one,two,three},continueOnError:true);
            Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsSaved);Assert.AreEqual(2,provider.Writes);
            var partial=(GoogleFirestorePartialMutationException)failed.Exception;
            Assert.AreEqual(one.Id,partial.AcknowledgedRecordIds[0]);Assert.AreSame(provider.Cause,partial.InnerException);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(partial));
        }

        private sealed class BatchProbeProvider : GoogleCloudOASIS
        {
            public int Writes;
            public readonly System.Exception Cause=new GoogleFirestoreMutationReconciliationException("owned/path",new System.IO.IOException("owned lost reply"));
            public BatchProbeProvider():base(enableStorage:false,enableFirestore:false,enableBigQuery:false) {}
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            {
                Writes++;
                return Task.FromResult(Writes==1 ? new NextGenSoftware.OASIS.Common.OASISResult<IHolon> {Result=holon,IsSaved=true}
                    : new NextGenSoftware.OASIS.Common.OASISResult<IHolon> {IsError=true,Exception=Cause,Message=Cause.Message});
            }
        }

        [TestMethod]
        public async Task ExportsValidateScopeAndPreserveStorageErrors()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.ExportAllDataForAvatarByIdAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.ExportAllDataForAvatarByEmailAsync(" ")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.ExportAllAsync(version:1)).Exception,typeof(System.NotSupportedException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.ExportAllDataForAvatarByUsernameAsync("owned");
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task DeleteSelectorsValidateAndPreserveLookupErrors()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.DeleteAvatarAsync("bad-key")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.DeleteAvatarByEmailAsync(" ")).Exception,typeof(System.ArgumentException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.DeleteAvatarByUsernameAsync("owned");
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task AvatarDeleteValidatesIdentityAndRetainsActivationFailure()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.DeleteAvatarAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.DeleteAvatarAsync(System.Guid.NewGuid());
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task AvatarReadsAndSaveValidateBeforeActivationAndRetainCause()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadAvatarAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadAvatarByEmailAsync(" ")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadAllAvatarsAsync(version:1)).Exception,typeof(System.NotSupportedException));
            Assert.IsInstanceOfType((await provider.SaveAvatarAsync(null!)).Exception,typeof(System.ArgumentNullException));
            Assert.AreEqual(0,provider.Activations);
            Assert.AreSame(provider.Cause,(await provider.LoadAvatarAsync(System.Guid.NewGuid())).Exception);
            Assert.AreSame(provider.Cause,(await provider.LoadAvatarByUsernameAsync("owned")).Exception);
            Assert.AreSame(provider.Cause,(await provider.LoadAllAvatarsAsync()).Exception);
            var failed=await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar());
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task SearchValidatesBeforeActivationAndPreservesStorageCause()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.SearchAsync(null!)).Exception,typeof(System.ArgumentNullException));
            var search=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,
                SearchGroups=new() {new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup {SearchQuery="owned",SearchHolons=true}}};
            Assert.IsInstanceOfType((await provider.SearchAsync(search,version:1)).Exception,typeof(System.NotSupportedException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.SearchAsync(search);
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task MetadataQueriesValidateBeforeActivationAndPreserveCause()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadHolonsByMetaDataAsync((string)null,"x")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadHolonsByMetaDataAsync(" ","x")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadHolonsByMetaDataAsync("key","x",version:1)).Exception,typeof(System.NotSupportedException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.LoadHolonsByMetaDataAsync("key","x");
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

        [TestMethod]
        public async Task ParentQueriesValidateIdentityDepthVersionAndPreserveActivationCause()
        {
            var provider=new FailedActivationProvider();
            Assert.IsInstanceOfType((await provider.LoadHolonsForParentAsync(System.Guid.Empty)).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadHolonsForParentAsync("bad-key")).Exception,typeof(System.ArgumentException));
            Assert.IsInstanceOfType((await provider.LoadHolonsForParentAsync(System.Guid.NewGuid(),curentChildDepth:-1)).Exception,typeof(System.ArgumentOutOfRangeException));
            Assert.IsInstanceOfType((await provider.LoadHolonsForParentAsync(System.Guid.NewGuid(),version:1)).Exception,typeof(System.NotSupportedException));
            Assert.AreEqual(0,provider.Activations);
            var failed=await provider.LoadHolonsForParentAsync(System.Guid.NewGuid().ToString());
            Assert.AreSame(provider.Cause,failed.Exception);Assert.AreEqual("owned-code",failed.ErrorCode);
        }

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
