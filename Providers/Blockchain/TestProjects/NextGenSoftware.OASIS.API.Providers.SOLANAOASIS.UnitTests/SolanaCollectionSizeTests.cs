using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaCollectionSizeTests
{
    [DataTestMethod]
    [DataRow("finalized",false)] [DataRow("pending",false)]
    [DataRow("lost-reply",false)] [DataRow("simulation-failure",false)] [DataRow("missing-simulation",false)]
    [DataRow("finalized",true)] [DataRow("pending",true)]
    [DataRow("lost-reply",true)] [DataRow("simulation-failure",true)] [DataRow("missing-simulation",true)]
    public async Task CollectionSizeRequiresSimulationAndFinalityWithoutRebroadcast(string scenario,bool verify)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,CollectionProxy>();
        var proxy=(CollectionProxy)(object)rpc;proxy.Scenario=scenario;
        var service=new SolanaService(owner,rpc);
        Task<string> Mutate() => verify ? service.SetAndVerifyCollectionAsync(new Account().PublicKey.Key,new Account().PublicKey.Key)
            : service.SetCollectionSizeAsync(new Account().PublicKey.Key,19);
        if(scenario=="lost-reply")
        {
            var failure=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(Mutate);
            Assert.AreEqual(proxy.Signature,failure.OriginalTransactionId);Assert.AreSame(proxy.LostReply,failure.InnerException);
        }
        else if(scenario.Contains("simulation"))
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(Mutate);
        else
        {
            var confirmed=await Mutate();
            Assert.AreEqual(proxy.Signature,confirmed);
        }
        Assert.AreEqual(scenario.Contains("simulation") ? 0:1,proxy.Sends);
        Assert.AreEqual(scenario=="pending" ? 2:scenario=="finalized" ? 1:0,proxy.StatusReads);
        if(proxy.Signed!=null)
        {
            Assert.IsTrue(owner.Verify(proxy.Signed.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
            var instruction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed).Instructions.Single();
            Assert.AreEqual(verify ? 1:9,instruction.Data.Length);Assert.AreEqual(verify ? (byte)32:(byte)34,instruction.Data[0]);
            if(!verify) Assert.AreEqual(19UL,System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(instruction.Data.AsSpan(1)));
        }
    }

    [TestMethod]
    public async Task VerificationRetryPolicyNeverRebroadcastsUnknownNativeOutcome()
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,CollectionProxy>();
        var proxy=(CollectionProxy)(object)rpc;proxy.Scenario="lost-reply";
        var service=new SolanaService(owner,rpc);
        var pending=(Task<(string,string)>)typeof(SolanaService).GetMethod("VerifyCollectionWithRetryAsync",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(service,new object?[] {new Account().PublicKey.Key,new Account().PublicKey.Key,TimeSpan.FromSeconds(2),TimeSpan.FromMilliseconds(10)})!;
        var failure=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(()=>pending);
        Assert.AreEqual(proxy.Signature,failure.OriginalTransactionId);Assert.AreSame(proxy.LostReply,failure.InnerException);
        Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
    }

    [DataTestMethod]
    [DataRow("finalized")] [DataRow("pending")] [DataRow("lost-reply")]
    public async Task MetadataFreezeUsesFinalizedSubmissionAndRetainsUnknownReceipt(string scenario)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,CollectionProxy>();
        var proxy=(CollectionProxy)(object)rpc;proxy.Scenario=scenario;
        var service=new SolanaService(owner,rpc);
        var pending=(Task)typeof(SolanaService).GetMethod("FreezeMetadataAsync",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(service,new object[] {new Account().PublicKey.Key})!;
        if(scenario=="lost-reply")
        {
            var failure=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(()=>pending);
            Assert.AreEqual(proxy.Signature,failure.OriginalTransactionId);Assert.AreSame(proxy.LostReply,failure.InnerException);
        }
        else await pending;
        Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(scenario=="pending" ? 2:scenario=="finalized" ? 1:0,proxy.StatusReads);
        var instruction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!).Instructions.Single();
        CollectionAssert.AreEqual(new byte[] {15,0,0,0,1,0},instruction.Data);
        Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    public class CollectionProxy:DispatchProxy
    {
        public string Scenario="",Signature="";public byte[]? Signed;public int Sends,StatusReads;
        public IOException LostReply=new("owned lost broadcast reply");
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var resultType=method!.ReturnType.GetGenericArguments()[0];var valueType=resultType.GetGenericArguments()[0];
            string json;
            switch(method.Name)
            {
                case "GetLatestBlockHashAsync":json=JsonConvert.SerializeObject(new {Value=new {Blockhash=Encoders.Base58.EncodeData(new byte[32])}});break;
                case "SimulateTransactionAsync":json=Scenario=="missing-simulation" ? "null":"{\"value\":{}}";break;
                case "SendTransactionAsync":
                    Sends++;Signed=(byte[])args![0]!;Signature=Encoders.Base58.EncodeData(Signed.AsSpan(1,64).ToArray());
                    if(Scenario=="lost-reply") throw LostReply;
                    json=JsonConvert.SerializeObject(Signature);break;
                case "GetSignatureStatusesAsync":
                    StatusReads++;json=JsonConvert.SerializeObject(new {Value=new[] {new {confirmationStatus=Scenario=="pending" && StatusReads==1 ? "confirmed":"finalized"}}});break;
                default:throw new InvalidOperationException("Unexpected RPC "+method.Name);
            }
            var response=Activator.CreateInstance(resultType,new HttpResponseMessage(System.Net.HttpStatusCode.OK),JsonConvert.DeserializeObject(json,valueType));
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,!(method.Name=="SimulateTransactionAsync" && Scenario=="simulation-failure"));
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}
