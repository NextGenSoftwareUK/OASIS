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
            return ActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                ReleaseClients();
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(15);

                // Get configuration from OASIS DNA
                var pinataConfig = _OASISDNA?.OASIS?.StorageProviders?.PinataOASIS;
                if (pinataConfig != null && string.IsNullOrWhiteSpace(_jwt) && string.IsNullOrWhiteSpace(_apiKey))
                    ParseConnectionString(pinataConfig.ConnectionString);

                if (string.IsNullOrWhiteSpace(_jwt) &&
                    (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_secretKey)))
                    throw new InvalidOperationException("Pinata credentials are missing. Configure a JWT or API key and secret.");

                _httpClient.BaseAddress = new Uri(_apiUrl);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                SetAuthenticationHeaders();

                using var response = await _httpClient.GetAsync("/data/testAuthentication").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Pinata authentication failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");

                _pinataService = new PinataService(_apiKey, _secretKey, _jwt, _apiUrl, _gatewayUrl);

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "PinataOASIS Provider activated successfully";
            }
            catch (Exception e)
            {
                ReleaseClients();
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in ActivateProviderAsync Method. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                ReleaseClients();
                result.Result = true;
                result.Message = "PinataOASIS Provider deactivated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in PinataOASIS Provider in DeActivateProvider Method. Reason: {e}");
            }

            return result;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync() =>
            Task.FromResult(DeActivateProvider());

        private void ReleaseClients()
        {
            IsProviderActivated = false;
            _httpClient?.Dispose();
            _httpClient = null;
            _pinataService?.Dispose();
            _pinataService = null;
        }

        private void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;

            // Parse connection string format: "https://api.pinata.cloud?apiKey=xxx&secretKey=yyy&jwt=zzz&gateway=aaa"
            var uri = new Uri(connectionString);
            _apiUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            var query = ParseQueryString(uri.Query);
            
            _apiKey = query.ContainsKey("apiKey") ? query["apiKey"] : null;
            _secretKey = query.ContainsKey("secretKey") ? query["secretKey"] : null;
            _jwt = query.ContainsKey("jwt") ? query["jwt"] : null;
            _gatewayUrl = query.ContainsKey("gateway") ? query["gateway"].TrimEnd('/') : "https://gateway.pinata.cloud";
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

                    using var response = await _httpClient.PostAsync("/pinning/pinFileToIPFS", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var pinataResponse = JsonConvert.DeserializeObject<PinataFileResponse>(responseContent);
                        result.Result = RequireAcknowledgedCid(pinataResponse?.IpfsHash);
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
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync("/pinning/pinJSONToIPFS", content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var pin = JsonConvert.DeserializeObject<PinataFileResponse>(responseContent);
                result.Result = RequireAcknowledgedCid(pin?.IpfsHash);
                result.Message = "JSON uploaded to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading JSON to Pinata. Reason: {e.Message}", e);
            }

            return result;
        }

        internal static string RequireAcknowledgedCid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new System.IO.InvalidDataException("Pinata did not acknowledge a pinned content ID.");

            _ = Ipfs.Cid.Decode(value);
            return value;
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

                result.Result = await _pinataService.GetFileBytesAsync(ipfsHash).ConfigureAwait(false);
                result.Message = "File downloaded from Pinata successfully";
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

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) =>
            LoadNamedRecordAsync<IAvatar, Avatar>(id, "Avatar");

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) =>
            LoadAvatarBySelectorAsync(avatarEmail, avatar => avatar.Email, version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            LoadAvatarBySelectorAsync(avatarUsername, avatar => avatar.Username, version);

        private async Task<OASISResult<IAvatar>> LoadAvatarBySelectorAsync(
            string value, Func<IAvatar, string> select, int version)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                var all = await LoadAllAvatarsAsync(version).ConfigureAwait(false);
                if (all.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>, IAvatar>(all);

                var matches = all.Result.Where(avatar => string.Equals(select(avatar), value, StringComparison.OrdinalIgnoreCase))
                    .Take(2).ToArray();
                if (matches.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Pinata avatar was not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }
                if (matches.Length > 1)
                    throw new StorageIdentityAmbiguousException();

                result.Result = matches[0];
                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error loading Pinata avatar.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
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
                await EnsureActivatedForPinataAsync(result).ConfigureAwait(false);
                if (result.IsError)
                    return result;

                var avatars = new List<IAvatar>();
                var pins = await _pinataService.GetFilesAsync().ConfigureAwait(false);
                foreach (var pin in pins)
                {
                    var avatar = await ReadNamedPinAsync<Avatar>(pin, "Avatar_").ConfigureAwait(false);
                    if (avatar != null && (version <= 0 || avatar.Version == version))
                        avatars.Add(avatar);
                }

                result.Result = avatars;
                result.IsLoaded = true;
                result.LoadedCount = avatars.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error listing Pinata avatars.", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            LoadNamedRecordAsync<IAvatarDetail, AvatarDetail>(id, "AvatarDetail");

        private async Task<OASISResult<TRecord>> LoadNamedRecordAsync<TRecord, TStored>(Guid id, string recordType)
            where TRecord : class, IHolonBase
            where TStored : class, TRecord
        {
            var result = new OASISResult<TRecord>();
            try
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("A nonempty record ID is required.", nameof(id));

                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync().ConfigureAwait(false);
                    if (activation.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, TRecord>(activation);
                }

                var files = await _pinataService.GetFilesAsync().ConfigureAwait(false);
                var matches = files.Where(file =>
                    file.Metadata != null && file.Metadata.TryGetValue("name", out var name) &&
                    string.Equals(name as string, $"{recordType}_{id:D}", StringComparison.Ordinal))
                    .Take(2).ToArray();
                if (matches.Length == 0)
                    return result;
                if (matches.Length > 1)
                    throw new StorageIdentityAmbiguousException();

                var pin = matches[0];
                var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash).ConfigureAwait(false);
                var record = JsonConvert.DeserializeObject<TStored>(content);
                if (record == null || record.Id != id)
                    throw new System.IO.InvalidDataException("Pinata content does not match the requested record ID.");

                record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = pin.IpfsPinHash;
                result.Result = record;
                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error loading Pinata record.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
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
                ArgumentNullException.ThrowIfNull(avatar);
                var id = avatar.Id == Guid.Empty ? Guid.NewGuid() : avatar.Id;
                var payload = Newtonsoft.Json.Linq.JObject.FromObject(avatar);
                payload[nameof(IAvatar.Id)] = id;
                if (payload.Property(nameof(IAvatar.AvatarId)) != null)
                    payload[nameof(IAvatar.AvatarId)] = id;
                var uploadResult = await UploadJsonToPinataAsync(payload, $"Avatar_{id:D}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Pinata. Reason: {uploadResult.Message}", uploadResult.Exception);
                    return result;
                }

                // Store the IPFS hash as the provider key
                avatar.Id = id;
                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
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
