using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearSignedAcknowledgementTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public async Task RealSignedCallAndSelfTransferHaveBoundFinalAcknowledgements()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));
        Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));
        Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var type=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(type,new object[]{Required("NEAROASIS_RPCENDPOINT"),Required("NEAROASIS_NETWORKID"),"test.near","test.near",Required("NEAROASIS_PRIVATEKEY"),Required("OASIS_NEAR_NODE"),Required("OASIS_NEAR_SDK_BRIDGE")})!;
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY"));
        var root=new DirectoryInfo(Environment.CurrentDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;
        Assert.IsNotNull(root);
        var reports=Path.Combine(root.FullName,"TestResults","NearRuntime");
        var pending=Path.Combine(reports,"near-signed-ack.pending.json");
        var complete=Path.Combine(reports,"near-signed-ack.complete.json");
        Assert.IsFalse(File.Exists(pending)||File.Exists(complete),"Preserve/reconcile original evidence before another signed run.");
        const string token="oasis-signed-ack-probe";
        const string metadata="{\"purpose\":\"public synthetic acknowledgement verification\"}";
        var receipts=new List<object>();
        void Persist(string phase,bool first=false){using var file=new FileStream(pending,first?FileMode.CreateNew:FileMode.Create,FileAccess.Write,FileShare.None);file.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{phase,token,metadata,receipts})));file.Flush(true);}
        Task<JsonElement> View()=> (Task<JsonElement>)type.GetMethod("ViewAsync")!.Invoke(service,new object?[]{"nft_token",new{token_id=token},null})!;
        Task<string> Call(string method,object args)=>(Task<string>)type.GetMethod("CallAsync")!.Invoke(service,new object?[]{method,args,null,"0",null,null})!;
        async Task Observe(string label,string hash){receipts.Add(new{label,hash});Persist("acknowledged-"+label);var result=await provider.GetTransactionStatusAsync(hash);Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(BridgeTransactionStatus.Completed,result.Result);}
        Assert.AreEqual(JsonValueKind.Null,(await View()).ValueKind,"Fixed fixture must be absent; never overwrite a prior NFT.");
        Persist("planned",true);
        Persist("before-self-transfer");
        var transfer=(Task<string>)type.GetMethod("TransferAsync")!.Invoke(service,new object?[]{"test.near","1",null,null})!;
        await Observe("self-transfer",await transfer);
        Persist("before-mint");
        await Observe("mint",await Call("nft_mint",new{token_id=token,owner_id="test.near",metadata_json=metadata,precision_probe=long.MaxValue}));
        var nft=await View();
        Assert.AreEqual(token,nft.GetProperty("token_id").GetString());
        Assert.AreEqual("test.near",nft.GetProperty("owner_id").GetString());
        Assert.AreEqual(metadata,nft.GetProperty("metadata_json").GetString());
        Assert.IsFalse(nft.GetProperty("locked").GetBoolean());
        Persist("before-known-burn");
        await Observe("burn",await Call("nft_burn",new{token_id=token}));
        Assert.AreEqual(JsonValueKind.Null,(await View()).ValueKind);
        Persist("verified-fixture-absent");File.Move(pending,complete);
        Console.WriteLine("NEAR_SIGNED_ACK_VERIFIED "+JsonSerializer.Serialize(new{token,receipts}));
    }
}
