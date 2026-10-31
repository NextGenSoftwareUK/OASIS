using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class EthereumMintOutcomeControlledTests
{
    private const string Url = "http://127.0.0.1:18662/";
    private const string Contract = "0x1111111111111111111111111111111111111111";
    private const string Recipient = "0x2222222222222222222222222222222222222222";
    private const string Key = "1111111111111111111111111111111111111111111111111111111111111111";

    [DataTestMethod,
        DataRow("ack-loss","mint"), DataRow("receipt-loss","mint"), DataRow("foreign-receipt","mint"), DataRow("no-op","mint"),
        DataRow("success","mint"), DataRow("reverted","mint"), DataRow("missing-status","mint"), DataRow("reverted-unmined","mint"),
        DataRow("success-zero","mint"), DataRow("success-max","mint"),
        DataRow("ack-loss","send"), DataRow("receipt-loss","send"), DataRow("foreign-receipt","send"), DataRow("no-op","send"),
        DataRow("success","send"), DataRow("reverted","send"), DataRow("missing-status","send"), DataRow("reverted-unmined","send"),
        DataRow("ack-loss","burn"), DataRow("receipt-loss","burn"), DataRow("foreign-receipt","burn"), DataRow("no-op","burn"),
        DataRow("success","burn"), DataRow("reverted","burn"), DataRow("missing-status","burn"), DataRow("reverted-unmined","burn")]
    public async Task NftOutcomesPreserveNativeIdentityAndReplayClassification(string mode, string operation)
    {
        using var listener = new HttpListener(); listener.Prefixes.Add(Url); listener.Start();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var provider = new MintProvider { Web3Client = new Web3(Url) };
        typeof(EthereumOASIS).GetProperty("IsProviderActivated")!.SetValue(provider, true);
        string hash = "", token = mode=="success-max"?new string('f',64):mode=="success-zero"?new string('0',64):new string('0',63) + "1"; var submissions = 0;
        var sender = new Account(Key,31337).Address;
        var mintIntent = new MintWeb3NFTRequest { MintedByAvatarId=Guid.NewGuid(),SendToAddressAfterMinting=Recipient,
            JSONMetaData="{\"name\":\"Original oak 🌳\"}",JSONMetaDataURL="ipfs://original",StoreNFTMetaDataOnChain=true,MemoText="original mint memo" };
        const string mintAbi="[{\"type\":\"function\",\"name\":\"mint\",\"inputs\":[{\"name\":\"to\",\"type\":\"address\"},{\"name\":\"metadataJson\",\"type\":\"string\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"}]";
        var metadata=Newtonsoft.Json.JsonConvert.SerializeObject(new {mintIntent.JSONMetaDataURL,mintIntent.JSONMetaData});
        var expectedMintData=provider.Web3Client.Eth.GetContract(mintAbi,Contract).GetFunction("mint").GetData(Recipient,metadata);
        var serving = Task.Run(async () =>
        {
            for (var i = 0; i < 16; i++)
            {
                var context = await listener.GetContextAsync().WaitAsync(limit.Token);
                var request = JsonNode.Parse(await new StreamReader(context.Request.InputStream).ReadToEndAsync())!;
                var method = request["method"]!.GetValue<string>(); JsonNode? response; var error = false; var terminal = false;
                switch (method)
                {
                    case "eth_call": terminal = submissions > 0; response = JsonValue.Create("0x" + (submissions > 0 && mode != "no-op" ? Recipient : sender)[2..].PadLeft(64,'0')); break;
                    case "eth_estimateGas":
                        // This fixture's contract allocates token1; metadata bytes
                        // are not an explicit token-ID argument to the mint call.
                        Assert.AreEqual(expectedMintData,request["params"]![0]!["data"]!.GetValue<string>());
                        mintIntent.JSONMetaData="changed after preflight";mintIntent.JSONMetaDataURL="changed";mintIntent.MemoText="changed";
                        response = JsonValue.Create("0x493e0"); break;
                    case "eth_getTransactionCount": response = JsonValue.Create("0x0"); break;
                    case "eth_chainId": response = JsonValue.Create("0x7a69"); break;
                    case "eth_gasPrice": case "eth_maxPriorityFeePerGas": response = JsonValue.Create("0x1"); break;
                    case "eth_getBlockByNumber": response = JsonNode.Parse("{\"number\":\"0x7\",\"gasLimit\":\"0x1c9c380\",\"baseFeePerGas\":\"0x1\"}"); break;
                    case "eth_feeHistory": response = JsonNode.Parse("{\"oldestBlock\":\"0x1\",\"baseFeePerGas\":[\"0x1\",\"0x1\"],\"gasUsedRatio\":[0.5],\"reward\":[[\"0x1\"]]}"); break;
                    case "eth_sendRawTransaction":
                        if(operation=="mint")StringAssert.Contains(request["params"]![0]!.GetValue<string>().ToLowerInvariant(),expectedMintData[2..].ToLowerInvariant(),
                            "The actual SDK-signed transaction must contain the same admitted metadata calldata as preflight.");
                        submissions++; hash = "0x" + Sha3Keccack.Current.CalculateHash(request["params"]![0]!.GetValue<string>().HexToByteArray()).ToHex();
                        // Signature is consumed only here, never persisted/relayed to a chain.
                        error = terminal = mode == "ack-loss";
                        response = error ? Error() : JsonValue.Create(hash); break;
                    case "eth_getTransactionReceipt":
                        Assert.AreEqual(hash, request["params"]![0]!.GetValue<string>());
                        terminal = operation != "send"; error = mode == "receipt-loss";
                        response = error ? Error() : new JsonObject { ["transactionHash"] = mode == "foreign-receipt" ? "0x" + new string('b',64) : hash,
                            ["status"] = mode == "missing-status" ? null : mode.StartsWith("reverted") ? "0x0" : "0x1", ["blockNumber"] = mode == "reverted-unmined" ? null : "0x7", ["logs"] = mode == "no-op" ? new JsonArray() : new JsonArray(new JsonObject {
                                ["address"] = Contract, ["data"] = "0x", ["topics"] = new JsonArray(
                                    "0x" + Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)"), operation == "mint" ? "0x" + new string('0',64) : "0x" + sender[2..].PadLeft(64,'0'),
                                    operation == "burn" ? "0x" + new string('0',64) : "0x" + Recipient[2..].PadLeft(64,'0'), "0x" + token) }) }; break;
                    default: throw new InvalidOperationException("Unexpected controlled RPC: " + method);
                }
                var envelope = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = request["id"]!.DeepClone(), [error ? "error" : "result"] = response };
                var bytes = Encoding.UTF8.GetBytes(envelope.ToJsonString());
                context.Response.ContentType = "application/json"; context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, limit.Token); context.Response.Close();
                if (terminal) return;
            }
            Assert.Fail("Controlled RPC request bound exceeded.");
        });
        var pending = operation == "mint"
            ? provider.MintNFTAsync(mintIntent)
            : operation == "send" ? provider.SendNFTAsync(new SendWeb3NFTRequest { TokenAddress = Contract, FromWalletAddress = sender,
                ToWalletAddress = Recipient, TokenId = "1", Amount = 1, OwnerPrivateKey = Key })
            : provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = Contract, TokenId = "1", OwnerPrivateKey = Key, OwnerPublicKey = "", OwnerSeedPhrase = "" });
        await pending.WaitAsync(limit.Token);
        limit.Cancel();
        try { await serving; }
        catch (OperationCanceledException) when (limit.IsCancellationRequested) { /* Owned responder teardown after the provider completed. */ }
        var result = await pending;
        Assert.AreEqual(1, submissions);
        if (mode.StartsWith("success",StringComparison.Ordinal))
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message); Assert.IsTrue(result.IsSaved);
            Assert.AreEqual(hash, result.Result.TransactionResult); Assert.AreEqual(Contract, result.Result.Web3NFT.NFTTokenAddress);
            Assert.AreEqual(System.Numerics.BigInteger.Parse("0" + token, System.Globalization.NumberStyles.AllowHexSpecifier).ToString(), result.Result.Web3NFT.TokenId);
            if(operation=="mint")Assert.AreEqual("original mint memo",result.Result.Web3NFT.MemoText);
            return;
        }
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
        if (mode == "reverted")
        {
            Assert.AreEqual("ETHEREUM_NFT_REVERTED", result.ErrorCode); Assert.AreEqual(hash, result.Result.TransactionResult);
            Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired, "Known matching revert is not unknown execution.");
            return;
        }
        Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
        var original = ((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId;
        if (mode == "ack-loss") Assert.IsTrue(string.IsNullOrEmpty(original), "Lost SDK submission acknowledgement must not invent a returned hash.");
        else Assert.AreEqual(hash, original, "Keep original submission identity, not foreign receipt identity.");
    }

    private static JsonObject Error() => new() { ["code"] = -32000, ["message"] = "Controlled mint response loss" };
    private sealed class MintProvider : EthereumOASIS
    {
        public MintProvider() : base(Url, Key, 31337, Contract) { }
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId) => new() { Result = new() { Key } };
    }
}
