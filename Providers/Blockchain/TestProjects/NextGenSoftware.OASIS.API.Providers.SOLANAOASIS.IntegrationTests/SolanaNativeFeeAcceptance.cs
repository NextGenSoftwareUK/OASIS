using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Solnet.Rpc;
using Solnet.Rpc.Types;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public sealed partial class SOLANAOASISIntegrationTests
{
    [TestMethod][TestCategory("NativeFeeReadOnly")]
    public async Task OriginalNativeFeeReceiptsMatchSdkMetadataWithoutSpending()
    {
        var rpc=ClientFactory.GetClient(Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!);
        var owner=Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!;
        var before=await rpc.GetBalanceAsync(owner,Commitment.Finalized);Assert.IsTrue(before.WasSuccessful);
        var work=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!;
        var reader=(IOASISNativeFeeReceiptProvider)_provider;
        foreach(var name in new[]{"spl-token-async-cost.json","spl-token-sync-cost.json"}){
            using var cost=JsonDocument.Parse(File.ReadAllText(Path.Combine(work,name)));
            ulong total=0;
            foreach(var signature in cost.RootElement.GetProperty("Signatures").EnumerateArray().Select(item=>item.GetString()!)){
                var receipt=await reader.GetNativeFeeReceiptAsync(signature);
                Assert.IsFalse(receipt.IsError,receipt.Exception?.ToString());Assert.IsTrue(receipt.IsLoaded);
                var evidence=receipt.Result;
                Assert.AreEqual(signature,evidence.GetProperty("transactionHash").GetString());
                Assert.AreEqual("F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj",evidence.GetProperty("chainIdentifier").GetString());
                Assert.AreEqual(owner,evidence.GetProperty("payer").GetString());Assert.IsTrue(evidence.GetProperty("success").GetBoolean());
                Assert.AreEqual("SOL",evidence.GetProperty("currency").GetString());Assert.AreEqual("lamport",evidence.GetProperty("unit").GetString());
                Assert.AreEqual("transaction-fee-only",evidence.GetProperty("coverage").GetString());
                var native=await rpc.GetTransactionAsync(signature,Commitment.Finalized);
                Assert.IsTrue(native.WasSuccessful);Assert.IsNull(native.Result.Meta.Error);
                var exact=ulong.Parse(evidence.GetProperty("netUnits").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
                Assert.AreEqual(Convert.ToUInt64(native.Result.Meta.Fee),exact);total=checked(total+exact);
            }
            Assert.AreEqual(cost.RootElement.GetProperty("FeeLamports").GetUInt64(),total);
        }
        var after=await rpc.GetBalanceAsync(owner,Commitment.Finalized);Assert.IsTrue(after.WasSuccessful);
        Assert.AreEqual(before.Result.Value,after.Result.Value,"Receipt reads must not spend payer funds.");
    }
}
