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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

// In-memory RPC client only: real SDK ABI/signing/receipt decode, no network or real chain.
[TestClass, DoNotParallelize]
public class Web3MintReceiptControlledTests
{
    [DataTestMethod, DataRow(false,false,false), DataRow(true,false,false),
        DataRow(false,true,false), DataRow(true,true,false), DataRow(false,true,true)]
    public async Task MintAndTransferRequireExactNativeEventAndOriginalIntent(bool noOp, bool transfer, bool unmined)
        => await Run(noOp,transfer,unmined,null,false);

    [DataTestMethod,DataRow(false,false),DataRow(true,false),DataRow(null,false),DataRow(false,true),DataRow(true,true),DataRow(null,true)]
    public async Task MintPublishesOnlyAdmittedMetadataAndUsesExplicitCollection(bool? inlineMetadata,bool explicitCollection)
        => await Run(false,false,false,inlineMetadata,explicitCollection);

    [DataTestMethod,DataRow(false,false),DataRow(true,false),DataRow(null,false),DataRow(false,true),DataRow(true,true),DataRow(null,true)]
    public async Task ArbitrumMintUsesCommonMetadataAndCollectionPolicy(bool? inlineMetadata,bool explicitCollection)
        => await Run(false,false,false,inlineMetadata,explicitCollection,arbitrum:true);

    [DataTestMethod,DataRow(false),DataRow(true)]
    public async Task OmittedRecipientAndPreparedConfiguredOwnerRemainValid(bool prepared)
        => await Run(false,false,false,true,false,prepared:prepared,omittedRecipient:true);

