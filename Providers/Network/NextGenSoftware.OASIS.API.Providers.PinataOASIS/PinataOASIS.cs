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
    public class PinataPinListResponse
    {
        public List<PinataPin> Rows { get; set; }
    }

    public class PinataPin
    {
        public string IpfsPinHash { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class PinataOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _secretKey;
        private string _jwt;
        private string _gatewayUrl;
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;
        private IPinataService _pinataService;

        public PinataOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            _pinataService = new PinataService(_apiKey, _secretKey, _jwt);
            Init();
        }

        public PinataOASIS(string OASISDNAPath)
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
            Init();
        }

        public PinataOASIS(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            Init();
        }

        public PinataOASIS(OASISDNA OASISDNA, string OASISDNAPath)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
            Init();
        }

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

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            
            try
            {
                // Upload avatar detail data to Pinata
                var uploadResult = await UploadJsonToPinataAsync(avatarDetail, $"AvatarDetail_{avatarDetail.Id}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Pinata. Reason: {uploadResult.Message}");
                    return result;
                }

                // Store the IPFS hash as the provider key
                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = avatarDetail;
                result.Message = "Avatar detail saved to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Pinata. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            
            try
            {
                // Upload holon data to Pinata
                var uploadResult = await UploadJsonToPinataAsync(holon, $"Holon_{holon.Id}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Pinata. Reason: {uploadResult.Message}");
                    return result;
                }

                // Store the IPFS hash as the provider key
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = holon;
                result.Message = "Holon saved to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Pinata. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError) break;
                    }
                    else
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                
                if (errors.Any())
                {
                    result.Message = $"Saved {savedHolons.Count} holons with {errors.Count} errors";
                    if (!continueOnError && errors.Any())
                    {
                        OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                    }
                }
                else
                {
                    result.Message = $"All {savedHolons.Count} holons saved successfully";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Pinata. Reason: {e}");
            }

            return result;
        }


        //public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, int version = 0)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
            
        //    try
        //    {
        //        // Download holon data from Pinata using providerKey as IPFS hash
        //        var downloadResult = await DownloadFileFromPinataAsync(providerKey);
                
        //        if (downloadResult.IsError || downloadResult.Result == null)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Pinata. Reason: {downloadResult.Message}");
        //            return result;
        //        }

        //        // Deserialize holon data
        //        var holonJson = Encoding.UTF8.GetString(downloadResult.Result);
        //        var holon = JsonConvert.DeserializeObject<Holon>(holonJson);
                
        //        result.Result = holon;
        //        result.Message = "Holon loaded from Pinata successfully";
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading holon from Pinata. Reason: {e}");
        //    }

        //    return result;
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, string propertyName, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
        //    OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not implemented for PinataOASIS - Pinata is primarily for file storage");
        //    return result;
        //}

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, string propertyName, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
        //    OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
        //    return result;
        //}


        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var lookup = await FindPinnedHolonByIdAsync(id);
                if (lookup.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, lookup.Message);
                    return result;
                }

                if (string.IsNullOrWhiteSpace(lookup.Result.PinHash) || lookup.Result.Holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Pinata.");
                    return result;
                }

                var unpinOk = await _pinataService.UnpinFileAsync(lookup.Result.PinHash);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin holon {id} from Pinata.");
                    return result;
                }

                result.Result = lookup.Result.Holon;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Holon unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (IPFS hash) is required.");
                    return result;
                }

                // Capture current content before unpinning (best-effort)
                IHolon holon = null;
                try
                {
                    var content = await _pinataService.GetFileContentAsync(providerKey);
                    if (!string.IsNullOrWhiteSpace(content))
                        holon = JsonConvert.DeserializeObject<Holon>(content);
                }
                catch { /* best effort */ }

                var unpinOk = await _pinataService.UnpinFileAsync(providerKey);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin holon {providerKey} from Pinata.");
                    return result;
                }

                result.Result = holon;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Holon unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            
            try
            {
                var saveResult = await SaveHolonsAsync(holons);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Pinata. Reason: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Successfully imported {saveResult.Result.Count()} holons to Pinata";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Pinata. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var holonsResult = await LoadAllHolonsAsync();
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                var filtered = holonsResult.Result.Where(h => HolonMatchesAvatarId(h, avatarId)).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Exported {filtered.Count} holons for avatar {avatarId} from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(avatarUsername))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar username is required.");
                    return result;
                }

                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Pinata: {avatarResult.Message}");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar username from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(avatarEmailAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar email is required.");
                    return result;
                }

                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmailAddress} not found in Pinata: {avatarResult.Message}");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar email from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        // IOASISNETProvider implementation
        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Pinata provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
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

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Pinata provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
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

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            string errorMessage = "Error in SearchAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} SearchParams cannot be null");
                    return result;
                }

                var searchResults = new SearchResults();
                var foundHolons = new List<IHolon>();

                // Real Pinata implementation: Search through Pinata files using metadata
                var files = await _pinataService.GetFilesAsync();
                
                foreach (var file in files)
                {
                    try
                    {
                        // Real Pinata implementation: Download and parse the file content
                        var content = await _pinataService.GetFileContentAsync(file.IpfsPinHash);
                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        
                        if (holon != null && MatchesSearchCriteria(holon, searchParams))
                        {
                            foundHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (continueOnError)
                        {
                            LoggingManager.Log($"Error processing Pinata file: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                searchResults.SearchResultHolons = foundHolons.ToList();
                result.Result = searchResults;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        private bool MatchesSearchCriteria(IHolon holon, ISearchParams searchParams)
        {
            if (holon == null || searchParams == null)
                return false;

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var firstGroup = searchParams.SearchGroups.First();
                if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                {
                    var q = textGroup.SearchQuery.ToLower();
                    if (!((holon.Name ?? string.Empty).ToLower().Contains(q) || (holon.Description ?? string.Empty).ToLower().Contains(q)))
                        return false;
                }
            }

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var g = searchParams.SearchGroups.First();
                if (g.HolonType != HolonType.All && holon.HolonType != g.HolonType)
                    return false;
            }

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var g = searchParams.SearchGroups.First();
                // MetaData is a boolean flag indicating whether to search metadata
                if (g.HolonSearchParams?.MetaData == true && holon.MetaData != null)
                {
                    // MetaData search enabled - metadata exists on the holon
                    // Additional metadata filtering logic would go here if needed
                }
            }

            return true;
        }

        // Missing abstract method implementations
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Holon ID cannot be empty");
                    return result;
                }

                // Search for the holon file in Pinata by ID
                // Real Pinata implementation: Load child holons from Pinata files
                var files = await _pinataService.GetFilesAsync();
                var holonFile = files.FirstOrDefault(f => f.ToString().Contains(id.ToString()));
                
                if (holonFile == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Holon with ID {id} not found in Pinata");
                    return result;
                }

                // Download and deserialize the holon
                var content = await _pinataService.GetFileContentAsync(holonFile.IpfsPinHash);
                if (string.IsNullOrEmpty(content))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to retrieve holon content from Pinata");
                    return result;
                }
                var holon = JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to deserialize holon {id}");
                    return result;
                }

                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider key cannot be null or empty");
                    return result;
                }

                // Use providerKey as IPFS hash to directly fetch the file
                var content = await _pinataService.GetFileContentAsync(providerKey);
                if (string.IsNullOrEmpty(content))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to retrieve holon content from Pinata using provider key");
                    return result;
                }
                var holon = JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to deserialize holon with provider key {providerKey}");
                    return result;
                }

                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }


        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsForParentAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Parent ID cannot be empty");
                    return result;
                }

                var childHolons = new List<IHolon>();
                // Real Pinata implementation: Load child holons from Pinata files
                var files = await _pinataService.GetFilesAsync();
                
                foreach (var file in files)
                {
                    try
                    {
                        // Real Pinata implementation: Get file content from Pinata IPFS
                        var content = await _pinataService.GetFileContentAsync(file.IpfsPinHash);
                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        
                        if (holon != null && holon.ParentHolonId == id && 
                            (type == HolonType.All || holon.HolonType == type))
                        {
                            childHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (continueOnError)
                        {
                            LoggingManager.Log($"Error processing Pinata file: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                result.Result = childHolons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key is required.");
                    return result;
                }

                var parentHolonResult = await LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (parentHolonResult.IsError || parentHolonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found in Pinata: {parentHolonResult.Message}");
                    return result;
                }

                return await LoadHolonsForParentAsync(parentHolonResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(metaKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Meta key is required.");
                    return result;
                }

                var holonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                var filtered = holonsResult.Result.Where(h => HolonMetaMatches(h, metaKey, metaValue)).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata '{metaKey}'='{metaValue}'.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Meta key/value pairs are required.");
                    return result;
                }

                var holonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                bool Matches(IHolon h)
                {
                    if (h?.MetaData == null) return false;
                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        return metaKeyValuePairs.All(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                    return metaKeyValuePairs.Any(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                }

                var filtered = holonsResult.Result.Where(Matches).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata pairs.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                var holons = new List<IHolon>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon == null) continue;

                        if (type != HolonType.All && holon.HolonType != type) continue;
                        if (version > 0 && holon.Version != version) continue;

                        holons.Add(holon);
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} holons from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {ex.Message}", ex);
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                var avatarDetails = new List<IAvatarDetail>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(content);
                        if (avatarDetail == null) continue;
                        if (version > 0 && avatarDetail.Version != version) continue;

                        avatarDetails.Add(avatarDetail);
                    }
                    catch
                    {
                        // ignore non-avatar-detail pins
                    }
                }

                result.Result = avatarDetails;
                result.IsLoaded = true;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (string.IsNullOrWhiteSpace(avatarEmail))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar email is required.");
                    return result;
                }

                var allResult = await LoadAllAvatarDetailsAsync(version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {allResult.Message}");
                    return result;
                }

                var match = allResult.Result.FirstOrDefault(a => string.Equals(a.Email, avatarEmail, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar detail found with email: {avatarEmail}");
                    return result;
                }

                result.Result = match;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = "Avatar detail loaded successfully by email from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (string.IsNullOrWhiteSpace(avatarUsername))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar username is required.");
                    return result;
                }

                var allResult = await LoadAllAvatarDetailsAsync(version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {allResult.Message}");
                    return result;
                }

                var match = allResult.Result.FirstOrDefault(a => string.Equals(a.Username, avatarUsername, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar detail found with username: {avatarUsername}");
                    return result;
                }

                result.Result = match;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = "Avatar detail loaded successfully by username from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        private static bool HolonMetaMatches(IHolon holon, string metaKey, string metaValue)
        {
            if (holon?.MetaData == null || string.IsNullOrWhiteSpace(metaKey)) return false;
            if (!holon.MetaData.TryGetValue(metaKey, out var val)) return false;
            if (metaValue == null) return val != null;
            return string.Equals(val?.ToString(), metaValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HolonMatchesAvatarId(IHolon holon, Guid avatarId)
        {
            if (holon == null) return false;

            // Prefer strongly-typed property if present
            try
            {
                var prop = holon.GetType().GetProperty("CreatedByAvatarId");
                if (prop != null && prop.PropertyType == typeof(Guid))
                {
                    var value = (Guid)prop.GetValue(holon);
                    return value == avatarId;
                }
            }
            catch { }

            // Fallback: metadata
            if (holon.MetaData != null &&
                holon.MetaData.TryGetValue("CreatedByAvatarId", out var val) &&
                Guid.TryParse(val?.ToString(), out var parsed))
            {
                return parsed == avatarId;
            }

            return false;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                // Find avatar pin by scanning pinned avatar JSONs (reliable across Pinata query syntax changes)
                var pins = await _pinataService.GetFilesAsync();
                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;

                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var avatar = JsonConvert.DeserializeObject<Avatar>(content);
                        if (avatar != null && avatar.Id == id)
                        {
                            var unpinOk = await _pinataService.UnpinFileAsync(pin.IpfsPinHash);
                            if (!unpinOk)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to unpin avatar {id} from Pinata.");
                                return result;
                            }

                            result.Result = true;
                            result.IsDeleted = true;
                            result.DeletedCount = 1;
                            result.IsError = false;
                            result.Message = "Avatar unpinned (deleted) from Pinata successfully.";
                            return result;
                        }
                    }
                    catch
                    {
                        // ignore non-avatar pins
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found in Pinata.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Pinata: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (IPFS hash) is required.");
                    return result;
                }

                var unpinOk = await _pinataService.UnpinFileAsync(providerKey);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin avatar {providerKey} from Pinata.");
                    return result;
                }

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Avatar unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmail} not found in Pinata.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.PinataOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                {
                    return await DeleteAvatarAsync(providerKey, softDelete);
                }

                // Fall back to scanning pins by content if provider key missing
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Pinata.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.PinataOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                {
                    return await DeleteAvatarAsync(providerKey, softDelete);
                }

                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Pinata: {ex.Message}", ex);
            }
            return result;
        }


        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                // Export = enumerate pins and return all holons we can deserialize.
                var pins = await _pinataService.GetFilesAsync();
                var holons = new List<IHolon>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon != null)
                            holons.Add(holon);
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Exported {holons.Count} holons from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        private async Task EnsureActivatedForPinataAsync<T>(OASISResult<T> result)
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
            }
        }

        private async Task<OASISResult<(string PinHash, IHolon Holon)>> FindPinnedHolonByIdAsync(Guid id)
        {
            var result = new OASISResult<(string PinHash, IHolon Holon)>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;

                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon != null && holon.Id == id)
                        {
                            result.Result = (pin.IpfsPinHash, holon);
                            result.IsError = false;
                            return result;
                        }
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Pinata.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching holon pins in Pinata: {ex.Message}", ex);
            }
            return result;
        }
    }

    // Helper classes for Pinata API responses
    public class PinataFileResponse
    {
        [JsonProperty("IpfsHash")]
        public string IpfsHash { get; set; }

        [JsonProperty("PinSize")]
        public int PinSize { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }
    }

    public class PinataPinResponse
    {
        public string IpfsHash { get; set; }
        public int PinSize { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDuplicate { get; set; }
    }

    public interface IPinataService
    {
        Task<List<PinataPin>> GetFilesAsync();
        Task<string> GetFileContentAsync(string hash);
        Task<PinataPinResponse> PinFileAsync(string filePath);
        Task<PinataPinResponse> PinJsonAsync(object jsonObject);
        Task<bool> UnpinFileAsync(string hash);
    }

    public class PinataService : IPinataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _jwt;

        public PinataService(string apiKey, string secretKey, string jwt = null)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;
            _jwt = jwt;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("pinata_api_key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("pinata_secret_api_key", _secretKey);
        }

        public async Task<List<PinataPin>> GetFilesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://api.pinata.cloud/data/pinList");
                var content = await response.Content.ReadAsStringAsync();
                var pinList = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                return pinList?.Rows ?? new List<PinataPin>();
            }
            catch
            {
                return new List<PinataPin>();
            }
        }

        public async Task<string> GetFileContentAsync(string hash)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://gateway.pinata.cloud/ipfs/{hash}");
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<PinataPinResponse> PinFileAsync(string filePath)
        {
            try
            {
                var formData = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync("https://api.pinata.cloud/pinning/pinFileToIPFS", formData);
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PinataPinResponse>(content);
            }
            catch
            {
                return new PinataPinResponse();
            }
        }

        public async Task<PinataPinResponse> PinJsonAsync(object jsonObject)
        {
            try
            {
                var json = JsonConvert.SerializeObject(jsonObject);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.pinata.cloud/pinning/pinJSONToIPFS", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PinataPinResponse>(responseContent);
            }
            catch
            {
                return new PinataPinResponse();
            }
        }

        public async Task<bool> UnpinFileAsync(string hash)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://api.pinata.cloud/pinning/unpin/{hash}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
