using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class Web3NftRequestSnapshotTests
{
    private const string Contract = "0x1111111111111111111111111111111111111111";
    private const string Other = "0x2222222222222222222222222222222222222222";
    // Public, unfunded unit-only key. No RPC transport, signing or broadcast exists here.
    private const string Key = "1111111111111111111111111111111111111111111111111111111111111111";

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task TransferAdmissionUsesOriginalContractFieldsWhileWaiting(bool conflicting)
    {
        var provider = Create(out _);
        var request = new SendWeb3NFTRequest { TokenId = "1", Amount = 1,
            TokenAddress = conflicting ? Contract : "invalid-contract", FromNFTTokenAddress = conflicting ? Other : "" };
        var gate = Field<SemaphoreSlim>(provider, "_nftSigningGate");
        await gate.WaitAsync();
        var pending = provider.SendNFTAsync(request);
        Assert.IsFalse(pending.IsCompleted, "Must exercise the actual signing-gate await.");
        request.TokenAddress = Contract; request.FromNFTTokenAddress = Contract;
        gate.Release();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, conflicting ? "addresses conflict" : "address is invalid");
    }

    [TestMethod]
    public async Task BurnOwnerReadUsesOriginalContractWhileWaiting()
    {
        var provider = Create(out var rpc);
        typeof(Web3CoreOASISBaseProvider).GetProperty("IsProviderActivated")!.SetValue(provider, true);
        var request = new BurnWeb3NFTRequest { TokenId = "1", NFTTokenAddress = Contract,
            OwnerPrivateKey = Key, OwnerPublicKey = "", OwnerSeedPhrase = "" };
        var gate = Field<SemaphoreSlim>(provider, "_nftSigningGate");
        await gate.WaitAsync();
        var pending = provider.BurnNFTAsync(request);
        Assert.IsFalse(pending.IsCompleted);
        request.NFTTokenAddress = Other;
        gate.Release();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, rpc.Calls);
        Assert.AreEqual(Contract, rpc.Target, "Owner query must bind the original admitted contract.");
        StringAssert.Contains(result.Message, "Controlled owner read stopped");
    }

    private static Web3CoreOASISBaseProvider Create(out StopRpc rpc)
    {
        var provider = new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1", Key, Contract);
        var client = DispatchProxy.Create<IClient, StopRpc>(); rpc = (StopRpc)client;
        var web3 = new Web3(client);
        var sdk = new Web3CoreOASIS(Key, "http://127.0.0.1:1", Contract, Web3CoreOASISBaseProviderHelper.Abi);
        typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, sdk);
        typeof(Web3CoreOASISBaseProvider).GetField("_web3Client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, web3);
        return provider;
    }

    private static T Field<T>(object provider, string name) =>
        (T)typeof(Web3CoreOASISBaseProvider).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;

    public class StopRpc : DispatchProxy
    {
        public int Calls;
        public string? Target;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name.StartsWith("get_") || method.Name.StartsWith("set_")) return null;
            if (method.Name != "SendRequestAsync") throw new InvalidOperationException(method.Name);
            var request = (RpcRequest)args![0]!;
            Assert.AreEqual("eth_call", request.Method);
            Calls++; Target = JObject.FromObject(request.RawParameters[0])["to"]!.Value<string>();
            throw new IOException("Controlled owner read stopped; no external RPC or mutation.");
        }
    }
}
