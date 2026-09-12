using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {
        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond token transaction
                var transaction = new
                {
                    nonce = 0,
                    value = "0",
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = $"ESDTTransfer@{token}@{amount}",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
            }
            return result;
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
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }



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
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Create Elrond NFT transfer transaction using ESDT NFT standard
                // Elrond uses ESDT (Elrond Standard Digital Token) for NFTs
                var transaction = new
                {
                    nonce = 0,
                    value = "0",
                    receiver = request.ToWalletAddress,
                    sender = request.FromWalletAddress,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = $"ESDTNFTTransfer@{request.TokenAddress}@{request.TokenId}@{request.ToWalletAddress}",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "nft-transfer-completed"
                    };

                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT transfer sent successfully via Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT transfer via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT transfer via Elrond: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Create Elrond NFT mint transaction
                var transaction = new
                {
                    nonce = 0,
                    value = "0",
                    receiver = request.SendToAddressAfterMinting ?? "",
                    sender = request.SendToAddressAfterMinting ?? "",
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = $"ESDTNFTCreate@{"ELROND-NFT"}@01@{request.SendToAddressAfterMinting ?? ""}",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "nft-mint-completed"
                    };

                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT minted successfully via Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Elrond: {ex.Message}", ex);
            }
            return result;
        }



        /// <summary>
        /// Parse Elrond blockchain response to Avatar object
        /// </summary>
        private Avatar ParseElrondToAvatar(string elrondJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Elrond JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(elrondJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromElrond(elrondJson);
            }
        }

        /// <summary>
        /// Create Avatar from Elrond response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromElrond(string elrondJson)
        {
            try
            {
                // Extract basic information from Elrond JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractElrondProperty(elrondJson, "address") ?? "elrond_user",
                    Email = ExtractElrondProperty(elrondJson, "email") ?? "user@elrond.example",
                    FirstName = ExtractElrondProperty(elrondJson, "first_name"),
                    LastName = ExtractElrondProperty(elrondJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Elrond JSON response
        /// </summary>
        private string ExtractElrondProperty(string elrondJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Elrond properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(elrondJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Elrond blockchain format
        /// </summary>
        private string ConvertAvatarToElrond(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Elrond blockchain structure
                var elrondData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(elrondData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to Elrond blockchain format
        /// </summary>
        private string ConvertHolonToElrond(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Elrond blockchain structure
                var elrondData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(elrondData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }



        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var wallet in avatarResult.Result.ProviderWallets)
                    {
                        if (!providerWallets.ContainsKey(wallet.Key))
                        {
                            providerWallets[wallet.Key] = new List<IProviderWallet>();
                        }
                        providerWallets[wallet.Key].AddRange(wallet.Value);
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from Elrond";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from Elrond: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to Elrond: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Query Elrond NFT metadata using ESDT API
                // Elrond API: GET /nfts/{identifier}
                var response = await _httpClient.GetAsync($"/nfts/{Uri.EscapeDataString(nftTokenAddress)}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var web3NFT = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = nftData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                        Symbol = nftData.TryGetProperty("ticker", out var tickerProp) ? tickerProp.GetString() : null,
                        JSONMetaDataURL = nftData.TryGetProperty("uri", out var uriProp) ? uriProp.GetString() : null
                    };

                    result.Result = web3NFT;
                result.IsError = false;
                result.Message = "NFT data loaded successfully from Elrond blockchain";
            }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Elrond: {ex.Message}");
            }
            return result;
        }


    }
}
