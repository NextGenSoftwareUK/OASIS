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
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(IAvatar avatar, double radiusKm)
        {
            return GetAvatarsNearMeAsync(avatar, radiusKm).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Real Moralis implementation - get holons near location using avatar's coordinates
                if (avatar.MetaData != null && 
                    avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                {
                    var lat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                    var lon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                    return await GetHolonsNearMeAsync((long)(lat * 1000000), (long)(lon * 1000000), (int)(radiusKm * 1000), holonType);
                }

                // If no coordinates, return empty result
                result.Result = new List<IHolon>();
                result.IsError = false;
                result.Message = "Avatar does not have location coordinates";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(avatar, radiusKm, holonType).Result;
        }

        // IOASISNETProvider methods with correct signatures
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long x, long y, int radius)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
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

                // Real Moralis implementation - get avatars near coordinates
                // Convert coordinates (x, y are in microdegrees: lat*1000000, lon*1000000)
                var centerLat = x / 1000000.0;
                var centerLon = y / 1000000.0;
                var radiusKm = radius / 1000.0;

                var allAvatarsResult = await LoadAllAvatarsAsync(0);
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var nearbyAvatars = new List<IAvatar>();
                if (allAvatarsResult.Result != null)
                {
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        if (avatar.MetaData != null && 
                            avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                        {
                            var avatarLat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                            var avatarLon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                            
                            // Calculate distance using Haversine (GeoHelper returns meters)
                            var distanceMeters = GeoHelper.CalculateDistance(centerLat, centerLon, avatarLat, avatarLon);
                            if (distanceMeters <= radius)
                            {
                                nearbyAvatars.Add(avatar);
                            }
                        }
                    }
                }

                result.Result = nearbyAvatars;
                result.IsError = false;
                result.Message = $"Found {nearbyAvatars.Count} avatars within {radiusKm}km of coordinates ({centerLat}, {centerLon})";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near coordinates: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            return GetAvatarsNearMeAsync(x, y, radius).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Real Moralis implementation - get holons near coordinates
                // Convert coordinates (x, y are in microdegrees: lat*1000000, lon*1000000)
                var centerLat = x / 1000000.0;
                var centerLon = y / 1000000.0;
                var radiusKm = radius / 1000.0;

                var allHolonsResult = await LoadAllHolonsAsync(holonType, false, false, 0, 0, true, false, 0);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                var nearbyHolons = new List<IHolon>();
                if (allHolonsResult.Result != null)
                {
                    foreach (var holon in allHolonsResult.Result)
                    {
                        if (holon.MetaData != null && 
                            holon.MetaData.ContainsKey("Latitude") && holon.MetaData.ContainsKey("Longitude"))
                        {
                            var holonLat = Convert.ToDouble(holon.MetaData["Latitude"]);
                            var holonLon = Convert.ToDouble(holon.MetaData["Longitude"]);
                            
                            // Calculate distance using Haversine (GeoHelper returns meters)
                            var distanceMeters = GeoHelper.CalculateDistance(centerLat, centerLon, holonLat, holonLon);
                            if (distanceMeters <= radius)
                            {
                                nearbyHolons.Add(holon);
                            }
                        }
                    }
                }

                result.Result = nearbyHolons;
                result.IsError = false;
                result.Message = $"Found {nearbyHolons.Count} holons within {radiusKm}km of coordinates ({centerLat}, {centerLon})";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near coordinates: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(x, y, radius, holonType).Result;
        }

        // IOASISNFTProvider Methods
        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
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

                // Moralis Web3 Data API is read-only - it doesn't support sending NFTs
                // For sending NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to send NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
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

                // Moralis Web3 Data API is read-only - it doesn't support minting NFTs
                // For minting NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to mint NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
                return result;
            }
        }

    }
}
