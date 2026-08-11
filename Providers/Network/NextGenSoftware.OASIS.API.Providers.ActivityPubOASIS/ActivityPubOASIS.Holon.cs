using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS
{
    public partial class ActivityPubOASIS
    {

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holon from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{id}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon object
                    var holon = ParseActivityPubToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holon by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon object
                    var holon = ParseActivityPubToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon collection
                    var holons = ParseActivityPubToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.Message = "Holons loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all holons from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Save holon to ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses";
                var activityPubJson = ConvertHolonToActivityPub(holon);
                
                var content = new StringContent(activityPubJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(apiUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = holon;
                    response.Message = "Holon saved to ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete holon from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = null; // Return null since holon is deleted
                    response.Message = "Holon deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete holon by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = null; // Return null since holon is deleted
                    response.Message = "Holon deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon by provider key from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars and filter by geo-location
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading avatars: {avatarsResult.Message}");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from ActivityPub: {ex.Message}", ex);
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons and filter by geo-location and type
                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holons: {holonsResult.Message}");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from ActivityPub: {ex.Message}", ex);
            }

            return response;
        }


    }
}
