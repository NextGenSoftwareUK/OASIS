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










        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (radiusInMeters < 0 || geoLat < -90000000 || geoLat > 90000000
                    || geoLong < -180000000 || geoLong > 180000000)
                    throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Valid coordinates and nonnegative radius are required.");
                result = LoadAllAvatarsAsync().GetAwaiter().GetResult();
                if (result.IsError) return result;
                result.Result = result.Result.Where(item => item.MetaData != null
                    && item.MetaData.TryGetValue("Latitude", out var lat)
                    && item.MetaData.TryGetValue("Longitude", out var lon)
                    && GeoHelper.CalculateDistance(geoLat / 1000000d, geoLong / 1000000d,
                        Convert.ToDouble(lat, System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToDouble(lon, System.Globalization.CultureInfo.InvariantCulture)) <= radiusInMeters).ToList();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (radiusInMeters < 0 || geoLat < -90000000 || geoLat > 90000000
                    || geoLong < -180000000 || geoLong > 180000000)
                    throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Valid coordinates and nonnegative radius are required.");
                result = LoadAllHolonsAsync(holonType).GetAwaiter().GetResult();
                if (result.IsError) return result;
                result.Result = result.Result.Where(item => item.MetaData != null
                    && item.MetaData.TryGetValue("Latitude", out var lat)
                    && item.MetaData.TryGetValue("Longitude", out var lon)
                    && GeoHelper.CalculateDistance(geoLat / 1000000d, geoLong / 1000000d,
                        Convert.ToDouble(lat, System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToDouble(lon, System.Globalization.CultureInfo.InvariantCulture)) <= radiusInMeters).ToList();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
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


        /// <summary>
        /// Create Avatar from Sui response when JSON deserialization fails
        /// </summary>

        /// <summary>
        /// Extract property value from Sui JSON response
        /// </summary>

        /// <summary>
        /// Convert Avatar to Sui blockchain format
        /// </summary>

        /// <summary>
        /// Convert Holon to Sui blockchain format
        /// </summary>



    }
}
