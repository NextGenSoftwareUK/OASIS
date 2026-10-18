using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotLocalNativeTests
{
    private const string Endpoint = "http://127.0.0.1:19944";
    private const string Genesis = "0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private const string Sender = "0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Recipient = "0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("LocalNative")]
    public async Task OwnedNativeTransferFinalizesAndAccountsForExactFees(bool synchronous)
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var journal = Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL")!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(journal), "Explicit fixed journal required.");
        Assert.IsFalse(File.Exists(journal), "Pending original exists: reconcile read-only, never replay or overwrite.");
        using var provider = new PolkadotOASIS(Endpoint, Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        Assert.AreEqual(Genesis, provider.ReportedGenesisHash);
        var beforeSender = await Balance(provider, Sender);
        var beforeRecipient = await Balance(provider, Recipient);
        var store = DispatchProxy.Create<IUsageOutboxStore, FlushedAttemptOwner>();
        var observed = (FlushedAttemptOwner)(object)store;
        observed.Journal = journal;
        var constructor = typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == 5);
        // Test admission owner with real fsync'd file durability. NOT Mongo outbox/quota/hosted authorization proof.
        var execution = (UsageExecutionHandle)constructor.Invoke(new object?[] {
            new UsageOutboxOperation { Id = Guid.NewGuid().ToString("D"), ExecutionOwner = "local-native-owner", Policy = new UsageEndpointPolicy() },
            store, new Web4UsageLedgerOptions(), null, null });
        var request = new SendWeb3TokenRequest { FromWalletAddress = Sender, ToWalletAddress = Recipient,
            Amount = 1.000000000001m, OwnerPrivateKey = "0x" + string.Concat(Enumerable.Repeat("02", 32)),
            MemoText = synchronous ? null : "Local native transfer 🌳" }; // Public LOCAL-ONLY test seed; never fund it on public chains.
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var sent = await provider.ExecutePreparedNativeOperationAsync(() => synchronous
            ? Task.FromResult(provider.SendToken(request)) : provider.SendTokenAsync(request),
            execution, "local-native-transfer", Sender, deadline.Token);
        Assert.IsFalse(sent.IsError, sent.Exception?.ToString());
        Assert.AreEqual(1, observed.Calls);
        Assert.IsNotNull(observed.Attempt);
        Assert.AreEqual(observed.Attempt.ProviderRequestId, sent.Result.TransactionResult);
        var original = JsonSerializer.SerializeToElement(new { chainId = Genesis, sender = Sender,
            transactionHash = observed.Attempt.ProviderRequestId, signedTransactionBase64 = observed.Attempt.NativePreparation.SignedTransactionBase64,
            signature = observed.Attempt.NativePreparation.Signature, signDocBase64 = observed.Attempt.NativePreparation.SignDocBase64 });
        using var signing = JsonDocument.Parse(Convert.FromBase64String(observed.Attempt.NativePreparation.SignDocBase64));
        var block = signing.RootElement.GetProperty("signingPayload").GetProperty("blockNumber").GetString()!;
        var start = Convert.ToUInt64(block[2..], 16).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var recovered = await provider.ReadOriginalNativeOutcomeAsync(original, start, 16, deadline.Token);
        Assert.IsFalse(recovered.IsError, recovered.Exception?.ToString());
        Assert.IsTrue(recovered.Result.GetProperty("found").GetBoolean());
        var receipt = recovered.Result.GetProperty("receipt");
        Assert.IsTrue(receipt.GetProperty("finalized").GetBoolean());
        Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
        var actualFee = BigInteger.Parse(receipt.GetProperty("actualFeeUnits").GetString()!);
        Assert.IsTrue(actualFee > 0);
        var sharedFee = await provider.ReadPreparedNativeFeeEvidenceAsync(observed.Attempt, deadline.Token);
        Assert.IsFalse(sharedFee.IsError, sharedFee.Exception?.ToString());
        Assert.IsTrue(sharedFee.Result.NativeExecutionSucceeded);
        Assert.AreEqual(NativeFeeCoverage.NetFinal, sharedFee.Result.Coverage);
        Assert.AreEqual("native-base-unit", sharedFee.Result.Components.Single().Denomination);
        Assert.AreEqual(actualFee.ToString(), sharedFee.Result.Components.Single().NetUnits);
        var transferred = BigInteger.Parse("1000000000001");
        Assert.AreEqual(transferred, await Balance(provider, Recipient) - beforeRecipient);
        Assert.AreEqual(transferred + actualFee, beforeSender - await Balance(provider, Sender));
        using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        Assert.AreEqual(sent.Result.TransactionResult, retained.RootElement.GetProperty("ProviderRequestId").GetString());
        var completed = journal + (synchronous ? ".sync.completed.json" : ".async.completed.json");
        using (var file = new FileStream(completed, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new { original, receipt, transferredUnits = transferred.ToString(), actualFeeUnits = actualFee.ToString() }));
            file.Flush(true);
        }
        File.Delete(journal); // Exact confirmed original retained in flushed completed evidence; failure paths preserve pending file.
    }

    private static async Task<BigInteger> Balance(PolkadotOASIS provider, string address)
    {
        var method = typeof(PolkadotOASIS).GetMethod("InvokePolkadotSdkAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<JsonElement>)method.Invoke(provider, new object?[] {
            new { operation = "balance", endpoint = Endpoint, genesisHash = Genesis, address }, CancellationToken.None, null })!;
        var result = await task;
        Assert.AreEqual(12, result.GetProperty("decimals").GetInt32());
        return BigInteger.Parse(result.GetProperty("units").GetString()!);
    }

    public class FlushedAttemptOwner : DispatchProxy
    {
        public string Journal = null!;
        public int Calls;
        public UsageProviderAttempt Attempt = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != "BeginProviderAttemptAsync") throw new InvalidOperationException("Unexpected local-native owner call.");
            var attempt = (UsageProviderAttempt)args![2]!;
            using (var file = new FileStream(Journal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                file.Write(JsonSerializer.SerializeToUtf8Bytes(attempt));
                file.Flush(true);
            }
            Attempt = attempt;
            Calls++;
            return Task.CompletedTask;
        }
    }
}
