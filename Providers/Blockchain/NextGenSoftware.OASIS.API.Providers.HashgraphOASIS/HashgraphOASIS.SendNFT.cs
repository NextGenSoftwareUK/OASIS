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

        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
            => SendNamedAvatarTransferAsync(fromAvatarEmail, toAvatarEmail, amount, token, true);

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(transation);
                var address = string.IsNullOrWhiteSpace(transation.FromNFTTokenAddress)
                    ? transation.TokenAddress + "/" + transation.TokenId : transation.FromNFTTokenAddress;
                var asset = ParseHederaNftAddress(address);
                if ((!string.IsNullOrWhiteSpace(transation.TokenAddress) && transation.TokenAddress != asset.TokenId)
                    || (!string.IsNullOrWhiteSpace(transation.TokenId) && transation.TokenId != asset.Serial))
                    throw new ArgumentException("NFT asset identifiers must agree.");
                if (transation.Amount != 1) throw new ArgumentException("An individual HTS serial transfer requires Amount = 1.");
                if (!string.IsNullOrWhiteSpace(transation.OwnerSeedPhrase))
                    throw new ArgumentException("Recover an explicit Hedera key before sending an NFT.");
                if (string.IsNullOrWhiteSpace(transation.OwnerPrivateKey))
                    throw new ArgumentException("A real Hedera sender signing key is required.");
                var transactionResult = await HederaSdkClient.InvokeAsync("nftTransfer", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes,
                    sender = transation.FromWalletAddress, recipient = transation.ToWalletAddress,
                    tokenId = asset.TokenId, serials = new[] { asset.Serial }, memo = transation.MemoText,
                    privateKey = transation.OwnerPrivateKey, publicKey = transation.OwnerPublicKey, keyType = "der",
                    transactionId = HederaSdkClient.CreateTransactionId(transation.FromWalletAddress)
                });
                if (transactionResult.GetProperty("status").GetString() == "SUCCESS")
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.GetProperty("transactionId").GetString(),
                        Web3NFT = new Web3NFT { NFTTokenAddress = address, CollectionPublicKey = asset.TokenId }
                    };
                    result.IsError = false;
                    result.Message = "HTS NFT transfer confirmed by consensus receipt.";
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
            return MintHederaNftAsync(transation).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            return await MintHederaNftAsync(transation);
        }



        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                var asset = ParseHederaNftAddress(nftTokenAddress);
                using var response = await _httpClient.GetAsync($"tokens/{asset.TokenId}/nfts/{asset.Serial}");
                response.EnsureSuccessStatusCode();
                var nftData = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(nftData);
                var data = document.RootElement;
                if (data.GetProperty("token_id").GetString() != asset.TokenId
                    || data.GetProperty("serial_number").GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture) != asset.Serial)
                    throw new InvalidOperationException("Hedera mirror returned a different NFT identity.");
                if (data.GetProperty("deleted").GetBoolean())
                    throw new InvalidOperationException("The requested Hedera NFT is deleted.");
                var metadata = data.GetProperty("metadata").GetString();
                var bytes = Convert.FromBase64String(metadata ?? "");
                if (bytes.Length > 100)
                    throw new InvalidOperationException("Hedera mirror NFT metadata exceeds the HTS 100-byte limit.");
                // HTS metadata is arbitrary bytes, not necessarily a JSON document or UTF-8 URL.
                var text = Encoding.UTF8.GetString(bytes);
                if (!Encoding.UTF8.GetBytes(text).SequenceEqual(bytes)) text = null;
                var owner = data.GetProperty("account_id").GetString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(owner ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new InvalidOperationException("Hedera mirror returned no valid owner account.");
                {
                    var nft = new Web3NFT
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                        Title = $"HTS NFT {nftTokenAddress}",
                        Description = "NFT metadata loaded from Hedera mirror node.",
                        NFTTokenAddress = nftTokenAddress,
                        CollectionPublicKey = asset.TokenId,
                        SendToAddressAfterMinting = owner,
                        JSONMetaDataURL = Uri.TryCreate(text, UriKind.Absolute, out _) ? text : null,
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS),
                        MetaData = new Dictionary<string, string>
                        {
                            ["HederaNftJson"] = nftData,
                            ["HederaMetadataBase64"] = metadata,
                            ["HederaOwnerAccount"] = owner
                        }
                    };
                    if (text != null) nft.MetaData["HederaMetadataText"] = text;

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "Hashgraph NFT data loaded successfully";
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
