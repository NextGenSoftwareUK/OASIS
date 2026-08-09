using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class WalletManager
    {
        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByUsernameAsync(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsernameAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByUsername(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsername. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByEmailAsync(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmailAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByEmail(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmail. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4TokenResponse>> SendTokenAsync(Guid avatarId, ISendWeb4TokenRequest request)
        {
            OASISResult<ISendWeb4TokenResponse> result = new OASISResult<ISendWeb4TokenResponse>(new SendWeb4TokenResponse());
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The send token request is required. Please provide a valid request with FromWalletAddress or FromProvider, ToWalletAddress, Amount, and ProviderType.";
                return result;
            }
            OASISResult<ITransactionResponse> blockchainResult = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error Occured in SendTokenAsync function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, request.FromProvider.Value);


                //if (request.FromAvatarId != Guid.Empty)
                //    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.FromAvatarId, false, false, request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                //    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.FromAvatarUsername, false, false, request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                //    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.FromAvatarEmail, false, false, request.FromProvider.Value);

                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the sender: FromWalletAddress, FromAvatarId, FromAvatarUsername or FromAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.FromProvider.Value) && walletsResult.Result[request.FromProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.FromProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.FromWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (string.IsNullOrEmpty(request.ToWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
                if (request.ToAvatarId != Guid.Empty)

                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.ToAvatarId, false, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.ToAvatarUsername, false, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.ToAvatarEmail, false, false, false, request.ToProvider.Value);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the receiver: ToWalletAddress, ToAvatarId, ToAvatarUsername or ToAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.ToProvider.Value) && walletsResult.Result[request.ToProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.ToProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.ToWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            //else
            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The FromProviderType {Enum.GetName(typeof(ProviderType), request.FromProvider)} is not a OASIS Blockchain  Provider. Please make sure you sepcify a OASIS Blockchain Provider.");


            if (result.IsError)
                return result;

            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                blockchainResult = await SendTokenInternalAsync(request);

                if (blockchainResult != null && blockchainResult.Result != null && !blockchainResult.IsError)
                {
                    result.Message = "Token Sent Successfully";
                    result.Result.SendTransactionResult = blockchainResult.Result.TransactionResult;
                }
            }
            else
            {
                // Cross-chain transfer: Use BridgeManager for atomic swaps
                try
                {
                    // Get token symbols from provider types
                    var fromToken = GetTokenSymbolForProvider(request.FromProvider.Value);
                    var toToken = GetTokenSymbolForProvider(request.ToProvider.Value);

                    if (string.IsNullOrEmpty(fromToken) || string.IsNullOrEmpty(toToken))
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Unable to determine token symbols for providers {request.FromProvider.Name} and {request.ToProvider.Name}. Cross-chain transfers require valid blockchain providers.");
                        return result;
                    }

                    // Get BridgeManager instance
                    var bridgeManager = BridgeManager.Instance;

                    // Create bridge order request
                    var bridgeOrderRequest = new NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.CreateBridgeOrderRequest
                    {
                        FromToken = fromToken,
                        ToToken = toToken,
                        Amount = request.Amount,
                        FromAddress = request.FromWalletAddress,
                        DestinationAddress = request.ToWalletAddress,
                        UserId = avatarId,
                        ExpiresInMinutes = 30
                    };

                    // Execute cross-chain bridge order (atomic swap)
                    var bridgeResult = await bridgeManager.CreateBridgeOrderAsync(bridgeOrderRequest);

                    if (bridgeResult != null && !bridgeResult.IsError && bridgeResult.Result != null)
                    {
                        result.Message = $"Cross-chain token transfer initiated successfully. Bridge Order ID: {bridgeResult.Result.OrderId}";
                        result.Result.SendTransactionResult = bridgeResult.Result.OrderId.ToString();
                        result.IsError = false;

                        // Store bridge order ID for tracking
                        result.Result.BridgeOrderId = bridgeResult.Result.OrderId.ToString();
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Cross-chain bridge operation failed. Reason: {bridgeResult?.Message ?? "Unknown error"}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"{errorMessage} Exception during cross-chain transfer: {ex.Message}", ex);
                }
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(Guid avatarId, ISendWeb4NFTRequest request)
        {
            OASISResult<ISendWeb4NFTResponse> result = new OASISResult<ISendWeb4NFTResponse>(new SendWeb4NFTResponse());
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The send NFT request is required. Please provide a valid request with FromWalletAddress or FromProvider, ToWalletAddress, and NFT details.";
                return result;
            }
            OASISResult<IWeb3NFTTransactionResponse> blockchainResult = new OASISResult<IWeb3NFTTransactionResponse>();
            string errorMessage = "Error Occured in SendNFTAsync function. Reason: ";

            // Resolve wallet addresses if not provided
            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, request.FromProvider.Value);

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.FromProvider.Value) && walletsResult.Result[request.FromProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.FromProvider.Value].FirstOrDefault();
                    if (wallet != null)
                        request.FromWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name}.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (string.IsNullOrEmpty(request.ToWalletAddress))
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
                if (request.ToAvatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.ToAvatarId, false, false, false, request.ToProvider.Value);
                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.ToAvatarUsername, false, false, false, request.ToProvider.Value);
                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.ToAvatarEmail, false, false, false, request.ToProvider.Value);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the receiver: ToWalletAddress, ToAvatarId, ToAvatarUsername or ToAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.ToProvider.Value) && walletsResult.Result[request.ToProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.ToProvider.Value].FirstOrDefault();
                    if (wallet != null)
                        request.ToWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name}.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.IsError)
                return result;

            // Check if same-chain or cross-chain
            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                // Same-chain: Direct NFT transfer
                IOASISNFTProvider nftProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISNFTProvider;
                if (nftProvider != null)
                {
                    var sendRequest = new SendWeb3NFTRequest
                    {
                        FromNFTTokenAddress = request.FromNFTTokenAddress,
                        FromWalletAddress = request.FromWalletAddress,
                        ToWalletAddress = request.ToWalletAddress,
                        TokenAddress = request.TokenAddress,
                        TokenId = request.TokenId,
                        Amount = request.Amount,
                        MemoText = request.MemoText
                    };

                    blockchainResult = await nftProvider.SendNFTAsync(sendRequest);
                    if (blockchainResult != null && blockchainResult.Result != null && !blockchainResult.IsError)
                    {
                        result.Message = "NFT Sent Successfully";
                        result.Result.SendTransactionResult = blockchainResult.Result.TransactionResult;
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider {request.FromProvider.Name} does not support NFT operations.");
                }
            }
            else
            {
                // Cross-chain NFT transfer: Use bridge
                try
                {
                    // Get NFT providers for both chains
                    IOASISNFTProvider fromNFTProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISNFTProvider;
                    IOASISNFTProvider toNFTProvider = ProviderManager.Instance.GetProvider(request.ToProvider.Value) as IOASISNFTProvider;

                    if (fromNFTProvider == null || toNFTProvider == null)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} One or both providers do not support NFT operations.");
                        return result;
                    }

                    // Step 1: Withdraw NFT from source chain (locks it)
                    var withdrawResult = await fromNFTProvider.WithdrawNFTAsync(
                        request.TokenAddress ?? request.FromNFTTokenAddress,
                        request.TokenId,
                        request.FromWalletAddress,
                        string.Empty // Private key would be retrieved securely in production
                    );

                    if (withdrawResult.IsError || withdrawResult.Result == null)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Failed to withdraw/lock NFT on source chain: {withdrawResult.Message}");
                        return result;
                    }

                    result.Result.LockTransactionResult = withdrawResult.Result.TransactionId;

                    // Step 2: Deposit NFT to destination chain (mints wrapped NFT)
                    var depositResult = await toNFTProvider.DepositNFTAsync(
                        request.TokenAddress ?? request.FromNFTTokenAddress, // Would be destination chain NFT contract
                        request.TokenId, // May be different on destination if wrapped
                        request.ToWalletAddress,
                        withdrawResult.Result.TransactionId // Source transaction hash for verification
                    );

                    if (depositResult.IsError || depositResult.Result == null)
                    {
                        // Rollback: Unlock NFT on source chain
                        var unlockRequest = new UnlockWeb3NFTRequest
                        {
                            NFTTokenAddress = request.TokenAddress ?? request.FromNFTTokenAddress,
                            Web3NFTId = Guid.TryParse(request.TokenId, out var guid) ? guid : Guid.NewGuid(),
                            UnlockedByAvatarId = avatarId
                        };

                        var unlockResult = await fromNFTProvider.UnlockNFTAsync(unlockRequest);
                        if (unlockResult != null && !unlockResult.IsError)
                        {
                            result.Result.UnlockTransactionResult = unlockResult.Result.TransactionResult;
                            result.Message = $"NFT deposit failed, but NFT was successfully unlocked on source chain. Deposit error: {depositResult.Message}";
                        }
                        else
                        {
                            result.Message = $"CRITICAL: NFT deposit failed AND unlock failed. Deposit error: {depositResult.Message}. Unlock error: {unlockResult?.Message}";
                        }
                        result.IsError = true;
                        return result;
                    }

                    result.Result.SendTransactionResult = depositResult.Result.TransactionId;
                    result.Message = "Cross-chain NFT transfer completed successfully";
                    result.IsError = false;
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"{errorMessage} Exception during cross-chain NFT transfer: {ex.Message}", ex);
                }
            }

            return result;
        }

    }
}
