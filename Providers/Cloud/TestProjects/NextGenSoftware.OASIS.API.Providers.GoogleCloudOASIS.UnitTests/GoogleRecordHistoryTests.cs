using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleRecordHistoryTests
{
    [TestMethod]
    public async Task HistoryReadReturnsExactCurrentAndArchivedMetadata()
    {
        var initial=1.0000000000000000000000000001m;var later=1.0000000000000000000000000002m;
        var row=new Holon{Id=Guid.NewGuid(),MetaData=new(){["precise"]=initial}};
        var first=await Stage(row,null);row.MetaData["precise"]=later;
        var second=await Stage(row,Fields(row.Id,first));
        var fields=Fields(row.Id,second);
        Assert.AreEqual(initial,decimal.Parse((await Read(fields,row.Id,1)).MetaData["precise"].ToString()!,System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(later,decimal.Parse((await Read(fields,row.Id,0)).MetaData["precise"].ToString()!,System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(0,row.Version);
    }

    [TestMethod]
    public void CanonicalHistoryDocumentPreservesExactMetadata()
    {
        var value=1.0000000000000000000000000001m;
        var row=new Holon{Id=Guid.NewGuid(),MetaData=new(){["precise"]=value}};
        var fields=new Dictionary<string,object>{["id"]=row.Id.ToString(),["data_json"]=OasisJson.Serialize(row)};
        var method=typeof(GoogleCloudOASIS).GetMethod("CanonicalFirestoreHistoryDocument",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon));
        var document=(JObject)method.Invoke(null,new object[]{fields,row.Id.ToString()})!;
        using var json=System.Text.Json.JsonDocument.Parse(document.ToString());
        Assert.AreEqual(value,json.RootElement.GetProperty("MetaData").GetProperty("precise").GetDecimal());
        Assert.AreEqual(OasisJson.Serialize(row),fields["data_json"]);
    }

    [DataTestMethod][DataRow("version-int")][DataRow("version-double")][DataRow("version-string")][DataRow("active")][DataRow("deleted")]
    public async Task NativeProjectionMustNotContradictCanonicalRecord(string mode)
    {
        var row=new Holon{Id=Guid.NewGuid(),Version=3,IsActive=true};var fields=Fields(row.Id,JObject.Parse(OasisJson.Serialize(row)));
        if(mode=="active")fields["isActive"]=false;
        else if(mode=="deleted")fields["isDeleted"]=true;
        else fields["version"]=mode=="version-int"?4:mode=="version-double"?(object)3.0:"3";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Read(fields,row.Id,0));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task FirestoreIntegerProjectionAcceptsSdkLongAndEncodedInt(bool sdkLong)
    {
        var row=new Holon{Id=Guid.NewGuid(),Version=3,IsActive=true};var fields=Fields(row.Id,JObject.Parse(OasisJson.Serialize(row)));
        fields["version"]=sdkLong?(object)3L:3;fields["isActive"]=true;fields["isDeleted"]=false;
        Assert.AreEqual(3,(await Read(fields,row.Id,0)).Version);
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task EachCanonicalRecordKindSelectsExactHistoryInsteadOfLatest(int kind)
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        row.MetaData=new(){["history-marker"]="first"};
        var stage=typeof(GoogleCloudOASIS).GetMethod("StageFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType());
        var first=await (Task<JObject>)stage.Invoke(null,new object?[]{row,null,row.Id.ToString()})!;
        row.MetaData["history-marker"]="second";
        var next=await (Task<JObject>)stage.Invoke(null,new object[]{row,Fields(row.Id,first),row.Id.ToString()})!;
        var read=typeof(GoogleCloudOASIS).GetMethod("ReadFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType());
        foreach(var version in new[]{0,1,99}){
            var task=(Task)read.Invoke(null,new object[]{Fields(row.Id,next),row.Id.ToString(),version})!;await task;
            var value=(NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase?)task.GetType().GetProperty("Result")!.GetValue(task);
            if(version==99)Assert.IsNull(value);
            else{Assert.AreEqual(version==0?2:1,value!.Version);Assert.AreEqual(version==0?"second":"first",value.MetaData["history-marker"]);}
        }
        Assert.AreEqual(0,row.Version);
    }
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)]
    public async Task HistoricalCredentialSelectorsRejectBeforeActivationIndependentlyOfDataHistory(int kind,bool synchronous)
    {
        var provider=new GoogleCloudOASIS(projectId:"owned-history-boundary",enableStorage:false,enableFirestore:true,enableBigQuery:false);
        foreach(var version in new[]{-1,1}){
            var result=kind switch{
                0=>synchronous?provider.LoadAvatarByVerificationToken("owned",version):await provider.LoadAvatarByVerificationTokenAsync("owned",version),
                1=>synchronous?provider.LoadAvatarByResetToken("owned",version):await provider.LoadAvatarByResetTokenAsync("owned",version),
                2=>synchronous?provider.LoadAvatarByRefreshToken("owned",version):await provider.LoadAvatarByRefreshTokenAsync("owned",version),
                3=>synchronous?provider.LoadAvatarByPublicKey("owned",version):await provider.LoadAvatarByPublicKeyAsync("owned",version),
                _=>synchronous?provider.LoadAvatarByPrivateKey("owned",version):await provider.LoadAvatarByPrivateKeyAsync("owned",version)};
            Assert.IsTrue(result.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsFalse(provider.IsProviderActivated);
            if(version<0)Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
            else{Assert.IsInstanceOfType<NotSupportedException>(result.Exception);StringAssert.Contains(result.Exception.Message,"Historical credentials");}
        }
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task PublicSavesDoNotPublishIdentityOrVersionsWhenPreReadFails(int kind)
    {
        var provider=new GoogleCloudOASIS(projectId:"owned-history-boundary",enableStorage:false,enableFirestore:true,enableBigQuery:false);
        provider.IsProviderActivated=true;
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,Google.Cloud.Firestore.FirestoreDb.Create("owned-history-boundary",new NoNativeCallsClient()));
        var versionId=Guid.NewGuid();var previous=Guid.NewGuid();
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
        row.Version=9;row.VersionId=versionId;row.PreviousVersionId=previous;
        Exception cause;bool failed,saved;
        if(kind==0){var result=await provider.SaveAvatarAsync((Avatar)row);cause=result.Exception;failed=result.IsError;saved=result.IsSaved;}
        else if(kind==1){var result=await provider.SaveAvatarDetailAsync((AvatarDetail)row);cause=result.Exception;failed=result.IsError;saved=result.IsSaved;}
        else{var result=await provider.SaveHolonAsync((Holon)row,saveChildren:false);cause=result.Exception;failed=result.IsError;saved=result.IsSaved;}
        Assert.IsTrue(failed);Assert.IsFalse(saved);Assert.IsInstanceOfType<NotImplementedException>(cause,cause?.ToString());
        Assert.AreEqual(Guid.Empty,row.Id);Assert.AreEqual(9,row.Version);Assert.AreEqual(versionId,row.VersionId);Assert.AreEqual(previous,row.PreviousVersionId);
        Assert.IsFalse(row.ProviderUniqueStorageKey.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.GoogleCloudOASIS));
    }
    [DataTestMethod][DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)][DataRow(false,false,true)][DataRow(true,false,true)]
    public async Task LifecycleStagePreservesExistingHistoryAndDoesNotInventLegacyHistory(bool detail,bool history,bool createHistory)
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=detail?new AvatarDetail{Id=Guid.NewGuid(),IsActive=true}:new Avatar{Id=Guid.NewGuid(),IsActive=true};
        var canonical=JObject.Parse(OasisJson.Serialize(row));
        if(history){var stage=(Task<JObject>)typeof(GoogleCloudOASIS).GetMethod("StageFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType()).Invoke(null,new object?[]{row,null,row.Id.ToString()})!;canonical=await stage;}
        var previous=Fields(row.Id,canonical);previous["version"]=history?1:0;previous["isDeleted"]=false;previous["isActive"]=true;var original=(string)previous["data_json"];
        row.IsActive=false;row.DeletedDate=DateTime.UtcNow;
        var task=(Task<Dictionary<string,object>>)typeof(GoogleCloudOASIS).GetMethod("StageFirestoreLifecycleUpdateAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType()).Invoke(null,new object[]{row,previous,row.Id.ToString(),createHistory})!;
        var update=await task;var stored=JObject.Parse((string)update["data_json"]);
        Assert.AreEqual(true,update["isDeleted"]);
        var committed=new Dictionary<string,object>(previous);foreach(var pair in update)committed[pair.Key]=pair.Value;
        var read=typeof(GoogleCloudOASIS).GetMethod("ReadFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType());
        var readTask=(Task)read.Invoke(null,new object[]{committed,row.Id.ToString(),0})!;await readTask;
        var readBack=(NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase)readTask.GetType().GetProperty("Result")!.GetValue(readTask)!;
        Assert.IsTrue(readBack.IsDeleted);Assert.IsFalse(readBack.IsActive);
        Assert.AreEqual(false,previous["isDeleted"]);Assert.AreEqual(true,previous["isActive"]);
        Assert.AreEqual(false,update["isActive"]);Assert.AreEqual(history?2:createHistory?1:0,update["version"]);Assert.AreEqual(original,previous["data_json"]);Assert.AreEqual(0,row.Version);
        if(history||createHistory){var records=(JArray)stored[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,records.Count);Assert.AreEqual(history?1:0,records[0]!["Version"]!.Value<int>());Assert.IsTrue(records[0]!["IsActive"]!.Value<bool>());}
        else Assert.IsNull(stored[EmbeddedRecordHistory.PropertyName]);
    }
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public void OrdinaryLatestDecodersRetainLegacySupportAndRejectCorruptArchives(int kind,bool corrupt)
    {
        var id=Guid.NewGuid();NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};
        var document=JObject.Parse(OasisJson.Serialize(row));
        if(corrupt)document[EmbeddedRecordHistory.PropertyName]=new JObject{["schema"]=42,["records"]=new JArray()};
        var method=typeof(GoogleCloudOASIS).GetMethod(kind==0?"DecodeAvatar":kind==1?"DecodeAvatarDetail":"DecodeHolon",BindingFlags.Static|BindingFlags.NonPublic)!;
        if(corrupt){var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{Fields(id,document),id.ToString()}));Assert.IsInstanceOfType<InvalidDataException>(error.InnerException);}
        else{var decoded=(NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase)method.Invoke(null,new object[]{Fields(id,document),id.ToString()})!;Assert.AreEqual(id,decoded.Id);Assert.AreEqual(0,decoded.Version);}
    }
    private sealed class NoNativeCallsClient:Google.Cloud.Firestore.V1.FirestoreClient
    {
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>(Google.Cloud.Firestore.V1.FirestoreSettings)typeof(GoogleCloudOASIS).GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
            =>throw new NotImplementedException("Owned controlled pre-read failure; no SDK transport.");
    }
    private static Task<Holon> WriteBoundary(Dictionary<string,object> fields,string collection,Guid id)
    {
        var database=Google.Cloud.Firestore.FirestoreDb.Create("owned-history-boundary",new NoNativeCallsClient());
        return (Task<Holon>)typeof(GoogleCloudOASIS).GetMethod("WriteFirestoreRecordAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon)).Invoke(null,new object?[]{database.Collection(collection).Document(id.ToString()),fields,true,(Action)(()=>{}),null})!;
    }
    [TestMethod]
    public async Task NativeWriterRejectsWrongCollectionBeforeSdkCalls()
    {
        var row=new Holon{Id=Guid.NewGuid()};
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>WriteBoundary(Fields(row.Id,JObject.Parse(OasisJson.Serialize(row))),"avatars",row.Id));
    }
    [TestMethod]
    public async Task NativeWriterRejectsMutableProjectionBeforeSdkCalls()
    {
        var row=new Holon{Id=Guid.NewGuid()};var fields=Fields(row.Id,JObject.Parse(OasisJson.Serialize(row)));fields["mutable"]=new Dictionary<string,object>();
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>WriteBoundary(fields,"holons",row.Id));
    }
    [TestMethod]
    public async Task NativeWriterRejectsForeignKeyBeforeSdkCalls()
    {
        var row=new Holon{Id=Guid.NewGuid()};
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>WriteBoundary(Fields(row.Id,JObject.Parse(OasisJson.Serialize(row))),"holons",Guid.NewGuid()));
    }
    [TestMethod]
    public void MutationSettingsDisableSdkReplayWithoutChangingReadPolicyOrDeadline()
    {
        var settings=(Google.Cloud.Firestore.V1.FirestoreSettings)typeof(GoogleCloudOASIS).GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        var original=Google.Cloud.Firestore.V1.FirestoreSettings.GetDefault();
        Assert.AreEqual(1,settings.CommitSettings.Retry.MaxAttempts);
        Assert.IsFalse(settings.CommitSettings.Retry.RetryFilter(new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable,"owned response loss"))));
        Assert.AreEqual(original.CommitSettings.Expiration.Type,settings.CommitSettings.Expiration.Type);
        Assert.AreEqual(original.CommitSettings.Expiration.Timeout,settings.CommitSettings.Expiration.Timeout);
        Assert.AreEqual(original.CommitSettings.Expiration.Deadline,settings.CommitSettings.Expiration.Deadline);
        Assert.AreEqual(original.GetDocumentSettings.Retry.MaxAttempts,settings.GetDocumentSettings.Retry.MaxAttempts);
    }
    private static Dictionary<string,object> Fields(Guid id,JObject document)=>new(){["id"]=id.ToString(),["data_json"]=document.ToString()};
    private static Task<JObject> Stage(Holon row,Dictionary<string,object>? previous,string? key=null)
        =>(Task<JObject>)typeof(GoogleCloudOASIS).GetMethod("StageFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon)).Invoke(null,new object?[]{row,previous,key??row.Id.ToString()})!;
    private static Task<Holon> Read(Dictionary<string,object> fields,Guid id,int version)
        =>(Task<Holon>)typeof(GoogleCloudOASIS).GetMethod("ReadFirestoreHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon)).Invoke(null,new object[]{fields,id.ToString(),version})!;

    [TestMethod]
    public async Task DetachedStageAndExactHistoricalReadReuseSharedCodec()
    {
        var row=new Holon{Id=Guid.NewGuid(),Name="first"};var first=await Stage(row,null);var original=first.ToString();
        row.Name="second";var second=await Stage(row,Fields(row.Id,first));
        Assert.AreEqual(0,row.Version);Assert.AreEqual(original,first.ToString());
        var current=await Read(Fields(row.Id,second),row.Id,0);var previous=await Read(Fields(row.Id,second),row.Id,1);
        Assert.AreEqual(2,current.Version);Assert.AreEqual("first",previous.Name);Assert.AreEqual(previous.VersionId,current.PreviousVersionId);
        Assert.IsNull(await Read(Fields(row.Id,second),row.Id,99));
    }
    [TestMethod]
    public async Task LegacyCanonicalPayloadCanBecomeVersionedWithoutMutatingPrevious()
    {
        var row=new Holon{Id=Guid.NewGuid(),Version=4};var prior=JObject.Parse(OasisJson.Serialize(row));
        var staged=await Stage(row,Fields(row.Id,prior));Assert.AreEqual(5,staged["Version"]!.Value<int>());Assert.IsNull(prior["id"]);Assert.AreEqual(4,row.Version);
    }
    [TestMethod]
    public async Task MismatchedProposedKeyIsRejectedBeforeStaging()
        =>await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(new Holon{Id=Guid.NewGuid()},null,Guid.NewGuid().ToString()));
    [TestMethod]
    public async Task NativeAndEmbeddedIdentityMustBothMatch()
    {
        var row=new Holon{Id=Guid.NewGuid()};var first=await Stage(row,null);var fields=Fields(row.Id,first);fields["id"]=Guid.NewGuid().ToString();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(row,fields));
        first["id"]=Guid.NewGuid().ToString();await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(row,Fields(row.Id,first)));
    }
    [TestMethod]
    public async Task CorruptArchiveIsRejectedEvenForLatestAndReplacement()
    {
        var row=new Holon{Id=Guid.NewGuid()};var first=await Stage(row,null);first[EmbeddedRecordHistory.PropertyName]!["schema"]=42;
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Read(Fields(row.Id,first),row.Id,0));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(row,Fields(row.Id,first)));
    }
}
