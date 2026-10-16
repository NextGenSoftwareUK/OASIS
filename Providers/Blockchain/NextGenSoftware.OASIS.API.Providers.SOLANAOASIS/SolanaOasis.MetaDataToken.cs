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
            var signer=new Account(key,request.FromWalletAddress);
            var challenge=System.Text.Encoding.UTF8.GetBytes("oasis/solana/token-signer/v1");
            if(!signer.Verify(challenge,signer.Sign(challenge))) throw new ArgumentException("Sender signing key does not match its public address.");
            var recipient=new PublicKey(request.ToWalletAddress);
            if(!string.IsNullOrWhiteSpace(request.FromTokenAddress))
            {
                var splSignature=await SendLegacySplAsync(signer,recipient,new PublicKey(request.FromTokenAddress),request.Amount,request.MemoText);
                result.Result=new TransactionResponse{TransactionResult=splSignature};result.IsSaved=true;return result;
            }
            var block=await _rpcClient.GetLatestBlockHashAsync();
            if(!block.WasSuccessful || string.IsNullOrWhiteSpace(block.Result?.Value?.Blockhash))
                throw new InvalidOperationException("Solana writable blockhash was unavailable.");
            var builder=new TransactionBuilder().SetRecentBlockHash(block.Result.Value.Blockhash).SetFeePayer(signer.PublicKey)
                .AddInstruction(SystemProgram.Transfer(signer.PublicKey,recipient,amount));
            if(!string.IsNullOrEmpty(request.MemoText)) builder.AddInstruction(MemoProgram.NewMemoV2(request.MemoText));
            var signed=builder.Build(signer);
            var signature=await new SolanaRepository(signer,_rpcClient).SubmitSignedTransactionAsync(signed);
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
    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        => Task.FromResult(UnimplementedSolanaTokenMutation("mint",request));
    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)=>BurnTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        => Task.FromResult(UnimplementedSolanaTokenMutation("burn",request));

    private static OASISResult<ITransactionResponse> UnimplementedSolanaTokenMutation(string operation,object request)
    {
        var result=new OASISResult<ITransactionResponse>();
        Exception cause=request==null ? new ArgumentNullException(nameof(request))
            :new NotSupportedException("Solana token "+operation+" is not implemented; no transaction was submitted.");
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode="SOLANA_TOKEN_OPERATION_UNAVAILABLE";
        return result;
    }
}
