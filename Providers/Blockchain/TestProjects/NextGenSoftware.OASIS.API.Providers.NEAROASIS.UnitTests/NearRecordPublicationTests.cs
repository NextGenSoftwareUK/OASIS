using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearRecordPublicationTests
{
    private sealed class FixtureScheduleAbortedException:Exception {}
    [TestMethod]
    public async Task AbortedBeforeCallerMutationPreservesOriginalFailureThroughCleanup()
        =>await Assert.ThrowsExceptionAsync<FixtureScheduleAbortedException>(()=>RunPublicationSchedule(3,true,true));
    [TestMethod]
    public async Task BoundedGeneratedAndExistingGraphPublicationSchedules()
    {
        for(var schedule=0;schedule<12;schedule++)await PublicSaveRetainsControlledAcknowledgementAndChangedCaller(3,schedule%2==0);
    }
    [DataTestMethod][DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)]
    public async Task PublicSaveRetainsControlledAcknowledgementAndChangedCaller(int kind,bool generated)
        =>await RunPublicationSchedule(kind,generated,false);

    private async Task RunPublicationSchedule(int kind,bool generated,bool abortBeforeMutation)
    {
        var root=Path.Combine(AppContext.BaseDirectory,"TestResults","NearPublication");
        Directory.CreateDirectory(root);
        if(Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Retained fixture must be reviewed, not overwritten.");
        var names=new[]{"OASIS_NEAR_SDK_BRIDGE","OASIS_NEAR_STORAGE_ENCRYPTION_KEY","OASIS_NEAR_PUBLICATION_FIXTURE"};
        var previous=names.ToDictionary(name=>name,Environment.GetEnvironmentVariable);
        Task? pending=null;
        try
        {
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures","record-publication.mjs"));
            Environment.SetEnvironmentVariable(names[1],Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            Environment.SetEnvironmentVariable(names[2],root);
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-unused-key");
            IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
            var child=new Holon();if(kind==3)((IHolon)source).Children.Add(child);
            source.Id=generated?Guid.Empty:Guid.NewGuid();var original=source.Id;source.Version=7;
            async Task<(bool IsError,bool IsSaved,IHolonBase? Result,Exception? Cause)> Save(){
                if(kind==0){var result=await provider.SaveAvatarAsync((IAvatar)source);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
                if(kind==1){var result=await provider.SaveAvatarDetailAsync((IAvatarDetail)source);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
                var graph=await provider.SaveHolonAsync((IHolon)source,saveChildren:kind==3);return(graph.IsError,graph.IsSaved,graph.Result,graph.Exception);
            }
            var save=Save();pending=save;
            var ready=Path.Combine(root,"ready.json");var deadline=DateTime.UtcNow.AddSeconds(10);
            while(!File.Exists(ready)){if(save.IsCompleted){await save;Assert.Fail("Save ended before controlled acknowledgement");}if(DateTime.UtcNow>deadline)Assert.Fail("Controlled fixture never reached write");await Task.Delay(10);}
            using var signal=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(ready));
            var key=signal.RootElement.GetProperty("key").GetString()!;
            Assert.AreEqual(original,source.Id,"Caller must still be unpublished while the fixture holds its acknowledgement.");
            Assert.IsFalse(File.Exists(Path.Combine(root,"release")),"No earlier release may satisfy the current acknowledgement.");
            Assert.AreEqual(1,File.ReadAllLines(Path.Combine(root,"writes.ndjson")).Length);
            if(!generated)StringAssert.EndsWith(key,original.ToString("D"));
            if(abortBeforeMutation)throw new FixtureScheduleAbortedException();
            var changed=Guid.NewGuid();source.Id=changed;
            await File.WriteAllTextAsync(Path.Combine(root,"release"),"release controlled acknowledgement");
            var saved=await save.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(saved.IsError);Assert.IsFalse(saved.IsSaved);
            Assert.IsNull(saved.Result,$"Unexpected partial root: result={saved.Result?.Id}, caller={source.Id}, cause={saved.Cause?.GetType().Name}; writes={File.ReadAllLines(Path.Combine(root,"writes.ndjson")).Length}");
            var cause=saved.Cause;
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(cause));
            CollectionAssert.Contains(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(cause).ToArray(),"controlled-record-acknowledgement");
            Assert.AreEqual(changed,source.Id);Assert.AreEqual(7,source.Version);Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.NEAROASIS));
            Assert.AreEqual(1,File.ReadAllLines(Path.Combine(root,"writes.ndjson")).Length);
            Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(0,child.Version);
        }
        finally
        {
            try{if(pending!=null&&!pending.IsCompleted){await File.WriteAllTextAsync(Path.Combine(root,"release"),"release cleanup");await pending.WaitAsync(TimeSpan.FromSeconds(20));}}
            finally{
                foreach(var name in names)Environment.SetEnvironmentVariable(name,previous[name]);
                if(pending==null||pending.IsCompleted){foreach(var name in new[]{"ready.pending","ready.json","release","writes.ndjson"}){var path=Path.Combine(root,name);if(File.Exists(path))File.Delete(path);}if(!Directory.EnumerateFileSystemEntries(root).Any())Directory.Delete(root);}
            }
        }
    }
    [DataTestMethod][DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public void AcknowledgedRecordCannotPublishIntoChangedCallerIdentity(int kind,bool generated)
    {
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
        var original=generated?Guid.Empty:Guid.NewGuid();source.Id=original;
        var stored=new Holon{Id=generated?Guid.NewGuid():original,Version=4,VersionId=Guid.NewGuid()};
        var changed=Guid.NewGuid();source.Id=changed;source.Version=7;var versionId=source.VersionId;
        var method=typeof(NEAROASIS).GetMethod("PublishRecordStamp",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{source,stored,original,"acknowledged-fixture-receipt"}));
        var followup=(NativeTransactionFollowupException)error.InnerException!;
        Assert.AreEqual("acknowledged-fixture-receipt",followup.OriginalTransactionId);
        Assert.AreEqual(changed,source.Id);Assert.AreEqual(7,source.Version);Assert.AreEqual(versionId,source.VersionId);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void UnchangedCallerCanReceiveAcknowledgedGeneratedOrExistingIdentity(bool generated)
    {
        var source=new Holon{Id=generated?Guid.Empty:Guid.NewGuid()};
        var stored=new Holon{Id=generated?Guid.NewGuid():source.Id,Version=4,VersionId=Guid.NewGuid()};
        typeof(NEAROASIS).GetMethod("PublishRecordStamp",BindingFlags.Static|BindingFlags.NonPublic)!
            .Invoke(null,new object[]{source,stored,source.Id,"acknowledged-fixture-receipt"});
        Assert.AreEqual(stored.Id,source.Id);Assert.AreEqual(stored.Version,source.Version);Assert.AreEqual(stored.VersionId,source.VersionId);
    }
}
