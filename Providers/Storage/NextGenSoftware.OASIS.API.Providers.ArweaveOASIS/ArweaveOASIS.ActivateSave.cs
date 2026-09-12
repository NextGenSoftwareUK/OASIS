using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
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

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    public partial class ArweaveOASIS
    {
        private void Init()
        {
            this.ProviderName = "ArweaveOASIS";
            this.ProviderDescription = "Arweave Permanent Storage Provider for OASIS";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArweaveOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var arweaveConfig = _OASISDNA?.OASIS?.StorageProviders?.ArweaveOASIS;
                if (arweaveConfig != null)
                    ParseConnectionString(arweaveConfig.ConnectionString);

                _gatewayUrl ??= "https://arweave.net";
                _httpClient.BaseAddress = new Uri(_gatewayUrl);

                _arweaveService = new ArweaveService(_walletJson, _gatewayUrl);

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "ArweaveOASIS Provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in ArweaveOASIS Provider in ActivateProvider. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var arweaveConfig = _OASISDNA?.OASIS?.StorageProviders?.ArweaveOASIS;
                if (arweaveConfig != null)
                    ParseConnectionString(arweaveConfig.ConnectionString);

                _gatewayUrl ??= "https://arweave.net";
                _httpClient.BaseAddress = new Uri(_gatewayUrl);

                _arweaveService = new ArweaveService(_walletJson, _gatewayUrl);

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "ArweaveOASIS Provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in ArweaveOASIS Provider in ActivateProviderAsync. Reason: {e}");
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
                result.Message = "ArweaveOASIS Provider deactivated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in ArweaveOASIS Provider in DeActivateProvider. Reason: {e}");
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
                result.Message = "ArweaveOASIS Provider deactivated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occurred in ArweaveOASIS Provider in DeActivateProviderAsync. Reason: {e}");
            }

            return result;
        }

        // Connection string format: "wallet=/path/to/wallet.json&gateway=https://arweave.net"
        // Or with embedded JSON:    "walletjson=<base64>&gateway=https://arweave.net"
        private void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;

            var parts = connectionString.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLowerInvariant();
                var value = Uri.UnescapeDataString(kv[1].Trim());

                switch (key)
                {
                    case "wallet":
                        _walletPath = value;
                        if (File.Exists(_walletPath))
                            _walletJson = File.ReadAllText(_walletPath);
                        break;
                    case "walletjson":
                        _walletJson = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                        break;
                    case "gateway":
                        _gatewayUrl = value;
                        break;
                }
            }
        }

        // Upload data to Arweave permanently; returns the transaction ID
        public async Task<OASISResult<string>> UploadDataToArweaveAsync(byte[] data, string contentType = "application/octet-stream", Dictionary<string, string> tags = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txId = await _arweaveService.PostTransactionAsync(data, contentType, tags);

                if (string.IsNullOrEmpty(txId))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to upload data to Arweave: empty transaction ID returned.");
                    return result;
                }

                result.Result = txId;
                result.Message = $"Data uploaded to Arweave permanently. TxId: {txId}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading data to Arweave. Reason: {e}");
            }

            return result;
        }

        // Upload JSON object to Arweave; returns transaction ID
        public async Task<OASISResult<string>> UploadJsonToArweaveAsync(object data, string name = null, Dictionary<string, string> extraTags = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);

                var tags = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "App-Name", "OASIS" },
                    { "App-Version", "2.0" },
                    { "Unix-Time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                };

                if (!string.IsNullOrEmpty(name))
                    tags["OASIS-Name"] = name;

                if (extraTags != null)
                    foreach (var kvp in extraTags)
                        tags[kvp.Key] = kvp.Value;

                var txId = await _arweaveService.PostTransactionAsync(bytes, "application/json", tags);

                if (string.IsNullOrEmpty(txId))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to upload JSON to Arweave: empty transaction ID returned.");
                    return result;
                }

                result.Result = txId;
                result.Message = $"JSON uploaded to Arweave permanently. TxId: {txId}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading JSON to Arweave. Reason: {e}");
            }

            return result;
        }

        // Download data from Arweave using transaction ID
        public async Task<OASISResult<byte[]>> DownloadDataFromArweaveAsync(string txId)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var data = await _arweaveService.GetTransactionDataAsync(txId);

                if (data == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to download data from Arweave for TxId: {txId}");
                    return result;
                }

                result.Result = data;
                result.Message = $"Data downloaded from Arweave successfully. TxId: {txId}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error downloading data from Arweave. Reason: {e}");
            }

            return result;
        }

        // Returns the permanent URL for an Arweave transaction
        public string GetTransactionUrl(string txId)
        {
            return $"{_gatewayUrl ?? "https://arweave.net"}/{txId}";
        }

        // Avatar load/save methods
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                var downloadResult = await DownloadDataFromArweaveAsync(providerKey);

                if (downloadResult.IsError || downloadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from Arweave. Reason: {downloadResult.Message}");
                    return result;
                }

                var avatar = JsonConvert.DeserializeObject<Avatar>(Encoding.UTF8.GetString(downloadResult.Result));
                result.Result = avatar;
                result.Message = "Avatar loaded from Arweave successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Arweave. Reason: {e}");
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
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Avatar" },
                    { "OASIS-Id", id.ToString() }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found in Arweave.");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                var avatar = JsonConvert.DeserializeObject<Avatar>(Encoding.UTF8.GetString(data));
                result.Result = avatar;
                result.Message = "Avatar loaded from Arweave successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Arweave. Reason: {e}");
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
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Avatar" },
                    { "OASIS-Email", avatarEmail }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar found with email: {avatarEmail}");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                var avatar = JsonConvert.DeserializeObject<Avatar>(Encoding.UTF8.GetString(data));
                result.Result = avatar;
                result.Message = "Avatar loaded from Arweave successfully by email";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Arweave. Reason: {e}");
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
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Avatar" },
                    { "OASIS-Username", avatarUsername }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar found with username: {avatarUsername}");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                var avatar = JsonConvert.DeserializeObject<Avatar>(Encoding.UTF8.GetString(data));
                result.Result = avatar;
                result.Message = "Avatar loaded from Arweave successfully by username";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Arweave. Reason: {e}");
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
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Avatar" }
                });

                var avatars = new List<IAvatar>();

                foreach (var txId in txIds ?? new List<string>())
                {
                    try
                    {
                        var data = await _arweaveService.GetTransactionDataAsync(txId);
                        if (data == null) continue;
                        var avatar = JsonConvert.DeserializeObject<Avatar>(Encoding.UTF8.GetString(data));
                        if (avatar != null)
                            avatars.Add(avatar);
                    }
                    catch { /* ignore non-avatar transactions */ }
                }

                result.Result = avatars;
                result.Message = $"Loaded {avatars.Count} avatars from Arweave";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Arweave. Reason: {e}");
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
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "AvatarDetail" },
                    { "OASIS-Id", id.ToString() }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar detail found with ID: {id}");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(Encoding.UTF8.GetString(data));
                result.Result = avatarDetail;
                result.Message = "Avatar detail loaded from Arweave successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Arweave. Reason: {e}");
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
                var tags = new Dictionary<string, string>
                {
                    { "OASIS-Type", "Avatar" },
                    { "OASIS-Id", avatar.Id.ToString() },
                    { "OASIS-Username", avatar.Username ?? "" },
                    { "OASIS-Email", avatar.Email ?? "" }
                };

                var uploadResult = await UploadJsonToArweaveAsync(avatar, $"Avatar_{avatar.Id}", tags);

                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Arweave. Reason: {uploadResult.Message}");
                    return result;
                }

                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = uploadResult.Result;
                result.Result = avatar;
                result.Message = $"Avatar saved to Arweave permanently. TxId: {uploadResult.Result}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Arweave. Reason: {e}");
            }

            return result;
        }

        private async Task EnsureActivatedAsync<T>(OASISResult<T> result)
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate ArweaveOASIS Provider: {activateResult.Message}");
            }
        }
    }
}
