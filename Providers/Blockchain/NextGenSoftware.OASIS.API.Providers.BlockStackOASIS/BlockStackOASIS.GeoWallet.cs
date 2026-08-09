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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
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
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                    {
                        providerWallets[group.Key] = group.SelectMany(g => g.Value).ToList();
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from BlockStack";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from BlockStack: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
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
                    // Set the provider wallets dictionary directly
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    // Count total wallets
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to BlockStack";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to BlockStack: {ex.Message}", ex);
            }
            return result;
        }

    }
}
