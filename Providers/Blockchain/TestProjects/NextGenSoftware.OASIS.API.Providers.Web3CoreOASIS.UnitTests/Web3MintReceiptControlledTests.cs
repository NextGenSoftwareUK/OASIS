using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

// In-memory RPC client only: real SDK ABI/signing/receipt decode, no network or real chain.
[TestClass, DoNotParallelize]
public class Web3MintReceiptControlledTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task MintRequiresExactNativeEventAndReturnsOriginalIntent(bool noOp)
    {
        const string key = "1111111111111111111111111111111111111111111111111111111111111111";
        const string contract = "0x1111111111111111111111111111111111111111";
        const string recipient = "0x2222222222222222222222222222222222222222";
        var account = new Account(key, 31337);
        var client = DispatchProxy.Create<IClient, MintRpc>(); var rpc = (MintRpc)client;
        var web3 = new Web3(account, client);
        var sdk = new Web3CoreOASIS(key, "http://127.0.0.1:1", contract, Web3CoreOASISBaseProviderHelper.Abi, 31337);
        typeof(Web3CoreOASIS).GetField("_web3", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sdk, web3);
        var provider = new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1", key, contract);
        typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, sdk);
        var request = new MintWeb3NFTRequest { SendToAddressAfterMinting = recipient, MemoText = "original mint" };
        rpc.ExpectedData = web3.Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi, contract).GetFunction("mint")
            .GetData(recipient, JsonConvert.SerializeObject(request));
        rpc.Sender = account.Address; rpc.Contract = contract; rpc.Recipient = recipient; rpc.NoOp = noOp;
        rpc.BeforeNonce = () => { request.SendToAddressAfterMinting = contract; request.MemoText = "changed after admission"; };
        var result = await provider.MintNFTAsync(request);
        Assert.AreEqual(1, rpc.Submissions, "Exactly one in-memory submission, never a public RPC. " + result.Exception?.ToString() + result.Message);
        if (noOp)
        {
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
            Assert.AreEqual(rpc.Hash, ((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId);
        }
        else
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message); Assert.IsTrue(result.IsSaved);
            Assert.AreEqual(((BigInteger.One << 256) - 1).ToString(), result.Result.Web3NFT.TokenId);
            Assert.AreEqual(contract, result.Result.Web3NFT.NFTTokenAddress);
            Assert.AreEqual("original mint", result.Result.Web3NFT.MemoText);
            Assert.AreEqual(rpc.Hash, result.Result.TransactionResult);
        }
    }

    public class MintRpc : DispatchProxy
    {
        public Action? BeforeNonce;
        public string ExpectedData = "", Sender = "", Contract = "", Recipient = "", Hash = "";
        public bool NoOp;
        public int Submissions;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name.StartsWith("get_") || method.Name.StartsWith("set_")) return null;
            if (method.Name != "SendRequestAsync") throw new InvalidOperationException(method.Name);
            var request = (RpcRequest)args![0]!; object response;
            switch (request.Method)
            {
                case "eth_getTransactionCount": BeforeNonce?.Invoke(); response = "0x0"; break;
                case "eth_gasPrice": response = "0x1"; break;
                case "eth_chainId": response = "0x7a69"; break;
                case "eth_getBlockByNumber": response = new { number = "0x7", gasLimit = "0x1c9c380", baseFeePerGas = "0x1" }; break;
                case "eth_maxPriorityFeePerGas": response = "0x1"; break;
                case "eth_feeHistory": response = new { oldestBlock = "0x1", baseFeePerGas = new[] { "0x1", "0x1" },
                    gasUsedRatio = new[] { 0.5 }, reward = new[] { new[] { "0x1" } } }; break;
                case "eth_sendRawTransaction":
                    var bytes = ((string)request.RawParameters[0]).HexToByteArray();
                    Hash = Web3SignedPreparation.Hash(bytes);
                    Web3SignedPreparation.Verify(bytes, Hash, 31337, Sender, Contract, ExpectedData.HexToByteArray());
                    Submissions++; response = Hash; break;
                case "eth_getTransactionReceipt":
                    response = new { transactionHash = Hash, status = "0x1", blockNumber = "0x7", logs = NoOp ? Array.Empty<object>() : new object[] {
                        new { address = Contract, topics = new[] { "0x" + Nethereum.Util.Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)"),
                            "0x" + new string('0',64), "0x" + Recipient.Substring(2).PadLeft(64,'0'), "0x" + new string('f',64) }, data = "0x" } } }; break;
                default: throw new InvalidOperationException("Unexpected controlled SDK RPC: " + request.Method);
            }
            var type = method.ReturnType.GetGenericArguments()[0];
            var decoded = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(response), type);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(type).Invoke(null, new[] { decoded });
        }
    }
}
