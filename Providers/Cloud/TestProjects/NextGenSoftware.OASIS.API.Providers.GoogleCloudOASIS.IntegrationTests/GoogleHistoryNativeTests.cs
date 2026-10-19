using System.Reflection;
using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("HistoryNative")]
public class GoogleHistoryNativeTests
{
    private sealed class NativeCommitBoundary(Google.Cloud.Firestore.V1.FirestoreClient inner,string mode):Google.Cloud.Firestore.V1.FirestoreClient
    {
        public int Attempts,CompetitorCommits;
        public Grpc.Core.RpcException? NativeFailure;
        public readonly IOException Lost=new("Owned native commit acknowledgement withheld after actual success.");
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
            =>inner.BatchGetDocuments(request,callSettings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
        {
            Attempts++;Assert.AreEqual(1,request.Writes.Count);
            if(mode=="create-race")Assert.IsFalse(request.Writes.Single().CurrentDocument.Exists);
            else Assert.IsNotNull(request.Writes.Single().CurrentDocument.UpdateTime);
            if(mode is "conflict" or "create-race"){
                // Deterministic real service interleaving: an independent writer wins the observed revision.
                await inner.CommitAsync(request.Clone(),callSettings);CompetitorCommits++;
            }
            Google.Cloud.Firestore.V1.CommitResponse actual;
            try{actual=await inner.CommitAsync(request,callSettings);}
            catch(Grpc.Core.RpcException cause){NativeFailure=cause;throw;}
            if(mode=="lost")throw Lost;
            if(mode=="invalid"){
                var invalid=new Google.Cloud.Firestore.V1.CommitResponse{CommitTime=new()};
                invalid.WriteResults.Add(new Google.Cloud.Firestore.V1.WriteResult{UpdateTime=new()});return invalid;
            }
            return actual;
        }
    }
    private static string Admit()
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")??throw new InvalidOperationException("Durable history runner required.");
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/GoogleCurrent"),Path.GetFullPath(work));
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length,"Reconcile originals before another fixture.");
        return work;
    }
    private static void Journal(string path,Guid id,string collection)
    {
        using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        stream.Write(JsonSerializer.SerializeToUtf8Bytes(new{id,collection,project="oasis-google-native",state="before-public-history-create"}));stream.Flush(true);
    }
    [DataTestMethod]
    [DataRow(0,"conflict")][DataRow(1,"conflict")][DataRow(2,"conflict")]
    [DataRow(0,"lost")][DataRow(1,"lost")][DataRow(2,"lost")]
    [DataRow(0,"invalid")][DataRow(1,"invalid")][DataRow(2,"invalid")]
    [DataRow(0,"create-race")][DataRow(1,"create-race")][DataRow(2,"create-race")]
    public async Task ActualConditionalCommitPreservesHistoryAndSeparatesRejectionFromUnknown(int kind,string mode)
    {
        var work=Admit();var id=Guid.NewGuid();var collection=kind==0?"avatars":kind==1?"avatarDetails":"holons";
        IHolonBase row=kind==0?new Avatar{Id=id,IsActive=true}:kind==1?new AvatarDetail{Id=id,IsActive=true}:new Holon{Id=id,IsActive=true};
        row.MetaData=new(){["owned_history_native"]="first"};
        var healthy=Database();var verifier=Database();var reference=verifier.Collection(collection).Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;field.SetValue(provider,healthy);
        var journal=Path.Combine(work,$"history-native-fault-{kind}.pending.json");
        try{
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,collection);
            async Task Save(bool initial){
                if(kind==0)Check(await provider.SaveAvatarAsync((IAvatar)row),initial);
                else if(kind==1)Check(await provider.SaveAvatarDetailAsync((IAvatarDetail)row),initial);
                else Check(await provider.SaveHolonAsync((IHolon)row,saveChildren:false),initial);
            }
            void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result,bool initial){
                if(initial){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);return;}
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                if(mode is "conflict" or "create-race"){
                    Assert.IsInstanceOfType<Grpc.Core.RpcException>(result.Exception);
                    Assert.AreEqual(mode=="create-race"?Grpc.Core.StatusCode.AlreadyExists:Grpc.Core.StatusCode.FailedPrecondition,((Grpc.Core.RpcException)result.Exception).StatusCode);
                    Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
                }else{
                    Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
                    if(mode=="lost")Assert.IsInstanceOfType<IOException>(result.Exception.InnerException);
                }
                var policy=typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive).GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
                Assert.AreEqual(false,policy.Invoke(null,new object?[]{null,result}));
            }
            if(mode!="create-race")await Save(true);
            var baseline=mode=="create-race"?0:1;var version=row.VersionId;Assert.AreEqual(baseline,row.Version);
            row.MetaData["owned_history_native"]="second";var boundary=new NativeCommitBoundary(healthy.Client,mode);
            field.SetValue(provider,FirestoreDb.Create("oasis-google-native",boundary));await Save(false);
            Assert.AreEqual(1,boundary.Attempts);Assert.AreEqual(mode is "conflict" or "create-race"?1:0,boundary.CompetitorCommits);
            Assert.AreEqual(baseline,row.Version);Assert.AreEqual(version,row.VersionId);
            var actual=await reference.GetSnapshotAsync();Assert.IsTrue(actual.Exists);
            var document=JObject.Parse(actual.GetValue<string>("data_json"));Assert.AreEqual(baseline+1,document["Version"]!.Value<int>());
            var archive=(JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(baseline,archive.Count);
            if(baseline>0)Assert.AreEqual("first",archive[0]!["MetaData"]!["owned_history_native"]!.Value<string>());
            Assert.AreEqual("second",document["MetaData"]!["owned_history_native"]!.Value<string>());
            await reference.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private static FirestoreDb Database()
        =>GoogleNativeFirestore.Database();

    [DataTestMethod][DataRow(true)][DataRow(false)]
    public async Task AtomicAvatarLifecyclePreservesBothHistoriesAndIdempotentTombstone(bool keepOptionEnabled)
    {
        var work=Admit();var id=Guid.NewGuid();var db=Database();var verifier=Database();
        var profile=verifier.Collection("avatars").Document(id.ToString());var detail=verifier.Collection("avatarDetails").Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        var journal=Path.Combine(work,"history-native-lifecycle.pending.json");
        try{
            Assert.IsFalse((await profile.GetSnapshotAsync()).Exists);Assert.IsFalse((await detail.GetSnapshotAsync()).Exists);
            Journal(journal,id,"avatars+avatarDetails");
            var first=await provider.SaveAvatarAsync(new Avatar{Id=id,IsActive=true});Assert.IsFalse(first.IsError,first.Message);
            var second=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,IsActive=true,XP=123});Assert.IsFalse(second.IsError,second.Message);
            provider.IsVersionControlEnabled=keepOptionEnabled;
            var removed=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsTrue(removed.IsDeleted);
            var revisions=new List<Timestamp>();
            foreach(var reference in new[]{profile,detail}){
                var native=await reference.GetSnapshotAsync();Assert.IsTrue(native.Exists);revisions.Add(native.UpdateTime!.Value);
                Assert.AreEqual(false,native.GetValue<bool>("isActive"));Assert.AreEqual(true,native.GetValue<bool>("isDeleted"));Assert.AreEqual(2L,native.GetValue<long>("version"));
                var current=JObject.Parse(native.GetValue<string>("data_json"));Assert.AreEqual(false,current["IsActive"]!.Value<bool>());Assert.AreEqual(true,current["IsDeleted"]!.Value<bool>());
                var archive=(JArray)current[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,archive.Count);Assert.AreEqual(1,archive[0]!["Version"]!.Value<int>());
                Assert.AreEqual(true,archive[0]!["IsActive"]!.Value<bool>());
            }
            var repeated=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(repeated.IsError,repeated.Exception?.ToString());Assert.IsTrue(repeated.IsDeleted);Assert.IsFalse(repeated.IsSaved);
            Assert.AreEqual(revisions[0],(await profile.GetSnapshotAsync()).UpdateTime!.Value);Assert.AreEqual(revisions[1],(await detail.GetSnapshotAsync()).UpdateTime!.Value);
            Assert.IsNull((await provider.LoadAvatarAsync(id)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(id)).Result);
            var historicalProfile=provider.LoadAvatar(id,1);Assert.IsFalse(historicalProfile.IsError,historicalProfile.Exception?.ToString());Assert.IsTrue(historicalProfile.Result.IsActive);Assert.IsFalse(historicalProfile.Result.IsDeleted);
            var historicalDetail=await provider.LoadAvatarDetailAsync(id,1);Assert.IsFalse(historicalDetail.IsError,historicalDetail.Exception?.ToString());Assert.AreEqual(123L,historicalDetail.Result.XP);Assert.IsFalse(historicalDetail.Result.IsDeleted);
            var hard=await provider.DeleteAvatarAsync(id,false);Assert.IsFalse(hard.IsError,hard.Exception?.ToString());Assert.IsTrue(hard.IsDeleted);
            Assert.IsFalse((await profile.GetSnapshotAsync()).Exists);Assert.IsFalse((await detail.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task OversizedNativeWritePreservesCommittedHistoryWithoutTrimming()
    {
        var work=Admit();var id=Guid.NewGuid();var healthy=Database();var verifier=Database();
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;field.SetValue(provider,healthy);
        var reference=verifier.Collection("holons").Document(id.ToString());var journal=Path.Combine(work,"history-native-capacity.pending.json");
        try{
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,"holons");
            var row=new Holon{Id=id,IsActive=true,Name="first"};
            var first=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(first.IsError,first.Message);
            row.Name="second";var second=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(second.IsError,second.Message);
            var before=await reference.GetSnapshotAsync();var canonical=before.GetValue<string>("data_json");var version=row.VersionId;
            var boundary=new NativeCommitBoundary(healthy.Client,"capacity");field.SetValue(provider,FirestoreDb.Create("oasis-google-native",boundary));
            row.Name=new string('x',1_100_000);
            var failed=await provider.SaveHolonAsync(row,saveChildren:false);
            Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsSaved);Assert.AreEqual(1,boundary.Attempts);Assert.IsNotNull(boundary.NativeFailure);
            Assert.AreEqual(Grpc.Core.StatusCode.InvalidArgument,boundary.NativeFailure.StatusCode);
            Assert.AreSame(boundary.NativeFailure,failed.Exception);Assert.IsNull(failed.Result);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));
            Assert.AreNotEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,failed.ErrorCode);
            Assert.AreEqual(2,row.Version);Assert.AreEqual(version,row.VersionId);
            var actual=await reference.GetSnapshotAsync();Assert.IsTrue(actual.Exists);Assert.AreEqual(before.UpdateTime,actual.UpdateTime);
            Assert.IsTrue(string.Equals(canonical,actual.GetValue<string>("data_json"),StringComparison.Ordinal),"Native rejection must preserve the entire original including its archive; never trim or overwrite.");
            var stored=JObject.Parse(actual.GetValue<string>("data_json"));Assert.AreEqual(1,((JArray)stored[EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
            await reference.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task HistoricalRootPredicatesKeepCurrentIndependentChildren()
    {
        var work=Admit();var id=Guid.NewGuid();var childId=Guid.NewGuid();var oldParent=Guid.NewGuid();var newParent=Guid.NewGuid();var owner=Guid.NewGuid();
        var db=Database();var verifier=Database();var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        var root=new Holon{Id=id,IsActive=true,Name="owned-historical-root-"+id,ParentHolonId=oldParent,CreatedByAvatarId=owner,MetaData=new(){["owned-query"]=id.ToString()}};
        var child=new Holon{Id=childId,IsActive=true,Name="child1",ParentHolonId=id};
        var journal=Path.Combine(work,"history-native-query.pending.json");
        try{
            foreach(var key in new[]{id,childId})Assert.IsFalse((await verifier.Collection("holons").Document(key.ToString()).GetSnapshotAsync()).Exists);
            using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                stream.Write(JsonSerializer.SerializeToUtf8Bytes(new{collection="holons",ids=new[]{id,childId},state="before-historical-query-fixture"}));stream.Flush(true);
            }
            async Task Save(Holon row){var result=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
            await Save(root);await Save(child);child.Name="child2";await Save(child);child.Name="child3";await Save(child);
            var oldName=root.Name;root.Name="current-root";root.ParentHolonId=newParent;root.CreatedByAvatarId=Guid.NewGuid();root.MetaData["owned-query"]="changed";await Save(root);
            void CheckRoot(IHolon selected){Assert.AreEqual(id,selected.Id);Assert.AreEqual(1,selected.Version);Assert.AreEqual(oldParent,selected.ParentHolonId);Assert.AreEqual(owner,selected.CreatedByAvatarId);Assert.AreEqual(3,selected.Children.Single().Version);Assert.AreEqual("child3",selected.Children.Single().Name);}
            var point=await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,version:1);Assert.IsFalse(point.IsError,point.Exception?.ToString());CheckRoot(point.Result);
            var listed=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:1);Assert.IsFalse(listed.IsError,listed.Message);CheckRoot(listed.Result.Single(row=>row.Id==id));
            var parent=await provider.LoadHolonsForParentAsync(oldParent,loadChildrenFromProvider:true,version:1);Assert.IsFalse(parent.IsError,parent.Message);CheckRoot(parent.Result.Single());
            var currentParent=await provider.LoadHolonsForParentAsync(newParent,loadChildren:false,version:1);Assert.IsFalse(currentParent.IsError,currentParent.Message);Assert.IsFalse(currentParent.Result.Any(row=>row.Id==id));
            var metadata=await provider.LoadHolonsByMetaDataAsync("owned-query",id.ToString(),loadChildrenFromProvider:true,version:1);Assert.IsFalse(metadata.IsError,metadata.Message);CheckRoot(metadata.Result.Single());
            var search=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){{"owned-query",id.ToString()}},SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery=oldName,SearchHolons=true}}},version:1);
            Assert.IsFalse(search.IsError,search.Exception?.ToString());CheckRoot(search.Result.SearchResultHolons.Single());
            var owned=await provider.ExportAllDataForAvatarByIdAsync(owner,1);Assert.IsFalse(owned.IsError,owned.Message);CheckRoot(owned.Result.Single());
            var exported=provider.ExportAll(1);Assert.IsFalse(exported.IsError,exported.Message);CheckRoot(exported.Result.Single(row=>row.Id==id));
            var absent=await provider.LoadAllHolonsAsync(loadChildren:false,version:99);Assert.IsFalse(absent.IsError,absent.Message);Assert.IsFalse(absent.Result.Any());
            foreach(var key in new[]{id,childId}){
                var reference=verifier.Collection("holons").Document(key.ToString());var actual=await reference.GetSnapshotAsync();Assert.IsTrue(actual.Exists);Assert.AreEqual(key.ToString(),actual.GetValue<string>("id"));
                await reference.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task PublicCreateUpdatePreservesNativeHistoryAfterDisablingOption(int kind)
    {
        // No native call before durable-runner admission; never run under the old disposable emulator harness.
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")??throw new InvalidOperationException("Durable history runner required.");
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/GoogleCurrent"),Path.GetFullPath(work));
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")),"Actual checkpoint/revision acceptance must precede native history writes.");
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length,"Reconcile original native intents before another fixture.");
        var id=Guid.NewGuid();var collection=kind==0?"avatars":kind==1?"avatarDetails":"holons";
        IHolonBase row=kind==0?new Avatar{Id=id,IsActive=true}:kind==1?new AvatarDetail{Id=id,IsActive=true}:new Holon{Id=id,IsActive=true};
        row.MetaData=new(){["owned_history_native"]="first"};
        var oldName="owned-history-"+id;var oldEmail=oldName+"@example.invalid";
        if(row is IAvatar profile){profile.Username=oldName;profile.Email=oldEmail;profile.VerificationToken=oldName;}
        if(row is IAvatarDetail identity){identity.Username=oldName;identity.Email=oldEmail;}
        var provider=new GoogleCloudOASIS(projectId:"oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsVersionControlEnabled=true,IsProviderActivated=true};
        var writer=Database();var verifier=Database();
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,writer);
        var reference=verifier.Collection(collection).Document(id.ToString());
        var journal=Path.Combine(work,$"history-native-{kind}.pending.json");
        try{
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,collection,project="oasis-google-native",state="before-public-history-create"}));stream.Flush(true);
            }
            async Task Save(){
                if(row is IAvatar avatar){var result=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
                else if(row is IAvatarDetail detail){var result=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
                else{var result=await provider.SaveHolonAsync((IHolon)row,saveChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
            }
            await Save();Assert.AreEqual(1,row.Version);var firstVersion=row.VersionId;
            provider.IsVersionControlEnabled=false;row.MetaData["owned_history_native"]="second";
            if(row is IAvatar currentProfile){currentProfile.Username=oldName+"-changed";currentProfile.Email="changed-"+oldEmail;currentProfile.VerificationToken=oldName+"-changed";}
            if(row is IAvatarDetail currentIdentity){currentIdentity.Username=oldName+"-changed";currentIdentity.Email="changed-"+oldEmail;}
            await Save();Assert.AreEqual(2,row.Version);Assert.AreEqual(firstVersion,row.PreviousVersionId);
            var actual=await reference.GetSnapshotAsync();Assert.IsTrue(actual.Exists);Assert.IsTrue(actual.UpdateTime.HasValue);
            var document=JObject.Parse(actual.GetValue<string>("data_json"));Assert.AreEqual(2,document["Version"]!.Value<int>());
            Assert.AreEqual(2L,actual.GetValue<long>("version"));Assert.AreEqual("second",document["MetaData"]!["owned_history_native"]!.Value<string>());
            var archive=(JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,archive.Count);
            Assert.AreEqual(id.ToString(),archive[0]!["Id"]!.Value<string>());Assert.AreEqual(1,archive[0]!["Version"]!.Value<int>());
            Assert.AreEqual("first",archive[0]!["MetaData"]!["owned_history_native"]!.Value<string>());
            // Public ID/key/list pipelines must select the requested record, never substitute latest.
            if(kind==0){
                var historical=await provider.LoadAvatarAsync(id,1);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(1,historical.Result.Version);
                Assert.AreEqual("first",historical.Result.MetaData["owned_history_native"]);
                var byKey=await provider.LoadAvatarByProviderKeyAsync(id.ToString(),1);Assert.IsFalse(byKey.IsError,byKey.Message);Assert.AreEqual(1,byKey.Result.Version);
                var listed=await provider.LoadAllAvatarsAsync(1);Assert.IsFalse(listed.IsError,listed.Message);Assert.AreEqual(1,listed.Result.Single(value=>value.Id==id).Version);
                var username=await provider.LoadAvatarByUsernameAsync(oldName,1);Assert.IsFalse(username.IsError,username.Message);Assert.AreEqual(id,username.Result.Id);
                var email=provider.LoadAvatarByEmail(oldEmail,1);Assert.IsFalse(email.IsError,email.Message);Assert.AreEqual(id,email.Result.Id);
                Assert.IsNull((await provider.LoadAvatarByUsernameAsync(oldName)).Result);
                Assert.IsInstanceOfType<NotSupportedException>((await provider.LoadAvatarByVerificationTokenAsync(oldName,1)).Exception);
                Assert.IsNull((await provider.LoadAvatarByVerificationTokenAsync(oldName)).Result,"An archived token must not regain current authority.");
                Assert.IsNull((await provider.LoadAvatarAsync(id,99)).Result);
            }else if(kind==1){
                var historical=await provider.LoadAvatarDetailAsync(id,1);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(1,historical.Result.Version);
                Assert.AreEqual("first",historical.Result.MetaData["owned_history_native"]);
                var listed=await provider.LoadAllAvatarDetailsAsync(1);Assert.IsFalse(listed.IsError,listed.Message);Assert.AreEqual(1,listed.Result.Single(value=>value.Id==id).Version);
                var username=await provider.LoadAvatarDetailByUsernameAsync(oldName,1);Assert.IsFalse(username.IsError,username.Message);Assert.AreEqual(id,username.Result.Id);
                var email=provider.LoadAvatarDetailByEmail(oldEmail,1);Assert.IsFalse(email.IsError,email.Message);Assert.AreEqual(id,email.Result.Id);
                Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(oldName)).Result);
                Assert.IsNull((await provider.LoadAvatarDetailAsync(id,99)).Result);
            }else{
                var historical=await provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(1,historical.Result.Version);
                Assert.AreEqual("first",historical.Result.MetaData["owned_history_native"]);
                var byKey=await provider.LoadHolonAsync(id.ToString(),loadChildren:false,version:1);Assert.IsFalse(byKey.IsError,byKey.Message);Assert.AreEqual(1,byKey.Result.Version);
                var listed=await provider.LoadAllHolonsAsync(loadChildren:false,version:1);Assert.IsFalse(listed.IsError,listed.Message);Assert.AreEqual(1,listed.Result.Single(value=>value.Id==id).Version);
                Assert.IsNull((await provider.LoadHolonAsync(id,loadChildren:false,version:99)).Result);
            }
            var ack=await reference.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime.Value));Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
