using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class CosmosProcessBoundaryTests
{
    [DataTestMethod]
    [DataRow("upload", "missing")] [DataRow("upload", "zero")] [DataRow("upload", "negative")]
    [DataRow("upload", "string")] [DataRow("upload", "null")] [DataRow("upload", "overflow")] [DataRow("upload", "fraction")]
    [DataRow("instantiate", "missing")] [DataRow("instantiate", "blank")]
    [DataRow("instantiate", "number")] [DataRow("instantiate", "null")]
    public async Task DeploymentReplyMissingFieldsRetainsOriginalIdentityAndCannotEnableReplay(string operation, string mode)
    {
        await WithFixture(async provider => {
            var error = await Assert.ThrowsExceptionAsync<CosmosNativeMutationReconciliationException>(
                () => Invoke(provider, operation, new { mode }, CancellationToken.None));
            Assert.AreEqual(new string('a', 64), error.OriginalTransactionId);
            Assert.IsNull(error.ExecutionSucceeded);
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            var valid = await Invoke(provider, operation, new { mode = "valid" }, CancellationToken.None);
            Assert.AreEqual(new string('a', 64), valid.GetProperty("transactionHash").GetString());
        });
    }

    [TestMethod]
    public async Task PublicDeploymentMethodsPreserveMissingResultReconciliationEvidence()
    {
        await WithFixture(async provider => {
            var upload = await provider.UploadContractAsync(new byte[] { 1 });
            var instantiate = await provider.InstantiateContractAsync(2, new { }, "controlled fixture");
            Assert.IsTrue(upload.IsError); Assert.IsTrue(instantiate.IsError);
            foreach (var failure in new[] { upload.Exception, instantiate.Exception })
            {
                Assert.IsInstanceOfType<CosmosNativeMutationReconciliationException>(failure);
                var evidence = (CosmosNativeMutationReconciliationException)failure;
                Assert.AreEqual(new string('a', 64), evidence.OriginalTransactionId);
                Assert.IsNull(evidence.ExecutionSucceeded);
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
            }
        });
    }

    private static Task<JsonElement> Invoke(CosmosBlockChainOASIS provider, string operation, object args, CancellationToken token)
    {
        var backend = typeof(KeyValueStorageProviderBase).GetProperty("Backend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
        return (Task<JsonElement>)backend.GetType().GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(backend, new object[] { operation, args, token })!;
    }

    private static async Task WithFixture(Func<CosmosBlockChainOASIS, Task> test)
    {
        var prior = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "nft-signer.mjs"));
        try { using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "test-only-secret", "fixture-contract"); await test(provider); }
        finally { Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", prior); }
    }

    [DataTestMethod]
    [DataRow("empty")] [DataRow("bad-json")] [DataRow("bad-utf8")] [DataRow("no-ok")] [DataRow("bad-ok")] [DataRow("invalid-hash")]
    [DataRow("no-result")] [DataRow("null-result")] [DataRow("no-hash")]
    public async Task MalformedMutationRepliesCannotEnableReplayOrSavedSuccess(string mode)
    {
        await WithFixture(async provider => {
            var error = await Assert.ThrowsExceptionAsync<CosmosNativeMutationReconciliationException>(() => Invoke(provider, "put", new { mode }, CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.IsNull(error.OriginalTransactionId); Assert.IsNull(error.ExecutionSucceeded);
            Assert.IsFalse(error.Message.Contains("test-only-secret"));
        });
    }

    [DataTestMethod]
    [DataRow("put", true)] [DataRow("get", false)]
    public async Task RunningProcessCancellationOnlyClassifiesMutationAsUnreconciled(string operation, bool reconcile)
    {
        await WithFixture(async provider => {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Exception error;
            try { await Invoke(provider, operation, new { mode = "hold" }, cancellation.Token); Assert.Fail("Expected cancellation."); return; }
            catch (OperationCanceledException canceled) { error = canceled; }
            Assert.AreEqual(reconcile, Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            if (reconcile) { var canceled = (CosmosNativeMutationCanceledException)error; Assert.AreEqual(cancellation.Token, canceled.CancellationToken); Assert.IsNull(canceled.OriginalTransactionId); }
        });
    }

    [TestMethod]
    public async Task PreCanceledMutationDoesNotLaunchSdk()
    {
        await WithFixture(async provider => {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var error = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Invoke(provider, "put", new { mode = "hold" }, cancellation.Token));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        });
    }

    [TestMethod]
    public async Task ExplicitPreflightRemainsOrdinaryAndArgumentsCannotOverrideOperation()
    {
        await WithFixture(async provider => {
            var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Invoke(provider, "put", new { mode = "preflight" }, CancellationToken.None));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => Invoke(provider, "get", new { operation = "put", mode = "hold" }, CancellationToken.None));
        });
    }

    [TestMethod]
    public async Task RealProcessDeadlineRequiresReconciliationWithoutInventedTransactionId()
    {
        await WithFixture(async provider => {
            var error = await Assert.ThrowsExceptionAsync<CosmosNativeMutationTimeoutException>(() => Invoke(provider, "put", new { mode = "hold" }, CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error)); Assert.IsNull(error.OriginalTransactionId);
        });
    }

    [TestMethod]
    public async Task ValidMutationReplyStillReturnsOriginalIdentity()
    {
        await WithFixture(async provider => {
            var result = await Invoke(provider, "put", new { mode = "valid" }, CancellationToken.None);
            Assert.AreEqual(new string('a', 64), result.GetProperty("transactionHash").GetString());
        });
    }
}
