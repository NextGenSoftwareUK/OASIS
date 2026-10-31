using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solnet.Wallet;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass]
public sealed class SolanaVersionedFeeReadAcceptance
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod][TestCategory("NativeFeeReadOnly")]
    public async Task UnobservedPublicSignatureUsesSuccessfulNativeHistoryLookupWithoutSubmission()
    {
        var endpoint=Environment.GetEnvironmentVariable("OASIS_SOLANA_READONLY_RPC_URL")??"https://api.devnet.solana.com";
        var account=new Account();
        var provider=new SolanaOASIS(endpoint,account.PrivateKey.Key,account.PublicKey.Key);
        var signature=Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(new byte[64]);
        using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result=await provider.GetTransactionStatusAsync(signature,cancellation.Token);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(BridgeTransactionStatus.NotFound,result.Result);
        TestContext.WriteLine("Read-only original lookup returned no finalized transaction and no native history status: "+signature);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][TestCategory("NativeFeeReadOnly")]
    public async Task PublicFinalizedVersionedReceiptAndBridgeStatusMatchOriginalMetadataWithoutSigning(int transactionVersion)
    {
        var endpoint=Environment.GetEnvironmentVariable("OASIS_SOLANA_READONLY_RPC_URL")
            ?? "https://api.devnet.solana.com";
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=new Uri(endpoint)};
        async Task<JsonElement> Read(string method,object[] parameters)
        {
            using var response=await http.PostAsJsonAsync("",new{jsonrpc="2.0",id=1,method,@params=parameters},timeout.Token);
            Assert.AreEqual(HttpStatusCode.OK,response.StatusCode);
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            var envelope=document.RootElement;
            Assert.IsFalse(envelope.TryGetProperty("error",out _),envelope.ToString());
            Assert.AreEqual(1,envelope.GetProperty("id").GetInt32());
            return envelope.GetProperty("result").Clone();
        }
        var genesis=(await Read("getGenesisHash",Array.Empty<object>())).GetString();
        string? signature=Environment.GetEnvironmentVariable($"OASIS_SOLANA_V{transactionVersion}_READONLY_SIGNATURE");
        if(string.IsNullOrWhiteSpace(signature)){
            var slot=(await Read("getSlot",new object[]{new{commitment="finalized"}})).GetUInt64();
            var blocks=await Read("getBlocksWithLimit",new object[]{slot>32 ? slot-32:0,3,new{commitment="finalized"}});
            foreach(var blockSlot in blocks.EnumerateArray()){
                var block=await Read("getBlock",new object[]{blockSlot.GetUInt64(),new{
                    commitment="finalized",encoding="json",maxSupportedTransactionVersion=1,
                    transactionDetails="accounts",rewards=false}});
                foreach(var entry in block.GetProperty("transactions").EnumerateArray()){
                    if(entry.TryGetProperty("version",out var version) && version.ValueKind==JsonValueKind.Number && version.GetInt32()==transactionVersion){
                        signature=entry.GetProperty("transaction").GetProperty("signatures")[0].GetString();break;
                    }
                }
                if(signature is not null)break;
            }
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(signature),
            $"No v{transactionVersion} transaction in three finalized blocks; provide an existing public OASIS_SOLANA_V{transactionVersion}_READONLY_SIGNATURE. No transaction was submitted.");
        var native=await Read("getTransaction",new object[]{signature!,new{
            commitment="finalized",encoding="json",maxSupportedTransactionVersion=1}});
        Assert.AreEqual(transactionVersion,native.GetProperty("version").GetInt32());
        var account=new Account(); // Synthetic local constructor requirement; never signed, persisted or funded.
        var provider=new SolanaOASIS(endpoint,account.PrivateKey.Key,account.PublicKey.Key);
        var observed=await provider.GetNativeFeeReceiptAsync(signature!,timeout.Token);
        Assert.IsFalse(observed.IsError,observed.Exception?.ToString());Assert.IsTrue(observed.IsLoaded);
        var receipt=observed.Result;
        Assert.AreEqual(genesis,receipt.GetProperty("chainIdentifier").GetString());
        Assert.AreEqual(signature,receipt.GetProperty("transactionHash").GetString());
        Assert.AreEqual(native.GetProperty("slot").GetUInt64().ToString(),receipt.GetProperty("slot").GetString());
        Assert.AreEqual(native.GetProperty("meta").GetProperty("fee").GetUInt64().ToString(),receipt.GetProperty("netUnits").GetString());
        Assert.AreEqual(native.GetProperty("meta").GetProperty("err").ValueKind==JsonValueKind.Null,receipt.GetProperty("success").GetBoolean());
        Assert.AreEqual(native.GetProperty("transaction").GetProperty("message").GetProperty("accountKeys")[0].GetString(),receipt.GetProperty("payer").GetString());
        var bridge=await provider.GetTransactionStatusAsync(signature!,timeout.Token);
        Assert.IsFalse(bridge.IsError,bridge.Exception?.ToString());Assert.IsTrue(bridge.IsLoaded);
        Assert.AreEqual(native.GetProperty("meta").GetProperty("err").ValueKind==JsonValueKind.Null
            ?BridgeTransactionStatus.Completed:BridgeTransactionStatus.Canceled,bridge.Result);
        TestContext.WriteLine($"Read-only v{transactionVersion} fee and finalized bridge evidence: "+receipt.GetRawText());
    }
}
