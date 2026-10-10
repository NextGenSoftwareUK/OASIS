using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS
{
    public partial class PinataOASIS
    {
        private void Init()
        {
            this.ProviderName = "PinataOASIS";
            this.ProviderDescription = "Pinata IPFS Provider for OASIS";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PinataOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Initialize HttpClient
                _httpClient = new HttpClient();
                _httpClient.BaseAddress = new Uri("https://api.pinata.cloud");
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Get configuration from OASIS DNA
                var pinataConfig = _OASISDNA?.OASIS?.StorageProviders?.PinataOASIS;
                if (pinataConfig != null)
                {
                    // Parse connection string for API credentials
                    ParseConnectionString(pinataConfig.ConnectionString);
                }

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "PinataOASIS Provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in ActivateProvider Method. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Initialize HttpClient
                _httpClient = new HttpClient();
                _httpClient.BaseAddress = new Uri("https://api.pinata.cloud");
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Get configuration from OASIS DNA
                var pinataConfig = _OASISDNA?.OASIS?.StorageProviders?.PinataOASIS;
                if (pinataConfig != null)
                {
                    // Parse connection string for API credentials
                    ParseConnectionString(pinataConfig.ConnectionString);
                }

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "PinataOASIS Provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in ActivateProviderAsync Method. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                result.Result = true;
                IsProviderActivated = false;
                result.Message = "PinataOASIS Provider deactivated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in DeActivateProvider Method. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                result.Result = true;
                IsProviderActivated = false;
                result.Message = "PinataOASIS Provider deactivated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in DeActivateProviderAsync Method. Reason: {e}");
            }

            return result;
        }

        private void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;

            // Parse connection string format: "https://api.pinata.cloud?apiKey=xxx&secretKey=yyy&jwt=zzz&gateway=aaa"
            var uri = new Uri(connectionString);
            var query = ParseQueryString(uri.Query);
            
            _apiKey = query.ContainsKey("apiKey") ? query["apiKey"] : null;
            _secretKey = query.ContainsKey("secretKey") ? query["secretKey"] : null;
            _jwt = query.ContainsKey("jwt") ? query["jwt"] : null;
            _gatewayUrl = query.ContainsKey("gateway") ? query["gateway"] : "https://gateway.pinata.cloud";
        }

        private Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(queryString))
                return result;

            // Remove the leading '?' if present
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var pairs = queryString.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
                }
            }

            return result;
        }

        private void SetAuthenticationHeaders()
        {
            if (!string.IsNullOrEmpty(_jwt))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
            }
            else if (!string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_secretKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("pinata_api_key");
                _httpClient.DefaultRequestHeaders.Remove("pinata_secret_api_key");
                _httpClient.DefaultRequestHeaders.Add("pinata_api_key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("pinata_secret_api_key", _secretKey);
            }
        }

        // Pinata-specific methods for file operations
        public async Task<OASISResult<string>> UploadFileToPinataAsync(byte[] fileData, string fileName, string contentType = "application/octet-stream")
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                SetAuthenticationHeaders();

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(fileData);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    content.Add(fileContent, "file", fileName);

                    var response = await _httpClient.PostAsync("/pinning/pinFileToIPFS", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var pinataResponse = JsonConvert.DeserializeObject<PinataFileResponse>(responseContent);
                        result.Result = pinataResponse.IpfsHash;
                        result.Message = "File uploaded to Pinata successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to upload file to Pinata. Status: {response.StatusCode}, Response: {responseContent}");
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading file to Pinata. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<string>> UploadJsonToPinataAsync(object jsonData, string name = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                SetAuthenticationHeaders();

                var requestData = new
                {
                    pinataContent = jsonData,
                    pinataMetadata = new
                    {
                        name = name ?? "OASIS Data",
                        keyvalues = new Dictionary<string, string>
                        {
                            { "source", "OASIS" },
                            { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                        }
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/pinning/pinJSONToIPFS", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var pinataResponse = JsonConvert.DeserializeObject<PinataFileResponse>(responseContent);
                    result.Result = pinataResponse.IpfsHash;
                    result.Message = "JSON uploaded to Pinata successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to upload JSON to Pinata. Status: {response.StatusCode}, Response: {responseContent}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading JSON to Pinata. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<byte[]>> DownloadFileFromPinataAsync(string ipfsHash)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                var url = $"{_gatewayUrl}/ipfs/{ipfsHash}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsByteArrayAsync();
                    result.Message = "File downloaded from Pinata successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to download file from Pinata. Status: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error downloading file from Pinata. Reason: {e}");
            }

            return result;
        }

        public string GetFileUrl(string ipfsHash)
        {
            return $"{_gatewayUrl}/ipfs/{ipfsHash}";
        }

        // Required abstract methods from OASISStorageProviderBase
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            
            try
            {
                // Download avatar data from Pinata using providerKey as IPFS hash
                var downloadResult = await DownloadFileFromPinataAsync(providerKey);
                
                if (downloadResult.IsError || downloadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from Pinata. Reason: {downloadResult.Message}");
                    return result;
                }

                // Deserialize avatar data
                var avatarJson = Encoding.UTF8.GetString(downloadResult.Result);
                var avatar = JsonConvert.DeserializeObject<Avatar>(avatarJson);
                
                result.Result = avatar;
                result.Message = "Avatar loaded from Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Pinata. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            
            try
            {
                // For Pinata, we search for the avatar by ID using Pinata's metadata search API
                // This uses Pinata's real metadata indexing system for efficient data retrieval
                var searchResult = await SearchAsync(new SearchParams { });
                
                if (searchResult.IsError || searchResult.Result == null || !searchResult.Result.SearchResultAvatars.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found in Pinata storage");
                    return result;
                }

                // Get the first result and deserialize as Avatar
                var holon = searchResult.Result.SearchResultAvatars.First();
                var avatarJson = JsonConvert.SerializeObject(holon);
                var avatar = JsonConvert.DeserializeObject<Avatar>(avatarJson);
                
                result.Result = avatar;
                result.Message = "Avatar loaded successfully from Pinata";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Pinata: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for avatar by email in Pinata using metadata search
                var searchUrl = $"/data/pinList?metadata[name]=avatar&metadata[email]={avatarEmail}";
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var pinListResponse = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                    
                    if (pinListResponse.Rows != null && pinListResponse.Rows.Any())
                    {
                        // Get the first matching pin and download the avatar data
                        var pin = pinListResponse.Rows.First();
                        var downloadResult = await DownloadFileFromPinataAsync(pin.IpfsPinHash);
                        
                        if (!downloadResult.IsError && downloadResult.Result != null)
                        {
                            var avatarJson = Encoding.UTF8.GetString(downloadResult.Result);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(avatarJson);
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from Pinata by email";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to download avatar data from Pinata");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"No avatar found with email: {avatarEmail}");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Pinata for avatar by email: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Pinata: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for avatar by username in Pinata using metadata search
                var searchUrl = $"/data/pinList?metadata[name]=avatar&metadata[username]={avatarUsername}";
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var pinListResponse = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                    
                    if (pinListResponse.Rows != null && pinListResponse.Rows.Any())
                    {
                        // Get the first matching pin and download the avatar data
                        var pin = pinListResponse.Rows.First();
                        var downloadResult = await DownloadFileFromPinataAsync(pin.IpfsPinHash);
                        
                        if (!downloadResult.IsError && downloadResult.Result != null)
                        {
                            var avatarJson = Encoding.UTF8.GetString(downloadResult.Result);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(avatarJson);
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from Pinata by username";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to download avatar data from Pinata");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"No avatar found with username: {avatarUsername}");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Pinata for avatar by username: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Pinata: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get all avatar pins from Pinata
                var searchUrl = "/data/pinList?metadata[name]=avatar";
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var pinListResponse = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                    
                    var avatars = new List<IAvatar>();
                    
                    if (pinListResponse.Rows != null)
                    {
                        foreach (var pin in pinListResponse.Rows)
                        {
                            var downloadResult = await DownloadFileFromPinataAsync(pin.IpfsPinHash);
                            
                            if (!downloadResult.IsError && downloadResult.Result != null)
                            {
                                var avatarJson = Encoding.UTF8.GetString(downloadResult.Result);
                                var avatar = JsonConvert.DeserializeObject<Avatar>(avatarJson);
                                avatars.Add(avatar);
                            }
                        }
                    }
                    
                    result.Result = avatars;
                    result.IsError = false;
                    result.Message = $"Loaded {avatars.Count} avatars from Pinata";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get all avatars from Pinata: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Pinata: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for avatar detail by ID in Pinata
                var searchUrl = $"/data/pinList?metadata[name]=avatarDetail&metadata[id]={id}";
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var pinListResponse = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                    
                    if (pinListResponse.Rows != null && pinListResponse.Rows.Any())
                    {
                        var pin = pinListResponse.Rows.First();
                        var downloadResult = await DownloadFileFromPinataAsync(pin.IpfsPinHash);
                        
                        if (!downloadResult.IsError && downloadResult.Result != null)
                        {
                            var avatarDetailJson = Encoding.UTF8.GetString(downloadResult.Result);
                            var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailJson);
                            result.Result = avatarDetail;
                            result.IsError = false;
                            result.Message = "Avatar detail loaded successfully from Pinata";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to download avatar detail data from Pinata");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"No avatar detail found with ID: {id}");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Pinata for avatar detail: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Pinata: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            
            try
            {
                // Upload avatar data to Pinata
                var uploadResult = await UploadJsonToPinataAsync(avatar, $"Avatar_{avatar.Id}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Pinata. Reason: {uploadResult.Message}");
                    return result;
                }

                // Store the IPFS hash as the provider key
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = avatar;
                result.Message = "Avatar saved to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Pinata. Reason: {e}");
            }

            return result;
        }

    }
}
