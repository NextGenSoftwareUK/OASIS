using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

// Real Nethereum JSON-RPC against a bounded controlled loopback responder, not a chain.
[TestClass, DoNotParallelize]
public class EthereumNftRequestSnapshotControlledTests
{
    private const string Url = "http://127.0.0.1:18662/";
    private const string Contract = "0x1111111111111111111111111111111111111111";
    private const string Other = "0x2222222222222222222222222222222222222222";
    // Public, never-funded unit-only identity; never signs or submits anything.
    private const string Key = "1111111111111111111111111111111111111111111111111111111111111111";

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task OriginalContractAndTokenSurviveSigningGateWait(bool burn)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(Url); listener.Start();
        var provider = new EthereumOASIS(Url, Key, 31337, Contract) { Web3Client = new Web3(Url) };
        typeof(EthereumOASIS).GetProperty("IsProviderActivated")!.SetValue(provider, true);
        var account = new Account(Key, 31337);
        var transfer = new SendWeb3NFTRequest { TokenAddress = Contract, FromNFTTokenAddress = Contract,
            FromWalletAddress = account.Address, ToWalletAddress = Other, TokenId = "1", Amount = 1, OwnerPrivateKey = Key };
        var deletion = new BurnWeb3NFTRequest { NFTTokenAddress = Contract, TokenId = "1",
            OwnerPrivateKey = Key, OwnerPublicKey = "", OwnerSeedPhrase = "" };
        var gate = (SemaphoreSlim)typeof(EthereumOASIS).GetField("_nftSendGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
        await gate.WaitAsync();
        var pending = burn ? provider.BurnNFTAsync(deletion) : provider.SendNFTAsync(transfer);
        Assert.IsFalse(pending.IsCompleted, "Must reach the actual signing gate.");
        transfer.TokenAddress = Other; transfer.TokenId = "2";
        deletion.NFTTokenAddress = Other; deletion.TokenId = "2";
        gate.Release();
        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var body = JsonNode.Parse(await new StreamReader(context.Request.InputStream).ReadToEndAsync())!;
        var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = body["id"]!.DeepClone(),
            ["error"] = new JsonObject { ["code"] = -32000, ["message"] = "Controlled owner read stopped before mutation" } };
        var bytes = Encoding.UTF8.GetBytes(response.ToJsonString());
        context.Response.ContentType = "application/json"; context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
        StringAssert.Contains(result.Message, "Controlled owner read stopped");
        Assert.AreEqual("eth_call", body["method"]!.GetValue<string>());
        Assert.AreEqual(Contract, body["params"]![0]!["to"]!.GetValue<string>());
        var data = body["params"]![0]!["data"]!.GetValue<string>();
        Assert.AreEqual("0x6352211e" + new string('0', 63) + "1", data, "Native uint256 ID must also remain original.");
    }
}
