using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearTokenEscrowTests
{
    private static readonly Guid Actor=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),Deposit=Guid.Parse("11111111-1111-1111-1111-111111111111");
    private sealed class Provider:NEAROASIS
    {
        public OASISResult<IProviderWallet> Wallet=new(new ProviderWallet{AvatarId=Actor,ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.NEAROASIS,WalletAddress="holder.near",PrivateKey="owner-key",PublicKey="ed25519:fixture-public"});
        public readonly TaskCompletionSource Entered=new(TaskCreationOptions.RunContinuationsAsynchronously),Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Pause;public int WalletLoads;public Guid SeenActor;
        public Provider(string chain="fixture"):base("http://127.0.0.1:1","fixture",chain,"contract.near","service.near","service-key"){}
        protected override Task<OASISResult<IProviderWallet>> LoadSigningWalletAsync(Guid actor){WalletLoads++;SeenActor=actor;return Task.FromResult(Wallet);}
        public override async Task<OASISResult<bool>> ActivateProviderAsync(){if(Pause){Entered.TrySetResult();await Release.Task;}return new(true);}
    }
    private sealed class Fixture:IDisposable
    {
        private readonly string[] names={"OASIS_NEAR_SDK_BRIDGE","OASIS_NEAR_ESCROW_FIXTURE_ROOT"};private readonly string?[] prior;
        private readonly string root=Path.Combine(AppContext.BaseDirectory,"TestResults","NearTokenEscrowCurrent");private readonly Guid ownerToken=Guid.NewGuid();
        public Fixture(string mode,bool releasing=false){
            Directory.CreateDirectory(root);if(Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Preserve existing owned fixture before reuse");
            prior=names.Select(Environment.GetEnvironmentVariable).ToArray();Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures","token-escrow.mjs"));Environment.SetEnvironmentVariable(names[1],root);
            object? deposit=releasing||mode=="duplicate"?new{protocol=1,lock_id=Deposit.ToString("N"),symbol="OWN",owner_id=mode=="foreign"?"attacker.near":"holder.near",amount="250000000",released=mode=="released"}:null;
            using var file=new FileStream(Path.Combine(root,"state.json"),FileMode.CreateNew);file.Write(JsonSerializer.SerializeToUtf8Bytes(new{ownerToken,mode,deposit,balance=releasing?"750000000":"1000000000",frozen=releasing,calls=0}));
        }
        public int NativeCalls{get{using var state=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"state.json")));return state.RootElement.GetProperty("calls").GetInt32();}}
        public string[] Methods=>File.Exists(Path.Combine(root,"calls.ndjson"))?File.ReadAllLines(Path.Combine(root,"calls.ndjson")).Select(line=>{using var row=JsonDocument.Parse(line);return row.RootElement.GetProperty("method").GetString()??"";}).ToArray():Array.Empty<string>();
        public void Dispose(){try{var paths=Directory.GetFiles(root);if(paths.Any(p=>Path.GetFileName(p) is not ("state.json" or "calls.ndjson")))throw new InvalidOperationException("Unowned fixture content preserved");using var state=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"state.json")));Assert.AreEqual(ownerToken.ToString("D"),state.RootElement.GetProperty("ownerToken").GetString());foreach(var path in paths)File.Delete(path);}finally{for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}}
    }
    private static LockWeb3TokenRequest Request()=>new(){TokenAddress="OWN",LockId=Deposit,Amount=2.5m,EscrowContractAddress="contract.near",LockedByAvatarId=Actor};
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)]
    public async Task ExactLockAndReleaseUseRequestedOrAvatarWallet(bool releasing,bool explicitKey){
        using var fixture=new Fixture("accepted",releasing);var provider=new Provider();Assert.IsInstanceOfType(provider,typeof(IOASISIdBoundTokenEscrowProvider));
        var request=Request();if(explicitKey&&!releasing){request.FromWalletAddress="holder.near";request.FromWalletPrivateKey="owner-key";}
        var result=releasing?await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress="OWN",LockId=Deposit,EscrowContractAddress="contract.near",UnlockedByAvatarId=Actor}):await provider.LockTokenAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual("controlled-token-escrow-original",result.Result.TransactionResult);Assert.AreEqual(1,fixture.NativeCalls);
        Assert.AreEqual(explicitKey&&!releasing?0:1,provider.WalletLoads);CollectionAssert.Contains(fixture.Methods,releasing?"token_release":"token_lock");Assert.IsFalse(fixture.Methods.Contains("token_set_locked"));
    }
    [DataTestMethod][DataRow("amount-zero")][DataRow("id")][DataRow("escrow")][DataRow("escrow-blank")][DataRow("precision")][DataRow("source")][DataRow("key-without-source")][DataRow("wallet-foreign")][DataRow("wallet-warning")][DataRow("wallet-provider")][DataRow("unowned-source")][DataRow("token")][DataRow("chain")]
    public async Task InvalidSelectorsAndWalletCannotFallBackToFreezeOrServiceKey(string mode){
        using var fixture=new Fixture("accepted");var provider=new Provider(mode=="chain"?"different-network":"fixture");var request=Request();
        switch(mode){case "unowned-source":request.LockedByAvatarId=Guid.Empty;request.FromWalletAddress="holder.near";break;case "token":request.TokenAddress="";break;case "amount-zero":request.Amount=0;break;case "id":request.LockId=Guid.Empty;break;case "escrow":request.EscrowContractAddress="other.near";break;case "escrow-blank":request.EscrowContractAddress="";break;case "precision":request.Amount=0.000000001m;break;case "source":request.FromWalletAddress="attacker.near";break;case "key-without-source":request.FromWalletPrivateKey="owner-key";break;case "wallet-foreign":provider.Wallet.Result.AvatarId=Guid.NewGuid();break;case "wallet-warning":provider.Wallet.IsWarning=true;break;case "wallet-provider":provider.Wallet.Result.ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS;break;}
        var result=await provider.LockTokenAsync(request);Assert.IsTrue(result.IsError);Assert.AreEqual(0,fixture.NativeCalls);Assert.IsFalse(fixture.Methods.Contains("token_set_locked"));
    }
    [DataTestMethod][DataRow("protocol",false)][DataRow("duplicate",false)][DataRow("foreign",true)][DataRow("released",true)][DataRow("balance-format",false)]
    public async Task NativePreflightRefusesUnknownProtocolOrWrongOriginalBeforeMutation(string mode,bool releasing){
        using var fixture=new Fixture(mode,releasing);var provider=new Provider();var result=releasing?await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress="OWN",LockId=Deposit,EscrowContractAddress="contract.near",UnlockedByAvatarId=Actor}):await provider.LockTokenAsync(Request());
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,fixture.NativeCalls);Assert.IsFalse(fixture.Methods.Contains("token_set_locked"));
    }
    [DataTestMethod][DataRow("post-id")][DataRow("post-owner")][DataRow("post-amount")][DataRow("post-state")][DataRow("post-extra")][DataRow("post-balance")]
    public async Task EveryAcknowledgedMismatchKeepsOriginalForReconciliation(string mode){
        using var fixture=new Fixture(mode);var result=await new Provider().LockTokenAsync(Request());Assert.IsTrue(result.IsError);Assert.AreEqual(1,fixture.NativeCalls);
        Assert.IsInstanceOfType(result.Exception,typeof(NativeTransactionFollowupException));Assert.AreEqual("controlled-token-escrow-original",((NativeTransactionFollowupException)result.Exception).OriginalTransactionId);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
    }
    [TestMethod]
    public async Task KnownNativeRejectionRemainsRejectedWithOriginalIdentity(){using var fixture=new Fixture("reject");var result=await new Provider().LockTokenAsync(Request());Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(NearMutationRejectedException));Assert.AreEqual("controlled-token-escrow-original",((NearMutationRejectedException)result.Exception).OriginalTransactionId);Assert.AreEqual(1,fixture.NativeCalls);}
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SelectorFreeLegacyFreezeUsesActualIssuerWalletAndReadback(bool unlocking){using var fixture=new Fixture("accepted",unlocking);var provider=new Provider();var result=unlocking?await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress="OWN",UnlockedByAvatarId=Actor}):await provider.LockTokenAsync(new LockWeb3TokenRequest{TokenAddress="OWN",LockedByAvatarId=Actor});Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual(1,fixture.NativeCalls);Assert.IsFalse(fixture.Methods.Contains("token_escrow_protocol"));CollectionAssert.Contains(fixture.Methods,"token_set_locked");}
    [DataTestMethod][DataRow("issuer")][DataRow("post-freeze")]
    public async Task LegacyFreezeRejectsForeignIssuerAndPreservesAcknowledgedReadbackFailure(string mode){using var fixture=new Fixture(mode);var result=await new Provider().LockTokenAsync(new LockWeb3TokenRequest{TokenAddress="OWN",LockedByAvatarId=Actor});Assert.IsTrue(result.IsError);Assert.AreEqual(mode=="issuer"?0:1,fixture.NativeCalls);if(mode=="post-freeze")Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));}
    [TestMethod]
    public async Task CallerChangesDuringActivationCannotChangeCapturedDeposit(){using var fixture=new Fixture("accepted");var provider=new Provider{Pause=true};var request=Request();var pending=provider.LockTokenAsync(request);try{await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));request.LockId=Guid.NewGuid();request.Amount=9;request.TokenAddress="CHANGED";request.EscrowContractAddress="changed.near";request.LockedByAvatarId=Guid.NewGuid();provider.Release.SetResult();var result=await pending.WaitAsync(TimeSpan.FromSeconds(10));Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual(Actor,provider.SeenActor);Assert.AreEqual(1,fixture.NativeCalls);}finally{provider.Release.TrySetResult();}}
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NftLockUsesExactAvatarWallet(bool unlocking){
        using var fixture=new Fixture("accepted",unlocking);var provider=new Provider();
        var result=unlocking?await provider.UnlockNFTAsync(new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.UnlockWeb3NFTRequest{NFTTokenAddress="owned-nft",UnlockedByAvatarId=Actor})
            :await provider.LockNFTAsync(new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.LockWeb3NFTRequest{NFTTokenAddress="owned-nft",LockedByAvatarId=Actor});
        Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual(Actor,provider.SeenActor);Assert.AreEqual(1,provider.WalletLoads);Assert.AreEqual(1,fixture.NativeCalls);
        CollectionAssert.Contains(fixture.Methods,"nft_set_locked");
    }
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task NftActorCannotUseForeignOrWarningWallet(bool unlocking,bool warning){
        using var fixture=new Fixture("accepted");var provider=new Provider();if(warning)provider.Wallet.IsWarning=true;else provider.Wallet.Result.AvatarId=Guid.NewGuid();
        var result=unlocking?await provider.UnlockNFTAsync(new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.UnlockWeb3NFTRequest{NFTTokenAddress="owned-nft",UnlockedByAvatarId=Actor})
            :await provider.LockNFTAsync(new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.LockWeb3NFTRequest{NFTTokenAddress="owned-nft",LockedByAvatarId=Actor});
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,fixture.NativeCalls);Assert.AreEqual(Actor,provider.SeenActor);
    }

    [TestMethod]
    public async Task NftActorAndTokenAreCapturedBeforeActivation(){
        using var fixture=new Fixture("accepted");var provider=new Provider{Pause=true};
        var request=new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.LockWeb3NFTRequest{NFTTokenAddress="owned-nft",LockedByAvatarId=Actor};
        var pending=provider.LockNFTAsync(request);
        try{await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));request.NFTTokenAddress="changed";request.LockedByAvatarId=Guid.NewGuid();provider.Release.SetResult();
            var result=await pending.WaitAsync(TimeSpan.FromSeconds(10));Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual(Actor,provider.SeenActor);Assert.AreEqual(1,fixture.NativeCalls);
        }finally{provider.Release.TrySetResult();}
    }

}
