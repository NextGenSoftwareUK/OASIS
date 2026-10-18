using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotVerificationCancellationTests
{
    [TestMethod]
    public void ExistingOneArgumentPreparationAndVerificationEntryPointsRemainAvailable()
    {
        Assert.IsNotNull(typeof(PolkadotOASIS).GetMethod("VerifyNativeTokenTransferPreparationAsync", new[] { typeof(JsonElement) }));
        Assert.IsNotNull(typeof(PolkadotOASIS).GetMethod("PrepareNativeTokenTransferAsync", new[] {
            typeof(NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests.ISendWeb3TokenRequest) }));
    }

    [TestMethod]
    public async Task PrecancelledVerificationDoesNotRequireActivationOrStartAChild()
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await provider.VerifyNativeTokenTransferPreparationAsync(default, cancelled.Token);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(OperationCanceledException));
        Assert.AreEqual(cancelled.Token, ((OperationCanceledException)result.Exception).CancellationToken);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RunningVerifierStopsItsOwnedChildOnCallerCancellation(bool throughPersistenceOwner)
    {
        // Controlled child/pipe, no SDK/network/signing/ledger. Observe real entry/PID before cancellation.
        var names = new[] { "OASIS_POLKADOT_SDK_BRIDGE", "OASIS_POLKADOT_TEST_OUTCOME", "OASIS_POLKADOT_TEST_FINALITY_PIPE" };
        var prior = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        var pipeName = "oasis-polkadot-verifier-" + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var caller = new CancellationTokenSource();
        try
        {
            Environment.SetEnvironmentVariable(names[0], Path.Combine(AppContext.BaseDirectory, "submission-process-fixture.mjs"));
            Environment.SetEnvironmentVariable(names[1], "cancel-verification");
            Environment.SetEnvironmentVariable(names[2], OperatingSystem.IsWindows() ? @"\\.\pipe\" + pipeName : Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + pipeName));
            using var provider = new PolkadotOASIS("http://127.0.0.1:1");
            typeof(PolkadotOASIS).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, true);
            typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider, "0x" + new string('a', 64));
            var constructor = typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(value => value.GetParameters().Length == 5);
            // No outbox exists: cancellation must terminate verification BEFORE persistence is reached.
            var owner = (UsageExecutionHandle)constructor.Invoke(new object?[] {
                new UsageOutboxOperation { Policy = new UsageEndpointPolicy() }, null, null, null, null });
            var evidence = JsonSerializer.SerializeToElement(new { chainId = "0x" + new string('a', 64), sender = "sender",
                transactionHash = "0x" + new string('b', 64), signedTransactionBase64 = "AQID", signDocBase64 = "AQID", signature = "fixture" });
            var verification = throughPersistenceOwner ? null : provider.VerifyNativeTokenTransferPreparationAsync(evidence, caller.Token);
            Task pending = verification ?? (Task)provider.PersistNativeTokenTransferPreparationAsync(evidence, owner, "cancel-original", "sender", caller.Token);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await pipe.WaitForConnectionAsync(deadline.Token);
            using var reader = new StreamReader(pipe, leaveOpen: true);
            var pid = int.Parse((await reader.ReadLineAsync(deadline.Token))!);
            caller.Cancel();
            if (throughPersistenceOwner)
            {
                try { await pending.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Fail("Cancelled owner verification cannot reach persistence/ACK."); }
                catch (OperationCanceledException) { }
            }
            else
            {
                await pending.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsTrue(verification!.Result.IsError);
                Assert.IsInstanceOfType(verification.Result.Exception, typeof(OperationCanceledException));
            }
            try { using var child = Process.GetProcessById(pid); Assert.IsTrue(child.HasExited, "Owned verifier child must be stopped before cancellation returns."); }
            catch (ArgumentException) { /* Process already reaped by the owned SDK runner. */ }
        }
        finally
        {
            caller.Cancel();
            foreach (var name in names) Environment.SetEnvironmentVariable(name, prior[name]);
        }
    }
}
