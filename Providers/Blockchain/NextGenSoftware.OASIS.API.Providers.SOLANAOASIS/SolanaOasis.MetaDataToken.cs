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
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
        Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
        => ReadHolonMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
        Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }



    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in SendTokenAsync method in SolanaOASIS. Reason: ";

        //try
        //{
        //    if (!IsProviderActivated || _solanaService == null)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
        //        return result;
        //    }

        //    if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "To wallet address is required");
        //        return result;
        //    }

        //    // Get private key from request or KeyManager
        //    string privateKey = null;
        //    if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
        //        privateKey = request.OwnerPrivateKey;
        //    else if (request is SendWeb3TokenRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletPrivateKey))
        //        privateKey = sendRequest.FromWalletPrivateKey;

        //    if (string.IsNullOrWhiteSpace(privateKey))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Private key is required (OwnerPrivateKey or FromWalletPrivateKey)");
        //        return result;
        //    }

        //    // If FromTokenAddress is provided, it's an SPL token transfer
        //    if (!string.IsNullOrWhiteSpace(request.FromTokenAddress))
        //    {
        //        // SPL Token transfer
        //        // Get public key from wallet address or derive from private key
        //        var fromPublicKey = new PublicKey(request.FromWalletAddress ?? string.Empty);
        //        if (string.IsNullOrWhiteSpace(request.FromWalletAddress))
        //        {
        //            // Derive public key from private key
        //            var privateKeyBytes = Convert.FromBase64String(privateKey);
        //            var fromAccount = new Account(privateKey, request.FromWalletAddress ?? string.Empty);
        //            fromPublicKey = new PublicKey(fromAccount.PublicKey.Key);
        //        }
        //        var toPublicKey = new PublicKey(request.ToWalletAddress);
        //        var tokenMint = new PublicKey(request.FromTokenAddress);

        //        // Get associated token accounts
        //        var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPublicKey, tokenMint);
        //        var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPublicKey, tokenMint);

        //        // Get recent blockhash
        //        var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
        //        if (!blockHashResult.WasSuccessful)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Failed to get blockhash: {blockHashResult.Reason}");
        //            return result;
        //        }

        //        // Build transfer instruction
        //        var transferInstruction = TokenProgram.Transfer(
        //            fromTokenAccount,
        //            toTokenAccount,
        //            (ulong)(request.Amount * 1_000_000_000), // Convert to token decimals (assuming 9 decimals)
        //            fromPublicKey);

        //        // Build and send transaction
        //        var transaction = new TransactionBuilder()
        //            .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
        //            .SetFeePayer(fromPublicKey)
        //            .AddInstruction(transferInstruction)
        //            .Build(fromAccount);

        //        var sendResult = await _rpcClient.SendTransactionAsync(transaction);
        //        if (!sendResult.WasSuccessful)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"SPL token transfer failed: {sendResult.Reason}");
        //            return result;
        //        }

        //        result.Result.TransactionResult = sendResult.Result;
        //        result.IsError = false;
        //        result.Message = "SPL token sent successfully.";
        //    }
        //    else
        //    {
        //        // Native SOL transfer
        //        var fromPublicKey = request.FromWalletAddress;
        //        if (string.IsNullOrWhiteSpace(fromPublicKey))
        //        {
        //            var privateKeyBytes = Convert.FromBase64String(privateKey);
        //            var fromAccount = new Account(privateKeyBytes, fromIndex: 0);
        //            fromPublicKey = fromAccount.PublicKey.Key;
        //        }
        //        var sendRequest = new SendTransactionRequest
        //        {
        //            FromAccount = new BaseAccountRequest { PublicKey = fromPublicKey },
        //            ToAccount = new BaseAccountRequest { PublicKey = request.ToWalletAddress },
        //            Amount = (ulong)(request.Amount * 1_000_000_000), // Convert SOL to lamports
        //            MemoText = request.MemoText ?? string.Empty
        //        };

        //        var transactionResult = await _solanaService.SendTransaction(sendRequest);
        //        if (transactionResult.IsError || string.IsNullOrEmpty(transactionResult.Result?.TransactionHash))
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"SOL transfer failed: {transactionResult.Message}");
        //            return result;
        //        }

        //        result.Result.TransactionResult = transactionResult.Result.TransactionHash;
        //        result.IsError = false;
        //        result.Message = "SOL sent successfully.";
        //    }
        //}
        //catch (Exception ex)
        //{
        //    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        //}
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in MintTokenAsync method in SolanaOASIS. Reason: ";

        //try
        //{
        //    if (!IsProviderActivated || _solanaService == null)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
        //        return result;
        //    }

        //    if (request == null)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Mint request is required");
        //        return result;
        //    }

        //    // Get private key from KeyManager using MintedByAvatarId
        //    var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, Core.Enums.ProviderType.SolanaOASIS);
        //    if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
        //        return result;
        //    }

        //    var privateKeyBytes = Convert.FromBase64String(keysResult.Result[0]);
        //    var mintAccount = new Account(privateKeyBytes, fromIndex: 0);
        //    var mintPublicKey = new PublicKey(mintAccount.PublicKey.Key);
        //    var mintToPublicKey = new PublicKey(mintAccount.PublicKey.Key); // Default to minter's address

        //    // Get recent blockhash
        //    var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
        //    if (!blockHashResult.WasSuccessful)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Failed to get blockhash: {blockHashResult.Reason}");
        //        return result;
        //    }

        //    // For SPL token minting, we need to create a mint account first
        //    // This is a simplified implementation - in production, you'd need proper mint account setup
        //    var mintInstruction = TokenProgram.InitializeMint(
        //        mintPublicKey,
        //        9, // 9 decimals (standard for most tokens)
        //        mintPublicKey,
        //        null);

        //    var transaction = new TransactionBuilder()
        //        .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
        //        .SetFeePayer(mintPublicKey)
        //        .AddInstruction(mintInstruction)
        //        .Build(mintAccount);

        //    var sendResult = await _rpcClient.SendTransactionAsync(transaction);
        //    if (!sendResult.WasSuccessful)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Token mint failed: {sendResult.Reason}");
        //        return result;
        //    }

        //    result.Result.TransactionResult = sendResult.Result;
        //    result.IsError = false;
        //    result.Message = "Token minted successfully.";
        //}
        //catch (Exception ex)
        //{
        //    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        //}
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in BurnTokenAsync method in SolanaOASIS. Reason: ";

        //try
        //{
        //    if (!IsProviderActivated || _solanaService == null)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
        //        return result;
        //    }

        //    if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
        //        string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
        //        return result;
        //    }

        //    var privateKeyBytes = Convert.FromBase64String(request.OwnerPrivateKey);
        //    var ownerAccount = new Account(privateKeyBytes, fromIndex: 0);
        //    var ownerPublicKey = new PublicKey(ownerAccount.PublicKey.Key);
        //    var tokenMint = new PublicKey(request.TokenAddress);

        //    // Get associated token account
        //    var tokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(ownerPublicKey, tokenMint);

        //    // Get recent blockhash
        //    var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
        //    if (!blockHashResult.WasSuccessful)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Failed to get blockhash: {blockHashResult.Reason}");
        //        return result;
        //    }

        //    // Get token balance to determine burn amount
        //    var balanceResult = await _rpcClient.GetTokenAccountBalanceAsync(tokenAccount);
        //    ulong burnAmount = 1_000_000_000; // Default 1 token (9 decimals)
        //    if (balanceResult.WasSuccessful && balanceResult.Result.Value != null)
        //    {
        //        burnAmount = balanceResult.Result.Value.AmountUlong;
        //    }

        //    // Build burn instruction
        //    var burnInstruction = TokenProgram.Burn(
        //        tokenAccount,
        //        tokenMint,
        //        burnAmount,
        //        ownerPublicKey);

        //    var transaction = new TransactionBuilder()
        //        .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
        //        .SetFeePayer(ownerPublicKey)
        //        .AddInstruction(burnInstruction)
        //        .Build(ownerAccount);

        //    var sendResult = await _rpcClient.SendTransactionAsync(transaction);
        //    if (!sendResult.WasSuccessful)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Token burn failed: {sendResult.Reason}");
        //        return result;
        //    }

        //    result.Result.TransactionResult = sendResult.Result;
        //    result.IsError = false;
        //    result.Message = "Token burned successfully.";
        //}
        //catch (Exception ex)
        //{
        //    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        //}
        return result;
    }

}
