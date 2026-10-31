using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryHistoryWriteTests
{
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task NegativeRecordVersionRejectsBeforeNativeIo(bool sync,int kind)
    {
        var source=Source(kind);source.Version=-1;var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);provider.IsVersionControlEnabled=false;
        var result=await Save(provider,source,sync);Assert.IsTrue(result.Error);Assert.IsFalse(result.Saved);Assert.AreEqual(0,result.Count);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Cause);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);Assert.AreEqual(Guid.Empty,source.Id);
    }
    private static BigQueryOASIS Provider(BigQueryGraphWriteTests.Ledger client){var provider=new BigQueryOASIS("my-project"){IsVersionControlEnabled=true};typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,client);return provider;}
    private static IHolonBase Source(int kind)=>kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
    private static async Task<(bool Error,bool Saved,int Count,Exception? Cause)> Save(BigQueryOASIS provider,IHolonBase source,bool sync)
    {
        if(source is IAvatar avatar){var r=sync?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);return(r.IsError,r.IsSaved,r.SavedCount,r.Exception);}
        if(source is IAvatarDetail detail){var r=sync?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);return(r.IsError,r.IsSaved,r.SavedCount,r.Exception);}
        var result=sync?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);return(result.IsError,result.IsSaved,result.SavedCount,result.Exception);
    }
    private static async Task<IHolonBase> Read(BigQueryOASIS provider,int kind,Guid id,int version)
    {
        if(kind==0){var r=await provider.LoadAvatarAsync(id,version);Assert.IsFalse(r.IsError,r.Exception?.ToString());return r.Result;}
        if(kind==1){var r=await provider.LoadAvatarDetailAsync(id,version);Assert.IsFalse(r.IsError,r.Exception?.ToString());return r.Result;}
        var result=await provider.LoadHolonAsync(id,loadChildren:false,version:version);Assert.IsFalse(result.IsError,result.Exception?.ToString());return result.Result;
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task SavesCreateVersionsAndRetainArchivesAfterFlagIsDisabled(bool sync,int kind)
    {
        var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);var source=Source(kind);source.Description="first";source.MetaData=new(){{"large",9007199254740993L}};
        var first=await Save(provider,source,sync);Assert.IsFalse(first.Error,first.Cause?.ToString());Assert.IsTrue(first.Saved);Assert.AreEqual(1,source.Version);Assert.AreNotEqual(Guid.Empty,source.VersionId);var firstVersion=source.VersionId;
        source.Description="second";var second=await Save(provider,source,sync);Assert.IsFalse(second.Error,second.Cause?.ToString());Assert.AreEqual(2,source.Version);Assert.AreEqual(firstVersion,source.PreviousVersionId);Assert.AreNotEqual(firstVersion,source.VersionId);
        provider.IsVersionControlEnabled=false;source.Description="third";var third=await Save(provider,source,sync);Assert.IsFalse(third.Error,third.Cause?.ToString());Assert.AreEqual(3,source.Version);
        foreach(var version in new[]{1,2,3}){var row=await Read(provider,kind,source.Id,version);Assert.AreEqual(version,row.Version);Assert.AreEqual(version==1?"first":version==2?"second":"third",row.Description);Assert.AreEqual("9007199254740993",row.MetaData["large"].ToString());}
        Assert.AreEqual(3,client.Writes);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SoftDeleteArchivesPreviousActiveRevision(bool sync,bool holon)
    {
        var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);var source=Source(holon?2:0);source.Description="active";var saved=await Save(provider,source,sync);Assert.IsFalse(saved.Error,saved.Cause?.ToString());
        var deleted=holon?(sync?provider.DeleteHolonSoft(source.Id):await provider.DeleteHolonSoftAsync(source.Id)):(sync?provider.DeleteAvatar(source.Id):await provider.DeleteAvatarAsync(source.Id));
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.IsDeleted);Assert.AreEqual(1,deleted.DeletedCount);
        var active=await Read(provider,holon?2:0,source.Id,1);Assert.IsFalse(active.IsDeleted);Assert.AreEqual("active",active.Description);
        var document=OasisJson.ParseStoredObject(client.Records[source.Id.ToString()]);Assert.AreEqual("2",document["Version"]!.ToString());Assert.AreEqual(1,((Newtonsoft.Json.Linq.JArray)document[NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task ConcurrentNativeChangeCannotPublishProposedVersion(bool sync,int kind)
    {
        var source=Source(kind);source.Id=Guid.NewGuid();source.Version=1;source.Description="proposed";var original=Source(kind);original.Id=source.Id;original.Version=1;original.Description="original";
        var client=new BigQueryGraphWriteTests.Ledger();client.Records[source.Id.ToString()]=StoredRecordJson.Serialize(original);string? concurrent=null;
        client.AfterRead=key=>{original.Description="concurrent";concurrent=StoredRecordJson.Serialize(original);client.Records[key]=concurrent;};var provider=Provider(client);
        var result=await Save(provider,source,sync);Assert.IsTrue(result.Error);Assert.IsFalse(result.Saved);Assert.AreEqual(0,result.Count);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Cause));Assert.AreEqual(1,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);Assert.IsNotNull(concurrent);Assert.AreEqual(concurrent,client.Records[source.Id.ToString()]);
    }
    public static IEnumerable<object[]> SafetyCases(){foreach(var sync in new[]{false,true})foreach(var kind in new[]{0,1,2})foreach(var fault in new[]{0,1,2,3})yield return new object[]{sync,kind,fault};}
    [DataTestMethod][DynamicData(nameof(SafetyCases),DynamicDataSourceType.Method)]
    public async Task InvalidOrChangedRecordCannotDispatchHistoryMutation(bool sync,int kind,int fault)
    {
        var source=Source(kind);source.Id=Guid.NewGuid();source.Version=fault==1?int.MaxValue:2;source.Description="proposed";var previous=Source(kind);previous.Id=source.Id;previous.Version=source.Version;previous.Description="stored";
        if(fault==2)previous.DeletedDate=DateTime.UtcNow;
        var raw=StoredRecordJson.Serialize(previous);
        if(fault==0){var old=Source(kind);old.Id=Guid.NewGuid();old.Version=1;var document=OasisJson.ParseStoredObject(raw);document["id"]=source.Id.ToString();document[EmbeddedRecordHistory.PropertyName]=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["records"]=new Newtonsoft.Json.Linq.JArray(OasisJson.ParseStoredObject(StoredRecordJson.Serialize(old)))};raw=document.ToString(Newtonsoft.Json.Formatting.None);}
        var client=new BigQueryGraphWriteTests.Ledger();client.Records[source.Id.ToString()]=raw;if(fault==3)client.AfterRead=_=>source.Description="newer caller edit";var provider=Provider(client);
        var result=await Save(provider,source,sync);Assert.IsTrue(result.Error);Assert.IsFalse(result.Saved);Assert.AreEqual(0,result.Count);Assert.AreEqual(0,client.Writes);Assert.AreEqual(raw,client.Records[source.Id.ToString()]);Assert.AreEqual(previous.Version,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);Assert.AreEqual(fault==3?"newer caller edit":"proposed",source.Description);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Cause));
        if(fault<2)Assert.IsInstanceOfType<InvalidDataException>(result.Cause);else Assert.IsInstanceOfType<InvalidOperationException>(result.Cause);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task CommittedHistoryWithLostAcknowledgementRemainsUncertain(bool sync,int kind)
    {
        var source=Source(kind);source.Id=Guid.NewGuid();source.Version=1;source.VersionId=Guid.NewGuid();source.Description="proposed";var previous=Source(kind);previous.Id=source.Id;previous.Version=1;previous.VersionId=source.VersionId;previous.Description="stored";
        var versionId=source.VersionId;var cause=new IOException("Controlled committed write acknowledgement loss.");var client=new BigQueryGraphWriteTests.Ledger{FirstWrite=()=>throw cause};client.Records[source.Id.ToString()]=StoredRecordJson.Serialize(previous);var provider=Provider(client);
        var result=await Save(provider,source,sync);Assert.IsTrue(result.Error);Assert.IsFalse(result.Saved);Assert.AreEqual(0,result.Count);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Cause));var native=result.Cause as BigQueryRecordReconciliationException;Assert.IsNotNull(native);Assert.IsFalse(native.WriteAcknowledged);Assert.AreSame(cause,native.InnerException);Assert.AreEqual(source.Id,native.RecordId);StringAssert.StartsWith(native.OriginalTransactionId,"oasis-record-");
        Assert.AreEqual(1,source.Version);Assert.AreEqual(versionId,source.VersionId);var actual=OasisJson.ParseStoredObject(client.Records[source.Id.ToString()]);Assert.AreEqual("2",actual["Version"]!.ToString());Assert.AreEqual(1,((Newtonsoft.Json.Linq.JArray)actual[EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task VersionedGraphRoundtripKeepsCurrentChildrenIndependent(bool sync)
    {
        var child=new Holon{Description="child first"};var root=new Holon{Description="root first",Children=new List<IHolon>{child}};var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);
        var first=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);
        root.Description="root second";child.Description="child second";var second=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.IsFalse(second.IsError,second.Exception?.ToString());Assert.AreEqual(2,root.Version);Assert.AreEqual(2,child.Version);
        var loaded=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:1);Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(1,loaded.Result.Version);Assert.AreEqual("root first",loaded.Result.Description);Assert.AreEqual(2,loaded.Result.Children.Single().Version);Assert.AreEqual("child second",loaded.Result.Children.Single().Description);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SoftDeleteBindsOriginalObservationInsteadOfRereadingNewerData(bool sync,bool holon)
    {
        var source=Source(holon?2:0);source.Id=Guid.NewGuid();source.Version=1;source.Description="original";var client=new BigQueryGraphWriteTests.Ledger();client.Records[source.Id.ToString()]=StoredRecordJson.Serialize(source);string? concurrent=null;
        client.AfterRead=key=>{source.Description="concurrent";concurrent=StoredRecordJson.Serialize(source);client.Records[key]=concurrent;};var provider=Provider(client);
        var result=holon?(sync?provider.DeleteHolonSoft(source.Id):await provider.DeleteHolonSoftAsync(source.Id)):(sync?provider.DeleteAvatar(source.Id):await provider.DeleteAvatarAsync(source.Id));
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(1,client.Reads);Assert.AreEqual(1,client.Writes);Assert.IsNotNull(concurrent);Assert.AreEqual(concurrent,client.Records[source.Id.ToString()]);Assert.IsFalse(source.IsDeleted);
    }
}
