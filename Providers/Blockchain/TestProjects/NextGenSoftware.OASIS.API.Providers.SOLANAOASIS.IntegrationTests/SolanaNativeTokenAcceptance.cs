using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public sealed partial class SOLANAOASISIntegrationTests
{
    // Isolates only avatar key lookup; every token instruction/read uses the actual retained validator.
    private sealed class NativeIssuer : SolanaOASIS
    {
        public int KeyLookups;public Guid LastAvatar;
        public NativeIssuer():base(Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!,Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!,Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!){}
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)
        {KeyLookups++;LastAvatar=avatarId;return new(){Result=new(){Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!}};}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task LegacySplPublicCreateMintBurnVerifiesFinalizedEffectsAndCosts(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE") ?? throw new InvalidOperationException("Native evidence directory required.");
        Assert.IsTrue(Path.IsPathFullyQualified(work));Assert.IsTrue(Directory.Exists(work));
        var journal=Path.Combine(work,"spl-token.pending.json");Assert.IsFalse(File.Exists(journal));
        var costReport=Path.Combine(work,synchronous?"spl-token-sync-cost.json":"spl-token-async-cost.json");
        Assert.IsFalse(File.Exists(costReport),"Preserve the original cost evidence before another run.");
        var rpc=ClientFactory.GetClient(Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!);
        var owner=Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!;var avatar=Guid.NewGuid();
        var before=await rpc.GetBalanceAsync(owner,Commitment.Finalized);Assert.IsTrue(before.WasSuccessful);
        var mintRent=await rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize,Commitment.Finalized);
        var holderRent=await rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize,Commitment.Finalized);
        Assert.IsTrue(mintRent.WasSuccessful);Assert.IsTrue(holderRent.WasSuccessful);
        var intent=new Dictionary<string,object>{{"Avatar",avatar},{"Authority",owner},{"Synchronous",synchronous},{"State","before-create"}};
        void SaveIntent()=>File.WriteAllText(journal,System.Text.Json.JsonSerializer.Serialize(intent));
        var provider=new NativeIssuer();Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try {
            SaveIntent();
            var request=new CreateWeb3TokenRequest{CreatedByAvatarId=avatar,AuthorityWalletAddress=owner,Decimals=6,InitialSupply=2.5m,MemoText="owned-native-token-"+avatar.ToString("N")};
            var created=synchronous?provider.CreateToken(request):await provider.CreateTokenAsync(request);
            Assert.IsFalse(created.IsError,created.Exception?.ToString());Assert.IsTrue(created.IsSaved);
            var mint=created.Result.TokenAddress;var signatures=new List<string>{created.Result.TransactionResult};
            intent["Mint"]=mint;intent["CreateSignature"]=signatures[0];intent["State"]="before-mint";SaveIntent();
            var mintRequest=new MintWeb3TokenRequest{MintedByAvatarId=avatar,TokenAddress=mint,ToWalletAddress=owner,Amount=1.25m,MemoText="owned-native-mint"};
            var minted=synchronous?provider.MintToken(mintRequest):await provider.MintTokenAsync(mintRequest);
            Assert.IsFalse(minted.IsError,minted.Exception?.ToString());Assert.IsTrue(minted.IsSaved);signatures.Add(minted.Result.TransactionResult);
            intent["MintSignature"]=signatures[1];intent["State"]="before-burn";SaveIntent();
            var burnRequest=new BurnWeb3TokenRequest{OwnerPublicKey=owner,OwnerPrivateKey=Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!,OwnerSeedPhrase="",TokenAddress=mint,Amount=.75m};
            var burned=synchronous?provider.BurnToken(burnRequest):await provider.BurnTokenAsync(burnRequest);
            Assert.IsFalse(burned.IsError,burned.Exception?.ToString());Assert.IsTrue(burned.IsSaved);signatures.Add(burned.Result.TransactionResult);
            intent["BurnSignature"]=signatures[2];intent["State"]="verify-effects";SaveIntent();
            var nativeMint=await rpc.GetAccountInfoAsync(mint,Commitment.Finalized);Assert.IsTrue(nativeMint.WasSuccessful);
            Assert.AreEqual(TokenProgram.ProgramIdKey.Key,nativeMint.Result.Value.Owner);
            var state=Solnet.Programs.Models.TokenProgram.TokenMint.Deserialize(Convert.FromBase64String(nativeMint.Result.Value.Data[0]));
            Assert.AreEqual(3_000_000UL,state.Supply);Assert.AreEqual(owner,state.MintAuthority.Key);Assert.IsNull(state.FreezeAuthority);
            var holder=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(new PublicKey(owner),new PublicKey(mint));
            var nativeHolder=await rpc.GetAccountInfoAsync(holder.Key,Commitment.Finalized);Assert.IsTrue(nativeHolder.WasSuccessful);
            var account=Solnet.Programs.Models.TokenProgram.TokenAccount.Deserialize(Convert.FromBase64String(nativeHolder.Result.Value.Data[0]));
            Assert.AreEqual(3_000_000UL,account.Amount);Assert.AreEqual(owner,account.Owner.Key);Assert.AreEqual(mint,account.Mint.Key);
            ulong fees=0;foreach(var signature in signatures){var transaction=await rpc.GetTransactionAsync(signature,Commitment.Finalized);Assert.IsTrue(transaction.WasSuccessful);Assert.IsNotNull(transaction.Result);Assert.IsNull(transaction.Result.Meta.Error);fees=checked(fees+Convert.ToUInt64(transaction.Result.Meta.Fee));}
            var after=await rpc.GetBalanceAsync(owner,Commitment.Finalized);Assert.IsTrue(after.WasSuccessful);Assert.IsTrue(fees>0);
            var rent=checked(mintRent.Result+holderRent.Result);Assert.AreEqual(checked(rent+fees),checked(before.Result.Value-after.Result.Value));
            Assert.AreEqual(2,provider.KeyLookups);Assert.AreEqual(avatar,provider.LastAvatar);
            File.WriteAllText(costReport,System.Text.Json.JsonSerializer.Serialize(new{Mint=mint,Signatures=signatures,FeeLamports=fees,RentLamports=rent,TotalDebitLamports=rent+fees,LocalTestOnly=true}));
            intent["State"]="effects-verified";SaveIntent();File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
