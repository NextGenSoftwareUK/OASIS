using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotNftMutationTests
{
    private static readonly Guid Avatar=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");
    private static readonly string Sender="0x"+new string('1',64),Recipient="0x"+new string('2',64),
        Contract="0x"+new string('d',64),Code="0x"+new string('c',64);
    private const string Token="{\"U128\":\"18446744073709551617\"}";
    private static MintWeb3NFTRequest Mint()=>new(){MintedByAvatarId=Avatar,CollectionPublicKey=Contract,NumberToMint=1,Amount=1,
        Title="Living tree",Symbol="TREE",Description="Native metadata",ImageUrl="oasis://test/image",
        MetaData=new(){{"polkadot:token-id",Token}}};
    private static SendWeb3NFTRequest Send()=>new(){FromNFTTokenAddress=Contract,FromWalletAddress=Sender,
        ToWalletAddress=Recipient,TokenId=Token,Amount=1,OwnerPublicKey=Sender,OwnerPrivateKey="fixture"};
    private static BurnWeb3NFTRequest Burn()=>new(){BurntByAvatarId=Avatar,NFTTokenAddress=Contract,
        TokenId=Token,OwnerPublicKey=Sender,OwnerPrivateKey="fixture",OwnerSeedPhrase=null!};
    private static MintWeb3NFTRequest DirectMint(){var value=Mint();value.SendToAddressAfterMinting=Recipient;return value;}
    private static Task<OASISResult<IWeb3NFTTransactionResponse>> Invoke(Provider provider,string action,bool sync)=>action switch {
        "mint"=>sync?Task.FromResult(provider.MintNFT(Mint())):provider.MintNFTAsync(Mint()),
        "mint-direct"=>sync?Task.FromResult(provider.MintNFT(DirectMint())):provider.MintNFTAsync(DirectMint()),
        "transfer"=>sync?Task.FromResult(provider.SendNFT(Send())):provider.SendNFTAsync(Send()),
        "burn"=>sync?Task.FromResult(provider.BurnNFT(Burn())):provider.BurnNFTAsync(Burn()),
        _=>throw new ArgumentException()
    };

    [DataTestMethod][DataRow("mint",false)][DataRow("mint",true)][DataRow("transfer",false)][DataRow("transfer",true)]
    [DataRow("burn",false)][DataRow("burn",true)]
    public async Task OrdinaryNftCallsRequireExistingAdmissionBeforeActivationOrWallet(string action,bool sync)
    {
        using var provider=new Provider();var result=await Invoke(provider,action,sync);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"admitted execution owner");Assert.AreEqual(0,provider.WalletLoads);
    }

    [DataTestMethod][DataRow("mint",false,"accepted")][DataRow("mint",true,"accepted")]
    [DataRow("transfer",false,"accepted")][DataRow("transfer",true,"accepted")]
    [DataRow("burn",false,"accepted")][DataRow("burn",true,"accepted")]
    [DataRow("mint-direct",false,"accepted")]
    [DataRow("mint",false,"effect-mismatch")][DataRow("transfer",false,"wrong-from")]
    [DataRow("transfer",false,"wrong-to")][DataRow("burn",false,"wrong-token")]
    [DataRow("mint",false,"wrong-action")][DataRow("burn",false,"read-error")]
    [DataRow("transfer",false,"dispatch-failed")][DataRow("mint",false,"wrong-contract")]
    public async Task PublicNftMethodsJoinSameOwnerAndRequireOriginalFinalizedEffects(string action,bool sync,string outcome)
    {
        // Controlled SDK replies/physical owner pipe, not native signature/ledger proof.
        var names=new[]{"OASIS_POLKADOT_SDK_BRIDGE","OASIS_POLKADOT_TEST_OUTCOME"};
        var prior=names.Select(Environment.GetEnvironmentVariable).ToArray();
        Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable(names[1],outcome);
        try {
            using var provider=new Provider();Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>Invoke(provider,action,sync),
                PolkadotOwnedTransferTests.Handle(store),"owned-original",Sender);
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual(action.StartsWith("mint",StringComparison.Ordinal)?1:0,provider.WalletLoads);
            Assert.AreEqual(outcome!="accepted",result.IsError,result.Exception?.ToString());
            if(result.IsError)Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
            else {
                Assert.IsTrue(result.IsSaved);Assert.AreEqual("0x"+new string('b',64),result.Result.TransactionResult);
                Assert.AreEqual(Contract,result.Result.Web3NFT.NFTTokenAddress);Assert.AreEqual(Token,result.Result.Web3NFT.TokenId);
                Assert.AreEqual(NFTStandardType.PSP34,result.Result.Web3NFT.NFTStandardType.Value);
                if(action=="mint-direct") {
                    Assert.AreEqual(result.Result.Web3NFT.MintTransactionHash,result.Result.Web3NFT.SendNFTTransactionHash);
                    Assert.AreEqual("mint-to-recipient",result.Result.Web3NFT.MetaData["polkadot:delivery-kind"]);
                }
            }
        } finally {for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}
    }

    [DataTestMethod][DataRow("prepared-token-mismatch")][DataRow("prepared-metadata-mismatch")]
    public async Task PreparedIntentMustMatchFrozenCallerBeforeAnyDurableAcknowledgement(string outcome)
    {
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);
        try {
            using var provider=new Provider();Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.MintNFTAsync(Mint()),
                PolkadotOwnedTransferTests.Handle(store),"owned-original",Sender);
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            Assert.AreEqual(0,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    [DataTestMethod][DataRow("1")][DataRow("{\"U128\":1}")][DataRow("{\"U8\":\"256\"}")]
    [DataRow("{\"U128\":\"01\"}")][DataRow("{\"Bytes\":\"0xAA\"}")]
    [DataRow("{\"U8\":\"1\",\"U8\":\"2\"}")][DataRow("{\"Guid\":\"application-record\"}")]
    public void AmbiguousOrNoncanonicalNativeIdsAreRejected(string value)
    {
        var method=typeof(PolkadotOASIS).GetMethod("CanonicalNftTokenId",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{value}));
    }
    [TestMethod]
    public void U128IdentityAboveJsSafeIntegerSurvivesWithoutNumericCoercion()
    {
        var method=typeof(PolkadotOASIS).GetMethod("CanonicalNftTokenId",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.AreEqual(Token,(string)method.Invoke(null,new object[]{Token})!);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RealSdkNormalizesSs58AndChecksCollectionAliasesBeforeSigning(bool conflict)
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Providers","Blockchain","NextGenSoftware.OASIS.API.Providers.PolkadotOASIS","SdkBridge","node_modules")))root=root.Parent;
        Assert.IsNotNull(root,"Reuse the repository's provisioned SDK dependencies; do not install a second copy in the test output.");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(root.FullName,"Providers","Blockchain","NextGenSoftware.OASIS.API.Providers.PolkadotOASIS","SdkBridge","bridge.mjs"));
        try {
            using var provider=new Provider();
            var type=typeof(PolkadotOASIS).GetNestedType("NftIntent",BindingFlags.NonPublic)!;
            var constructor=type.GetConstructors().Single();
            var parameters=constructor.GetParameters();
            var values=parameters.Select(parameter=>parameter.HasDefaultValue?parameter.DefaultValue:null).ToArray();
            values[0]="transfer";values[1]="161uVTfpLKVVoX217rxpbSxuJ2C4BVYemHM7bnxXbsiVNqYX";
            values[2]=Token;values[3]="5CT5jwBEAhveEjgiSCQbkaKcKcUyF3VJ8qNXM9rXsuQyn3Kd";
            values[4]=Recipient;values[5]=Sender;
            values[Array.FindIndex(parameters,parameter=>parameter.Name=="ContractAlias")]=conflict?Recipient:Contract;
            var intent=constructor.Invoke(values);
            var method=typeof(PolkadotOASIS).GetMethod("NormalizeNftIntentAccountsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var pending=(Task)method.Invoke(provider,new[]{intent,(object)CancellationToken.None})!;
            if(conflict)await Assert.ThrowsExceptionAsync<ArgumentException>(async()=>await pending);
            else {
                await pending;
                var normalized=pending.GetType().GetProperty("Result")!.GetValue(pending)!;
                Assert.AreEqual(Sender,type.GetProperty("Sender")!.GetValue(normalized));
                Assert.AreEqual(Contract,type.GetProperty("Contract")!.GetValue(normalized));
                Assert.AreEqual(Contract,type.GetProperty("ContractAlias")!.GetValue(normalized));
            }
        } finally { Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",prior); }
    }

    [DataTestMethod][DataRow("accepted")][DataRow("wrong-token")][DataRow("read-error")]
    public async Task WithdrawalKeepsNativeIdAndOriginalReconciliationEvidence(string outcome)
    {
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);
        try {
            using var provider=new Provider();Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.WithdrawNFTAsync(Contract,Token,Sender,"fixture"),
                PolkadotOwnedTransferTests.Handle(store),"owned-original",Sender);
            Assert.AreEqual(1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual(outcome!="accepted",result.IsError,result.Exception?.ToString());
            Assert.AreEqual("0x"+new string('b',64),result.Result.TransactionId);
            Assert.AreEqual(outcome=="accepted"?Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed:
                Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending,result.Result.Status);
            if(outcome!="accepted")Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    [DataTestMethod][DataRow("accepted",false)][DataRow("accepted",true)]
    [DataRow("wrong-token",true)][DataRow("read-error",false)][DataRow("source-mismatch",true)]
    public async Task DepositReleasesExistingNativeTokenAndBindsOptionalCustodyProof(string outcome,bool source)
    {
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);
        try {
            using var provider=new Provider();Activate(provider);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotOwnedTransferTests.AttemptObserver>();
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>provider.DepositNFTAsync(Contract,Token,Sender,
                source?"0x"+new string('e',64):null),PolkadotOwnedTransferTests.Handle(store),"owned-original",Recipient);
            Assert.AreEqual(outcome=="source-mismatch"?0:1,((PolkadotOwnedTransferTests.AttemptObserver)(object)store).Calls);
            Assert.AreEqual(outcome!="accepted",result.IsError,result.Exception?.ToString());
            Assert.AreEqual(0,provider.WalletLoads,"Releasing custody must not mint or resolve a new mint wallet.");
            if(outcome=="accepted")Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,result.Result.Status);
            else if(outcome!="source-mismatch") {
                Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending,result.Result.Status);
                Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
                Assert.AreEqual("0x"+new string('b',64),result.Result.TransactionId);
            }
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    private static void Activate(PolkadotOASIS provider) {
        typeof(PolkadotOASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
        typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider,"0x"+new string('a',64));
    }
    private sealed class Provider():PolkadotOASIS("http://127.0.0.1:1","fixture","fixture",Recipient,null,new PolkadotNftConfiguration(Code,Sender))
    {
        public int WalletLoads;
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId) {
            WalletLoads++;return Task.FromResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet{
                AvatarId=avatarId,ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress=Sender,PublicKey=Sender,PrivateKey="fixture"}});
        }
    }
}
