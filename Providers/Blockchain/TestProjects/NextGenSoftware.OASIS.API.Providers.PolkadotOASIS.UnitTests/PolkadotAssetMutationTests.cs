using System.Reflection;
using System.IO.Pipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotAssetMutationTests
{
    private static readonly Guid Avatar = Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");
    private static MintWeb3TokenRequest Mint() => new() { MintedByAvatarId = Avatar, TokenAddress = "424242",
        ToWalletAddress = "sender", Amount = 1.250001m,
        ProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.Default) };
    private static BurnWeb3TokenRequest Burn() => new() { BurntByAvatarId = Avatar, TokenAddress = "424242",
        Amount = 1.250001m, OwnerPublicKey = "sender", OwnerPrivateKey = "fixture", OwnerSeedPhrase = null! };
    private static SendWeb3TokenRequest Send() => new() { FromTokenAddress="424242",FromWalletAddress="sender",
        ToWalletAddress="sender",Amount=1.250001m,OwnerPublicKey="sender",OwnerPrivateKey="fixture",OwnerSeedPhrase=null! };

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task AssetSendRequiresAnAdmittedOwner(bool synchronous)
    {
        using var provider=new WalletProvider();
        var result=synchronous ? provider.SendToken(Send()) : await provider.SendTokenAsync(Send());
        Assert.IsTrue(result.IsError);StringAssert.Contains(result.Message,"admitted execution owner");
    }

    [DataTestMethod][DataRow(false,"accepted")][DataRow(true,"accepted")][DataRow(false,"dispatch-failed")]
    [DataRow(false,"read-error")][DataRow(false,"effect-mismatch")][DataRow(false,"wrong-units")][DataRow(false,"wrong-action")]
    public async Task AssetSendUsesExistingOwnerAndExactEffectChecks(bool synchronous,string outcome)
    {
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);
        try {
            using var provider=new WalletProvider();Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>synchronous
                ? Task.FromResult(provider.SendToken(Send())) : provider.SendTokenAsync(Send()),
                PolkadotOwnedTransferTests.Handle(store),"owned-original","sender");
            Assert.AreEqual(outcome!="accepted",result.IsError,result.Exception?.ToString());
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual(0,provider.WalletLoads,"Explicit transfer signer must not load a service/default mint wallet.");
            if(result.IsError)Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    [DataTestMethod][DataRow("id")][DataRow("amount")][DataRow("recipient")][DataRow("memo")][DataRow("application-id")]
    public async Task InvalidAssetSendIntentFailsBeforeSigning(string mode)
    {
        using var provider=new WalletProvider();Activate(provider);
        var request=Send();
        if(mode=="id")request.FromTokenAddress="0424242";
        if(mode=="amount")request.Amount=0;
        if(mode=="recipient")request.ToWalletAddress="";
        if(mode=="memo")request.MemoText="do not silently discard";
        if(mode=="application-id")request.Web3TokenId=Guid.NewGuid();
        var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.SendTokenAsync(request),
            PolkadotOwnedTransferTests.Handle(),"owned-original","sender");
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task MintAndBurnRequireAnAdmittedOwnerBeforeWalletOrTransport(bool burn, bool sync)
    {
        using var provider = new WalletProvider();
        var result = burn ? (sync ? provider.BurnToken(Burn()) : await provider.BurnTokenAsync(Burn()))
            : (sync ? provider.MintToken(Mint()) : await provider.MintTokenAsync(Mint()));
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message,"admitted execution owner");
        Assert.AreEqual(0,provider.WalletLoads);
    }

    [DataTestMethod]
    [DataRow(false,false,"accepted")][DataRow(false,true,"accepted")]
    [DataRow(true,false,"accepted")][DataRow(true,true,"accepted")]
    [DataRow(false,false,"dispatch-failed")][DataRow(true,false,"dispatch-failed")]
    [DataRow(false,false,"read-error")][DataRow(true,false,"read-error")]
    [DataRow(false,false,"effect-mismatch")][DataRow(true,false,"effect-mismatch")]
    [DataRow(false,false,"wrong-units")][DataRow(true,false,"wrong-units")]
    [DataRow(false,false,"wrong-action")][DataRow(true,false,"wrong-action")]
    public async Task OrdinaryAssetMethodsUseSharedOwnerAndRequireExactFinalizedEffects(bool burn, bool sync, string outcome)
    {
        // Controlled signing replies/default wallet + real process/pipe. NOT SDK/ledger/Mongo/authorization proof.
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);
        try {
            using var provider=new WalletProvider();
            Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(() => burn
                ? (sync ? Task.FromResult(provider.BurnToken(Burn())) : provider.BurnTokenAsync(Burn()))
                : (sync ? Task.FromResult(provider.MintToken(Mint())) : provider.MintTokenAsync(Mint())),
                PolkadotOwnedTransferTests.Handle(store),"owned-original","sender");
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual(burn ? 0 : 1,provider.WalletLoads);
            Assert.AreEqual(outcome != "accepted",result.IsError,result.Exception?.ToString());
            if (result.IsError) Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
            else StringAssert.Contains(result.Message,"exact verified effects");
        }
        finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    [DataTestMethod][DataRow("asset")][DataRow("recipient")][DataRow("amount")][DataRow("zero")][DataRow("memo")][DataRow("avatar")][DataRow("id-leading-zero")][DataRow("request-provider")]
    public async Task InvalidMintIntentFailsBeforeWalletLookup(string mode)
    {
        using var provider=new WalletProvider();
        Activate(provider);
        var request=Mint(); request.MetaData=new();
        if(mode=="asset")request.MetaData["TokenAddress"]="1";
        if(mode=="recipient")request.MetaData["MintToWalletAddress"]="different";
        if(mode=="amount")request.MetaData["Amount"]="2";
        if(mode=="zero")request.Amount=0;
        if(mode=="memo")request.MemoText="must not discard";
        if(mode=="avatar")request.MintedByAvatarId=Guid.Empty;
        if(mode=="id-leading-zero")request.TokenAddress="0424242";
        if(mode=="request-provider")request.ProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.MintTokenAsync(request),
            PolkadotOwnedTransferTests.Handle(),"owned-original","sender");
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(0,provider.WalletLoads);
    }

    internal static void Activate(PolkadotOASIS provider) {
        typeof(PolkadotOASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
        typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider,"0x"+new string('a',64));
    }

    [DataTestMethod][DataRow("avatar")][DataRow("provider")][DataRow("missing")][DataRow("error")]
    public async Task MissingOrMismatchedWalletCannotBecomeAServiceWalletFallback(string mode)
    {
        using var provider=new WalletProvider();
        Activate(provider);
        provider.WalletFactory=id=>Task.FromResult(new OASISResult<IProviderWallet>{
            IsError=mode=="error", Result=mode=="missing" ? null! : new ProviderWallet {
                AvatarId=mode=="avatar" ? Guid.NewGuid() : id,
                ProviderType=mode=="provider" ? Core.Enums.ProviderType.EthereumOASIS : Core.Enums.ProviderType.PolkadotOASIS
            }});
        var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.MintTokenAsync(Mint()),
            PolkadotOwnedTransferTests.Handle(),"owned-original","sender");
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message,mode is "avatar" or "provider" ? "does not belong" : "could not be loaded");
        Assert.AreEqual(1,provider.WalletLoads);
    }

    [TestMethod]
    public async Task RequestMutationWhileLoadingWalletCannotChangeSignedIntent()
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        try {
            using var provider=new WalletProvider();Activate(provider);
            var release=new TaskCompletionSource<OASISResult<IProviderWallet>>(TaskCreationOptions.RunContinuationsAsynchronously);
            provider.WalletFactory=_=>release.Task;
            var request=Mint();
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var pending=provider.ExecutePreparedNativeOperationAsync(()=>provider.MintTokenAsync(request),
                PolkadotOwnedTransferTests.Handle(store),"owned-original","sender");
            Assert.AreEqual(1,provider.WalletLoads);
            request.TokenAddress="1";request.ToWalletAddress="changed";request.Amount=2;
            release.SetResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet{AvatarId=Avatar,
                ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress="sender",PublicKey="sender",PrivateKey="fixture"}});
            var result=await pending;
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
        } finally {Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",prior);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CancellationAfterAssetSubmissionRetainsOriginalAndStopsOwnedRead(bool burn)
    {
        var names=new[]{"OASIS_POLKADOT_SDK_BRIDGE","OASIS_POLKADOT_TEST_OUTCOME","OASIS_POLKADOT_TEST_FINALITY_PIPE"};
        var prior=names.Select(Environment.GetEnvironmentVariable).ToArray();
        var pipeName="oasis-polkadot-asset-finality-"+Guid.NewGuid().ToString("N");
        using var entered=new NamedPipeServerStream(pipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable(names[1],"wait-for-cancellation");
        Environment.SetEnvironmentVariable(names[2],OperatingSystem.IsWindows() ? @"\\.\pipe\"+pipeName : Path.Combine(Path.GetTempPath(),"CoreFxPipe_"+pipeName));
        try {
            using var provider=new WalletProvider();Activate(provider);
            using var cancelled=new CancellationTokenSource();
            using var observation=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var pending=provider.ExecutePreparedNativeOperationAsync(()=>burn ? provider.BurnTokenAsync(Burn()) : provider.MintTokenAsync(Mint()),
                PolkadotOwnedTransferTests.Handle(store),"owned-original","sender",cancelled.Token);
            await entered.WaitForConnectionAsync(observation.Token);
            Assert.AreEqual(1,await entered.ReadAsync(new byte[1],observation.Token));
            cancelled.Cancel();
            var result=await pending.WaitAsync(observation.Token);
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType(result.Exception,typeof(OperationCanceledException));
            Assert.AreEqual("0x"+new string('b',64),((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId);
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
        } finally {for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}
    }

    private sealed class WalletProvider : PolkadotOASIS {
        public int WalletLoads;
        public Func<Guid,Task<OASISResult<IProviderWallet>>>? WalletFactory;
        public WalletProvider() : base("http://127.0.0.1:1") { }
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId) {
            WalletLoads++;
            if(WalletFactory!=null)return WalletFactory(avatarId);
            return Task.FromResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet {
                AvatarId=avatarId,ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress="sender",PublicKey="sender",PrivateKey="fixture"
            }});
        }
    }
}
