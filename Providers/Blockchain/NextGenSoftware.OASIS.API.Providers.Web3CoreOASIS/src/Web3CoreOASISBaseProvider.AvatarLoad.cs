using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = _httpClient.GetAsync($"{_apiBaseUrl}/network/holons/nearby?type={Type}").Result;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons?.Count ?? 0} holons near you from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near you from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var allAvatarsResult = LoadAllAvatars();
            if (allAvatarsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                return result;
            }
            
            var nearbyAvatars = new List<IAvatar>();
            foreach (var avatar in allAvatarsResult.Result)
            {
                var meta = avatar.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double avatarLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double avatarLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, avatarLat, avatarLong);
                        if (distance <= radiusInMeters)
                            nearbyAvatars.Add(avatar);
                    }
                }
            }
            
            result.Result = nearbyAvatars;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearbyAvatars.Count} avatars within {radiusInMeters}m of ({geoLat}, {geoLong}) from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near you from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var allHolonsResult = LoadAllHolons(Type);
            if (allHolonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                return result;
            }
            
            var nearbyHolons = new List<IHolon>();
            foreach (var holon in allHolonsResult.Result)
            {
                var meta = holon.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double holonLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double holonLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, holonLat, holonLong);
                        if (distance <= radiusInMeters)
                            nearbyHolons.Add(holon);
                    }
                }
            }
            
            result.Result = nearbyHolons;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearbyHolons.Count} holons of type {Type} within {radiusInMeters}m of ({geoLat}, {geoLong}) from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near you from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    // distance helpers moved to GeoHelper for reuse

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var importedCount = 0;
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                    return result;
                }
                importedCount++;
            }

            result.Result = true;
            result.IsError = false;
            result.Message = $"Successfully imported {importedCount} holons to Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load all avatar details from Web3Core blockchain
            var avatarDetailsData = new OASISResult<List<IAvatarDetail>> { Result = new List<IAvatarDetail>() };
            if (avatarDetailsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details: {avatarDetailsData.Message}");
                return result;
            }

            if (avatarDetailsData.Result != null)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatarDetailData in avatarDetailsData.Result)
                {
                    var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailData.ToString());
                    if (avatarDetail != null)
                    {
                        avatarDetails.Add(avatarDetail);
                    }
                }
                
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatar details found on Web3Core blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load all avatars from Web3Core blockchain
            var avatarsData = new OASISResult<List<IAvatar>> { Result = new List<IAvatar>() };
            if (avatarsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsData.Message}");
                return result;
            }

            if (avatarsData.Result != null)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarData in avatarsData.Result)
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(avatarData.ToString());
                    if (avatar != null)
                    {
                        avatars.Add(avatar);
                    }
                }
                
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatars found on Web3Core blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/all?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/{id}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }


    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

}