    [TestMethod]
    public async Task ForeignMintTargetCannotBypassAdmittedPreparation()
    {
        var (sdk,provider,rpc)=Harness();var persisted=false;
        var result=await sdk.WithNativePreparationAsync(()=>provider.MintNFTAsync(new MintWeb3NFTRequest{
            CollectionPublicKey="0x3333333333333333333333333333333333333333",SendToAddressAfterMinting="0x2222222222222222222222222222222222222222"}),
            evidence=>{persisted=true;return Task.CompletedTask;});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
        Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired,"Rejected before any native dispatch.");
        Assert.AreEqual(0,rpc.Requests);Assert.AreEqual(0,rpc.Submissions);Assert.IsFalse(persisted);
    }

    [DataTestMethod,DataRow(true,""),DataRow(true," "),DataRow(true,"invalid"),DataRow(true,"0x0000000000000000000000000000000000000000"),
        DataRow(false,""),DataRow(false," "),DataRow(false,"invalid"),DataRow(false,"0x0000000000000000000000000000000000000000"),DataRow(false,null)]
    public async Task InvalidMintIntentCannotReadNonceOrSign(bool collection,string? invalid)
    {
        var (_,provider,rpc)=Harness();var request=new MintWeb3NFTRequest{SendToAddressAfterMinting="0x2222222222222222222222222222222222222222"};
        if(collection)request.CollectionPublicKey=invalid;else request.SendToAddressAfterMinting=invalid;
        var result=await provider.MintNFTAsync(invalid==null?null!:request);
        Assert.IsTrue(result.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsNull(result.Result);Assert.AreEqual(0,rpc.Requests);Assert.AreEqual(0,rpc.Submissions);
    }

    private static (Web3CoreOASIS Sdk,Web3CoreOASISBaseProvider Provider,MintRpc Rpc) Harness()
    {
        const string key="1111111111111111111111111111111111111111111111111111111111111111";
        const string contract="0x1111111111111111111111111111111111111111";
        var client=DispatchProxy.Create<IClient,MintRpc>();var web3=new Web3(new Account(key,31337),client);
        var sdk=new Web3CoreOASIS(key,"http://127.0.0.1:1",contract,Web3CoreOASISBaseProviderHelper.Abi,31337);
        typeof(Web3CoreOASIS).GetField("_web3",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(sdk,web3);
        var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1",key,contract);
        typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,sdk);
        return(sdk,provider,(MintRpc)client);
    }

    private static async Task Run(bool noOp,bool transfer,bool unmined,bool? inlineMetadata,bool explicitCollection,
        bool arbitrum=false,bool prepared=false,bool omittedRecipient=false)
    {
        const string key = "1111111111111111111111111111111111111111111111111111111111111111";
        const string contract = "0x1111111111111111111111111111111111111111";
        const string suppliedRecipient = "0x2222222222222222222222222222222222222222";
        const string otherContract="0x3333333333333333333333333333333333333333";
        var target=explicitCollection?otherContract:contract;
        var account = new Account(key, 31337);
        var recipient=omittedRecipient?account.Address:suppliedRecipient;
        var client = DispatchProxy.Create<IClient, MintRpc>(); var rpc = (MintRpc)client;
        var web3 = new Web3(account, client);
        var sdk = new Web3CoreOASIS(key, "http://127.0.0.1:1", contract, Web3CoreOASISBaseProviderHelper.Abi, 31337);
        typeof(Web3CoreOASIS).GetField("_web3", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sdk, web3);
        Web3CoreOASISBaseProvider provider = arbitrum
            ?new ArbitrumOASIS.ArbitrumOASIS_Web3Core("http://127.0.0.1:1",key,contract,31337)
            :new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1", key, contract);
        typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, sdk);
        var request = new MintWeb3NFTRequest { SendToAddressAfterMinting = omittedRecipient?null!:recipient, MemoText = "original mint",
            CollectionPublicKey=explicitCollection?otherContract:null,StoreNFTMetaDataOnChain=inlineMetadata,
            JSONMetaData="{\"name\":\"Original oak 🌳\"}",JSONMetaDataURL="ipfs://original-metadata",
            MintedByAvatarId=Guid.NewGuid(),SendToAvatarAfterMintingEmail="private@example.invalid" };
        var send = new SendWeb3NFTRequest { TokenAddress = contract, TokenId = ((BigInteger.One << 256)-1).ToString(),
            FromWalletAddress = account.Address, ToWalletAddress = recipient, Amount = 1, MemoText = "original mint" };
        var abi = web3.Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi, contract);
        rpc.ExpectedData = transfer ? abi.GetFunction("sendNFT").GetData(account.Address,recipient,(BigInteger.One<<256)-1,"None","None",BigInteger.One,"original mint")
            : abi.GetFunction("mint").GetData(recipient, JsonConvert.SerializeObject(new {request.JSONMetaDataURL,JSONMetaData=inlineMetadata==true?request.JSONMetaData:null}));
        rpc.Sender = account.Address; rpc.Contract = target; rpc.Recipient = recipient; rpc.NoOp = noOp;
        rpc.Transfer = transfer; rpc.Unmined = unmined;
        rpc.BeforeNonce = () => { request.SendToAddressAfterMinting = contract; request.MemoText = "changed after admission";
            request.CollectionPublicKey=contract;request.JSONMetaData="changed";request.JSONMetaDataURL="changed";request.StoreNFTMetaDataOnChain=inlineMetadata!=true;
            send.ToWalletAddress = contract; send.MemoText = "changed after admission"; };
        var preparations=0;
        Task<NextGenSoftware.OASIS.Common.OASISResult<IWeb3NFTTransactionResponse>> Invoke()
            =>transfer?provider.SendNFTAsync(send):provider.MintNFTAsync(request);
        var result=prepared?await sdk.WithNativePreparationAsync(Invoke,evidence=>{
            preparations++;Assert.AreEqual(target,evidence.GetProperty("contract").GetString());
            Assert.AreEqual(rpc.ExpectedData,evidence.GetProperty("calldata").GetString());return Task.CompletedTask;
        }):await Invoke();
        Assert.AreEqual(prepared?1:0,preparations);
        Assert.AreEqual(1, rpc.Submissions, "Exactly one in-memory submission, never a public RPC. " + result.Exception?.ToString() + result.Message);
        if (noOp || unmined)
        {
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
            Assert.AreEqual(rpc.Hash, ((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId);
        }
        else
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message); Assert.IsTrue(result.IsSaved);
            Assert.AreEqual(((BigInteger.One << 256) - 1).ToString(), result.Result.Web3NFT.TokenId);
            Assert.AreEqual(target, result.Result.Web3NFT.NFTTokenAddress);
            Assert.AreEqual("original mint", result.Result.Web3NFT.MemoText);
            Assert.AreEqual(rpc.Hash, result.Result.TransactionResult);
        }
    }

    public class MintRpc : DispatchProxy
    {
        public Action? BeforeNonce;
        public string ExpectedData = "", Sender = "", Contract = "", Recipient = "", Hash = "";
        public bool NoOp, Transfer, Unmined;
        public int Submissions,Requests;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name.StartsWith("get_") || method.Name.StartsWith("set_")) return null;
            if (method.Name != "SendRequestAsync") throw new InvalidOperationException(method.Name);
            Requests++;
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
                    response = new { transactionHash = Hash, status = "0x1", blockNumber = Unmined ? null : "0x7", logs = NoOp ? Array.Empty<object>() : new object[] {
                        new { address = Contract, topics = new[] { "0x" + Nethereum.Util.Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)"),
                            Transfer ? "0x" + Sender.Substring(2).PadLeft(64,'0') : "0x" + new string('0',64), "0x" + Recipient.Substring(2).PadLeft(64,'0'), "0x" + new string('f',64) }, data = "0x" } } }; break;
                default: throw new InvalidOperationException("Unexpected controlled SDK RPC: " + request.Method);
            }
            var type = method.ReturnType.GetGenericArguments()[0];
            var decoded = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(response), type);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(type).Invoke(null, new[] { decoded });
        }
    }
}
