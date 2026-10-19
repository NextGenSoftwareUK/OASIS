using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotBridgeTests
{
    [DataTestMethod][DataRow(false,"accepted")][DataRow(true,"accepted")]
    [DataRow(false,"dispatch-failed")][DataRow(true,"dispatch-failed")]
    [DataRow(false,"read-error")][DataRow(true,"read-error")]
    public async Task BothBridgeDirectionsRequireFinalityAndPreserveUncertainOriginal(bool deposit, string outcome)
    {
        var bridge = Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var mode = Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", outcome);
        try {
            var store = DispatchProxy.Create<IUsageOutboxStore, PolkadotOwnedTransferTests.AttemptObserver>();
            using var provider = new PolkadotOASIS("http://127.0.0.1:1","polkadot","fixture-key","sender");
            PolkadotAssetMutationTests.Activate(provider);
            var result = await provider.ExecutePreparedNativeOperationAsync(
                () => deposit ? provider.DepositAsync(1m,"recipient") : provider.WithdrawAsync(1m,"sender","fixture-key"),
                PolkadotOwnedTransferTests.Handle(store),"owned-original","sender");
            Assert.AreEqual(1, ((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual("0x" + new string('b',64), result.Result.TransactionId);
            Assert.AreEqual(outcome != "accepted", result.IsError);
            Assert.AreEqual(outcome == "accepted", result.Result.IsSuccessful);
            Assert.AreEqual(outcome == "accepted" ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Pending, result.Result.Status);
            if (result.IsError) Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE", bridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME", mode);
        }
    }

    [TestMethod]
    public async Task PoolReleaseRequiresConfiguredSigner()
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1","polkadot","","sender");
        PolkadotAssetMutationTests.Activate(provider);
        var result = await provider.DepositAsync(1m,"recipient");
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
        Assert.AreEqual(string.Empty, result.Result.TransactionId);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task WithdrawalCannotFabricateTransactionWithoutPoolOrExecutionOwner(bool configuredPool)
    {
        using var provider = new PolkadotOASIS("http://127.0.0.1:1", "polkadot", "",
            configuredPool ? "0x" + new string('3',64) : null!);
        PolkadotAssetMutationTests.Activate(provider);
        var result = await provider.WithdrawAsync(1m, "0x" + new string('2',64), "0x" + new string('2',64));
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));
        StringAssert.Contains(result.Exception.Message, configuredPool ? "admitted execution owner" : "explicit bridge pool");
        Assert.IsFalse(result.Result.IsSuccessful);
        Assert.AreEqual(string.Empty, result.Result.TransactionId);
        Assert.AreNotEqual(BridgeTransactionStatus.Completed, result.Result.Status);
    }
}
