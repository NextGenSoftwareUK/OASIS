using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearPreparedStorageTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public async Task PublicEncryptedHistoryAndConflictsPersistEveryOriginalBeforeSubmission()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));
        Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var repository=new DirectoryInfo(Environment.CurrentDirectory);
        while(repository!=null&&!File.Exists(Path.Combine(repository.FullName,"Scripts","run_near_provider_evidence.ps1")))repository=repository.Parent;
        Assert.IsNotNull(repository);
        var folder=Path.Combine(repository.FullName,"TestResults","NearRuntime");const string stem="near-prepared-storage";
        var pending=Path.Combine(folder,stem+".pending.json");var journal=Path.Combine(folder,stem+".attempts.jsonl");var complete=Path.Combine(folder,stem+".complete.json");
        var published=Path.Combine(repository.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence",stem+".complete.json");
        Assert.IsFalse(new[]{pending,journal,complete,published}.Any(File.Exists),"Preserve/reconcile originals; never replay a published or pending cohort.");
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY")){IsVersionControlEnabled=true};
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        var prepared=(IPreparedNativeStorageProvider)provider;
        var sdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;var serviceType=sdk.GetType();
        Task<string> Get(string key)=>(Task<string>)serviceType.GetMethod("GetAsync")!.Invoke(sdk,new object[]{key,CancellationToken.None})!;
        async Task<object> Snapshot(string key){var task=(Task)serviceType.GetMethod("ReadSnapshotAsync")!.Invoke(sdk,new object[]{key,CancellationToken.None})!;await task;return task.GetType().GetProperty("Result")!.GetValue(task)!;}
        string? Field(object snapshot,string name)=>(string?)snapshot.GetType().GetProperty(name)!.GetValue(snapshot);
        Task<string> Write(object observed,string json,string? archive=null,string? archived=null)=>(Task<string>)serviceType.GetMethod("PutRecordCheckedAsync")!.Invoke(sdk,new object?[]{observed,json,archive,archived})!;
        var id=Guid.Parse("5f4785d8-944e-4275-a4be-c26e00000001");var key="holon:"+id;
        var archives=new[]{"history:"+key+":0000000001","history:"+key+":0000000002"};var keys=new[]{key}.Concat(archives).ToArray();
        foreach(var target in keys)Assert.IsNull(await Get(target),"Owned fixture key must be absent before acceptance: "+target);
        using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{id,keys,plannedNativeActions=8,synthetic=true}));file.Flush(true);
        }
        var attempts=new List<UsageProviderAttempt>();var sequence=0;
        async Task<T> Own<T>(Func<Task<T>> operation){
            var store=DispatchProxy.Create<IUsageOutboxStore,NearPreparedNativeTests.FlushedOwner>();var owner=(NearPreparedNativeTests.FlushedOwner)(object)store;owner.Journal=journal;
            var execution=(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),
                    ExecutionOwner="native-near-prepared-storage",Policy=new UsageEndpointPolicy()},store,new Web4UsageLedgerOptions(),null,null});
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2));
            Exception? originalFailure=null;
            try{
                var result=await prepared.ExecutePreparedStorageAsync(operation,execution,stem+"-"+(sequence++),"test.near",timeout.Token);
                if(result!=null)originalFailure=result.GetType().GetProperty("Exception")?.GetValue(result) as Exception;
                return result;
            }catch(Exception failure){originalFailure=failure;throw;}
            finally{
                if(owner.Calls!=1)throw new InvalidOperationException("Expected one persisted original; preserve the pre-persistence failure.",originalFailure);
                Assert.IsNotNull(owner.Original);attempts.Add(owner.Original);
            }
        }
        var holon=new Holon{Id=id,Description="owned prepared encrypted history first",MetaData=null};
        var created=await Own(()=>provider.SaveHolonAsync(holon,saveChildren:false));Assert.IsFalse(created.IsError,created.Exception?.ToString()??created.Message);Assert.IsTrue(created.IsSaved);Assert.AreEqual(1,holon.Version);
        var first=await Snapshot(key);var firstJson=Field(first,"Plaintext")!;Assert.IsFalse(Field(first,"Ciphertext")!.Contains(holon.Description));
        holon.Description="owned prepared encrypted history second";
        var updated=await Own(()=>provider.SaveHolonAsync(holon,saveChildren:false));Assert.IsFalse(updated.IsError,updated.Exception?.ToString()??updated.Message);Assert.IsTrue(updated.IsSaved);Assert.AreEqual(2,holon.Version);
        foreach(var version in new[]{1,2}){
            var loaded=await provider.LoadHolonAsync(id,loadChildren:false,version:version);Assert.IsFalse(loaded.IsError,loaded.Message);
            Assert.AreEqual(version,loaded.Result.Version);Assert.AreEqual(version==1?"owned prepared encrypted history first":holon.Description,loaded.Result.Description);
        }
        var stale=await Assert.ThrowsExceptionAsync<NearRecordWriteRejectedException>(()=>Own(()=>Write(first,firstJson)));
        Assert.AreEqual(attempts[^1].ProviderRequestId,stale.OriginalTransactionId);Assert.IsTrue(stale.IsRecordConflict);
        var second=await Snapshot(key);var before=Field(second,"Ciphertext");
        var collision=await Assert.ThrowsExceptionAsync<NearRecordWriteRejectedException>(()=>Own(()=>Write(second,firstJson,archives[0],firstJson)));
        Assert.AreEqual(attempts[^1].ProviderRequestId,collision.OriginalTransactionId);Assert.IsTrue(collision.IsRecordConflict);Assert.AreEqual(before,Field(await Snapshot(key),"Ciphertext"));
        var deleted=await Own(()=>provider.DeleteHolonAsync(id));Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString()??deleted.Message);
        var missing=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(missing.IsError,missing.Message);Assert.IsNull(missing.Result);
        string Canonical(string value)=>OasisJson.Serialize<Holon>(OasisJson.DeserializeStored<Holon>(value));
        Assert.AreEqual(Canonical(firstJson),Canonical(await Get(archives[0])));Assert.AreEqual(Canonical(Field(second,"Plaintext")!),Canonical(await Get(archives[1])));
        var hashes=new Dictionary<string,string>();
        foreach(var target in keys){var snapshot=await Snapshot(target);Assert.IsNotNull(Field(snapshot,"Ciphertext"));hashes[target]=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Field(snapshot,"Ciphertext")!)));}
        foreach(var target in keys){
            var current=await Snapshot(target);Assert.AreEqual(hashes[target],Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Field(current,"Ciphertext")!))),"Fixture changed; preserve it instead of cleanup");
            var hash=await Own(()=>(Task<string>)serviceType.GetMethod("DeleteAsync")!.Invoke(sdk,new object[]{target})!);Assert.AreEqual(attempts[^1].ProviderRequestId,hash);Assert.IsNull(await Get(target));
        }
        Assert.AreEqual(8,attempts.Count);Assert.AreEqual(8,attempts.Select(row=>row.ProviderRequestId).Distinct().Count());
        var reader=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near","read-only-invalid-signing-key");
        var readerActive=await reader.ActivateProviderAsync();Assert.IsFalse(readerActive.IsError,readerActive.Message);var outcomes=new List<JsonElement>();
        for(var index=0;index<attempts.Count;index++){
            var recovered=await reader.RecoverNativePreparationAsync(attempts[index]);Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString()??recovered.Message);
            var observed=await reader.ReadPreparedNativeOutcomeAsync(recovered.Result);Assert.IsFalse(observed.IsError,observed.Exception?.ToString()??observed.Message);
            Assert.IsTrue(observed.Result.GetProperty("status").TryGetProperty(index is 2 or 3?"Failure":"SuccessValue",out _));outcomes.Add(observed.Result.Clone());
        }
        using(var file=new FileStream(complete,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{id,keys,hashes,attempts,outcomes,physicalFsyncOwner=true,syntheticAdmission=true,
                publicStorageInterfaces=true,encryptedCurrentAndHistory=true,verifiedFixturesAbsent=true,hostedMongoVerified=false,publicFunds=false}));file.Flush(true);
        }
        var retained=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToArray();
        CollectionAssert.AreEqual(attempts.Select(row=>row.ProviderRequestId).ToArray(),retained.Select(row=>row.ProviderRequestId).ToArray());
        File.Delete(journal);File.Delete(pending);
        Console.WriteLine("NEAR_PREPARED_STORAGE_VERIFIED "+JsonSerializer.Serialize(new{originals=attempts.Select(row=>row.ProviderRequestId).ToArray(),keys}));
    }
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public async Task ReconcileFiveKnownOriginalsAndCleanOnlyTheirExactOwnedCiphertexts()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));
        var root=new DirectoryInfo(Environment.CurrentDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;
        Assert.IsNotNull(root);const string stem="near-prepared-storage";
        var folder=Path.Combine(root.FullName,"TestResults","NearRuntime");var pending=Path.Combine(folder,stem+".pending.json");var journal=Path.Combine(folder,stem+".attempts.jsonl");
        var complete=Path.Combine(folder,stem+".complete.json");var published=Path.Combine(root.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence",stem+".complete.json");
        Assert.IsTrue(File.Exists(pending)&&File.Exists(journal));Assert.IsFalse(File.Exists(complete)||File.Exists(published));
        var attempts=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToList();Assert.AreEqual(5,attempts.Count,"No replay or automatic continuation of an unknown phase");
        using var marker=JsonDocument.Parse(File.ReadAllText(pending));var id=Guid.Parse("5f4785d8-944e-4275-a4be-c26e00000001");Assert.AreEqual(id.ToString("D"),marker.RootElement.GetProperty("id").GetString());
        var keys=marker.RootElement.GetProperty("keys").EnumerateArray().Select(value=>value.GetString()!).ToArray();
        CollectionAssert.AreEqual(new[]{"holon:"+id,"history:holon:"+id+":0000000001","history:holon:"+id+":0000000002"},keys);
        var reader=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near","read-only-invalid-signing-key");
        var readActive=await reader.ActivateProviderAsync();Assert.IsFalse(readActive.IsError,readActive.Message);
        var originals=new List<JsonElement>();var outcomes=new List<JsonElement>();
        async Task Observe(UsageProviderAttempt attempt,bool failed){
            var original=await reader.RecoverNativePreparationAsync(attempt);Assert.IsFalse(original.IsError,original.Exception?.ToString()??original.Message);
            var outcome=await reader.ReadPreparedNativeOutcomeAsync(original.Result);Assert.IsFalse(outcome.IsError,outcome.Exception?.ToString()??outcome.Message);
            Assert.IsTrue(outcome.Result.GetProperty("status").TryGetProperty(failed?"Failure":"SuccessValue",out _));originals.Add(original.Result.Clone());outcomes.Add(outcome.Result.Clone());
        }
        for(var i=0;i<attempts.Count;i++)await Observe(attempts[i],i is 2 or 3);
        JsonElement Args(int index){var intent=originals[index].GetProperty("intent");Assert.AreEqual("put_record_checked",intent.GetProperty("methodName").GetString());return JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(intent.GetProperty("argsBase64").GetString()!));}
        var updated=Args(1);var tombstone=Args(4);
        var expected=new Dictionary<string,string>{{keys[0],tombstone.GetProperty("value").GetString()!},{keys[1],updated.GetProperty("archive_value").GetString()!},{keys[2],tombstone.GetProperty("archive_value").GetString()!}};
        Assert.AreEqual(keys[0],tombstone.GetProperty("key").GetString());Assert.AreEqual(keys[1],updated.GetProperty("archive_key").GetString());Assert.AreEqual(keys[2],tombstone.GetProperty("archive_key").GetString());
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY"));
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        var sdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;var type=sdk.GetType();
        async Task<object> Snapshot(string key){var task=(Task)type.GetMethod("ReadSnapshotAsync")!.Invoke(sdk,new object[]{key,CancellationToken.None})!;await task;return task.GetType().GetProperty("Result")!.GetValue(task)!;}
        string? Field(object snapshot,string name)=>(string?)snapshot.GetType().GetProperty(name)!.GetValue(snapshot);
        var hashes=new Dictionary<string,string>();
        for(var i=0;i<keys.Length;i++){
            var snapshot=await Snapshot(keys[i]);Assert.AreEqual(expected[keys[i]],Field(snapshot,"Ciphertext"),"Preserve modified fixture; do not delete it");
            var stored=OasisJson.DeserializeStored<Holon>(Field(snapshot,"Plaintext")!);Assert.AreEqual(id,stored.Id);Assert.AreEqual(i==0?3:i,stored.Version);
            Assert.AreEqual(i==0,stored.IsDeleted);Assert.AreEqual(i==1?"owned prepared encrypted history first":"owned prepared encrypted history second",stored.Description);
            hashes[keys[i]]=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expected[keys[i]])));
        }
        var reconciliation=Path.Combine(folder,stem+".reconciliation.json");Assert.IsFalse(File.Exists(reconciliation));
        using(var file=new FileStream(reconciliation,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{originals=attempts.Select(row=>row.ProviderRequestId).ToArray(),hashes,readOnlyVerified=true,plannedCleanupOnly=keys}));file.Flush(true);
        }
        for(var i=0;i<keys.Length;i++){
            var key=keys[i];Assert.AreEqual(expected[key],Field(await Snapshot(key),"Ciphertext"));
            var store=DispatchProxy.Create<IUsageOutboxStore,NearPreparedNativeTests.FlushedOwner>();var owner=(NearPreparedNativeTests.FlushedOwner)(object)store;owner.Journal=journal;
            var execution=(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="native-near-prepared-storage-reconciliation",Policy=new UsageEndpointPolicy()},store,new Web4UsageLedgerOptions(),null,null});
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var hash=await ((IPreparedNativeStorageProvider)provider).ExecutePreparedStorageAsync(()=>(Task<string>)type.GetMethod("DeleteAsync")!.Invoke(sdk,new object[]{key})!,execution,stem+"-cleanup-"+i,"test.near",timeout.Token);
            Assert.AreEqual(1,owner.Calls);Assert.AreEqual(owner.Original.ProviderRequestId,hash);attempts.Add(owner.Original);await Observe(owner.Original,false);
            Assert.IsNull(Field(await Snapshot(key),"Ciphertext"));
        }
        Assert.AreEqual(8,attempts.Count);Assert.AreEqual(8,attempts.Select(row=>row.ProviderRequestId).Distinct().Count());
        using(var file=new FileStream(complete,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{id,keys,hashes,attempts,outcomes,physicalFsyncOwner=true,syntheticAdmission=true,publicStorageInterfaces=true,
                encryptedCurrentAndHistory=true,verifiedFixturesAbsent=true,hostedMongoVerified=false,publicFunds=false,reconciledWithoutReplayingFirstFive=true}));file.Flush(true);
        }
        var retained=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToArray();
        CollectionAssert.AreEqual(attempts.Select(row=>row.ProviderRequestId).ToArray(),retained.Select(row=>row.ProviderRequestId).ToArray());File.Delete(journal);File.Delete(pending);
        Console.WriteLine("NEAR_PREPARED_STORAGE_RECONCILED "+JsonSerializer.Serialize(new{originals=attempts.Select(row=>row.ProviderRequestId).ToArray(),keys}));
    }

}
