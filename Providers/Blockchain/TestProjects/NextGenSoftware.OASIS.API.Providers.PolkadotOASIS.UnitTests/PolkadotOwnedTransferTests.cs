using System.Reflection;
using System.IO.Pipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotOwnedTransferTests
{
    private static UsageExecutionHandle Handle(IUsageOutboxStore? store = null) => (UsageExecutionHandle)typeof(UsageExecutionHandle)
        .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(value => value.GetParameters().Length == 5)
        .Invoke(new object?[] { new UsageOutboxOperation { Id = Guid.NewGuid().ToString("D"), ExecutionOwner = "owner",
            Policy = new UsageEndpointPolicy() }, store, new Web4UsageLedgerOptions(), null, null });

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OrdinarySendCannotSubmitWithoutAnOwner(bool synchronous)
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1");
        var result = synchronous ? provider.SendToken(new SendWeb3TokenRequest()) : await provider.SendTokenAsync(new SendWeb3TokenRequest());
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "admitted execution owner");
    }

    [TestMethod]
    public async Task OwnerScopeRejectsNestingAndRestoresContextAfterFailure()
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1");
        await provider.ExecutePreparedNativeOperationAsync(async () => {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.ExecutePreparedNativeOperationAsync(
                () => Task.FromResult(true), Handle(), "second", "sender"));
            return true;
        }, Handle(), "first", "sender");
        var outside = await provider.SendTokenAsync(new SendWeb3TokenRequest());
        StringAssert.Contains(outside.Message, "admitted execution owner");
    }

    [TestMethod]
    public async Task OwnerScopeCannotBeReusedOrEscapeItsDelegate()
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.ITransactionResponse>>? escaped = null;
        await provider.ExecutePreparedNativeOperationAsync(async () => {
            var first = await provider.SendTokenAsync(new SendWeb3TokenRequest()); // Not activated; still consumes this scope's one claim.
            Assert.IsTrue(first.IsError);
            var second = await provider.SendTokenAsync(new SendWeb3TokenRequest());
            StringAssert.Contains(second.Message, "only one transfer");
            escaped = Task.Run(async () => { await release.Task; return await provider.SendTokenAsync(new SendWeb3TokenRequest()); });
            return true;
        }, Handle(), "owned", "sender");
        release.SetResult();
        var late = await escaped!;
        Assert.IsTrue(late.IsError);
        StringAssert.Contains(late.Message, "cannot outlive");
    }

    [DataTestMethod][DataRow("accepted", false)][DataRow("accepted", true)][DataRow("dispatch-failed", false)][DataRow("read-error", false)][DataRow("wait-for-cancellation", false)]
    public async Task OrdinarySendUsesOwnerAndRequiresFinalizedDispatch(string outcome, bool synchronous)
    {
        // Controlled fixture covers orchestration/physical pipe only, NOT SDK signatures, Mongo durability or ledger execution.
        var priorBridge = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome = Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        var priorPipe = Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_FINALITY_PIPE");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", outcome);
        try
        {
            var store = DispatchProxy.Create<IUsageOutboxStore, AttemptObserver>();
            using var provider = new PolkadotOASIS("http://127.0.0.1:1");
            typeof(PolkadotOASIS).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, true);
            typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider, "0x" + new string('a', 64));
            using var cancellation = new CancellationTokenSource();
            var pipeName = "oasis-polkadot-test-finality-" + Guid.NewGuid().ToString("N");
            using var enteredFinality = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_FINALITY_PIPE", OperatingSystem.IsWindows()
                ? @"\\.\pipe\" + pipeName : Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + pipeName));
            var pending = provider.ExecutePreparedNativeOperationAsync(() => synchronous
                ? Task.FromResult(provider.SendToken(new SendWeb3TokenRequest())) : provider.SendTokenAsync(new SendWeb3TokenRequest()),
                Handle(store), "owned-original", "sender", cancellation.Token);
            if (outcome == "wait-for-cancellation")
            {
                using var observedDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await enteredFinality.WaitForConnectionAsync(observedDeadline.Token);
                Assert.AreEqual(1, await enteredFinality.ReadAsync(new byte[1], observedDeadline.Token));
                cancellation.Cancel();
            }
            var result = await pending;
            Assert.AreEqual(1, ((AttemptObserver)(object)store).Calls);
            if (outcome == "accepted")
            {
                Assert.IsFalse(result.IsError, result.Exception?.ToString());
                Assert.AreEqual("0x" + new string('b', 64), result.Result.TransactionResult);
                StringAssert.Contains(result.Message, "finalized");
            }
            else
            {
                Assert.IsTrue(result.IsError);
                Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
                Assert.AreEqual("0x" + new string('b', 64), ((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId);
                if (outcome == "wait-for-cancellation") Assert.IsInstanceOfType(result.Exception, typeof(OperationCanceledException));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", priorOutcome);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_FINALITY_PIPE", priorPipe);
        }
    }

    public class AttemptObserver : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != "BeginProviderAttemptAsync") throw new InvalidOperationException("Unexpected controlled outbox call.");
            var attempt = (UsageProviderAttempt)args![2]!;
            Assert.AreEqual("owned-original", attempt.ClientRequestId);
            Assert.AreEqual("AQID", attempt.NativePreparation.SignedTransactionBase64);
            Assert.AreEqual("0x" + new string('b', 64), attempt.ProviderRequestId);
            Calls++;
            return Task.CompletedTask;
        }
    }
}
