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
    public async Task NativeDeleteConflictAndLostAcknowledgementPreserveEvidence(bool synchronous,bool loseAcknowledgement)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var id=Guid.NewGuid();var document=db.Collection("holons").Document(id.ToString());var acknowledged=false;
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=id,Name="owned-original",IsActive=true},saveChildren:false)).IsError);
            acknowledged=true;
            var client=new DeleteBoundaryClient(db.Client,document,loseAcknowledgement,id);
            field.SetValue(provider,FirestoreDb.Create(project,client));
            var failed=synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);
            Assert.IsTrue(failed.IsError);Assert.IsNull(failed.Result);Assert.IsFalse(failed.IsSaved);Assert.IsFalse(failed.IsDeleted);
            Assert.AreEqual(0,failed.DeletedCount);Assert.AreEqual(1,client.Attempts,failed.Exception?.ToString());
            var snapshot=await document.GetSnapshotAsync();
            if(loseAcknowledgement)
            {
                Assert.IsFalse(snapshot.Exists);Assert.AreEqual(1,client.Acknowledgements);
                Assert.IsInstanceOfType(failed.Exception,typeof(GoogleFirestoreMutationReconciliationException));
                Assert.AreSame(client.Cause,failed.Exception.InnerException);
                Assert.AreEqual(document.Path,((GoogleFirestoreMutationReconciliationException)failed.Exception).DocumentPath);
            }
            else
            {
                Assert.IsTrue(snapshot.Exists);Assert.AreEqual(0,client.Acknowledgements);
                Assert.AreEqual("newer-owned-record",NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Holon>(snapshot.GetValue<string>("data_json")).Name);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,failed.ErrorCode);
                Assert.IsInstanceOfType(failed.Exception,typeof(Grpc.Core.RpcException));
                Assert.AreEqual(Grpc.Core.StatusCode.FailedPrecondition,((Grpc.Core.RpcException)failed.Exception).StatusCode);
            }
        }
        finally {field.SetValue(provider,db);if(acknowledged) await document.DeleteAsync();await provider.DeActivateProviderAsync();}
    }

    private sealed class DeleteBoundaryClient:Google.Cloud.Firestore.V1.FirestoreClient
    {
        private readonly Google.Cloud.Firestore.V1.FirestoreClient _inner;
        private readonly DocumentReference _document;private readonly bool _lose;private readonly Guid _id;
        public readonly IOException Cause=new("Owned controlled post-commit acknowledgement loss.");
        public int Attempts;public int Acknowledgements;
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>_inner.Settings;
        public DeleteBoundaryClient(Google.Cloud.Firestore.V1.FirestoreClient inner,DocumentReference document,bool lose,Guid id)
            {_inner=inner;_document=document;_lose=lose;_id=id;}
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
            =>_inner.BatchGetDocuments(request,callSettings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
        {
            Assert.AreEqual(1,request.Writes.Count);Assert.AreEqual(_document.Path,request.Writes[0].Delete);
            Attempts++;
            if(!_lose) await _document.UpdateAsync("data_json",NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(new Holon {Id=_id,Name="newer-owned-record",IsActive=true}));
            var response=await _inner.CommitAsync(request,callSettings);Acknowledgements++;
            if(_lose) throw Cause;
            return response;
        }
    }

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
