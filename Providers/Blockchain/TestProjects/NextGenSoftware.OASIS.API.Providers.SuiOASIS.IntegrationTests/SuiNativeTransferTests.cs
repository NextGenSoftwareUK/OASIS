using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiNativeTransferTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task RetainedNativeBalanceSurfacesAgreeWithExactSdkUnitsWithoutMutation(bool recipient)
    {
        var rpc=Required("OASIS_SUI_TEST_RPC");Assert.IsTrue(new Uri(rpc).IsLoopback);
        var deploymentPath=Required("OASIS_SUI_TEST_DEPLOYMENT");
        using var deployment=System.Text.Json.JsonDocument.Parse(File.ReadAllText(deploymentPath));
        using var wallet=System.Text.Json.JsonDocument.Parse(File.ReadAllText(deploymentPath+".token-wallet.json"));
        var chain=deployment.RootElement.GetProperty("chainId").GetString()!;
        Assert.AreEqual(chain,wallet.RootElement.GetProperty("chainId").GetString());
        var address=recipient?wallet.RootElement.GetProperty("wallet").GetProperty("address").GetString()!
            :deployment.RootElement.GetProperty("address").GetString()!;
        // No signing key supplied: the entire test must remain read-only.
        using var provider=new SuiOASIS(rpc,"localnet",chainId:chain);
        var invoke=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        var raw=await (Task<System.Text.Json.JsonElement>)invoke.Invoke(provider,new object[]{"balance",new{walletAddress=address},CancellationToken.None})!;
        var units=raw.GetProperty("balance").GetString()!;
        StringAssert.Matches(units,new System.Text.RegularExpressions.Regex(@"\A(?:0|[1-9][0-9]*)\z"));
        Assert.IsTrue(System.Numerics.BigInteger.Parse(units,System.Globalization.CultureInfo.InvariantCulture)<=ulong.MaxValue);
        var exact=decimal.Parse(units,System.Globalization.CultureInfo.InvariantCulture)/1000000000m;
        var request=new GetWeb3WalletBalanceRequest{WalletAddress=address};
        var account=await provider.GetAccountBalanceAsync(address);Assert.IsFalse(account.IsError,account.Message);Assert.AreEqual(exact,account.Result);
        foreach(var legacy in new[]{provider.GetBalance(request),await provider.GetBalanceAsync(request)}){
            Assert.IsFalse(legacy.IsError,legacy.Message);Assert.AreEqual((double)exact,legacy.Result);
        }
        using var foreign=new SuiOASIS(rpc,"localnet",chainId:"not-this-genesis");
        Assert.IsTrue((await foreign.GetAccountBalanceAsync(address)).IsError);
        Assert.IsTrue(foreign.GetBalance(request).IsError);
        Assert.IsTrue((await foreign.GetBalanceAsync(request)).IsError);
        Console.WriteLine($"Retained wallet recipient={recipient}: exact native MIST={units}; decimal and sync/async legacy double surfaces agree; three wrong-chain reads rejected, no signing or mutation.");
    }

    [TestMethod]
    public async Task ConfirmedMoveAbortIsFailedNotLookupError()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback, "Never submit or query this fixture against a public chain.");
        using var provider = new SuiOASIS(rpc, "localnet");
        var result = await provider.GetTransactionStatusAsync(Required("OASIS_SUI_TEST_FAILED_DIGEST"));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(BridgeTransactionStatus.Failed, result.Result);
        StringAssert.Contains(result.Message, "false");
    }
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real Sui test setting {name} is required; no skipped or mocked evidence.");

    internal static async Task FundAsync(string address)
    {
        var path = Path.Combine(Path.GetDirectoryName(Required("OASIS_SUI_SDK_BRIDGE"))!, "test-fund.mjs");
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(path);
        start.ArgumentList.Add(address); // Public address only, never signing credentials.
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await output;
            Assert.AreEqual(0, process.ExitCode, await error);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    [TestMethod]
    public async Task OfficialSdkBackedProviderCommitsNativeTransferAndReadsConsensusStatus()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback, "Test must not spend public-chain assets.");
        var walletProvider = new SuiOASIS(rpc, "localnet");
        try
        {
            var sender = await walletProvider.GenerateKeyPairAsync();
            var recipient = await walletProvider.GenerateKeyPairAsync();
            Assert.IsFalse(sender.IsError, sender.Message);
            Assert.IsFalse(recipient.IsError, recipient.Message);
            await FundAsync(sender.Result.WalletAddressLegacy);
            var provider = new SuiOASIS(rpc, "localnet", privateKey: sender.Result.PrivateKey);
            try
            {
                var before = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsFalse(before.IsError, before.Message);
                var committed = await provider.SendTransactionAsync(sender.Result.WalletAddressLegacy, recipient.Result.WalletAddressLegacy, 1m);
                Assert.IsFalse(committed.IsError, committed.Message);
                Assert.IsFalse(string.IsNullOrWhiteSpace(committed.Result.TransactionResult));
                var status = await provider.GetTransactionStatusAsync(committed.Result.TransactionResult);
                Assert.IsFalse(status.IsError, status.Message);
                Assert.AreEqual(BridgeTransactionStatus.Completed, status.Result);
                var after = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsFalse(after.IsError, after.Message);
                Assert.AreEqual(1d, after.Result - before.Result);
                var exactBalance = await provider.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);
                Assert.IsFalse(exactBalance.IsError, exactBalance.Message);
                Assert.AreEqual((decimal)after.Result, exactBalance.Result);
                var wrongSender = await provider.SendTransactionAsync(recipient.Result.WalletAddressLegacy, sender.Result.WalletAddressLegacy, 1m);
                Assert.IsTrue(wrongSender.IsError);
                StringAssert.Contains(wrongSender.Message, "does not own sender");
            }
            finally { provider.Dispose(); }
            var wrongChain = new SuiOASIS(rpc, "localnet", chainId: "not-this-genesis");
            try
            {
                var rejected = await wrongChain.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsTrue(rejected.IsError, "Chain failure must not be reported as a successful zero balance.");
                StringAssert.Contains(rejected.Message, "chain identifier mismatch");
                var exactRejected = await wrongChain.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);
                Assert.IsTrue(exactRejected.IsError, "Decimal account balance must not hide a chain failure as zero.");
            }
            finally { wrongChain.Dispose(); }
        }
        finally { walletProvider.Dispose(); }
    }
}
