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
        public OASISResult<ITransactionResponse> SendToken(Guid avatarId, ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The send token request is required. Please provide a valid request with FromWalletAddress or FromProvider, ToWalletAddress, Amount, and ProviderType.";
                return result;
            }
            string errorMessage = "Error Occured in SendToken function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = LoadProviderWalletsForAvatarById(avatarId, false, false, false, request.FromProvider.Value);

                //if (request.FromAvatarId != Guid.Empty)
                //    walletsResult = LoadProviderWalletsForAvatarById(request.FromAvatarId, providerTypeToLoadFrom: request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                //    walletsResult = LoadProviderWalletsForAvatarByUsername(request.FromAvatarUsername, providerTypeToLoadFrom: request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                //    walletsResult = LoadProviderWalletsForAvatarByEmail(request.FromAvatarEmail, providerTypeToLoadFrom: request.FromProvider.Value);

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

                    walletsResult = LoadProviderWalletsForAvatarById(request.ToAvatarId, providerTypeToLoadFrom: request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = LoadProviderWalletsForAvatarByUsername(request.ToAvatarUsername, providerTypeToLoadFrom: request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = LoadProviderWalletsForAvatarByEmail(request.ToAvatarEmail, providerTypeToLoadFrom: request.ToProvider.Value);
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
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The FromProviderType {Enum.GetName(typeof(ProviderType), request.FromProvider)} is not a OASIS Blockchain  Provider. Please make sure you sepcify a OASIS Blockchain Provider.");


            if (result.IsError)
                return result;

            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

                if (oasisBlockchainProvider != null)
                {
                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
                    {
                         Amount = request.Amount,
                         //FromProvider = request.FromProvider,
                         FromWalletAddress = request.FromWalletAddress,
                         MemoText = request.MemoText,
                         //ToProvider = request.ToProvider,
                         ToWalletAddress = request.ToWalletAddress
                    };

                    do
                    {
                        result = oasisBlockchainProvider.SendToken(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            attemptingToSend = false;
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForTokenToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, WaitForTokenToSendInSeconds ({request.WaitForTokenToSendInSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }

                    } while (attemptingToSend);
                }
            }
            else
            {
                // Cross-chain transfer: Use BridgeManager for atomic swaps (synchronous wrapper)
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

                    // Execute cross-chain bridge order (atomic swap) - synchronous wrapper
                    var bridgeResult = bridgeManager.CreateBridgeOrderAsync(bridgeOrderRequest).Result;

                    if (bridgeResult != null && !bridgeResult.IsError && bridgeResult.Result != null)
                    {
                        result.Message = $"Cross-chain token transfer initiated successfully. Bridge Order ID: {bridgeResult.Result.OrderId}";
                        result.Result.TransactionResult = bridgeResult.Result.OrderId.ToString();
                        result.IsError = false;
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


        private async Task<OASISResult<ITransactionResponse>> SendTokenInternalAsync(ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in SendTokenInternalAsync. Reason: ";

            SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
            {
                Amount = request.Amount,
                //FromProvider = request.FromProvider,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                //ToProvider = request.ToProvider,
                ToWalletAddress = request.ToWalletAddress
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.SendTokenAsync(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToSendTokenEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, AttemptToSendTokenEveryXSeconds ({request.AttemptToSendTokenEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.FromProvider.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        private OASISResult<ITransactionResponse> SendTokenInternal(ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in SendTokenInternal. Reason: ";

            SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
            {
                Amount = request.Amount,
                //FromProvider = request.FromProvider,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                //ToProvider = request.ToProvider,
                ToWalletAddress = request.ToWalletAddress
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.SendToken(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToSendTokenEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, AttemptToSendTokenEveryXSeconds ({request.AttemptToSendTokenEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.FromProvider.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The burn token request is required. Please provide a valid request with TokenAddress, ProviderType, and owner credentials.";
                return result;
            }
            string errorMessage = "Error occured in BurnTokenAsync. Reason: ";

            BurnWeb3TokenRequest burnWeb3TokenRequest = new BurnWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId,
                OwnerPrivateKey = request.OwnerPrivateKey,
                OwnerPublicKey = request.OwnerPublicKey,
                OwnerSeedPhrase = request.OwnerSeedPhrase
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.BurnTokenAsync(burnWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Burnt Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenBurnt)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token & WaitTillTokenBurnt is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToBurnEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token. Reason: Timeout expired, AttemptToBurnEveryXSeconds ({request.AttemptToBurnEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The burn token request is required. Please provide a valid request with TokenAddress, ProviderType, and owner credentials.";
                return result;
            }
            string errorMessage = "Error occured in BurnToken. Reason: ";

            BurnWeb3TokenRequest burnWeb3TokenRequest = new BurnWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId,
                OwnerPrivateKey = request.OwnerPrivateKey,
                OwnerPublicKey = request.OwnerPublicKey,
                OwnerSeedPhrase = request.OwnerSeedPhrase
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.BurnToken(burnWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Burnt Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenBurnt)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token & WaitTillTokenBurnt is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToBurnEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token. Reason: Timeout expired, AttemptToBurnEveryXSeconds ({request.AttemptToBurnEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The lock token request is required. Please provide a valid request with TokenAddress, Web3TokenId, and ProviderType.";
                return result;
            }
            string errorMessage = "Error occured in LockTokenAsync. Reason: ";

            LockWeb3TokenRequest lockWeb3TokenRequest = new LockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.LockTokenAsync(lockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Locked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenLocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token & WaitTillTokenLocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToLockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToLockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token. Reason: Timeout expired, AttemptToLockEveryXSeconds ({request.AttemptToLockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The lock token request is required. Please provide a valid request with TokenAddress, Web3TokenId, and ProviderType.";
                return result;
            }
            string errorMessage = "Error occured in LockToken. Reason: ";

            LockWeb3TokenRequest lockWeb3TokenRequest = new LockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.LockToken(lockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Locked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenLocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token & WaitTillTokenLocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToLockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToLockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token. Reason: Timeout expired, AttemptToLockEveryXSeconds ({request.AttemptToLockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

    }
}
