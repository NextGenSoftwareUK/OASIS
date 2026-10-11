using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class HederaProcessBoundaryTests
{
    private const string Mutation = "0.0.1001@1700000000.000000001";
    private const string Approval = "0.0.1001@1700000000.000000002";
    private const string Secret = "never-log-this-test-key";
    private static Task<JsonElement> Invoke(string action, CancellationToken cancellationToken) =>
        (Task<JsonElement>)typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!
            .GetMethod("InvokeAsync", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { action, new { transactionId = Mutation, approvalTransactionId = Approval, privateKey = Secret }, cancellationToken })!;

    [TestMethod]
    public async Task FailedProcessRetainsConfirmedApprovalMutationAndStageWithoutKey()
    {
        await WithFixture(async () =>
        {
            var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Invoke("fail", CancellationToken.None));
            StringAssert.Contains(error.Message, "Confirmed approval: " + Approval);
            StringAssert.Contains(error.Message, "Original escrow mutation: " + Mutation);
            StringAssert.Contains(error.Message, "Failure stage: readback");
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [TestMethod]
    public async Task CanceledRunningProcessRetainsBothUnverifiedIdsWithoutClaimingApproval()
    {
        await WithFixture(async () =>
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var error = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Invoke("hold", cancellation.Token));
            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            StringAssert.Contains(error.Message, Mutation);
            StringAssert.Contains(error.Message, Approval);
            StringAssert.Contains(error.Message, "outcome unverified");
            Assert.IsFalse(error.Message.Contains("Confirmed approval"));
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    private static async Task WithFixture(Func<Task> test)
    {
        var prior = Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "hedera-process-boundary.mjs"));
        try { await test(); }
        finally { Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", prior); }
    }
}
