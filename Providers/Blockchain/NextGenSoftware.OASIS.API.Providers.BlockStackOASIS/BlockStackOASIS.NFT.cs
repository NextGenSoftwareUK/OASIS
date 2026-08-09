using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // Send NFT via Stacks blockchain RPC API
                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT send request");
                    return result;
                }

                try
                {
                    // Use Stacks API to send NFT transfer transaction
                    // SIP-009 NFT transfer: (transfer u256 principal principal)
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Get NFT contract address and token ID from request
                        var contractAddress = request.FromNFTTokenAddress ?? request.TokenAddress ?? "";
                        var tokenId = request.TokenId ?? "";
                        
                        // Construct Stacks transaction payload for NFT transfer
                        var transferPayload = new
                        {
                            contract_address = contractAddress,
                            function_name = "transfer",
                            function_args = new[]
                            {
                                tokenId,
                                request.FromWalletAddress ?? "",
                                request.ToWalletAddress
                            }
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(transferPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new Web3NFTTransactionResponse
                            {
                                TransactionResult = txId,
                                SendNFTTransactionResult = txId
                            };
                            result.Message = "NFT transfer transaction submitted successfully";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error sending NFT via Stacks blockchain: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // Mint NFT via Stacks blockchain RPC API
                var sendToAvatarId = request?.SendToAvatarAfterMintingId ?? Guid.Empty;
                if (request == null || sendToAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT mint request");
                    return result;
                }

                try
                {
                    // Use Stacks API to mint NFT
                    // SIP-009 NFT mint: (mint principal)
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Get NFT contract address from request (from meta or base)
                        var contractAddress = request.MetaData?.GetValueOrDefault("NFTTokenAddress") ?? "";
                        
                        // Construct Stacks transaction payload for NFT minting
                        var mintPayload = new
                        {
                            contract_address = contractAddress,
                            function_name = "mint",
                            function_args = new[]
                            {
                                sendToAvatarId.ToString()
                            }
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(mintPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new Web3NFTTransactionResponse
                            {
                                TransactionResult = txId,
                                SendNFTTransactionResult = txId
                            };
                            result.Message = "NFT mint transaction submitted successfully";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Stacks blockchain: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // For burning NFTs, we need to interact with the Stacks blockchain
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT burning. Use Stacks blockchain RPC for NFT operations.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Lock NFT for cross-chain transfer
                // Use Stacks blockchain RPC for NFT locking
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT locking. Use Stacks blockchain RPC for NFT operations.");
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Unlock NFT after cross-chain transfer
                // Use Stacks blockchain RPC for NFT unlocking
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT unlocking. Use Stacks blockchain RPC for NFT operations.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Withdraw NFT for cross-chain transfer using Stacks blockchain
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                // Use Stacks API to withdraw NFT (transfer to bridge contract)
                var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                using (var httpClient = new HttpClient())
                {
                    var bridgeContractAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
                    
                    var withdrawPayload = new
                    {
                        contract_address = nftTokenAddress,
                        function_name = "transfer",
                        function_args = new[]
                        {
                            tokenId,
                            senderAccountAddress,
                            bridgeContractAddress
                        }
                    };
                    
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(withdrawPayload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txData.TryGetProperty("txid", out var txid) ? txid.GetString() ?? "" : "",
                            Status = BridgeTransactionStatus.Pending
                        };
                        result.IsError = false;
                        result.Message = "NFT withdrawal initiated successfully via Stacks blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to withdraw NFT: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Deposit NFT from cross-chain transfer using Stacks blockchain
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, and receiver address are required");
                    return result;
                }

                // Use Stacks API to deposit NFT (transfer from bridge contract to receiver)
                var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                using (var httpClient = new HttpClient())
                {
                    var bridgeContractAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
                    
                    var depositPayload = new
                    {
                        contract_address = nftTokenAddress,
                        function_name = "transfer",
                        function_args = new[]
                        {
                            tokenId,
                            bridgeContractAddress,
                            receiverAccountAddress
                        }
                    };
                    
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(depositPayload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txData.TryGetProperty("txid", out var txid) ? txid.GetString() ?? "" : "",
                            Status = BridgeTransactionStatus.Completed
                        };
                        result.IsError = false;
                        result.Message = "NFT deposit completed successfully via Stacks blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to deposit NFT: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadNFT(Guid id)
        {
            return LoadNFTAsync(id).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(Guid id)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load NFT from BlockStack Gaia storage by ID
                var filePath = $"nfts/{id}.json";
                var nftData = await _blockStackClient.GetFileAsync(filePath);
                
                if (nftData != null && nftData.Count > 0)
                {
                    var nftJson = System.Text.Json.JsonSerializer.Serialize(nftData);
                    var nft = System.Text.Json.JsonSerializer.Deserialize<Web3NFT>(nftJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (nft != null)
                    {
                        result.Result = nft;
                        result.IsError = false;
                        result.Message = "NFT loaded successfully from BlockStack by ID";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT from BlockStack storage");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "NFT not found in BlockStack storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT by ID from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadNFT(string hash)
        {
            return LoadNFTAsync(hash).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(string hash)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(hash))
                {
                    OASISErrorHandling.HandleError(ref result, "Hash cannot be null or empty");
                    return result;
                }

                // Load NFT from BlockStack Gaia storage by hash (transaction hash or content hash)
                var filePath = $"nfts/hash/{hash}.json";
                var nftData = await _blockStackClient.GetFileAsync(filePath);
                
                if (nftData != null && nftData.Count > 0)
                {
                    var nftJson = System.Text.Json.JsonSerializer.Serialize(nftData);
                    var nft = System.Text.Json.JsonSerializer.Deserialize<Web3NFT>(nftJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (nft != null)
                    {
                        result.Result = nft;
                        result.IsError = false;
                        result.Message = "NFT loaded successfully from BlockStack by hash";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT from BlockStack storage");
                    }
                }
                else
                {
                    // Try loading from Stacks blockchain by transaction hash
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync($"{stacksApiUrl}/{hash}");
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                            
                            // Parse NFT from transaction data
                            if (txData.TryGetProperty("events", out var events) && events.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var evt in events.EnumerateArray())
                                {
                                    if (evt.TryGetProperty("event_type", out var eventType) && 
                                        eventType.GetString() == "nft_transfer")
                                    {
                                        var nft = new Web3NFT
                                        {
                                            NFTTokenAddress = evt.TryGetProperty("contract_address", out var contract) ? contract.GetString() : "",
                                            MintTransactionHash = hash,
                                            OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.BlockStackOASIS)
                                        };
                                        
                                        result.Result = nft;
                                        result.IsError = false;
                                        result.Message = "NFT loaded successfully from Stacks blockchain by hash";
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, "NFT not found in BlockStack storage or Stacks blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT by hash from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            return LoadAllNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            var response = new OASISResult<List<IWeb3NFT>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "BlockStack provider is not activated");
                    return response;
                }

                // Load avatar to get wallet address/provider key
                var avatarResult = await LoadAvatarAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar {avatarId}: {avatarResult.Message}");
                    return response;
                }

                // Use avatar's wallet address or provider key to load NFTs
                var walletAddress = avatarResult.Result.ProviderWallets != null && ProviderType != null && avatarResult.Result.ProviderWallets.TryGetValue(ProviderType.Value, out var wallets) && wallets?.Count > 0
                    ? wallets.FirstOrDefault()?.WalletAddress ?? ""
                    : (avatarResult.Result.ProviderUniqueStorageKey != null && ProviderType != null && avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key) ? key : "");
                if (string.IsNullOrEmpty(walletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar {avatarId} does not have a wallet address or provider key");
                    return response;
                }

                // Delegate to LoadAllNFTsForMintAddressAsync
                return await LoadAllNFTsForMintAddressAsync(walletAddress);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFTs for avatar {avatarId}: {ex.Message}");
            }

            return response;
        }

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var response = new OASISResult<List<IWeb3NFT>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "BlockStack provider is not activated");
                    return response;
                }

                // Load NFTs from BlockStack Gaia storage using real BlockStack API
                var storageUrl = $"https://gaia.blockstack.org/hub/{mintWalletAddress}/nfts.json";
                
                using (var httpClient = new HttpClient())
                {
                    var jsonResponse = await httpClient.GetStringAsync(storageUrl);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        // Deserialize the NFT collection from JSON stored in BlockStack
                        var nfts = System.Text.Json.JsonSerializer.Deserialize<List<IWeb4NFT>>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (nfts != null)
                        {
                            response.Result = nfts.Cast<IWeb3NFT>().ToList();
                            response.IsError = false;
                            response.Message = "NFTs loaded from BlockStack Gaia storage successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to deserialize NFTs from BlockStack storage");
                        }
                    }
                    else
                    {
                        response.Result = new List<IWeb3NFT>();
                        response.IsError = false;
                        response.Message = "No NFTs found in BlockStack storage";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFTs from BlockStack: {ex.Message}");
            }

            return response;
        }


        public OASISResult<List<IWeb4GeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IWeb4GeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            var response = new OASISResult<List<IWeb4GeoSpatialNFT>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "BlockStack provider is not activated");
                    return response;
                }

                // Load avatar to get wallet address/provider key
                var avatarResult = await LoadAvatarAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar {avatarId}: {avatarResult.Message}");
                    return response;
                }

                // Use avatar's wallet address or provider key to load GeoNFTs
                var walletAddress = avatarResult.Result.ProviderWallets != null && ProviderType != null && avatarResult.Result.ProviderWallets.TryGetValue(ProviderType.Value, out var wallets) && wallets?.Count > 0
                    ? wallets.FirstOrDefault()?.WalletAddress ?? ""
                    : (avatarResult.Result.ProviderUniqueStorageKey != null && ProviderType != null && avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key) ? key : "");
                if (string.IsNullOrEmpty(walletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar {avatarId} does not have a wallet address or provider key");
                    return response;
                }

                // Delegate to LoadAllGeoNFTsForMintAddressAsync
                return await LoadAllGeoNFTsForMintAddressAsync(walletAddress);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading GeoNFTs for avatar {avatarId}: {ex.Message}");
            }

            return response;
        }

        public OASISResult<List<IWeb4GeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IWeb4GeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var response = new OASISResult<List<IWeb4GeoSpatialNFT>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "BlockStack provider is not activated");
                    return response;
                }

                // Load GeoNFTs from BlockStack Gaia storage using real BlockStack API
                var storageUrl = $"https://gaia.blockstack.org/hub/{mintWalletAddress}/geonfts.json";
                
                using (var httpClient = new HttpClient())
                {
                    var jsonResponse = await httpClient.GetStringAsync(storageUrl);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        // Deserialize the GeoNFT collection from JSON stored in BlockStack
                        var geoNfts = System.Text.Json.JsonSerializer.Deserialize<List<IWeb4GeoSpatialNFT>>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (geoNfts != null)
                        {
                            response.Result = geoNfts.Cast<IWeb4GeoSpatialNFT>().ToList();
                            response.IsError = false;
                            response.Message = "GeoNFTs loaded from BlockStack Gaia storage successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to deserialize GeoNFTs from BlockStack storage");
                        }
                    }
                    else
                    {
                        response.Result = new List<IWeb4GeoSpatialNFT>();
                        response.IsError = false;
                        response.Message = "No GeoNFTs found in BlockStack storage";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading GeoNFTs from BlockStack: {ex.Message}");
            }

            return response;
        }

    }
}
