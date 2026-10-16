using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleHolonDeleteTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)]
    [DataRow(true,false)] [DataRow(true,true)]
    public async Task HardDeleteAcknowledgesOriginalRecordAndNativeAbsence(bool synchronous,bool storageKey)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var document=db.Collection("holons").Document(id.ToString());
        var acknowledged=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var saved=await provider.SaveHolonAsync(new Holon {Id=id,Name="owned-delete-original",ParentHolonId=parent,
                IsActive=true,MetaData=new Dictionary<string,object> {{"owned","original-value"}}},saveChildren:false);
            Assert.IsFalse(saved.IsError,saved.Message);acknowledged=true;
            Assert.IsTrue((await document.GetSnapshotAsync()).Exists);
            var deleted=storageKey ? synchronous ? provider.DeleteHolon(id.ToString()):await provider.DeleteHolonAsync(id.ToString())
                :synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);
            Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning);
            Assert.IsTrue(deleted.IsSaved);Assert.IsTrue(deleted.IsDeleted);Assert.AreEqual(1,deleted.DeletedCount);
            Assert.AreEqual(id,deleted.Result.Id);Assert.AreEqual(parent,deleted.Result.ParentHolonId);
            Assert.AreEqual("owned-delete-original",deleted.Result.Name);Assert.AreEqual("original-value",deleted.Result.MetaData["owned"]);
            Assert.IsFalse((await document.GetSnapshotAsync()).Exists);
            var missing=storageKey ? synchronous ? provider.DeleteHolon(id.ToString()):await provider.DeleteHolonAsync(id.ToString())
                :synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);
            Assert.IsTrue(missing.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
            Assert.IsNull(missing.Result);Assert.IsFalse(missing.IsDeleted);Assert.AreEqual(0,missing.DeletedCount);
        }
        finally {if(acknowledged) await document.DeleteAsync();await provider.DeActivateProviderAsync();}
    }
}
