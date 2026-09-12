using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
// using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request; // Removed - use Requests (plural) instead
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS
{
    public partial class MoralisOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Moralis Web3 Data API is read-only - it doesn't support burning NFTs
                // For burning NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to burn NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            try
            {
                // REAL Moralis Web3 Data API endpoint: GET /nft/{address}/metadata?chain={chain}
                // Gets NFT metadata for a contract address
                var response = await _httpClient.GetAsync($"{_baseUrl}/nft/{Uri.EscapeDataString(nftTokenAddress)}/metadata?chain={_chain}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    // Parse Moralis NFT response to OASIS NFT format
                    // Moralis returns: { "name": "...", "symbol": "...", "token_uri": "...", ... }
                    // Convert to IWeb3NFT object
                    var web3NFT = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = nftData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                        Symbol = nftData.TryGetProperty("symbol", out var symbolProp) ? symbolProp.GetString() : null,
                        JSONMetaDataURL = nftData.TryGetProperty("token_uri", out var uriProp) ? uriProp.GetString() : null
                    };
                    
                    return new OASISResult<IWeb3NFT>(web3NFT) { Message = "NFT metadata loaded from Moralis Web3 Data API successfully." };
                }
                return new OASISResult<IWeb3NFT>(null) { Message = "NFT not found" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFT>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
                return result;
            }
        }


        /// <summary>
        /// Load avatar data from Moralis Web3 API
        /// </summary>
        private async Task<string> LoadAvatarFromMoralisAsync(string avatarId, int version = 0)
        {
            try
            {
                // Query Moralis Web3 API for avatar data
                var request = new
                {
                    address = GetOASISContractAddress(),
                    function_name = "getAvatar",
                    abi = GetOASISContractABI(),
                    @params = new
                    {
                        avatarId = avatarId,
                        version = version
                    }
                };

                // REAL Moralis REST API endpoint: POST /{address}/function
                var response = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MoralisApiResult>(content);
                    return result?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading avatar from Moralis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar data using Moralis IPFS API for decentralized storage
        /// REAL Moralis IPFS API endpoint: POST /ipfs/uploadFolder
        /// Documentation: https://docs.moralis.com/web3-data-api/evm/reference/upload-folder-to-ipfs
        /// </summary>
        private async Task<string> SaveAvatarToMoralisAsync(IAvatar avatar)
        {
            try
            {
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // REAL Moralis IPFS API: POST /ipfs/uploadFolder
                // Request body format: { "path": "string", "content": "base64_encoded_content" }
                // For single file, we create a folder structure
                var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);
                var base64Content = Convert.ToBase64String(avatarBytes);
                
                var requestBody = new
                {
                    path = $"avatar_{avatar.Id}.json",
                    content = base64Content
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/uploadFolder", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    // Moralis IPFS returns: { "path": "ipfs://..." }
                    if (result.TryGetProperty("path", out var path))
                    {
                        return path.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar to Moralis IPFS: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load holon data from Moralis Web3 API
        /// </summary>
        private async Task<string> LoadHolonFromMoralisAsync(string holonId, int version = 0)
        {
            try
            {
                var request = new
                {
                    address = GetOASISContractAddress(),
                    function_name = "getHolon",
                    abi = GetOASISContractABI(),
                    @params = new
                    {
                        holonId = holonId,
                        version = version
                    }
                };

                // REAL Moralis REST API endpoint: POST /{address}/function
                var response = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MoralisApiResult>(content);
                    return result?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading holon from Moralis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save holon data to Moralis Web3 API
        /// </summary>
        private async Task<string> SaveHolonToMoralisAsync(IHolon holon)
        {
            try
            {
                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // REAL Moralis IPFS API: POST /ipfs/uploadFolder
                // Request body format: { "path": "string", "content": "base64_encoded_content" }
                var holonBytes = Encoding.UTF8.GetBytes(holonJson);
                var base64Content = Convert.ToBase64String(holonBytes);
                
                var requestBody = new
                {
                    path = $"holon_{holon.Id}.json",
                    content = base64Content
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/uploadFolder", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    // Moralis IPFS returns: { "path": "ipfs://..." }
                    if (result.TryGetProperty("path", out var path))
                    {
                        return path.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving holon to Moralis IPFS: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // This would be the deployed OASIS smart contract address
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        /// <summary>
        /// Get OASIS smart contract ABI
        /// </summary>
        private string GetOASISContractABI()
        {
            // This would be the OASIS smart contract ABI
            return @"[
                {
                    ""inputs"": [
                        {""name"": ""avatarId"", ""type"": ""string""},
                        {""name"": ""version"", ""type"": ""uint256""}
                    ],
                    ""name"": ""getAvatar"",
                    ""outputs"": [
                        {""name"": """", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""view"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""name"": ""avatarId"", ""type"": ""string""},
                        {""name"": ""avatarData"", ""type"": ""string""}
                    ],
                    ""name"": ""saveAvatar"",
                    ""outputs"": [
                        {""name"": """", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                }
            ]";
        }

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                    return result;
                }
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                    return result;
                }
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                LockedByAvatarId = Guid.Empty
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                    return result;
                }
            }

            // Moralis Web3 Data API is read-only - it doesn't support depositing/minting NFTs
            // For depositing NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
            // Moralis can be used to query NFT data after the transaction
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to deposit/mint NFTs, then query results via Moralis.",
                Status = BridgeTransactionStatus.Canceled
            };
            OASISErrorHandling.HandleError(ref result, "Moralis Web3 Data API is read-only. Use blockchain SDK to deposit NFTs.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

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
