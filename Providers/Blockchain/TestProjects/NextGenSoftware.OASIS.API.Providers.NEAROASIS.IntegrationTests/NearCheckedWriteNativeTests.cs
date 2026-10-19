using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearCheckedWriteNativeTests
{
    [TestMethod]
    public async Task RealSdkCheckedWritesRetainEncryptedSnapshotArchiveAndFailedOriginal()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));
        var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(serviceType,new object[]{Required("NEAROASIS_RPCENDPOINT"),Required("NEAROASIS_NETWORKID"),Required("NEAROASIS_CONTRACTADDRESS"),Required("NEAROASIS_ACCOUNTID"),Required("NEAROASIS_PRIVATEKEY"),Required("OASIS_NEAR_NODE"),Required("OASIS_NEAR_SDK_BRIDGE")})!;
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),Required("NEAROASIS_NETWORKID"),Required("NEAROASIS_CHAINID"),Required("NEAROASIS_CONTRACTADDRESS"),Required("NEAROASIS_ACCOUNTID"),Required("NEAROASIS_PRIVATEKEY"));
        async Task<object> Snapshot(string key){var task=(Task)serviceType.GetMethod("ReadSnapshotAsync")!.Invoke(service,new object[]{key})!;await task;return task.GetType().GetProperty("Result")!.GetValue(task)!;}
        string? Field(object snapshot,string name)=>(string?)snapshot.GetType().GetProperty(name)!.GetValue(snapshot);
        Task<string> Get(string key)=>(Task<string>)serviceType.GetMethod("GetAsync")!.Invoke(service,new object[]{key})!;
        Task<string> Write(object original,string json,string? archive=null,string? archived=null)=>(Task<string>)serviceType.GetMethod("PutRecordCheckedAsync")!.Invoke(service,new object?[]{original,json,archive,archived})!;
        var repository=new DirectoryInfo(Environment.CurrentDirectory);
        while(repository!=null&&!File.Exists(Path.Combine(repository.FullName,"Scripts","run_near_provider_evidence.ps1")))repository=repository.Parent;
        if(repository==null)throw new InvalidOperationException("Repository report root not found");
        var reports=Path.Combine(repository.FullName,"TestResults","NearRuntime");
        Directory.CreateDirectory(reports);string pending=Path.Combine(reports,"near-checked-sdk.pending.json"),complete=Path.Combine(reports,"near-checked-sdk.complete.json");
        if(File.Exists(pending))throw new InvalidOperationException("Original checked SDK write pending; reconcile before running again.");
        if(File.Exists(complete)){
            using var prior=JsonDocument.Parse(File.ReadAllText(complete));Assert.AreEqual("verified-fixtures-absent",prior.RootElement.GetProperty("phase").GetString());
            Assert.IsNull(await Get(prior.RootElement.GetProperty("key").GetString()!));Assert.IsNull(await Get(prior.RootElement.GetProperty("archive").GetString()!));
            using(var history=new FileStream(Path.Combine(reports,"near-checked-sdk-history.ndjson"),FileMode.Append,FileAccess.Write,FileShare.None)){history.Write(Encoding.UTF8.GetBytes(File.ReadAllText(complete)+Environment.NewLine));history.Flush(true);}File.Delete(complete);
        }
        var id=Guid.NewGuid();string key="holon:"+id,archive="history:"+key+":0000000001";var receipts=new List<object>();
        void Persist(string phase,bool first=false){using var file=new FileStream(pending,first?FileMode.CreateNew:FileMode.Create,FileAccess.Write,FileShare.None);var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{phase,id,key,archive,receipts}));file.Write(bytes);file.Flush(true);}
        async Task Receipt(string label,string hash,BridgeTransactionStatus expected){receipts.Add(new{label,hash});Persist("acknowledged-"+label);var observed=await provider.GetTransactionStatusAsync(hash);Assert.IsFalse(observed.IsError,observed.Message);Assert.AreEqual(expected,observed.Result);}
        Persist("planned",true);
        var absent=await Snapshot(key);Assert.IsNull(Field(absent,"Ciphertext"));Assert.IsNull(await Get(archive));
        var first=OasisJson.Serialize(new Holon{Id=id,Version=1,Description="owned synthetic encrypted C# native fixture"});
        Persist("before-create");await Receipt("create",await Write(absent,first),BridgeTransactionStatus.Completed);
        var original=await Snapshot(key);Assert.AreEqual(first,Field(original,"Plaintext"));Assert.IsFalse(Field(original,"Ciphertext")!.Contains("synthetic"));
        var second=OasisJson.Serialize(new Holon{Id=id,Version=2,Description="updated synthetic fixture"});
        Persist("before-replace");await Receipt("replace",await Write(original,second,archive,first),BridgeTransactionStatus.Completed);
        Assert.AreEqual(second,await Get(key));Assert.AreEqual(first,await Get(archive));
        Persist("before-stale");var stale=await Assert.ThrowsExceptionAsync<NearRecordWriteRejectedException>(()=>Write(original,first));
        StringAssert.Contains(stale.NativeStatusJson,"OASIS_RECORD_CONFLICT");await Receipt("stale",stale.OriginalTransactionId,BridgeTransactionStatus.Failed);
        var current=await Snapshot(key);var currentCiphertext=Field(current,"Ciphertext");
        Persist("before-archive-collision");var collision=await Assert.ThrowsExceptionAsync<NearRecordWriteRejectedException>(()=>Write(current,first,archive,second));
        StringAssert.Contains(collision.NativeStatusJson,"OASIS_ARCHIVE_CONFLICT");await Receipt("archive-collision",collision.OriginalTransactionId,BridgeTransactionStatus.Failed);
        Assert.AreEqual(currentCiphertext,Field(await Snapshot(key),"Ciphertext"));Assert.AreEqual(second,await Get(key));Assert.AreEqual(first,await Get(archive));
        foreach(var target in new[]{key,archive}){Persist("before-known-cleanup");var hash=await (Task<string>)serviceType.GetMethod("DeleteAsync")!.Invoke(service,new object[]{target})!;await Receipt("cleanup",hash,BridgeTransactionStatus.Completed);Assert.IsNull(await Get(target));}
        Persist("verified-fixtures-absent");File.Move(pending,complete);Console.WriteLine("NEAR_CHECKED_SDK_NATIVE_VERIFIED "+JsonSerializer.Serialize(new{id,key,archive,receipts}));
    }
}
