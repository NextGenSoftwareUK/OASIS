using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiUnlockReadbackFailureTests
{
    [DataTestMethod]
    [DataRow("-1",0)] [DataRow("-1",1)] [DataRow("-1",2)]
    [DataRow("1.5",0)] [DataRow("1.5",1)] [DataRow("1.5",2)]
    [DataRow("+1",0)] [DataRow("+1",1)] [DataRow("+1",2)]
    [DataRow("01",0)] [DataRow("01",1)] [DataRow("01",2)]
    [DataRow("18446744073709551616",0)] [DataRow("18446744073709551616",1)] [DataRow("18446744073709551616",2)]
    [DataRow(" 1",0)] [DataRow(" 1",1)] [DataRow(" 1",2)]
    [DataRow("1,000",0)] [DataRow("1,000",1)] [DataRow("1,000",2)]
    [DataRow("",0)] [DataRow("",1)] [DataRow("",2)]
    public async Task NativeBalanceRejectsNonCanonicalUnsignedUnits(string units,int surface)
    {
        await WithProcessFixture(async provider=>{
            var request=new Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest {WalletAddress="balance-units:"+units};
            if(surface==0){var result=await provider.GetAccountBalanceAsync(request.WalletAddress);Assert.IsTrue(result.IsError);}
            else {var result=surface==1?provider.GetBalance(request):await provider.GetBalanceAsync(request);Assert.IsTrue(result.IsError);}
        });
    }

    [DataTestMethod]
    [DataRow("0",0)] [DataRow("0",1)] [DataRow("0",2)]
    [DataRow("1",0)] [DataRow("1",1)] [DataRow("1",2)]
    [DataRow("18446744073709551615",0)] [DataRow("18446744073709551615",1)] [DataRow("18446744073709551615",2)]
    public async Task NativeBalancePreservesCanonicalUnitsThroughEachPublicSurface(string units,int surface)
    {
        await WithProcessFixture(async provider=>{
            var expected=decimal.Parse(units,System.Globalization.CultureInfo.InvariantCulture)/1000000000m;
            var request=new Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest {WalletAddress="balance-units:"+units};
            if(surface==0){var result=await provider.GetAccountBalanceAsync(request.WalletAddress);Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(expected,result.Result);}
            else {var result=surface==1?provider.GetBalance(request):await provider.GetBalanceAsync(request);Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual((double)expected,result.Result);}
        });
    }

    [DataTestMethod]
    [DataRow(false,"0xab",false)] [DataRow(true,"0xab",false)]
    [DataRow(false,"0xAB",false)] [DataRow(true,"0xAB",false)]
    [DataRow(false,"0xac",true)] [DataRow(true,"0xac",true)]
    public async Task TransferReadbackComparesCanonicalSuiRecipient(bool synchronous,string recipient,bool rejected)
    {
        await WithProcessFixture(async provider=>{
            var request=new SendWeb3NFTRequest { TokenAddress="canonical-owner",FromWalletAddress="fixture-owner",
                ToWalletAddress=recipient,OwnerPrivateKey="test-only-explicit",Amount=1 };
            var result=synchronous?provider.SendNFT(request):await provider.SendNFTAsync(request);
            Assert.AreEqual(rejected,result.IsError,result.Message);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            if(rejected)AssertCommittedReadbackEvidence(result.Exception);
            else Assert.AreEqual("0x"+"ab".PadLeft(64,'0'),result.Result.Web3NFT.SendToAddressAfterMinting);
        });
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SendPreservesOriginalWalletIntentAcrossSignerAwait(bool changeSender)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try{
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"different-configured-secret");
            var recipient="0x"+string.Concat(Enumerable.Repeat("ab",32));
            var request=new SendWeb3NFTRequest{TokenAddress="ownership-matching",FromWalletAddress="fixture-owner",
                ToWalletAddress=recipient,OwnerPrivateKey="test-only-explicit",Amount=1};
            var pending=provider.SendNFTAsync(request);Assert.IsFalse(pending.IsCompleted);
            if(changeSender)request.FromWalletAddress="changed-sender";else request.ToWalletAddress="changed-recipient";
            request.TokenAddress="changed-object";
            var result=await pending;Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            Assert.AreEqual(recipient,result.Result.Web3NFT.SendToAddressAfterMinting);
        }finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task HistoryMetadataPreservesCoinIdentityAndDoesNotInventAMemo(bool monetary)
    {
        await WithProcessFixture(async provider=>{
            var history=await provider.GetTransactionsAsync(new Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest {
                WalletAddress=monetary?"fixture-coin":"fixture-object"});
            Assert.IsFalse(history.IsError,history.Message);var row=history.Result.Single();
            Assert.AreEqual(monetary?"0x2::sui::SUI":null,row.Token);
            Assert.AreEqual(Core.Enums.ProviderType.SuiOASIS,row.ProviderType);
            Assert.IsNull(row.MemoText);Assert.IsNull(row.MemoBase64);
        });
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeBridgeErrorsPreserveOriginalReconciliationIdentity(bool deposit)
    {
        await WithProcessFixture(async provider=>{
            var result=deposit?await provider.DepositAsync(1m,"fixture-recipient")
                :await provider.WithdrawAsync(1m,"fixture-sender","test-only-secret");
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<SuiNativeMutationReconciliationException>(result.Exception);
            var original=(SuiNativeMutationReconciliationException)result.Exception;
            Assert.AreEqual(new string('1',32),original.OriginalTransactionId);
            Assert.AreEqual(true,original.ExecutionSucceeded);
        });
    }
    [TestMethod]
    public async Task OffChainOnlyMetadataCannotBeSilentlyPublished()
    {
        using var provider=new SuiOASIS();
        var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { StoreNFTMetaDataOnChain=false });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        StringAssert.Contains(result.Message,"must not be silently published");
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("fixture-chain",false)] [DataRow("foreign-chain",true)] [DataRow("",true)]
    public async Task NativeFeeEvidenceRequiresConfiguredExactChain(string chain,bool rejected)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try{
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",chainId:chain);
            Core.Interfaces.IOASISNativeFeeReceiptProvider capability=provider;
            var result=await capability.GetNativeFeeReceiptAsync(new string('1',32));
            Assert.AreEqual(rejected,result.IsError);Assert.IsFalse(provider.IsProviderActivated);
            if(rejected)Assert.AreEqual(System.Text.Json.JsonValueKind.Undefined,result.Result.ValueKind);
            else{
                Assert.AreEqual(chain,result.Result.GetProperty("chainIdentifier").GetString());
                Assert.IsFalse(result.Result.GetProperty("success").GetBoolean());Assert.AreEqual("5",result.Result.GetProperty("netUnits").GetString());
            }
        }finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
    [DataTestMethod]
    [DataRow(false,false,false,false)] [DataRow(true,false,false,false)] [DataRow(false,true,false,false)] [DataRow(false,false,true,false)] [DataRow(false,false,false,true)]
    public async Task PreparedProcessWaitsForParentAndRetainsPersistenceRejection(bool reject,bool wrongId,bool cancel,bool foreignError)
    {
        await WithProcessFixture(async provider=>{
            var backend=typeof(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase)
                .GetProperty("Backend",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var client=typeof(SuiOASIS).GetField("_sdkClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var calls=0;var failure=new InvalidOperationException("controlled durable persistence rejection");
            using var cancellation=new CancellationTokenSource();
            if(cancel)cancellation.CancelAfter(TimeSpan.FromSeconds(1));
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((evidence,_)=>{
                calls++;Assert.AreEqual(new string('1',32),evidence.GetProperty("transactionHash").GetString());
                return reject?Task.FromException(failure):Task.CompletedTask;
            });
            var operation=(Task<System.Text.Json.JsonElement>)client.GetType().GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(client,new object[]{"put",new{mode=cancel?"prepared-hold":foreignError?"prepared-error":wrongId?"prepared-wrong-id":"prepared"},server,cancellation.Token})!;
            if(cancel){var cancelled=await Assert.ThrowsExceptionAsync<SuiNativeMutationCanceledException>(()=>operation);Assert.AreEqual(new string('1',32),cancelled.OriginalTransactionId);Assert.AreEqual(cancellation.Token,cancelled.CancellationToken);}
            else if(reject)Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>operation));
            else if(wrongId || foreignError) {
                var mismatch=await Assert.ThrowsExceptionAsync<SuiNativeMutationReconciliationException>(()=>operation);
                Assert.AreEqual(new string('1',32),mismatch.OriginalTransactionId);Assert.IsNull(mismatch.ExecutionSucceeded);
            }
            else Assert.AreEqual(new string('1',32),(await operation).GetProperty("transactionHash").GetString());
            Assert.AreEqual(1,calls);
        });
    }

    [TestMethod]
    public async Task PreparedProcessCannotReturnSuccessWithoutPersistenceAcknowledgement()
    {
        await WithProcessFixture(async provider=>{
            var backend=typeof(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase)
                .GetProperty("Backend",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var client=typeof(SuiOASIS).GetField("_sdkClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
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
            AssertCommittedReadbackEvidence(result.Exception);
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow("valid", false)]
    [DataRow("short-recipient", false)]
    [DataRow("missing", true)]
    [DataRow("duplicate-token", true)]
    [DataRow("foreign-id", true)]
    [DataRow("duplicate-id", true)]
    [DataRow("foreign-creator", true)]
    [DataRow("foreign-recipient", true)]
    public async Task CommittedMintRequiresCompleteOriginalIdentitySet(string mode, bool rejected)
    {
        await WithProcessFixture(async provider =>
        {
            var result = await provider.MintNFTAsync(new MintWeb3NFTRequest { Title = "identity/" + mode, NumberToMint = 2,
                SendToAddressAfterMinting = mode == "short-recipient" ? "0xab" : null });
            Assert.AreEqual(rejected, result.IsError);
            Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1', 32), result.Result.TransactionResult);
            if (rejected) AssertCommittedReadbackEvidence(result.Exception);
            else
            {
                Assert.AreEqual(2, result.Result.Web3NFTs.Count());
                Assert.AreEqual(2, result.Result.Web3NFTs.Select(nft => nft.Id).Distinct().Count());
            }
        });
    }

    [DataTestMethod]
    [DataRow("valid", false)]
    [DataRow("valid", true)]
    [DataRow("foreign-preparation-chain", false)]
    [DataRow("invalid-signature", false)]
    public async Task PublicPreparedMintVerifiesAndAwaitsOwnerPersistence(string mode, bool rejectPersistence)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "unlock-readback-failure.mjs"));
        try
        {
            using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", chainId: "fixture-chain", privateKey: "test-only-secret");
            var calls = 0;
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var failure = new IOException("controlled owner persistence failure");
            var execution = provider.MintNFTAsync(new MintWeb3NFTRequest { Title = "identity/" + mode, NumberToMint = 2 },
                async (evidence, token) =>
                {
                    calls++;
                    Assert.AreEqual("fixture-chain", evidence.GetProperty("chainId").GetString());
                    Assert.AreEqual(new string('1', 32), evidence.GetProperty("transactionHash").GetString());
                    entered.TrySetResult();
                    await release.Task.WaitAsync(token);
                    if (rejectPersistence) throw failure;
                }, CancellationToken.None);
            if (mode == "valid")
            {
                try
                {
                    await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    Assert.IsFalse(execution.IsCompleted, "Mint must wait for durable owner acknowledgement.");
                }
                finally { release.TrySetResult(); }
            }
            var result = await execution;
            Assert.AreEqual(mode == "valid" ? 1 : 0, calls);
            Assert.AreEqual(rejectPersistence || mode != "valid", result.IsError);
            if (rejectPersistence) Assert.AreSame(failure, result.Exception);
            if (!result.IsError) Assert.AreEqual(2, result.Result.Web3NFTs.Count);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", previous); }
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
            AssertCommittedReadbackEvidence(result.Exception);
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            Assert.AreEqual(new string('1',32),result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,new string('1',32));
            Assert.IsFalse(result.Message.Contains("test-only-explicit"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CommittedSendRequiresReadbackOwnerToMatchRecipient(bool matching, bool mixedCase)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", privateKey:"different-configured-secret");
            var result = await provider.SendNFTAsync(new SendWeb3NFTRequest {
                TokenAddress = matching ? "ownership-matching" : "ownership-foreign",
                FromWalletAddress="fixture-owner", ToWalletAddress="0x" + string.Concat(Enumerable.Repeat(mixedCase ? "AB" : "ab",32)),
                OwnerPrivateKey="test-only-explicit", Amount=1 });
            Assert.AreEqual(!matching, result.IsError);
            Assert.AreEqual(new string('1',32), result.Result.SendNFTTransactionResult);
            if (matching) Assert.AreEqual("0x" + string.Concat(Enumerable.Repeat("ab",32)), result.Result.Web3NFT.SendToAddressAfterMinting);
            else AssertCommittedReadbackEvidence(result.Exception);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", previous); }
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
            AssertCommittedReadbackEvidence(result.Exception);
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('1',32),result.Result.TransactionResult);
            StringAssert.Contains(result.Message,new string('1',32));
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
    private static void AssertCommittedReadbackEvidence(Exception? error)
    {
        Assert.IsInstanceOfType<SuiNativeMutationReconciliationException>(error);
        var evidence=(SuiNativeMutationReconciliationException)error;
        Assert.AreEqual(new string('1',32),evidence.OriginalTransactionId);Assert.AreEqual(true,evidence.ExecutionSucceeded);
        Assert.IsNotNull(evidence.InnerException);
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
    }
}
