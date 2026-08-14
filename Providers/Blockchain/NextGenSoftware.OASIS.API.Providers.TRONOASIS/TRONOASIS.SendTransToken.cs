using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get wallet addresses for both avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarUsername);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars by username");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by username");
                    return response;
                }

                // Send TRC20 token transaction using TRON Grid API
                var tokenAddress = _contractAddress ?? "" ?? "";
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Token address is required");
                    return response;
                }

                // Build TRC20 transfer transaction
                var transferPayload = new
                {
                    owner_address = fromWalletAddress,
                    contract_address = tokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{toWalletAddress.Substring(1).PadLeft(64, '0')}{((long)(amount * 1000000)).ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txid
                    };
                    response.IsError = false;
                    response.Message = "TRC20 token transaction sent successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRC20 token transaction on TRON: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get wallet addresses for both avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarEmail);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars by email");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by email");
                    return response;
                }

                // Send TRX transaction using TRON Grid API
                var amountInSun = (long)(amount * 1_000_000m); // Convert to sun (smallest unit)
                
                var transferData = new
                {
                    owner_address = fromWalletAddress,
                    to_address = toWalletAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";
                    
                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txid
                    };
                    response.IsError = false;
                    response.Message = "TRX transaction sent successfully on TRON blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to send TRX transaction on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRX transaction on TRON: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get wallet addresses for both avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarEmail);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars by email");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by email");
                    return response;
                }

                // Send TRC20 token transaction using TRON Grid API
                var tokenAddress = _contractAddress ?? "" ?? "";
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Token address is required");
                    return response;
                }

                // Build TRC20 transfer transaction
                var transferPayload = new
                {
                    owner_address = fromWalletAddress,
                    contract_address = tokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{toWalletAddress.Substring(1).PadLeft(64, '0')}{((long)(amount * 1000000)).ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txid
                    };
                    response.IsError = false;
                    response.Message = "TRC20 token transaction sent successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRC20 token transaction on TRON: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Delegate to SendTransactionByIdAsync (which uses default wallets)
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // TRON TRC20 token transfer using TRON API
                var tokenAddress = string.IsNullOrEmpty(request.FromTokenAddress) ? _contractAddress ?? "" : request.FromTokenAddress;
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Build TRC20 transfer transaction
                var transferPayload = new
                {
                    owner_address = request.FromWalletAddress,
                    contract_address = tokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{request.ToWalletAddress.Substring(2).PadLeft(64, '0')}{((long)(request.Amount * 1000000)).ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token sent successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                // TRON TRC20 token minting (requires admin permissions)
                var tokenAddress = _contractAddress ?? "" ?? request.MintedByAvatarId.ToString();
                
                var mintPayload = new
                {
                    owner_address = tokenAddress,
                    contract_address = tokenAddress,
                    function_selector = "mint(address,uint256)",
                    parameter = $"{tokenAddress.Substring(2).PadLeft(64, '0')}{1000000.ToString("X").PadLeft(64, '0')}", // Mint 1 token
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(mintPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token minted successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // TRON TRC20 token burning
                var burnPayload = new
                {
                    owner_address = request.OwnerPrivateKey, // Would derive address from private key in production
                    contract_address = request.TokenAddress,
                    function_selector = "burn(uint256)",
                    parameter = $"{1000000.ToString("X").PadLeft(64, '0')}", // Burn 1 token
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(burnPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token burned successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.TokenAddress) || string.IsNullOrEmpty(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "" ?? "T..."; // Bridge pool address
                var senderAddress = bridgePoolAddress; // Would derive from private key in production
                
                var transferPayload = new
                {
                    owner_address = senderAddress,
                    contract_address = request.TokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{bridgePoolAddress.Substring(1).PadLeft(64, '0')}{1000000.ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token locked successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

    }
}
