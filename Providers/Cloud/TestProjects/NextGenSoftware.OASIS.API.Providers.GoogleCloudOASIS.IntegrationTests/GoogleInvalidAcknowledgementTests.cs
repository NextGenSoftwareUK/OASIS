using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class GoogleInvalidAcknowledgementTests
{
    private sealed class InvalidTimeClient(Google.Cloud.Firestore.V1.FirestoreClient inner):Google.Cloud.Firestore.V1.FirestoreClient
    {
        public int Commits;
        public string Path="";
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
        {
            Assert.AreEqual(1,request.Writes.Count);Path=request.Writes[0].Update.Name;
            var original=await inner.CommitAsync(request,callSettings);Commits++;
            Assert.AreEqual(1,original.WriteResults.Count);Assert.IsNotNull(original.WriteResults[0].UpdateTime);
            var invalid=new Google.Cloud.Firestore.V1.CommitResponse{CommitTime=new Google.Protobuf.WellKnownTypes.Timestamp()};
            invalid.WriteResults.Add(new Google.Cloud.Firestore.V1.WriteResult{UpdateTime=new Google.Protobuf.WellKnownTypes.Timestamp()});
            return invalid;
        }
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task CommittedWriteWithInvalidTimestampCannotPublishSuccess(int kind,bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var provider=new GoogleCloudOASIS(projectId:"oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=(FirestoreDb)field.GetValue(provider)!;
        var client=new InvalidTimeClient(healthy.Client);
        var avatar=new Avatar{Username="owned-invalid-ack",IsActive=true};
        var detail=new AvatarDetail{Username="owned-invalid-ack",IsActive=true,XP=321};
        var holon=new Holon{Name="owned-invalid-ack",IsActive=true};
        try
        {
            field.SetValue(provider,FirestoreDb.Create("oasis-google-native",client));
            Exception cause;bool failed,saved;
            if(kind==0){var r=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);cause=r.Exception;failed=r.IsError;saved=r.IsSaved;}
            else if(kind==1){var r=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);cause=r.Exception;failed=r.IsError;saved=r.IsSaved;}
            else{var r=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);cause=r.Exception;failed=r.IsError;saved=r.IsSaved;}
            Assert.AreEqual(1,client.Commits);
            var id=Guid.Parse(client.Path.Split('/').Last());var collection=kind==0?"avatars":kind==1?"avatarDetails":"holons";
            var original=healthy.Collection(collection).Document(id.ToString());var read=await original.GetSnapshotAsync();
            Assert.IsTrue(read.Exists);Assert.AreEqual(id.ToString(),read.GetValue<string>("id"));
            Assert.IsTrue(read.UpdateTime.HasValue);Assert.AreNotEqual(default(Timestamp),read.UpdateTime.Value);
            await original.DeleteAsync(); // Only this actual committed and independently read original.
            Assert.IsTrue(failed);Assert.IsFalse(saved);Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
            Assert.IsInstanceOfType<InvalidDataException>(cause.InnerException);
            Assert.AreEqual(Guid.Empty,avatar.Id);Assert.AreEqual(Guid.Empty,detail.Id);Assert.AreEqual(Guid.Empty,holon.Id);
        }
        finally{field.SetValue(provider,healthy);await provider.DeActivateProviderAsync();}
    }
}
