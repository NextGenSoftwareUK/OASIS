using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result=new OASISResult<bool>();
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            var planned=holons.ToList();
            if(planned.Count==0) throw new ArgumentException("At least one holon is required for import.");
            var saved=await SaveHolonsAsync(planned,saveChildren:true,recursive:true,continueOnError:false,saveChildrenOnProvider:true);
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);
            result.Result=!saved.IsError && saved.IsSaved;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Solana import failed: "+cause.Message,cause);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId,
        int version = 0)
    {
        return await ExportStoredHolonsForAvatarAsync(avatarId,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(
        string avatarUsername, int version = 0)
    {
        return await ExportStoredHolonsForSelectorAsync(avatarUsername,false,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(
        string avatarEmailAddress, int version = 0)
    {
        return await ExportStoredHolonsForSelectorAsync(avatarEmailAddress,true,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        return await ReadHolonCollectionAsync(row=>true,true,true,0,0,true,version);
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername,
        int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress,
        int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).GetAwaiter().GetResult();
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate = "Error occurred in SendTransactionByIdAsync (with token) method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            // Get wallet addresses for avatars
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                return result;
            }

            // If token is provided, use SPL token transfer; otherwise use native SOL transfer
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Use SPL token transfer via TokenProgram
                var fromPublicKey = new PublicKey(fromWalletResult.Result.WalletAddress);
                var toPublicKey = new PublicKey(toWalletResult.Result.WalletAddress);
                var tokenMint = new PublicKey(token);
                
                // Convert amount to lamports (SPL tokens use their own decimals, but we'll use 9 for consistency)
                var tokenAmount = (ulong)(amount * 1_000_000_000m);
                
                // Get associated token accounts
                var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPublicKey, tokenMint);
                var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPublicKey, tokenMint);
                
                // Build token transfer transaction
                var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
                if (!blockHashResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get latest block hash: {blockHashResult.Reason}");
                    return result;
                }
                
                var transferInstruction = TokenProgram.Transfer(
                    fromTokenAccount,
                    toTokenAccount,
                    tokenAmount,
                    fromPublicKey);
                
                byte[] tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                    .SetFeePayer(fromPublicKey)
                    .AddInstruction(transferInstruction)
                    .Build(_oasisSolanaAccount);
                
                var sendResult = await _rpcClient.SendTransactionAsync(tx);
                if (!sendResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction: {sendResult.Reason}");
                    return result;
                }
                
                result.Result = new TransactionResponse { TransactionResult = sendResult.Result };
                result.IsError = false;
                result.Message = "Token transaction sent successfully";
            }
            else
            {
                // Use native SOL transfer (delegate to existing method)
                result = await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessageTemplate}{ex.Message}", ex);
        }
        
        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate = "Error occurred in SendTransactionByUsernameAsync (with token) method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            // Get wallet addresses for avatars by username
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, fromAvatarUsername);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, toAvatarUsername);

            if (fromWalletResult.IsError || toWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result) || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "Error getting wallet addresses for avatars");
                return result;
            }

            var fromWallet = fromWalletResult.Result;
            var toWallet = toWalletResult.Result;

            // If token is provided, use SPL token transfer; otherwise use native SOL transfer
            if (!string.IsNullOrWhiteSpace(token))
            {
                var fromPublicKey = new PublicKey(fromWallet);
                var toPublicKey = new PublicKey(toWallet);
                var tokenMint = new PublicKey(token);
                var tokenAmount = (ulong)(amount * 1_000_000_000m);
                
                var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPublicKey, tokenMint);
                var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPublicKey, tokenMint);
                
                var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
                if (!blockHashResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get latest block hash: {blockHashResult.Reason}");
                    return result;
                }
                
                var transferInstruction = TokenProgram.Transfer(fromTokenAccount, toTokenAccount, tokenAmount, fromPublicKey);
                
                byte[] tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                    .SetFeePayer(fromPublicKey)
                    .AddInstruction(transferInstruction)
                    .Build(_oasisSolanaAccount);
                
                var sendResult = await _rpcClient.SendTransactionAsync(tx);
                if (!sendResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction: {sendResult.Reason}");
                    return result;
                }
                
                result.Result = new TransactionResponse { TransactionResult = sendResult.Result };
                result.IsError = false;
                result.Message = "Token transaction sent successfully";
            }
            else
            {
                // Use native SOL transfer (delegate to existing method)
                result = await SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessageTemplate}{ex.Message}", ex);
        }
        
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }
}
