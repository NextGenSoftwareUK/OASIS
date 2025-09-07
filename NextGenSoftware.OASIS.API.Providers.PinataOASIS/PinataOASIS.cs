using System;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS
{
    public class PinataOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _secretKey;
        private string _jwt;
        private string _gatewayUrl;
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;

        public PinataOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
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
                    OASISErrorHandling.HandleError(ref result, "PinataOASIS Provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "PinataOASIS Provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "PinataOASIS Provider is not activated");
                    return result;
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
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByProviderKey not implemented for PinataOASIS - use LoadAvatarByProviderKeyAsync");
            return result;
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
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatar not implemented for PinataOASIS - use LoadAvatarAsync");
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarAsync not implemented for PinataOASIS - Pinata is primarily for file storage, not avatar management");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByEmail not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByEmailAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByUsername not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByUsernameAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatars not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarsAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetail not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "SaveAvatar not implemented for PinataOASIS - use SaveAvatarAsync");
            return result;
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
                avatar.ProviderKey[ProviderType.PinataOASIS] = uploadResult.Result;
                
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
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "SaveAvatarDetail not implemented for PinataOASIS - use SaveAvatarDetailAsync");
            return result;
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
                avatarDetail.ProviderKey[ProviderType.PinataOASIS] = uploadResult.Result;
                
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
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "SaveHolon not implemented for PinataOASIS - use SaveHolonAsync");
            return result;
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
                holon.ProviderKey[ProviderType.PinataOASIS] = uploadResult.Result;
                
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
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "SaveHolons not implemented for PinataOASIS - use SaveHolonsAsync");
            return result;
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

        public override OASISResult<IHolon> LoadHolon(Guid id, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolon not implemented for PinataOASIS - use LoadHolonAsync");
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonAsync not implemented for PinataOASIS - Pinata requires IPFS hash to load data");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolon not implemented for PinataOASIS - use LoadHolonAsync");
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            
            try
            {
                // Download holon data from Pinata using providerKey as IPFS hash
                var downloadResult = await DownloadFileFromPinataAsync(providerKey);
                
                if (downloadResult.IsError || downloadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Pinata. Reason: {downloadResult.Message}");
                    return result;
                }

                // Deserialize holon data
                var holonJson = Encoding.UTF8.GetString(downloadResult.Result);
                var holon = JsonConvert.DeserializeObject<Holon>(holonJson);
                
                result.Result = holon;
                result.Message = "Holon loaded from Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Pinata. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, string propertyName, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, string propertyName, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, string propertyName, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, string propertyName, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon not implemented for PinataOASIS - Pinata does not support deletion");
            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolonAsync not implemented for PinataOASIS - Pinata does not support deletion");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon not implemented for PinataOASIS - Pinata does not support deletion");
            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolonAsync not implemented for PinataOASIS - Pinata does not support deletion");
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Import not implemented for PinataOASIS - use ImportAsync");
            return result;
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
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarById not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByIdAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByUsername not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByUsernameAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByEmail not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByEmailAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override OASISResult<IEnumerable<ISearchResult>> Search(ISearchParams searchParams)
        {
            OASISResult<IEnumerable<ISearchResult>> result = new OASISResult<IEnumerable<ISearchResult>>();
            OASISErrorHandling.HandleError(ref result, "Search not implemented for PinataOASIS - Pinata is primarily for file storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<ISearchResult>>> SearchAsync(ISearchParams searchParams)
        {
            OASISResult<IEnumerable<ISearchResult>> result = new OASISResult<IEnumerable<ISearchResult>>();
            OASISErrorHandling.HandleError(ref result, "SearchAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
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
}
