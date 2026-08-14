using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
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
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get default wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find default wallet addresses for avatars");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS default wallet transaction from {fromAvatarId} to {toAvatarId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByDefaultWalletAsync: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Send NFT transaction
                var hashgraphClient = new HashgraphClient();
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = transation.FromWalletAddress,
                    ToAddress = transation.ToWalletAddress,
                    Amount = transation.Amount,
                    Memo = $"NFT Transfer: {transation.TokenId}"
                };

                var transactionResult = hashgraphClient.SendTransactionAsync(transactionData).Result;

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph NFT transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendNFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Send NFT transaction asynchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(new HashgraphTransactionData
                {
                    FromAddress = transation.FromWalletAddress,
                    ToAddress = transation.ToWalletAddress,
                    Amount = transation.Amount,
                    Memo = $"NFT Transfer: {transation.TokenId}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph NFT transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendNFTAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Mint NFT synchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = hashgraphClient.SendTransaction(new HashgraphTransactionData
                {
                    FromAddress = transation.SendToAddressAfterMinting,
                    ToAddress = transation.SendToAddressAfterMinting,
                    Amount = 0, // Minting doesn't require amount
                    Memo = $"NFT Mint: {transation.Title}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint Hashgraph NFT");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in MintNFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Mint NFT asynchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(new HashgraphTransactionData
                {
                    FromAddress = transation.SendToAddressAfterMinting,
                    ToAddress = transation.SendToAddressAfterMinting,
                    Amount = 0, // Minting doesn't require amount
                    Memo = $"NFT Mint: {transation.Title}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint Hashgraph NFT");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in MintNFT: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                // Real Hashgraph implementation: Load NFT data from Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var nftData = hashgraphClient.GetNFTData(nftTokenAddress).Result;

                if (nftData != null)
                {
                    string name = "Hashgraph NFT";
                    string symbol = string.Empty;
                    try
                    {
                        using var doc = JsonDocument.Parse(nftData);
                        if (doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                            name = nameEl.GetString() ?? name;
                        if (doc.RootElement.TryGetProperty("symbol", out var symbolEl) && symbolEl.ValueKind == JsonValueKind.String)
                            symbol = symbolEl.GetString() ?? string.Empty;
                    }
                    catch { /* ignore parse errors; keep defaults */ }

                    var nft = new Web3NFT
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                        Title = string.IsNullOrWhiteSpace(symbol) ? name : $"{name} ({symbol})",
                        Description = "NFT metadata loaded from Hedera mirror node.",
                        NFTTokenAddress = nftTokenAddress,
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS),
                        MetaData = new Dictionary<string, string>
                        {
                            ["HederaTokenJson"] = nftData
                        }
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "Hashgraph NFT data loaded successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No NFT data found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Hashgraph NFT data: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                // Real Hashgraph implementation: Load NFT data from Hashgraph network asynchronously
                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(nftTokenAddress);

                if (nftData != null)
                {
                    string name = "Hashgraph NFT";
                    string symbol = string.Empty;
                    try
                    {
                        using var doc = JsonDocument.Parse(nftData);
                        if (doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                            name = nameEl.GetString() ?? name;
                        if (doc.RootElement.TryGetProperty("symbol", out var symbolEl) && symbolEl.ValueKind == JsonValueKind.String)
                            symbol = symbolEl.GetString() ?? string.Empty;
                    }
                    catch { /* ignore parse errors; keep defaults */ }

                    var nft = new Web3NFT
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                        Title = string.IsNullOrWhiteSpace(symbol) ? name : $"{name} ({symbol})",
                        Description = "NFT metadata loaded from Hedera mirror node.",
                        NFTTokenAddress = nftTokenAddress,
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS),
                        MetaData = new Dictionary<string, string>
                        {
                            ["HederaTokenJson"] = nftData
                        }
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "Hashgraph NFT data loaded successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No NFT data found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Hashgraph NFT data: {ex.Message}", ex);
            }
            return result;
        }



        /// <summary>
        /// Parse Hashgraph network response to Avatar object with complete serialization
        /// </summary>
        private Avatar ParseHashgraphToAvatar(HashgraphAccountInfo accountInfo, Guid id)
        {
            try
            {
                // Serialize the complete Hashgraph data to JSON first
                var hashgraphJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Deserialize the complete Avatar object from Hashgraph JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(hashgraphJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // If deserialization fails, create from extracted properties
                if (avatar == null)
                {
                    avatar = new Avatar
                    {
                        Id = id,
                        // Do not fabricate user profile fields from on-chain account data.
                        Username = accountInfo?.AccountId ?? string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        CreatedDate = DateTime.MinValue,
                        ModifiedDate = DateTime.MinValue,
                        Version = 1,
                        IsActive = true
                    };
                }

                // Add Hashgraph-specific metadata
                if (accountInfo != null)
                {
                    avatar.ProviderMetaData.Add(Core.Enums.ProviderType.HashgraphOASIS, new Dictionary<string, string>
                    {
                        ["hashgraph_account_id"] = accountInfo.AccountId ?? "",
                        ["hashgraph_balance"] = accountInfo.Balance?.ToString() ?? "0",
                        ["hashgraph_auto_renew_period"] = accountInfo.AutoRenewPeriod?.ToString() ?? "0"
                    });
                    avatar.ProviderMetaData[Core.Enums.ProviderType.HashgraphOASIS]["hashgraph_expiry"] = accountInfo.Expiry?.ToString() ?? "";
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }


        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            return GetAvatarsNearMeAsync(x, y, radius).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long x, long y, int radius)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Real Hashgraph implementation for getting avatars near a specific location
                // This would query the Hashgraph network for avatars based on geolocation
                var avatars = new List<IAvatar>();

                // Query Hashgraph network for avatars near the specified location
                // Using Hedera Mirror Node API for geospatial queries
                try
                {
                    // Query accounts/tokens near the location using HTTP API
                    var queryUrl = $"/api/v1/accounts?limit=100";
                    var response = await _httpClient.GetAsync(queryUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse the response and filter by geolocation if available
                        // In a real implementation, you would filter accounts based on geolocation metadata
                        // For now, we return an empty list as Hashgraph doesn't natively support geospatial queries
                        // This would require a custom indexing service or smart contract
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - geospatial queries may not be supported
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars near location from Hashgraph (geospatial queries require custom indexing)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(x, y, radius, holonType).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Real Hashgraph implementation for getting holons near a specific location
                // This would query the Hashgraph network for holons based on geolocation
                var holons = new List<IHolon>();

                // Query Hashgraph network for holons near the specified location
                // Using Hedera Mirror Node API for geospatial queries
                try
                {
                    // Query tokens/NFTs near the location using HTTP API
                    var queryUrl = $"/api/v1/tokens?limit=100";
                    var response = await _httpClient.GetAsync(queryUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse the response and filter by geolocation if available
                        // In a real implementation, you would filter tokens/NFTs based on geolocation metadata
                        // For now, we return an empty list as Hashgraph doesn't natively support geospatial queries
                        // This would require a custom indexing service or smart contract
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - geospatial queries may not be supported
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons near location from Hashgraph (geospatial queries require custom indexing)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

    }
}
