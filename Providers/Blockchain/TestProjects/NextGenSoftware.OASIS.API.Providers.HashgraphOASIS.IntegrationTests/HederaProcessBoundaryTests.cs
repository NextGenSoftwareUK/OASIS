using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class HederaProcessBoundaryTests
{
    [TestMethod]
    public async Task InvalidUtf8MutationReplyPreservesIdentityAndCannotEnableReplay()
    {
        await WithFixture(async () =>
        {
            var error = await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(() => Invoke("storageDelete", CancellationToken.None, "invalid-utf8"));
            Assert.AreEqual(Mutation, error.OriginalTransactionId);
            Assert.AreEqual("process-communication-unverified", error.Evidence.Stage);
            Assert.IsInstanceOfType<System.Text.DecoderFallbackException>(error.InnerException);
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        });
    }

    [TestMethod]
    public async Task NullReportedIdCannotEraseOriginalMutationIdentity()
    {
        await WithFixture(async () =>
        {
            var error = await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(() => Invoke("nftEscrowLock", CancellationToken.None, "null-primary"));
            Assert.AreEqual(Mutation, error.OriginalTransactionId);
            Assert.AreEqual(Approval, error.Evidence.ApprovalTransactionId);
        });
    }

    [DataTestMethod]
    [DataRow("wrong-primary")] [DataRow("wrong-approval")] [DataRow("wrong-mutation")]
    public async Task ErrorMetadataCannotReplaceOriginalIdsOrClaimUnverifiedApproval(string stage)
    {
        await WithFixture(async()=>{
            var error=await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(()=>Invoke("nftEscrowLock",CancellationToken.None,stage));
            Assert.AreEqual(Mutation,error.OriginalTransactionId);Assert.IsNull(error.Evidence.ApprovalTransactionId);
            Assert.IsNotNull(error.InnerException);Assert.IsFalse(error.Message.Contains("untrusted-other-id"));
            Assert.IsFalse(error.Message.Contains("Confirmed approval"));
        });
    }

    [DataTestMethod]
    [DataRow("bad-json")] [DataRow("missing")] [DataRow("scalar")] [DataRow("wrong-id")]
    [DataRow("failed")] [DataRow("bad-status")] [DataRow("foreign-approval")] [DataRow("invalid-approval")]
    public async Task SuccessfulExitWithoutMatchingMutationReceiptCannotEnableReplay(string mode)
    {
        await WithFixture(async()=>{
            var error=await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(()=>Invoke("storageDelete",CancellationToken.None,mode));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.AreEqual(Mutation,error.OriginalTransactionId);
            Assert.IsNull(error.Evidence.NativeStatus);
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [TestMethod]
    public async Task MatchingSuccessfulMutationReplyStillReturnsItsOriginalId()
    {
        await WithFixture(async()=>{
            var result=await Invoke("storageDelete",CancellationToken.None,"valid");
            Assert.AreEqual(Mutation,result.GetProperty("transactionId").GetString());
            Assert.AreEqual("SUCCESS",result.GetProperty("status").GetString());
        });
    }

    private const string Mutation = "0.0.1001@1700000000.000000001";
    private const string Approval = "0.0.1001@1700000000.000000002";
    private const string Secret = "never-log-this-test-key";
    private static Task<JsonElement> Invoke(string action, CancellationToken cancellationToken, string stage = "readback") =>
        (Task<JsonElement>)typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!
            .GetMethod("InvokeAsync", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { action, new { transactionId = Mutation, approvalTransactionId = Approval, privateKey = Secret, fixtureStage = stage }, cancellationToken })!;

    [TestMethod]
    public async Task FailedProcessRetainsConfirmedApprovalMutationAndStageWithoutKey()
    {
        await WithFixture(async () =>
        {
            var error = await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(() => Invoke("nftEscrowLock", CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.AreEqual(Mutation,error.Evidence.OriginalTransactionId);
            Assert.AreEqual(Approval,error.Evidence.ApprovalTransactionId);
            Assert.AreEqual("CONTRACT_REVERT_EXECUTED",error.Evidence.NativeStatus);
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
            var error = await Assert.ThrowsExceptionAsync<HederaNativeMutationCanceledException>(() => Invoke("storagePut", cancellation.Token));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            StringAssert.Contains(error.Message, Mutation);
            StringAssert.Contains(error.Message, Approval);
            StringAssert.Contains(error.Message, "outcome unverified");
            Assert.IsFalse(error.Message.Contains("Confirmed approval"));
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [DataTestMethod]
    [DataRow("construction",false)] [DataRow("signing",false)] [DataRow("validation",false)]
    [DataRow("submission",true)] [DataRow("receipt",true)] [DataRow("readback",true)]
    public async Task SubmissionStagesRequireReconciliationWithoutReclassifyingKnownPreSubmissionErrors(string stage,bool reconcile)
    {
        await WithFixture(async () =>
        {
            Exception error;
            try { await Invoke("transfer",CancellationToken.None,stage); Assert.Fail("Expected controlled SDK failure."); return; }
            catch(InvalidOperationException failure) {error=failure;}
            Assert.AreEqual(reconcile,Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [DataTestMethod]
    [DataRow("tokenBurn",true)] [DataRow("tokenMint",false)] [DataRow("generateKey",false)]
    public async Task MalformedMutationEvidenceStopsReplayButExplicitPreflightAndNonMutationErrorsDoNot(string action,bool reconcile)
    {
        await WithFixture(async () =>
        {
            Exception error;
            try {await Invoke(action,CancellationToken.None); Assert.Fail("Expected controlled SDK failure."); return;}
            catch(InvalidOperationException failure) {error=failure;}
            Assert.AreEqual(reconcile,Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            if(action=="tokenBurn") Assert.IsNotNull(error.InnerException);
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [TestMethod]
    public async Task CancellationBeforeProcessStartDoesNotDeclareNativeSubmission()
    {
        await WithFixture(async () =>
        {
            using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
            var error=await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>Invoke("storagePut",cancellation.Token));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        });
    }

    [TestMethod]
    public async Task ActualProcessTimeoutRetainsUnverifiedOriginalIdsAndStopsReplay()
    {
        await WithFixture(async () =>
        {
            var error=await Assert.ThrowsExceptionAsync<HederaNativeMutationTimeoutException>(()=>Invoke("storagePut",CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.AreEqual(Mutation,error.Evidence.OriginalTransactionId);
            Assert.AreEqual(Approval,error.Evidence.ApprovalTransactionId);
            Assert.AreEqual("process-timed-out",error.Evidence.Stage);
            Assert.IsNull(error.Evidence.NativeStatus);
            Assert.IsFalse(error.Message.Contains("Confirmed approval"));
            Assert.IsFalse(error.Message.Contains(Secret));
        });
    }

    [DataTestMethod]
    [DataRow(null)] [DataRow("")] [DataRow(" ")]
    public async Task NativeMutationRequiresOriginalIdBeforeLaunchingSdk(string? id)
    {
        await WithFixture(async () =>
        {
            var method=typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!
                .GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Static)!;
            var operation=(Task<JsonElement>)method.Invoke(null,new object[]{"storagePut",new{transactionId=id,privateKey=Secret},CancellationToken.None})!;
            var error=await Assert.ThrowsExceptionAsync<ArgumentException>(()=>operation);
            StringAssert.Contains(error.Message,"explicit original transaction ID before SDK launch");
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
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
