using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string,string> metaKeyValuePairs,MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        => ReadHolonMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string,string> metaKeyValuePairs,MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        => LoadHolonsByMetaDataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)=>SendTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            if(request==null) throw new ArgumentNullException(nameof(request));
            if(request.Amount<=0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
            var amount=string.IsNullOrWhiteSpace(request.FromTokenAddress) ? NativeSolAmount(request.Amount):0UL;
            var key=request.OwnerPrivateKey;
            if(request is NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest concrete && !string.IsNullOrWhiteSpace(concrete.FromWalletPrivateKey))
            {
                if(!string.IsNullOrWhiteSpace(key) && key!=concrete.FromWalletPrivateKey) throw new ArgumentException("Conflicting explicit sender signing keys.");
                key=concrete.FromWalletPrivateKey;
            }
            if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(request.FromWalletAddress) || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("Explicit sender signing key, sender address and recipient address are required.");
            if(!string.IsNullOrWhiteSpace(request.OwnerPublicKey) && request.OwnerPublicKey!=request.FromWalletAddress)
                throw new ArgumentException("Owner public key must match the sender address.");
            var signer=ExplicitTokenSigner(key,request.FromWalletAddress);
            var recipient=new PublicKey(request.ToWalletAddress);
            if(!string.IsNullOrWhiteSpace(request.FromTokenAddress))
            {
                var splSignature=await SendLegacySplAsync(signer,recipient,new PublicKey(request.FromTokenAddress),request.Amount,request.MemoText);
                result.Result=new TransactionResponse{TransactionResult=splSignature};result.IsSaved=true;return result;
            }
            var signature=await SubmitTokenInstructionsAsync(signer,new[]{SystemProgram.Transfer(signer.PublicKey,recipient,amount)},request.MemoText);
            result.Result=new TransactionResponse{TransactionResult=signature};result.IsSaved=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana token transfer failed.",cause);}
        return result;
    }

    private static ulong NativeSolAmount(decimal amount)
    {
        if(amount<=0 || amount>ulong.MaxValue/1_000_000_000m) throw new ArgumentOutOfRangeException(nameof(amount));
        var raw=amount*1_000_000_000m;
        if(decimal.Truncate(raw)!=raw) throw new ArgumentException("SOL amount must be exactly representable in lamports.",nameof(amount));
        return checked((ulong)raw);
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)=>MintTokenAsync(request).GetAwaiter().GetResult();
    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();string signature=null;
        try
        {
            if(request==null) throw new ArgumentNullException(nameof(request));
            if(request.MintedByAvatarId==Guid.Empty || request.Amount<=0 || string.IsNullOrWhiteSpace(request.TokenAddress) || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("An avatar, existing SPL mint, recipient and positive amount are required.");
            var mint=new PublicKey(request.TokenAddress);var recipient=new PublicKey(request.ToWalletAddress);
            var keys=ResolveTokenSigningKeys(request.MintedByAvatarId);
            if(keys.IsError)
            {
                OASISErrorHandling.HandleError(ref result,keys.Message,keys.Exception);result.ErrorCode=keys.ErrorCode;return result;
            }
            if(keys.Result==null || keys.Result.Count!=1 || string.IsNullOrWhiteSpace(keys.Result[0]))
                throw new InvalidOperationException("SPL minting requires exactly one unambiguous avatar signing wallet.");
            var state=await ReadLegacyMintAsync(mint);
            if(state.MintAuthority==null) throw new UnauthorizedAccessException("SPL mint authority has been revoked.");
            Account signer;
            try {signer=ExplicitTokenSigner(keys.Result[0],state.MintAuthority.Key);}
            catch(ArgumentException cause) {throw new UnauthorizedAccessException("Avatar signing wallet is not the mint authority.",cause);}
            var raw=SplRawAmount(request.Amount,state.Decimals);
            if(raw>ulong.MaxValue-state.Supply) throw new ArgumentOutOfRangeException(nameof(request.Amount),"SPL mint supply would overflow.");
            var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(recipient,mint);
            var before=await ReadLegacyTokenAccountAsync(destination,mint,recipient,true);
            var balance=before?.Amount ?? 0;
            if(raw>ulong.MaxValue-balance) throw new ArgumentOutOfRangeException(nameof(request.Amount),"SPL recipient balance would overflow.");
            var instructions=new List<Solnet.Rpc.Models.TransactionInstruction>();
            if(before==null) instructions.Add(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(signer.PublicKey,recipient,mint));
            instructions.Add(TokenProgram.MintToChecked(mint,destination,signer.PublicKey,raw,state.Decimals));
            signature=await SubmitTokenInstructionsAsync(signer,instructions,request.MemoText);
            var afterMint=await ReadLegacyMintAsync(mint);var after=await ReadLegacyTokenAccountAsync(destination,mint,recipient,false);
            if(afterMint.Supply!=state.Supply+raw || after.Amount!=balance+raw)
                throw new System.IO.InvalidDataException("Finalized SPL mint supply or recipient effect differs from the requested mint.");
            result.Result=new TransactionResponse{TransactionResult=signature};result.IsSaved=true;
        }
        catch(Exception cause)
        {
            if(signature!=null && cause is not SolanaTransactionReconciliationException) cause=new SolanaTransactionReconciliationException(signature,cause);
            OASISErrorHandling.HandleError(ref result,"Solana token mint failed.",cause);
        }
        return result;
    }

    protected virtual OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)
        => KeyManager.GetProviderPrivateKeysForAvatarById(avatarId,Core.Enums.ProviderType.SolanaOASIS);
    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)=>BurnTokenAsync(request).GetAwaiter().GetResult();
    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            if(request==null) throw new ArgumentNullException(nameof(request));
            if(request.Amount<=0 || string.IsNullOrWhiteSpace(request.TokenAddress)) throw new ArgumentException("A mint address and positive exact burn amount are required.");
            var signer=ExplicitTokenSigner(request.OwnerPrivateKey,request.OwnerPublicKey);
            var signature=await BurnLegacySplAsync(signer,new PublicKey(request.TokenAddress),request.Amount);
            result.Result=new TransactionResponse{TransactionResult=signature};result.IsSaved=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana token burn failed.",cause);}
        return result;
    }

    private static Account ExplicitTokenSigner(string key,string address)
    {
        if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(address)) throw new ArgumentException("An explicit signing key and its public address are required.");
        var signer=new Account(key,address);
        var challenge=System.Text.Encoding.UTF8.GetBytes("oasis/solana/token-signer/v1");
        if(!signer.Verify(challenge,signer.Sign(challenge))) throw new ArgumentException("Signing key does not match its public address.");
        return signer;
    }

}
