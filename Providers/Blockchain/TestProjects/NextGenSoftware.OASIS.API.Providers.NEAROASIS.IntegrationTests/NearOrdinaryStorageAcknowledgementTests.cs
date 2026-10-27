using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearOrdinaryStorageAcknowledgementTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public async Task RealEncryptedPutAndDeleteRequireBoundFinalOriginals()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));
        Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));
        Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var type=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(type,new object[]{Required("NEAROASIS_RPCENDPOINT"),Required("NEAROASIS_NETWORKID"),
            "test.near","test.near",Required("NEAROASIS_PRIVATEKEY"),Required("OASIS_NEAR_NODE"),Required("OASIS_NEAR_SDK_BRIDGE")})!;
        var root=new DirectoryInfo(Environment.CurrentDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;
        Assert.IsNotNull(root);
        Assert.IsFalse(File.Exists(Path.Combine(root.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence","near-ordinary-storage.complete.json")),
            "Published original evidence already exists; never repeat this cohort.");
        var folder=Path.Combine(root.FullName,"TestResults","NearRuntime");
        var pending=Path.Combine(folder,"near-ordinary-storage.pending.json");
        var complete=Path.Combine(folder,"near-ordinary-storage.complete.json");
        Assert.IsFalse(File.Exists(pending)||File.Exists(complete),"Preserve original evidence; never replay a completed or partial sequence.");
        const string key="holon:cb7486a8-9f90-45ae-9418-8827d43550e1";
        const string plaintext="{\"purpose\":\"synthetic ordinary acknowledgement\",\"private\":\"Café\"}";
        var receipts=new List<JsonElement>();
        void Persist(string phase,bool first=false) {
            using var file=new FileStream(pending,first?FileMode.CreateNew:FileMode.Create,FileAccess.Write,FileShare.None);
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{phase,key,receipts}));file.Flush(true);
        }
        Task<string> Read()=>(Task<string>)type.GetMethod("GetAsync")!.Invoke(service,new object[]{key})!;
        Task<JsonElement> Raw()=>(Task<JsonElement>)type.GetMethod("ViewAsync")!.Invoke(service,new object?[]{"get",new{key},null})!;
        async Task Observe(string hash) {
            using var response=await (Task<JsonDocument>)type.GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!
                .Invoke(service,new object?[]{"transactionStatus",new{transactionHash=hash,signerAccountId="test.near"},CancellationToken.None})!;
            var outcome=response.RootElement.GetProperty("outcome");
            Assert.AreEqual(hash,outcome.GetProperty("transaction_outcome").GetProperty("id").GetString());
            Assert.AreEqual("FINAL",outcome.GetProperty("final_execution_status").GetString());
            Assert.IsTrue(outcome.GetProperty("status").TryGetProperty("SuccessValue",out _));
            receipts.Add(outcome.Clone());Persist("acknowledged-"+receipts.Count);
        }
        Assert.IsNull(await Read(),"Fixed new fixture must be absent before writing.");
        Persist("before-put",true);
        var put=await (Task<string>)type.GetMethod("PutAsync")!.Invoke(service,new object[]{key,plaintext})!;
        await Observe(put);
        Assert.AreEqual(plaintext,await Read());
        var encrypted=(await Raw()).GetString();Assert.IsFalse(string.IsNullOrEmpty(encrypted));
        Assert.IsFalse(encrypted.Contains("synthetic ordinary"));Assert.IsFalse(encrypted.Contains("Café"));
        Persist("before-delete");
        var deleted=await (Task<string>)type.GetMethod("DeleteAsync")!.Invoke(service,new object[]{key})!;
        await Observe(deleted);Assert.IsNull(await Read());Assert.AreEqual(JsonValueKind.Null,(await Raw()).ValueKind);
        Assert.AreNotEqual(put,deleted);Assert.AreEqual(2,receipts.Count);
        Persist("verified-fixture-absent");File.Move(pending,complete);
        Console.WriteLine("NEAR_ORDINARY_STORAGE_ACK_VERIFIED "+JsonSerializer.Serialize(new{key,put,deleted}));
    }
}
