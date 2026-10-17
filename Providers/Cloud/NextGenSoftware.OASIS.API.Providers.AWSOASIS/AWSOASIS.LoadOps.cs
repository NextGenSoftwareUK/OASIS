using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS
{
    public partial class AWSOASIS
    {
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatar details from AWS DynamoDB
                var queryUrl = "/dynamodb/avatardetails";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetails = JsonSerializer.Deserialize<IEnumerable<AvatarDetail>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatarDetails != null)
                    {
                        response.Result = avatarDetails;
                        response.IsError = false;
                        response.Message = $"Loaded {avatarDetails.Count()} avatar details from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Save avatar to AWS DynamoDB
                var queryUrl = "/dynamodb/avatar";
                var awsJson = ConvertAvatarToAWS(Avatar);
                
                var content = new StringContent(awsJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = Avatar;
                    response.IsError = false;
                    response.IsSaved = true;
                    response.Message = "Avatar saved to AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar detail cannot be null");
                    return response;
                }

                // Save avatar detail to AWS DynamoDB
                var queryUrl = "/dynamodb/avatardetail";
                var awsJson = JsonSerializer.Serialize(Avatar, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                var content = new StringContent(awsJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = Avatar;
                    response.IsError = false;
                    response.IsSaved = true;
                    response.Message = "Avatar detail saved to AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar from AWS DynamoDB
                var queryUrl = $"/dynamodb/avatar/{id}";
                if (softDelete)
                {
                    // For soft delete, update the record instead
                    queryUrl += "?softDelete=true";
                    var httpResponse = await _httpClient.PutAsync(queryUrl, new StringContent("{}", Encoding.UTF8, "application/json"));
                    response.Result = httpResponse.IsSuccessStatusCode;
                    response.IsError = !httpResponse.IsSuccessStatusCode;
                    response.Message = httpResponse.IsSuccessStatusCode ? "Avatar soft deleted from AWS successfully" : $"Failed to soft delete avatar: {httpResponse.StatusCode}";
                }
                else
                {
                    var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                    response.Result = httpResponse.IsSuccessStatusCode;
                    response.IsError = !httpResponse.IsSuccessStatusCode;
                    response.Message = httpResponse.IsSuccessStatusCode ? "Avatar deleted from AWS successfully" : $"Failed to delete avatar: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/provider-key/{providerKey}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by provider key successfully"
                    : $"Failed to delete avatar by provider key: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/email/{Uri.EscapeDataString(avatarEmail)}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by email successfully"
                    : $"Failed to delete avatar by email: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/username/{Uri.EscapeDataString(avatarUsername)}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by username successfully"
                    : $"Failed to delete avatar by username: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }



        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // AWS implementation for search using DynamoDB query
                var searchResults = new SearchResults();
                
                // Build search query parameters
                var queryParams = new List<string>();
                string searchQuery = null;
                if (searchParams != null && searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    // Extract search query from SearchGroups (similar to LocalFileOASIS)
                    var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                    if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    {
                        searchQuery = textGroup.SearchQuery;
                    }
                }
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    queryParams.Add($"query={Uri.EscapeDataString(searchQuery)}");
                }
                if (version > 0)
                {
                    queryParams.Add($"version={version}");
                }
                
                var queryString = string.Join("&", queryParams);
                var searchUrl = $"/dynamodb/search?{queryString}";
                
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (searchData != null)
                    {
                        // Parse avatars from search results
                        if (searchData.ContainsKey("avatars") && searchData["avatars"] is JsonElement avatarsElement && avatarsElement.ValueKind == JsonValueKind.Array)
                        {
                            var avatars = new List<IAvatar>();
                            foreach (var item in avatarsElement.EnumerateArray())
                            {
                                var avatarJson = item.GetRawText();
                                var avatar = ParseAWSToAvatar(avatarJson);
                                if (avatar != null)
                                {
                                    avatars.Add(avatar);
                                }
                            }
                            searchResults.SearchResultAvatars = avatars;
                        }
                        
                        // Parse holons from search results
                        if (searchData.ContainsKey("holons") && searchData["holons"] is JsonElement holonsElement && holonsElement.ValueKind == JsonValueKind.Array)
                        {
                            var holons = new List<IHolon>();
                            foreach (var item in holonsElement.EnumerateArray())
                            {
                                var holonJson = item.GetRawText();
                                var holon = ParseAWSToHolon(holonJson);
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                            searchResults.SearchResultHolons = holons;
                        }
                        
                        searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                        
                        response.Result = searchResults;
                        response.IsError = false;
                        response.Message = $"Successfully searched AWS and found {searchResults.NumberOfResults} results";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize search results from AWS");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching in AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }




        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // AWS implementation for importing holons to DynamoDB
                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "No holons provided for import");
                    return response;
                }

                var holonsList = holons.ToList();
                var importData = new
                {
                    holons = holonsList.Select(h => ConvertHolonToAWS(h)).ToList()
                };

                var importJson = JsonSerializer.Serialize(importData);
                var content = new StringContent(importJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/dynamodb/import", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Successfully imported {holonsList.Count} holons to AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all holons for avatar from AWS DynamoDB
                var queryUrl = $"/dynamodb/holons/avatar/{avatarId}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count()} holons for avatar {avatarId} from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export data from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by ID from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmailAddress} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all holons from AWS DynamoDB
                var queryUrl = "/dynamodb/holons";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count()} holons from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export all data from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }




        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Example AWS implementation using a custom geospatial endpoint
                var queryUrl = "/net/players/near-me";
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    var players = ParseAWSToPlayers(content);
                    response.Result = players;
                    response.IsError = false;
                    response.Message = "Retrieved players near me from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from AWS: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType holonType = HolonType.All)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var queryUrl = "/net/holons/near-me";
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    var holons = ParseAWSToHolons(content) ?? Enumerable.Empty<IHolon>();

                    if (holonType != HolonType.All)
                        holons = holons.Where(h => h.HolonType == holonType);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Retrieved {holons.Count()} holons near me from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from AWS: {ex.Message}");
            }
            return response;
        }



        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
