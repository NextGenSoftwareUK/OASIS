using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
{
    public partial class ThreeFoldOASIS
    {
        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/bridge/transactions/{Uri.EscapeDataString(transactionHash)}/status", token);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (statusData.TryGetProperty("status", out var status))
                    {
                        var statusStr = status.GetString() ?? "NotFound";
                        if (Enum.TryParse<BridgeTransactionStatus>(statusStr, out var statusEnum))
                        {
                            result.Result = statusEnum;
                            result.IsError = false;
                            result.Message = "Transaction status retrieved successfully";
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.NotFound;
                            result.IsError = false;
                            result.Message = "Unknown transaction status";
                        }
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.NotFound;
                        result.IsError = false;
                        result.Message = "Transaction not found";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = false;
                    result.Message = "Transaction not found";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status from ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var lockRequest = new
                {
                    nftTokenAddress = request.NFTTokenAddress,
                    web3NFTId = request.Web3NFTId,
                    lockedByAvatarId = request.LockedByAvatarId
                };

                var jsonContent = JsonSerializer.Serialize(lockRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/lock", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nftResponse != null)
                    {
                        result.Result = nftResponse;
                        result.IsError = false;
                        result.Message = "NFT locked successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT lock response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var unlockRequest = new
                {
                    nftTokenAddress = request.NFTTokenAddress,
                    web3NFTId = request.Web3NFTId,
                    unlockedByAvatarId = request.UnlockedByAvatarId
                };

                var jsonContent = JsonSerializer.Serialize(unlockRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/unlock", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nftResponse != null)
                    {
                        result.Result = nftResponse;
                        result.IsError = false;
                        result.Message = "NFT unlocked successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT unlock response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var withdrawRequest = new
                {
                    nftTokenAddress = nftTokenAddress,
                    tokenId = tokenId,
                    senderAccountAddress = senderAccountAddress,
                    senderPrivateKey = senderPrivateKey
                };

                var jsonContent = JsonSerializer.Serialize(withdrawRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/nft/withdraw", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bridgeResponse != null)
                    {
                        result.Result = bridgeResponse;
                        result.IsError = false;
                        result.Message = "NFT withdrawal transaction initiated successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge NFT withdrawal response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT from ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var depositRequest = new
                {
                    nftTokenAddress = nftTokenAddress,
                    tokenId = tokenId,
                    receiverAccountAddress = receiverAccountAddress,
                    sourceTransactionHash = sourceTransactionHash
                };

                var jsonContent = JsonSerializer.Serialize(depositRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/nft/deposit", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bridgeResponse != null)
                    {
                        result.Result = bridgeResponse;
                        result.IsError = false;
                        result.Message = "NFT deposit transaction initiated successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge NFT deposit response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT to ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }


        /*

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            var response = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                // Load provider wallets from ThreeFold grid
                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Add ThreeFold wallet
                var walletAddress = $"threefold://{id}";
                var threeFoldWallet = new ProviderWallet
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:wallet:{id}"),
                    ProviderType = ProviderType.ThreeFoldOASIS,
                    Address = walletAddress,
                    PrivateKey = "encrypted_private_key",
                    PublicKey = "public_key"
                };
                
                wallets[ProviderType.ThreeFoldOASIS] = new List<IProviderWallet> { threeFoldWallet };
                
                response.Result = wallets;
                response.Message = "Provider wallets loaded from ThreeFold grid successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading provider wallets from ThreeFold: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var response = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                // Load provider wallets from ThreeFold grid
                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Add ThreeFold wallet
                var walletAddress = $"threefold://{id}";
                var threeFoldWallet = new ProviderWallet
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:wallet:{id}"),
                    ProviderType = ProviderType.ThreeFoldOASIS,
                    Address = walletAddress,
                    PrivateKey = "encrypted_private_key",
                    PublicKey = "public_key"
                };
                
                wallets[ProviderType.ThreeFoldOASIS] = new List<IProviderWallet> { threeFoldWallet };
                
                response.Result = wallets;
                response.Message = "Provider wallets loaded from ThreeFold grid successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading provider wallets from ThreeFold: {ex.Message}");
            }
            return response;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Save provider wallets to ThreeFold grid
                response.Result = true;
                response.Message = "Provider wallets saved to ThreeFold grid successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving provider wallets to ThreeFold: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Save provider wallets to ThreeFold grid
                response.Result = true;
                response.Message = "Provider wallets saved to ThreeFold grid successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving provider wallets to ThreeFold: {ex.Message}");
            }
            return response;
        }

        */

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}
