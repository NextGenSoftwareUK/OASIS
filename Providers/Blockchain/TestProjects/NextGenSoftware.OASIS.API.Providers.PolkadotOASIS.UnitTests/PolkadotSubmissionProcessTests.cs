using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotSubmissionProcessTests
{
    [DataTestMethod][DataRow("accepted")][DataRow("error")][DataRow("wrong-hash")][DataRow("false-finality")]
    public async Task AcknowledgedOriginalCannotLoseIdentityOrBecomeFinalSuccess(string outcome)
    {
        // Real owned child/physical pipe, controlled callback/response. NOT native ledger/outbox evidence.
        var prior = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "submission-process-fixture.mjs"));
        try
        {
            const string hash = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            var calls = 0;
            using var owner = new PreparedSubmissionServer((evidence, token) => { calls++; return Task.CompletedTask; }, hash);
            using var provider = new PolkadotOASIS("http://127.0.0.1:1");
            var method = typeof(PolkadotOASIS).GetMethod("InvokePolkadotSdkAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pending = (Task<JsonElement>)method.Invoke(provider, new object[] {
                new { operation = "submitNativePreparation", fixtureOutcome = outcome,
                    preparation = new { transactionHash = hash, signedTransactionBase64 = "AQID" } },
                CancellationToken.None, owner })!;
            if (outcome == "accepted")
            {
                var result = await pending;
                Assert.IsTrue(result.GetProperty("poolAccepted").GetBoolean());
                Assert.IsFalse(result.GetProperty("finalized").GetBoolean());
                Assert.IsTrue(result.GetProperty("reconciliationRequired").GetBoolean());
            }
            else
            {
                var error = await Assert.ThrowsExceptionAsync<PolkadotNativeMutationReconciliationException>(async () => await pending);
                Assert.AreEqual(hash, ((INativeMutationReconciliationRequired)error).OriginalTransactionId);
                Assert.IsNotNull(error.InnerException);
            }
            Assert.AreEqual(1, calls);
            Assert.AreEqual(hash, owner.PreparedTransactionId);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", prior); }
    }
}
