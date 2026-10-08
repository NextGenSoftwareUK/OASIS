using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosTransferTests
{
    // Public local-only development mnemonic. No public network or real assets.
    private static readonly string OwnerKey = string.Join(" ", Enumerable.Repeat("abandon", 23)) + " art";
    private const string Rpc = "http://127.0.0.1:26657";
    private const string Chain = "oasis-cosmos-local";

    private static async Task<JsonElement> InvokeSdk(string operation, object args)
    {
        var bridge = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE")
            ?? throw new InvalidOperationException("Set OASIS_COSMOS_SDK_BRIDGE to the reusable installed bridge.");
        var request = new Dictionary<string, object>
        {
            ["rpcEndpoint"] = Rpc, ["chainId"] = Chain, ["operation"] = operation,
            ["privateKey"] = OwnerKey, ["addressPrefix"] = "wasm", ["gasPrice"] = "0.025stake"
        };
        foreach (var property in args.GetType().GetProperties()) request[property.Name] = property.GetValue(args)!;
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(bridge);
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request));
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            using var envelope = JsonDocument.Parse(await output);
            Assert.AreEqual(0, process.ExitCode, await error);
            Assert.IsTrue(envelope.RootElement.GetProperty("ok").GetBoolean(), envelope.RootElement.ToString());
            return envelope.RootElement.GetProperty("result").Clone();
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    private static CosmosBlockChainOASIS Provider(string chain = Chain, int decimals = 0)
        => new(Rpc, chain, OwnerKey, "", "wasm", "0.025stake", "stake", decimals);

    [TestMethod]
    public async Task SignedProviderTransferCommitsAndChangesRealBankBalance()
    {
        var owner = (await InvokeSdk("restoreKey", new { })).GetProperty("address").GetString()!;
        var recipient = (await InvokeSdk("restoreKey", new { privateKey = new string('0', 63) + "2" }))
            .GetProperty("address").GetString()!;
        var before = long.Parse((await InvokeSdk("balance", new { walletAddress = recipient, denom = "stake" })).GetProperty("amount").GetString()!);
        using var provider = Provider();
        var result = await provider.SendTransactionAsync(owner, recipient, 3, "OASIS .NET transfer evidence");
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsNotNull(result.Result);
        var hash = result.Result.TransactionResult;
        Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
        var transaction = await InvokeSdk("transactionStatus", new { transactionHash = hash });
        Assert.AreEqual(0, transaction.GetProperty("code").GetInt32());
        Assert.IsTrue(transaction.GetProperty("height").GetInt64() > 0);
        var after = long.Parse((await InvokeSdk("balance", new { walletAddress = recipient, denom = "stake" })).GetProperty("amount").GetString()!);
        Assert.AreEqual(before + 3, after);
        var providerBalance = await provider.GetAccountBalanceAsync(recipient);
        Assert.IsFalse(providerBalance.IsError, providerBalance.Message);
        Assert.AreEqual((decimal)after, providerBalance.Result);
        var status = await provider.GetTransactionStatusAsync(hash);
        Assert.IsFalse(status.IsError, status.Message);
        Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, status.Result);
        var history = await provider.GetTransactionsAsync(new Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest { WalletAddress = recipient });
        Assert.IsFalse(history.IsError, history.Message);
        Assert.IsTrue(history.Result.Any(tx => tx.FromWalletAddress == owner && tx.ToWalletAddress == recipient
            && tx.Amount == 3 && tx.Description.Contains(hash)), "Committed transfer is absent from decoded SDK transaction history.");
    }

    [TestMethod]
    public async Task WrongSenderAndWrongChainAreRejectedWithoutSuccessResponse()
    {
        var owner = (await InvokeSdk("restoreKey", new { })).GetProperty("address").GetString()!;
        var other = (await InvokeSdk("restoreKey", new { privateKey = new string('0', 63) + "2" }))
            .GetProperty("address").GetString()!;
        using var provider = Provider();
        var wrongSender = await provider.SendTransactionAsync(other, owner, 1, "must fail");
        Assert.IsTrue(wrongSender.IsError);
        Assert.IsNull(wrongSender.Result);
        StringAssert.Contains(wrongSender.Message, "does not own");
        using var wrongChainProvider = Provider("wrong-chain");
        var wrongChain = await wrongChainProvider.SendTransactionAsync(owner, other, 1, "must fail");
        Assert.IsTrue(wrongChain.IsError);
        Assert.IsNull(wrongChain.Result);
        StringAssert.Contains(wrongChain.Message, "chain mismatch");
    }

    [TestMethod]
    public async Task InvalidAmountsAreRejectedBeforeNetworkIo()
    {
        using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "no-chain", OwnerKey);
        foreach (var amount in new[] { 0m, -1m, 0.0000001m, decimal.MaxValue })
        {
            var result = await provider.SendTransactionAsync("sender", "recipient", amount, "invalid");
            Assert.IsTrue(result.IsError, $"Accepted invalid amount {amount}");
            Assert.IsNull(result.Result);
            Assert.IsNotNull(result.Exception);
        }
    }

    [TestMethod]
    public async Task GeneratedWalletAndMnemonicRestoreHaveIdenticalRealPublicKey()
    {
        using var provider = Provider();
        var generated = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(generated.IsError, generated.Message);
        StringAssert.StartsWith(generated.Result.WalletAddressLegacy, "wasm1");
        Assert.AreEqual(66, generated.Result.PublicKey.Length);
        Assert.AreEqual(24, generated.Result.PrivateKey.Split(' ').Length);
        var restored = await provider.RestoreAccountAsync(generated.Result.PrivateKey);
        Assert.IsFalse(restored.IsError, restored.Message);
        Assert.AreEqual(generated.Result.PublicKey, restored.Result.PublicKey);
        var created = await provider.CreateAccountAsync();
        Assert.IsFalse(created.IsError, created.Message);
        var recreated = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
        Assert.IsFalse(recreated.IsError, recreated.Message);
        Assert.AreEqual(created.Result.PublicKey, recreated.Result.PublicKey);
        Assert.AreEqual(created.Result.PrivateKey, recreated.Result.PrivateKey);
        var invalid = await provider.RestoreAccountAsync("not a valid mnemonic");
        Assert.IsTrue(invalid.IsError);
    }

    [TestMethod]
    public async Task BridgePoolDepositAndWithdrawalChangeRealBalances()
    {
        using var provider = Provider();
        var key = new string('0', 63) + "2";
        var recipient = (await InvokeSdk("restoreKey", new { privateKey = key })).GetProperty("address").GetString()!;
        var before = await provider.GetAccountBalanceAsync(recipient);
        Assert.IsFalse(before.IsError, before.Message);
        // Enough local stake for the recipient's own withdrawal gas.
        var deposited = await provider.DepositAsync(100000, recipient);
        Assert.IsFalse(deposited.IsError, deposited.Message);
        Assert.IsTrue(deposited.Result.IsSuccessful);
        var funded = await provider.GetAccountBalanceAsync(recipient);
        Assert.IsFalse(funded.IsError, funded.Message);
        Assert.AreEqual(before.Result + 100000, funded.Result);
        var withdrawn = await provider.WithdrawAsync(10, recipient, key);
        Assert.IsFalse(withdrawn.IsError, withdrawn.Message);
        Assert.IsTrue(withdrawn.Result.IsSuccessful);
        var final = await provider.GetAccountBalanceAsync(recipient);
        Assert.IsFalse(final.IsError, final.Message);
        Assert.IsTrue(final.Result < funded.Result - 10, "Withdrawal must debit principal and real chain gas.");
    }
}
