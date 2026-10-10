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

        public OASISResult<IWeb4GeoSpatialNFT> PlaceGeoNFT(IPlaceWeb4GeoSpatialNFTRequest request)
        {
            return PlaceGeoNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceWeb4GeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IWeb4GeoSpatialNFT>();
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

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Geo NFT request cannot be null");
                    return result;
                }

                // Place Geo NFT by storing it in BlockStack Gaia with geospatial metadata
                // Load the original NFT if OriginalWeb4OASISNFTId is provided
                IWeb4GeoSpatialNFT geoNFT = null;
                if (request.OriginalWeb4OASISNFTId != Guid.Empty)
                {
                    var loadResult = await LoadNFTAsync(request.OriginalWeb4OASISNFTId);
                    if (!loadResult.IsError && loadResult.Result is IWeb4GeoSpatialNFT web4NFT)
                    {
                        geoNFT = web4NFT;
                    }
                }
                
                // Create new Geo NFT if not loaded
                if (geoNFT == null)
                {
                    geoNFT = new Web4OASISGeoSpatialNFT
                    {
                        Id = Guid.NewGuid(),
                        Lat = request.Lat,
                        Long = request.Long
                    };
                }
                else
                {
                    geoNFT.Lat = request.Lat;
                    geoNFT.Long = request.Long;
                }
                
                // Store Geo NFT in Gaia storage with location-based path
                var filePath = $"geonfts/{request.Lat}_{request.Long}_{geoNFT.Id}.json";
                var geoNFTJson = System.Text.Json.JsonSerializer.Serialize(geoNFT);
                var geoNFTDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(geoNFTJson);
                
                await _blockStackClient.PutFileAsync(filePath, geoNFTDict);
                
                result.Result = geoNFT;
                result.IsError = false;
                result.Message = "Geo NFT placed successfully in BlockStack Gaia storage";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error placing Geo NFT in BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceWeb4GeoSpatialNFTRequest request)
        {
            return MintAndPlaceGeoNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceWeb4GeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IWeb4GeoSpatialNFT>();
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

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint and place request cannot be null");
                    return result;
                }

                // First mint the NFT on Stacks blockchain
                var mintRequest = new MintWeb3NFTRequest
                {
                    SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                    MetaData = request.MetaData ?? new Dictionary<string, string>()
                };
                if (request.MetaData != null && request.MetaData.TryGetValue("NFTTokenAddress", out var nftAddr))
                    mintRequest.MetaData["NFTTokenAddress"] = nftAddr;

                var mintResult = await MintNFTAsync(mintRequest);
                if (mintResult.IsError || mintResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT: {mintResult.Message}");
                    return result;
                }

                // Create Geo NFT
                var geoNFT = new Web4OASISGeoSpatialNFT
                {
                    Id = Guid.NewGuid(),
                    Lat = request.Lat,
                    Long = request.Long
                };

                // Then place it at the geospatial location
                var placeRequest = new PlaceWeb4GeoSpatialNFTRequest
                {
                    OriginalWeb4OASISNFTId = geoNFT.Id,
                    Lat = request.Lat,
                    Long = request.Long,
                    PlacedByAvatarId = request.PlacedByAvatarId,
                    GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider
                };
                
                var placeResult = await PlaceGeoNFTAsync(placeRequest);
                if (placeResult.IsError || placeResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to place Geo NFT: {placeResult.Message}");
                    return result;
                }
                
                result.Result = placeResult.Result;
                result.IsError = false;
                result.Message = "Geo NFT minted and placed successfully in BlockStack";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting and placing Geo NFT in BlockStack: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load NFT metadata from BlockStack Gaia storage or Stacks blockchain
                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                try
                {
                    // First try to load from Gaia storage
                    var gaiaNftData = await _blockStackClient.GetFileAsync($"nfts/{nftTokenAddress}.json");
                    if (gaiaNftData != null && gaiaNftData.Count > 0)
                    {
                        var nftJson = System.Text.Json.JsonSerializer.Serialize(gaiaNftData);
                        var nft = System.Text.Json.JsonSerializer.Deserialize<Web3NFT>(nftJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (nft != null)
                        {
                            result.Result = nft;
                            result.Message = "NFT data loaded from BlockStack Gaia storage successfully";
                            return result;
                        }
                    }
                    
                    // Fallback: Query Stacks blockchain API for NFT metadata
                    var stacksApiUrl = "https://api.stacks.co/extended/v1/tokens/nft";
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync($"{stacksApiUrl}/{nftTokenAddress}");
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var stacksNftData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            if (stacksNftData != null)
                            {
                                var nft = new Web3NFT
                                {
                                    Id = Guid.TryParse(stacksNftData.GetValueOrDefault("token_id")?.ToString(), out var tid) ? tid : Guid.NewGuid(),
                                    NFTTokenAddress = nftTokenAddress,
                                    Title = stacksNftData.GetValueOrDefault("name")?.ToString() ?? "",
                                    Description = stacksNftData.GetValueOrDefault("description")?.ToString() ?? "",
                                    ImageUrl = stacksNftData.GetValueOrDefault("image_url")?.ToString() ?? ""
                                };
                                
                                result.Result = nft;
                                result.Message = "NFT data loaded from Stacks blockchain successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"NFT not found for token address: {nftTokenAddress}");
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
            }
            return result;
        }


    }
}
