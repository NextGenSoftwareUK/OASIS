using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiUnlockReadbackFailureTests
{
    [TestMethod]
    public async Task PreparedProcessCannotReturnSuccessWithoutPersistenceAcknowledgement()
    {
        await WithProcessFixture(async provider=>{
            var backend=typeof(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase)
                .GetProperty("Backend",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var client=backend.GetType().GetProperty("Client",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public)!.GetValue(backend)!;
            var calls=0;
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((_,_)=>{calls++;return Task.CompletedTask;});
            var operation=(Task<System.Text.Json.JsonElement>)client.GetType().GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(client,new object[]{"put",new{mode="valid"},server,CancellationToken.None})!;
            var error=await Assert.ThrowsExceptionAsync<SuiNativeMutationReconciliationException>(()=>operation);
            StringAssert.Contains(error.Message,"preparation acknowledgement");Assert.AreEqual(0,calls);
        });
    }

    [DataTestMethod]
    [DataRow("valid")] [DataRow("rejected")]
    public async Task OriginalPreparationVerifierRequiresExplicitMatchingSdkAcknowledgement(string mode)
    {
        await WithProcessFixture(async provider=>{
            using var document=System.Text.Json.JsonDocument.Parse("{\"transactionHash\":\"controlled\",\"sender\":\"controlled-sender\",\"mode\":\""+mode+"\"}");
            if(mode=="valid")await provider.VerifyOriginalSignedPreparationAsync(document.RootElement);
            else await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(()=>provider.VerifyOriginalSignedPreparationAsync(document.RootElement));
        });
    }

    [TestMethod]
    public void DigestValidationRequiresExactDecodedWidthAndBase58Alphabet()
    {
        var type=typeof(SuiOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiSdkClient")!;
        var method=type.GetMethod("IsValidDigest",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        bool Valid(string? digest)=>(bool)method.Invoke(null,new object?[]{digest})!;
        Assert.IsTrue(Valid(new string('1',32)));
        Assert.IsTrue(Valid("D83kgAWQQbdZdCFbFQivXuRF7MB5YqiqGStZAynz3GGk"));
        foreach(var invalid in new[]{null,"",new string('1',31),new string('1',33),new string('z',44),"0OIl", "fabricated-digest"})
            Assert.IsFalse(Valid(invalid));
    }

    private static Task<System.Text.Json.JsonElement> InvokeProcess(SuiOASIS provider,string operation,object args,CancellationToken token)
        => (Task<System.Text.Json.JsonElement>)typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
            .Invoke(provider,new object[]{operation,args,token})!;

    private static async Task WithProcessFixture(Func<SuiOASIS,Task> test)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try {using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");await test(provider);}
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow("empty")] [DataRow("bad-json")] [DataRow("bad-utf8")] [DataRow("no-ok")] [DataRow("bad-ok")]
    [DataRow("no-result")] [DataRow("null-result")] [DataRow("no-hash")]
    public async Task MalformedMutationProcessOutputCannotBecomeRetryableOrSavedSuccess(string mode)
    {
        await WithProcessFixture(async provider=>{
            var error=await Assert.ThrowsExceptionAsync<SuiNativeMutationReconciliationException>(()=>InvokeProcess(provider,"put",new{mode},CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            Assert.IsNull(error.OriginalTransactionId);Assert.IsNull(error.ExecutionSucceeded);
            Assert.IsFalse(error.Message.Contains("test-only-secret"));
        });
    }

    [DataTestMethod]
    [DataRow("put",true)] [DataRow("get",false)]
    public async Task RunningProcessCancellationRetainsTokenAndOnlyMutationUncertaintyRequiresReconciliation(string operation,bool reconcile)
    {
        await WithProcessFixture(async provider=>{
            using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Exception error;
            try {await InvokeProcess(provider,operation,new{mode="hold"},cancellation.Token);Assert.Fail("Expected cancellation.");return;}
            catch(OperationCanceledException canceled){error=canceled;}
            Assert.AreEqual(reconcile,Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            if(reconcile) {var canceled=(SuiNativeMutationCanceledException)error;Assert.AreEqual(cancellation.Token,canceled.CancellationToken);Assert.IsNull(canceled.OriginalTransactionId);}
        });
    }

    [TestMethod]
    public async Task PreCanceledMutationNeverLaunchesProcessAndCannotClaimSubmission()
    {
        await WithProcessFixture(async provider=>{
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();
            var error=await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>InvokeProcess(provider,"put",new{mode="hold"},cancellation.Token));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        });
    }

    [TestMethod]
    public async Task ExplicitPreflightErrorRemainsOrdinaryAndOperationCannotBeOverriddenByArguments()
    {
        await WithProcessFixture(async provider=>{
            var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>InvokeProcess(provider,"put",new{mode="preflight"},CancellationToken.None));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            await Assert.ThrowsExceptionAsync<ArgumentException>(()=>InvokeProcess(provider,"get",new{operation="put",mode="hold"},CancellationToken.None));
        });
    }

    [TestMethod]
    public async Task ActualMutationProcessDeadlineProducesTypedUnknownOutcomeWithoutInventedIdentity()
    {
        await WithProcessFixture(async provider=>{
            var error=await Assert.ThrowsExceptionAsync<SuiNativeMutationTimeoutException>(()=>InvokeProcess(provider,"put",new{mode="hold"},CancellationToken.None));
            Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));Assert.IsNull(error.OriginalTransactionId);
            Assert.IsFalse(error.Message.Contains("test-only-secret"));
        });
    }

    [DataTestMethod]
    [DataRow("failed")] [DataRow("executed")] [DataRow("unknown")]
    [DataRow("bad-status")] [DataRow("bad-shape")] [DataRow("missing-status")]
    [DataRow("bad-error")] [DataRow("missing-error")]
    [DataRow("ordinary")]
    [DataRow("invalid-digest")] [DataRow("unknown-executed")]
    public async Task NativeEvidenceSurvivesSdkProcessBoundaryWithoutGuessingMissingOutcomes(string mode)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Title="native-evidence/"+mode,NumberToMint=1 });
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            StringAssert.Contains(result.Message,mode is "bad-error" or "missing-error" ? "Malformed Sui native mutation error envelope" : "original controlled native error");
            Assert.AreEqual(mode!="ordinary",Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
            if(mode!="ordinary")
            {
                Assert.IsInstanceOfType<SuiNativeMutationReconciliationException>(result.Exception);
                var evidence=(SuiNativeMutationReconciliationException)result.Exception;
                Assert.AreEqual(mode is "unknown" or "bad-shape" or "bad-error" or "missing-error" or "invalid-digest" or "unknown-executed" ? null : new string('1',32),evidence.OriginalTransactionId);
                Assert.AreEqual(mode=="failed" ? (bool?)false : mode=="executed" ? (bool?)true : null,evidence.ExecutionSucceeded);
                if(mode is "bad-status" or "bad-shape" or "missing-status" or "bad-error" or "missing-error") Assert.IsNotNull(evidence.InnerException);
            }
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(null,null,null,"object ID is required")]
    [DataRow(" ","",null,"object ID is required")]
    [DataRow("first","second",null,"different objects")]
    [DataRow(null,"first","second","different objects")]
    [DataRow("first",null,"second","different objects")]
    public async Task InvalidNftSelectorsFailBeforeSignerAccess(string? tokenId,string? fromAddress,string? address,string error)
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
        var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId=tokenId,FromNFTTokenAddress=fromAddress,TokenAddress=address,Amount=1 });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,error);
    }

    [TestMethod]
    public async Task CommittedMintDigestSurvivesSeparateReadbackFailure()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Title="Receipt test",NumberToMint=1 });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExplicitNftSignerAndCommittedSendDigestSurviveReadbackFailure(bool agreeingSelectors)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"different-configured-secret");
            var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { FromNFTTokenAddress="fixture-nft",
                TokenId=agreeingSelectors ? "fixture-nft" : null,TokenAddress=agreeingSelectors ? "fixture-nft" : null,
                FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient",OwnerPrivateKey="test-only-explicit",Amount=1 });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,new string('1',32));
            Assert.IsFalse(result.Message.Contains("test-only-explicit"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task CommittedUnlockDigestSurvivesSeparateReadbackFailure()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var result=await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress="fixture-nft" });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            StringAssert.Contains(result.Message,new string('1',32));
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
}
