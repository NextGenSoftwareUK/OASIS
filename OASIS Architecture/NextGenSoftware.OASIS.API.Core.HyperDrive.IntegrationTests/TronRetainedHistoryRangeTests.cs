using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronRetainedHistoryRangeTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task ExistingSolidifiedOriginalUsesInclusiveBlockRangeWithoutMutation()
    {
        Assert.Equal("ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12",Environment.GetEnvironmentVariable("OASIS_TRON_TEST_OWNED_CONTAINER"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableTronInspection.ps1")))directory=directory.Parent;
        Assert.NotNull(directory);var root=directory.FullName;
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var old=Path.Combine(root,"TestResults/HyperDriveTron/pending-native-outcome.json");
        var pending=Path.Combine(root,"TestResults/HyperDriveTron/independent-prepared-native-pending.json");
        const string oldHash="9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE";
        const string pendingHash="2494A7BBEA823D2E0BE11A2869EDA891A23E1FD64ABABE975F75DDAC5A743070";
        Assert.Equal(oldHash,Hash(old));Assert.Equal(pendingHash,Hash(pending));
        const string original="a277665570415c9f16f48dc09642f77b43a0e967d40aab02eae97cf0681f39a3";
        Assert.Equal(original,File.ReadAllText(Path.Combine(root,"TestResults/TronRuntime/account-create-id.txt")).Trim());
        const string owner="TMVQGm1qAQYVdetCeGRRkTWYYrLXuHK2HC";
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));var token=deadline.Token;
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f");
        var client=runtime.NativeTransfers;var before=await client.GetNativeBalanceAsync(owner,token);
        var receipt=await client.GetNativeFeeReceiptAsync(original,token);Assert.True(receipt.GetProperty("success").GetBoolean());
        Assert.Equal(owner,receipt.GetProperty("payer").GetString());var height=checked((ulong)receipt.GetProperty("blockNumber").GetInt64());Assert.True(height>0);
        var all=Assert.Single(await client.ReadWalletTransactionsAsync(owner,new[]{original},token));
        var exact=Assert.Single(await client.ReadWalletTransactionsAsync(owner,new[]{original},height,height,token));
        Assert.Equal(original,exact.NativeTransactionId);Assert.Equal(all.TransactionId,exact.TransactionId);
        Assert.Equal("AccountCreateContract",exact.NativeOperation);Assert.Equal("Success",exact.NativeExecutionResult);
        Assert.Equal("0",exact.RawAmount);Assert.Equal("SUN",exact.AmountUnit);Assert.Equal(owner,exact.FromWalletAddress);
        Assert.Empty(await client.ReadWalletTransactionsAsync(owner,new[]{original},height+1,ulong.MaxValue,token));
        Assert.Empty(await client.ReadWalletTransactionsAsync(owner,new[]{original},0,height-1,token));
        Assert.Empty(await client.ReadWalletTransactionsAsync(owner,new[]{original},0,0,token));
        Assert.Empty(await client.ReadWalletTransactionsAsync(owner,new[]{original},ulong.MaxValue,ulong.MaxValue,token));
        Assert.Equal(before,await client.GetNativeBalanceAsync(owner,token));
        Assert.Equal(receipt.GetRawText(),(await client.GetNativeFeeReceiptAsync(original,token)).GetRawText());
        Assert.Equal(oldHash,Hash(old));Assert.Equal(pendingHash,Hash(pending));
        output.WriteLine(JsonSerializer.Serialize(new{original,block=height,receipt,exact.NativeOperation,exact.RawAmount,
            verifiedCandidateCount=1,rangeChecks=5,newNativeSubmissions=0,newFee=0,originalBalanceUnchanged=true,
            nativeIndexerCompletenessProven=false,hostedAcceptance=false}));
    }
}
