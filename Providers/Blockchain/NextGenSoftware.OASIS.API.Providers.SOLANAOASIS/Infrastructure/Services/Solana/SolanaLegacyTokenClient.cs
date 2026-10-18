using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using SplAccount=Solnet.Programs.Models.TokenProgram.TokenAccount;
using SplMint=Solnet.Programs.Models.TokenProgram.TokenMint;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

internal sealed class SolanaLegacyTokenClient(Solnet.Rpc.IRpcClient _rpcClient)
{
    internal static ulong SplRawAmount(decimal amount,int decimals)
    {
        if(amount<=0 || decimals<0 || decimals>255) throw new ArgumentOutOfRangeException(nameof(amount));
        var bits=decimal.GetBits(amount);var scale=(bits[3]>>16)&255;
        var raw=(BigInteger)(uint)bits[0] | ((BigInteger)(uint)bits[1]<<32) | ((BigInteger)(uint)bits[2]<<64);
        if(decimals>=scale) raw*=BigInteger.Pow(10,decimals-scale);
        else
        {
            raw=BigInteger.DivRem(raw,BigInteger.Pow(10,scale-decimals),out var remainder);
            if(remainder!=0) throw new ArgumentException("Token amount exceeds the mint's decimal precision.",nameof(amount));
        }
        if(raw<=0 || raw>ulong.MaxValue) throw new ArgumentOutOfRangeException(nameof(amount));
        return (ulong)raw;
    }

    internal async Task<SplMint> ReadLegacyMintAsync(PublicKey mint)
    {
        var info=await _rpcClient.GetAccountInfoAsync(mint.Key,Commitment.Finalized);
        if(!info.WasSuccessful || info.Result?.Value==null) throw new InvalidOperationException("SPL mint account could not be read.");
        if(info.Result.Value.Owner!=TokenProgram.ProgramIdKey.Key)
            throw new NotSupportedException("Only the legacy SPL Token Program is supported; other token programs must not be reinterpreted.");
        if(info.Result.Value.Executable || info.Result.Value.Data==null || info.Result.Value.Data.Count!=2 || info.Result.Value.Data[1]!="base64"
            || Convert.FromBase64String(info.Result.Value.Data[0]).Length!=TokenProgram.MintAccountDataSize)
            throw new InvalidDataException("SPL mint account data is malformed.");
        var state=SplMint.Deserialize(Convert.FromBase64String(info.Result.Value.Data[0]));
        if(!state.IsInitialized) throw new InvalidDataException("SPL mint is not initialized.");
        return state;
    }

    internal async Task<int> ReadLegacyMintDecimalsAsync(PublicKey mint) => (await ReadLegacyMintAsync(mint)).Decimals;

    internal async Task<SplAccount> ReadLegacyTokenAccountAsync(PublicKey address,PublicKey mint,PublicKey owner,bool allowMissing)
    {
        var info=await _rpcClient.GetAccountInfoAsync(address.Key,Commitment.Finalized);
        if(!info.WasSuccessful || info.Result==null) throw new InvalidOperationException("SPL token account could not be read.");
        if(info.Result.Value==null)
        {
            if(allowMissing) return null;
            throw new ArgumentException("Sender associated token account does not exist.");
        }
        var value=info.Result.Value;
        if(value.Owner!=TokenProgram.ProgramIdKey.Key || value.Executable || value.Data==null || value.Data.Count!=2 || value.Data[1]!="base64")
            throw new InvalidDataException("SPL account program or encoding is invalid.");
        var data=Convert.FromBase64String(value.Data[0]);
        if(data.Length!=TokenProgram.TokenAccountDataSize) throw new InvalidDataException("SPL token account length is invalid.");
        var account=SplAccount.Deserialize(data);
        if(account.Mint.Key!=mint.Key || account.Owner.Key!=owner.Key || account.State!=SplAccount.AccountState.Initialized)
            throw new InvalidDataException("SPL token account mint, owner or initialized state is invalid.");
        return account;
    }

    internal async Task<string> BurnLegacySplAsync(Account signer,PublicKey mint,decimal amount)
    {
        var decimals=await ReadLegacyMintDecimalsAsync(mint);var raw=SplRawAmount(amount,decimals);
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(signer.PublicKey,mint);
        var account=await ReadLegacyTokenAccountAsync(source,mint,signer.PublicKey,false);
        if(account.Amount<raw) throw new ArgumentException("SPL burn balance is insufficient.");
        return await SubmitTokenInstructionsAsync(signer,new[]{TokenProgram.BurnChecked(mint,source,signer.PublicKey,raw,decimals)},null);
    }

    internal async Task<string> SendLegacySplAsync(Account signer,PublicKey recipient,PublicKey mint,decimal amount,string memo,bool requireNft=false)
    {
        var state=await ReadLegacyMintAsync(mint);
        if(requireNft && (amount!=1 || state.Decimals!=0 || state.Supply!=1))
            throw new ArgumentException("Legacy NFT transfer requires one zero-decimal token with total supply one.");
        var decimals=state.Decimals;var raw=SplRawAmount(amount,decimals);
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(signer.PublicKey,mint);
        var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(recipient,mint);
        var sourceAccount=await ReadLegacyTokenAccountAsync(source,mint,signer.PublicKey,false);
        if(sourceAccount.Amount<raw) throw new ArgumentException("Sender SPL token balance is insufficient.");
        var destinationAccount=await ReadLegacyTokenAccountAsync(destination,mint,recipient,true);
        if(destinationAccount!=null && raw>ulong.MaxValue-destinationAccount.Amount) throw new ArgumentOutOfRangeException(nameof(amount),"SPL recipient balance would overflow.");
        var instructions=new System.Collections.Generic.List<Solnet.Rpc.Models.TransactionInstruction>();
        if(destinationAccount==null) instructions.Add(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(signer.PublicKey,recipient,mint));
        instructions.Add(TokenProgram.TransferChecked(source,destination,raw,decimals,signer.PublicKey,mint));
        return await SubmitTokenInstructionsAsync(signer,instructions,memo);
    }

    internal async Task<string> SubmitTokenInstructionsAsync(Account signer,System.Collections.Generic.IEnumerable<Solnet.Rpc.Models.TransactionInstruction> instructions,string memo)
    {
        var block=await _rpcClient.GetLatestBlockHashAsync();
        if(!block.WasSuccessful || string.IsNullOrWhiteSpace(block.Result?.Value?.Blockhash)) throw new InvalidOperationException("Solana writable blockhash was unavailable.");
        var builder=new TransactionBuilder().SetRecentBlockHash(block.Result.Value.Blockhash).SetFeePayer(signer.PublicKey);
        foreach(var instruction in instructions) builder.AddInstruction(instruction);
        if(!string.IsNullOrEmpty(memo)) builder.AddInstruction(MemoProgram.NewMemoV2(memo));
        return await new SolanaRepository(signer,_rpcClient).SubmitSignedTransactionAsync(builder.Build(signer));
    }
}
