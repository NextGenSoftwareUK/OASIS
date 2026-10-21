using System.Reflection;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

public partial class GoogleConditionalWriterTests
{
    [DataTestMethod]
    [DataRow(false,Grpc.Core.StatusCode.Aborted)][DataRow(true,Grpc.Core.StatusCode.Aborted)]
    [DataRow(false,Grpc.Core.StatusCode.InvalidArgument)][DataRow(true,Grpc.Core.StatusCode.InvalidArgument)]
    [DataRow(false,Grpc.Core.StatusCode.Unavailable)][DataRow(true,Grpc.Core.StatusCode.Unavailable)]
    [DataRow(false,Grpc.Core.StatusCode.DeadlineExceeded)][DataRow(true,Grpc.Core.StatusCode.DeadlineExceeded)]
    public async Task IdentityCommitNeverReplaysAndUnknownOutcomeRetainsOriginalToken(bool detail,Grpc.Core.StatusCode status)
    {
        var original=new Grpc.Core.RpcException(new Grpc.Core.Status(status,"owned transaction boundary failure"));
        var client=new Client{Rejection=original};var provider=new GoogleCloudOASIS("owned-controlled-history",enableFirestore:true){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        var unknown=status is Grpc.Core.StatusCode.Unavailable or Grpc.Core.StatusCode.DeadlineExceeded;
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
            if(unknown){
                var error=result.Exception as GoogleFirestoreMutationReconciliationException;Assert.IsNotNull(error);Assert.AreSame(original,error.InnerException);
                Assert.AreEqual(Google.Protobuf.ByteString.CopyFromUtf8("owned-controlled-transaction").ToBase64(),error.OriginalTransactionId);
            }else{
                Assert.AreSame(original,result.Exception);
                Assert.AreEqual(status==Grpc.Core.StatusCode.Aborted?StorageLifecycleConflictException.ErrorCode:"HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
            }
        }
        if(detail)Check(await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=Guid.NewGuid()}));
        else Check(await provider.SaveAvatarAsync(new Avatar{Id=Guid.NewGuid()}));
        Assert.AreEqual(1,client.Transactions);Assert.AreEqual(1,client.Writes);Assert.AreEqual(unknown?0:1,client.Rollbacks);
        Assert.AreEqual(Google.Protobuf.ByteString.CopyFromUtf8("owned-controlled-transaction"),client.Submitted!.Transaction);
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(true,false,false)][DataRow(false,true,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(true,false,true)][DataRow(false,true,true)][DataRow(true,true,true)]
    public async Task RelatedIdentityTombstoneRejectsSaveInOneTransaction(bool detail,bool history,bool synchronous)
    {
        var id=Guid.NewGuid();var collection=detail?"avatars":"avatarDetails";
        IHolonBase related=detail?new Avatar{Id=id}:new AvatarDetail{Id=id};
        related.IsActive=false;related.DeletedDate=DateTime.UtcNow;
        var now=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
        var original=OasisJson.Serialize(related);
        var native=new Google.Cloud.Firestore.V1.Document{Name=$"projects/owned-controlled-history/databases/(default)/documents/{collection}/{id}",CreateTime=now,UpdateTime=now};
        native.Fields.Add("id",new Value{StringValue=id.ToString()});native.Fields.Add("data_json",new Value{StringValue=original});
        native.Fields.Add("isActive",new Value{BooleanValue=false});native.Fields.Add("isDeleted",new Value{BooleanValue=true});
        var client=new Client{Stored=native};
        var provider=new GoogleCloudOASIS("owned-controlled-history",enableFirestore:true){IsProviderActivated=true,IsVersionControlEnabled=history};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);}
        if(detail){var active=new AvatarDetail{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatarDetail(active):await provider.SaveAvatarDetailAsync(active));}
        else{var active=new Avatar{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatar(active):await provider.SaveAvatarAsync(active));}
        Assert.AreEqual(1,client.Transactions);Assert.AreEqual(1,client.Rollbacks);Assert.AreEqual(0,client.Writes);
        Assert.AreEqual(original,client.Stored!.Fields["data_json"].StringValue);
    }
}
