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
    public async Task PublicNativeAndSplTokenOperationsVerifyFinalizedEffectsAndCosts(bool synchronous)
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
            var recipient=new Account().PublicKey.Key;
            intent["BurnSignature"]=signatures[2];intent["Recipient"]=recipient;intent["State"]="before-spl-transfer";SaveIntent();
            var sendSplRequest=new SendWeb3TokenRequest{FromWalletAddress=owner,ToWalletAddress=recipient,FromTokenAddress=mint,Amount=.5m,
                OwnerPublicKey=owner,OwnerPrivateKey=Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!,OwnerSeedPhrase="",MemoText="owned-native-spl-transfer"};
            var sentSpl=synchronous?provider.SendToken(sendSplRequest):await provider.SendTokenAsync(sendSplRequest);
            Assert.IsFalse(sentSpl.IsError,sentSpl.Exception?.ToString());Assert.IsTrue(sentSpl.IsSaved);signatures.Add(sentSpl.Result.TransactionResult);
            intent["SplTransferSignature"]=signatures[3];intent["State"]="before-sol-transfer";SaveIntent();
            var recipientBefore=await rpc.GetBalanceAsync(recipient,Commitment.Finalized);Assert.IsTrue(recipientBefore.WasSuccessful);
            const ulong transferredLamports=1_000_000;
            var sendSolRequest=new SendWeb3TokenRequest{FromWalletAddress=owner,ToWalletAddress=recipient,Amount=.001m,
                OwnerPublicKey=owner,OwnerPrivateKey=Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!,OwnerSeedPhrase="",MemoText="owned-native-sol-transfer"};
            var sentSol=synchronous?provider.SendToken(sendSolRequest):await provider.SendTokenAsync(sendSolRequest);
            Assert.IsFalse(sentSol.IsError,sentSol.Exception?.ToString());Assert.IsTrue(sentSol.IsSaved);signatures.Add(sentSol.Result.TransactionResult);
            intent["SolTransferSignature"]=signatures[4];intent["State"]="verify-effects";SaveIntent();
            var nativeMint=await rpc.GetAccountInfoAsync(mint,Commitment.Finalized);Assert.IsTrue(nativeMint.WasSuccessful);
            Assert.AreEqual(TokenProgram.ProgramIdKey.Key,nativeMint.Result.Value.Owner);
            var state=Solnet.Programs.Models.TokenProgram.TokenMint.Deserialize(Convert.FromBase64String(nativeMint.Result.Value.Data[0]));
            Assert.AreEqual(3_000_000UL,state.Supply);Assert.AreEqual(owner,state.MintAuthority.Key);Assert.IsNull(state.FreezeAuthority);
            var holder=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(new PublicKey(owner),new PublicKey(mint));
            var nativeHolder=await rpc.GetAccountInfoAsync(holder.Key,Commitment.Finalized);Assert.IsTrue(nativeHolder.WasSuccessful);
            var account=Solnet.Programs.Models.TokenProgram.TokenAccount.Deserialize(Convert.FromBase64String(nativeHolder.Result.Value.Data[0]));
            Assert.AreEqual(2_500_000UL,account.Amount);Assert.AreEqual(owner,account.Owner.Key);Assert.AreEqual(mint,account.Mint.Key);
            var recipientHolder=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(new PublicKey(recipient),new PublicKey(mint));
            var recipientAccount=await rpc.GetAccountInfoAsync(recipientHolder.Key,Commitment.Finalized);Assert.IsTrue(recipientAccount.WasSuccessful);
            Assert.AreEqual(TokenProgram.ProgramIdKey.Key,recipientAccount.Result.Value.Owner);
            var received=Solnet.Programs.Models.TokenProgram.TokenAccount.Deserialize(Convert.FromBase64String(recipientAccount.Result.Value.Data[0]));
            Assert.AreEqual(500_000UL,received.Amount);Assert.AreEqual(recipient,received.Owner.Key);Assert.AreEqual(mint,received.Mint.Key);
            var recipientAfter=await rpc.GetBalanceAsync(recipient,Commitment.Finalized);Assert.IsTrue(recipientAfter.WasSuccessful);
            Assert.AreEqual(transferredLamports,checked(recipientAfter.Result.Value-recipientBefore.Result.Value));
            ulong fees=0;foreach(var signature in signatures){var transaction=await rpc.GetTransactionAsync(signature,Commitment.Finalized);Assert.IsTrue(transaction.WasSuccessful);Assert.IsNotNull(transaction.Result);Assert.IsNull(transaction.Result.Meta.Error);fees=checked(fees+Convert.ToUInt64(transaction.Result.Meta.Fee));}
            var after=await rpc.GetBalanceAsync(owner,Commitment.Finalized);Assert.IsTrue(after.WasSuccessful);Assert.IsTrue(fees>0);
            var rent=checked(mintRent.Result+2*holderRent.Result);Assert.AreEqual(checked(rent+fees+transferredLamports),checked(before.Result.Value-after.Result.Value));
            Assert.AreEqual(2,provider.KeyLookups);Assert.AreEqual(avatar,provider.LastAvatar);
            File.WriteAllText(costReport,System.Text.Json.JsonSerializer.Serialize(new{Mint=mint,Recipient=recipient,Signatures=signatures,FeeLamports=fees,RentLamports=rent,TransferredLamports=transferredLamports,TotalDebitLamports=rent+fees+transferredLamports,LocalTestOnly=true}));
            intent["State"]="effects-verified";SaveIntent();File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
