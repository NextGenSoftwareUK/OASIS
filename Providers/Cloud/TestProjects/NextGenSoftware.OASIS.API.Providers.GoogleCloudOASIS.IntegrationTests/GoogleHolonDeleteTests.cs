using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleHolonDeleteTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)]
    [DataRow(false,1)] [DataRow(true,1)]
    [DataRow(false,2)] [DataRow(true,2)]
    public async Task V2HardDeleteReplicatesOnlyAcknowledgedSuccess(bool synchronous,int mode)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var path=Path.GetFullPath("TestResults/GoogleRoutingFiles/tombstone-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained Google routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const string project="oasis-google-native";
        var db=GoogleNativeFirestore.Database(project);
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var verified=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in mode==0 ? types:types.Take(1))
            {
                var id=Guid.NewGuid();var document=db.Collection("holons").Document(id.ToString());var acknowledged=false;
                try
                {
                    Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=id,Name="google-original",IsActive=true},saveChildren:false)).IsError);acknowledged=true;
                    Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=id,Name="sqlite-original",IsActive=true},saveChildren:false)).IsError);
                    var fault=new DeleteBoundaryClient(db.Client,document,mode==2,id);
                    var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                    if(mode!=0) field.SetValue(provider,FirestoreDb.Create(project,fault));
                    var request=new Core.StorageOperationRequest {Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=primary.Value};
                    var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                    field.SetValue(provider,db);
                    var native=await document.GetSnapshotAsync();
                    if(mode==0)
                    {
                        Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);
                        Assert.AreEqual(id,result.Result.Id);
                        Assert.AreEqual(primary.Value==Core.Enums.ProviderType.GoogleCloudOASIS ? "google-original":"sqlite-original",result.Result.Name);
                        Assert.IsFalse(native.Exists);Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);
                    }
                    else
                    {
                        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsSaved);
                        Assert.AreEqual(1,fault.Attempts);
                        Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                        Assert.AreEqual("sqlite-original",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                        if(mode==1)
                        {
                            Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.AreEqual(0,fault.Acknowledgements);
                            Assert.IsTrue(native.Exists);
                            Assert.AreEqual("newer-owned-record",NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Holon>(native.GetValue<string>("data_json")).Name);
                        }
                        else
                        {
                            Assert.IsFalse(native.Exists);Assert.AreEqual(1,fault.Acknowledgements);
                            Assert.IsInstanceOfType(result.Exception,typeof(GoogleFirestoreMutationReconciliationException));
                            var pending=(GoogleFirestoreMutationReconciliationException)result.Exception;
                            Assert.AreSame(fault.Cause,pending.InnerException);Assert.AreEqual(document.Path,pending.DocumentPath);
                            Assert.IsTrue(NativeMutationReconciliation.IsRequired(pending));
                        }
                    }
                }
                finally {field.SetValue(provider,db);if(acknowledged) await document.DeleteAsync();}
            }
            verified=true;
        }
        finally
        {
            field.SetValue(provider,db);await provider.DeActivateProviderAsync();await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)]
    [DataRow(true,false)] [DataRow(true,true)]
    public async Task NativeDeleteConflictAndLostAcknowledgementPreserveEvidence(bool synchronous,bool loseAcknowledgement)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=GoogleNativeFirestore.Database(project);
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
        var db=GoogleNativeFirestore.Database(project);
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
