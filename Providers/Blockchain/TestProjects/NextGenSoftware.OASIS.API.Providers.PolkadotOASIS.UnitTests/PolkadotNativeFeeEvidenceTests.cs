using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotNativeFeeEvidenceTests
{
    [DataTestMethod][DataRow("fee-success", false)][DataRow("fee-failed-dispatch", false)]
    [DataRow("fee-wrong-hash", true)][DataRow("fee-unfinalized", true)][DataRow("fee-invalid-units", true)][DataRow("fee-unresolved", true)]
    [DataRow("fee-wrong-provider", true)][DataRow("fee-wrong-chain", true)]
    public async Task SharedFeeEvidenceRetainsFinalizedOriginalOrFailsUnresolved(string mode, bool error)
    {
        // Controlled process response only, not SDK/ledger/authority proof.
        var oldBridge = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var oldMode = Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", mode);
        try
        {
            using var provider = new PolkadotOASIS("http://127.0.0.1:1");
            typeof(PolkadotOASIS).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, true);
            var chain = "0x" + new string('a', 64);
            typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider, chain);
            var registered = new RegisteredNativeFeeReader(ProviderType.PolkadotOASIS, chain, new[] { provider });
            Assert.AreEqual(NativeFeeCoverage.NetFinal, registered.DeclaredFeeCoverage);
            var result = await registered.ReadPreparedNativeFeeEvidenceAsync(new UsageProviderAttempt {
                Provider = mode == "fee-wrong-provider" ? "OtherProvider" : "PolkadotOASIS", ProviderRequestId = "0x" + new string('b', 64),
                NativePreparation = new UsageNativePreparation { ChainId = mode == "fee-wrong-chain" ? "other-chain" : chain, Sender = "sender",
                    SignedTransactionBase64 = "AQID", SignDocBase64 = "AQID", Signature = "fixture" } });
            Assert.AreEqual(error, result.IsError, result.Exception?.ToString());
            if (!error)
            {
                Assert.AreEqual(mode != "fee-failed-dispatch", result.Result.NativeExecutionSucceeded);
                Assert.AreEqual(NativeFeeCoverage.NetFinal, result.Result.Coverage);
                Assert.AreEqual("native-base-unit", result.Result.Components.Single().Denomination);
                Assert.AreEqual("12", result.Result.Components.Single().NetUnits);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", oldBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", oldMode);
        }
    }
}
