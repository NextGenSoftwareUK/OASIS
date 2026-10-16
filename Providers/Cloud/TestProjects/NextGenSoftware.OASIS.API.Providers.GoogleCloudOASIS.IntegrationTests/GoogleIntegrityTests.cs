using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleIntegrityTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    public async Task NativeMalformedDocumentsRemainIntegrityFailures(int kind,bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var id=Guid.NewGuid();
        var document=db.Collection(kind==0 ? "avatars":kind==1 ? "avatarDetails":"holons").Document(id.ToString());
        const string json="{ invalid canonical JSON";
        var acknowledged=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await document.CreateAsync(new Dictionary<string,object> {{"id",id.ToString()},{"data_json",json},{"isActive",true},{"isDeleted",false}});
            acknowledged=true;
            Assert.AreEqual(json,(await document.GetSnapshotAsync()).GetValue<string>("data_json"));
            if(kind==0)
            {
                Corrupt(synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id));
                Corrupt(synchronous ? provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
            }
            else if(kind==1)
            {
                Corrupt(synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id));
                Corrupt(synchronous ? provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
            }
            else
            {
                Corrupt(synchronous ? provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false));
                Corrupt(synchronous ? provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
            }
            Assert.AreEqual(json,(await document.GetSnapshotAsync()).GetValue<string>("data_json"));
        }
        finally
        {
            if(acknowledged) await document.DeleteAsync();
            await provider.DeActivateProviderAsync();
        }
    }

    private static void Corrupt<T>(OASISResult<T> result)
    {
        Assert.IsTrue(result.IsError,result.Message);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType(result.Exception,typeof(System.IO.InvalidDataException));
        Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
    }
}
