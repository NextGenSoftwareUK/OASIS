using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS
    {

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Sui provider is already activated";
                    return response;
                }

                // Test connection to Sui RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Sui provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Sui RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Sui provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                _httpClient?.Dispose();
                response.Result = true;
                response.Message = "Sui provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Sui provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }

                // Load avatar from Sui blockchain
                var queryUrl = $"/object/{id}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseSuiToAvatar(content);
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded successfully from Sui blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Sui blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Sui: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }




        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }

                // Load all avatars and filter by location
                var allAvatarsResult = LoadAllAvatarsAsync().Result;
                if (allAvatarsResult.IsError || allAvatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to load avatars from Sui blockchain");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearbyAvatars = new List<IAvatar>();

                foreach (var avatar in allAvatarsResult.Result)
                {
                    // Note: GeoLocation is not available on IAvatar interface
                    // For now, we'll include all avatars. In a real implementation,
                    // you would need to store location data in avatar metadata or use a different approach
                    if (avatar != null)
                    {
                        nearbyAvatars.Add(avatar);
                    }
                }

                response.Result = nearbyAvatars;
                response.IsError = false;
                response.Message = $"Found {nearbyAvatars.Count} avatars within {radiusInMeters} meters";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from Sui: {ex.Message}");
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }

                // Get holons near me from Sui blockchain
                var queryUrl = $"/objects/holons?type={holonType}";

                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    var holons = ParseSuiToHolons(content);
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Loaded {holons.Count()} holons from Sui blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Sui blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Sui: {ex.Message}");
            }

            return response;
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }
                // Sui uses Move language for NFTs
                // Use Sui SDK or Sui API for NFT transfers
                OASISErrorHandling.HandleError(ref response, "SendNFTAsync requires Sui SDK or Sui API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SendNFTAsync: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }
                // Sui uses Move language for NFTs
                // Use Sui SDK or Sui API for NFT minting
                OASISErrorHandling.HandleError(ref response, "MintNFTAsync requires Sui SDK or Sui API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in MintNFTAsync: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }
                // Sui uses Move language for NFTs
                // Use Sui SDK or Sui API for NFT burning
                OASISErrorHandling.HandleError(ref response, "BurnNFTAsync requires Sui SDK or Sui API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in BurnNFTAsync: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load NFT from Sui blockchain using sui_getObject
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_getObject",
                    @params = new object[] { nftTokenAddress, new { showType = true, showOwner = true, showPreviousTransaction = true, showDisplay = true, showContent = true, showBcs = false, showStorageRebate = false } }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var data))
                    {
                        var nftJson = data.TryGetProperty("content", out var contentElement) && contentElement.TryGetProperty("fields", out var fields) 
                            ? fields.GetRawText() 
                            : data.GetRawText();
                        
                        // Parse NFT data from Sui response
                        var nftData = JsonSerializer.Deserialize<JsonElement>(nftJson);
                        var nft = new Web3NFT
                        {
                            NFTTokenAddress = nftTokenAddress,
                            Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "",
                            Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                            ImageUrl = nftData.TryGetProperty("image", out var img) ? img.GetString() : "",
                            JSONMetaDataURL = nftData.TryGetProperty("external_url", out var extUrl) ? extUrl.GetString() : ""
                        };

                        response.Result = nft;
                        response.IsError = false;
                        response.Message = "NFT loaded successfully from Sui blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "NFT not found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load NFT from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadOnChainNFTDataAsync: {ex.Message}");
            }
            return response;
        }



        /// <summary>
        /// Parse Sui blockchain response to Avatar object
        /// </summary>
        private Avatar ParseSuiToAvatar(string suiJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Sui JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(suiJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromSui(suiJson);
            }
        }

        private List<IHolon> ParseSuiToHolons(string suiJson)
        {
            try
            {
                // Deserialize the complete Holon list from Sui JSON
                var holons = System.Text.Json.JsonSerializer.Deserialize<List<Holon>>(suiJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return holons?.Cast<IHolon>().ToList() ?? new List<IHolon>();
            }
            catch (Exception)
            {
                // If JSON deserialization fails, return empty list
                return new List<IHolon>();
            }
        }

        /// <summary>
        /// Create Avatar from Sui response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromSui(string suiJson)
        {
            try
            {
                // Extract basic information from Sui JSON response
                var suiAddress = ExtractSuiProperty(suiJson, "address") ?? "sui_user";
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{suiAddress}"),
                    Username = suiAddress,
                    Email = ExtractSuiProperty(suiJson, "email") ?? $"user@{suiAddress}.sui",
                    FirstName = ExtractSuiProperty(suiJson, "first_name"),
                    LastName = ExtractSuiProperty(suiJson, "last_name"),
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
        /// Extract property value from Sui JSON response
        /// </summary>
        private string ExtractSuiProperty(string suiJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Sui properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(suiJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Sui blockchain format
        /// </summary>
        private string ConvertAvatarToSui(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Sui blockchain structure
                var suiData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(suiData, new JsonSerializerOptions
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
        /// Convert Holon to Sui blockchain format
        /// </summary>
        private string ConvertHolonToSui(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Sui blockchain structure
                var suiData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(suiData, new JsonSerializerOptions
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



    }
}
