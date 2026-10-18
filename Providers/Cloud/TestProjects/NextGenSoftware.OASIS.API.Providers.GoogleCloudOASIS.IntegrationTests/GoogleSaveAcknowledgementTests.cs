using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleSaveAcknowledgementTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    public async Task FailedNativeSaveDoesNotPublishGeneratedIdentity(int kind,bool synchronous)
    {
        var detail=kind==1;var holon=kind==2;
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var provider=new GoogleCloudOASIS(projectId:"oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=field.GetValue(provider);
        var settings=Google.Api.Gax.Grpc.CallSettingsExtensions.WithRetry(
            Google.Api.Gax.Grpc.CallSettings.FromExpiration(Google.Api.Gax.Expiration.FromTimeout(TimeSpan.FromSeconds(2))),
            Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));
        var client=new Google.Cloud.Firestore.V1.FirestoreClientBuilder {Endpoint="127.0.0.1:18083",
            ChannelCredentials=Grpc.Core.ChannelCredentials.Insecure,
            Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {CommitSettings=settings}}.Build();
        try
        {
            field.SetValue(provider,FirestoreDb.Create("oasis-google-native",client));
            var profile=new Avatar {Username="owned-failed-profile",IsActive=true};
            var entity=new Holon {Name="owned-failed-holon",IsActive=true};
            var value=new AvatarDetail {Username="owned-failed-detail",IsActive=true,XP=321};
            value.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="preserved"});
            Exception cause;bool failed;
            if(holon)
            {
                var result=synchronous ? provider.SaveHolon(entity,saveChildren:false):await provider.SaveHolonAsync(entity,saveChildren:false);
                cause=result.Exception;failed=result.IsError;
            }
            else if(detail)
            {
                var result=synchronous ? provider.SaveAvatarDetail(value):await provider.SaveAvatarDetailAsync(value);
                cause=result.Exception;failed=result.IsError;
            }
            else
            {
                var result=synchronous ? provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile);
                cause=result.Exception;failed=result.IsError;
            }
            Assert.IsTrue(failed);Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
            Assert.IsInstanceOfType(cause,typeof(GoogleFirestoreMutationReconciliationException));
            var path=((GoogleFirestoreMutationReconciliationException)cause).DocumentPath;
            Assert.IsTrue(path.Contains(holon ? "/documents/holons/":detail ? "/documents/avatarDetails/":"/documents/avatars/",StringComparison.Ordinal),path);
            Assert.IsTrue(Guid.TryParse(path.Split('/').Last(),out var attempted));Assert.AreNotEqual(Guid.Empty,attempted);
            Assert.AreEqual(Guid.Empty,profile.Id,"Unacknowledged identity must remain reconciliation evidence, not caller state.");
            Assert.AreEqual(Guid.Empty,value.Id);
            Assert.AreEqual(Guid.Empty,entity.Id);
            Assert.IsFalse(entity.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.GoogleCloudOASIS));
            Assert.IsFalse(profile.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.GoogleCloudOASIS));
            Assert.AreEqual(321L,value.XP);Assert.AreEqual("preserved",value.Inventory.Single().Name);
            // A separate explicit healthy operation proves publication follows acknowledgement.
            field.SetValue(provider,healthy);
            if(holon)
            {
                var saved=synchronous ? provider.SaveHolon(entity,saveChildren:false):await provider.SaveHolonAsync(entity,saveChildren:false);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            }
            else if(detail)
            {
                var saved=synchronous ? provider.SaveAvatarDetail(value):await provider.SaveAvatarDetailAsync(value);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            }
            else
            {
                var saved=synchronous ? provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            }
            var acknowledged=holon ? entity.Id:detail ? value.Id:profile.Id;Assert.AreNotEqual(Guid.Empty,acknowledged);
            var document=((FirestoreDb)healthy!).Collection(holon ? "holons":detail ? "avatarDetails":"avatars").Document(acknowledged.ToString());
            try
            {
                var raw=await document.GetSnapshotAsync();Assert.IsTrue(raw.Exists);
                Assert.AreEqual(acknowledged.ToString(),raw.GetValue<string>("id"));
                if(holon) Assert.AreEqual(acknowledged.ToString(),entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]);
                else if(!detail) Assert.AreEqual(acknowledged.ToString(),profile.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]);
            }
            finally {await document.DeleteAsync();}
        }
        finally {field.SetValue(provider,healthy);await provider.DeActivateProviderAsync();}
    }
}
