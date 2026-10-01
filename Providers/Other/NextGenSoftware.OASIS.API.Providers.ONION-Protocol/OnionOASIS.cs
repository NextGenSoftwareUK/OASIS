using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol
{
    /// <summary>
    /// OnionOASIS Provider — routes all OASIS storage operations through the Tor network
    /// (SOCKS5 proxy) to an onion-service-hosted OASIS backend, preserving privacy at the transport layer.
    /// </summary>
    public class OnionOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly string _torProxyHost;
        private readonly int _torProxyPort;
        private readonly string _onionApiUrl;
        private HttpClient _httpClient;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public OnionOASIS(
            string torProxyHost = "127.0.0.1",
            int torProxyPort = 9050,
            string onionApiUrl = "")
        {
            this.ProviderName = "OnionOASIS";
            this.ProviderDescription = "Tor-routed OASIS storage provider — routes all calls through Tor SOCKS5 proxy to an onion-service OASIS backend";
            _torProxyHost = torProxyHost;
            _torProxyPort = torProxyPort;
            _onionApiUrl = onionApiUrl;
        }

        #region Activation

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (IsProviderActivated)
                {
                    result.Result = true;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(_onionApiUrl))
                {
                    OASISErrorHandling.HandleError(ref result, "OnionOASIS: onionApiUrl is required.");
                    return result;
                }

                var proxy = new WebProxy($"socks5://{_torProxyHost}:{_torProxyPort}");
                var handler = new HttpClientHandler { Proxy = proxy, UseProxy = true };
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(_onionApiUrl.TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(60)
                };

                var response = await _httpClient.GetAsync("api/status");

                if (response.IsSuccessStatusCode)
                {
                    IsProviderActivated = true;
                    result.Result = true;
                    result.Message = "OnionOASIS activated — Tor connectivity confirmed.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"OnionOASIS /api/status returned {(int)response.StatusCode} {response.ReasonPhrase}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error activating OnionOASIS: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            return await Task.FromResult(DeActivateProvider());
        }

        #endregion

        #region Helpers

        private async Task<OASISResult<T>> GetAsync<T>(string path)
        {
            var result = new OASISResult<T>();
            try
            {
                var response = await _httpClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    result.Result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"OnionOASIS GET {path} returned {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"OnionOASIS GET {path} error: {ex.Message}", ex); }
            return result;
        }

        private async Task<OASISResult<T>> PostAsync<T>(string path, object body)
        {
            var result = new OASISResult<T>();
            try
            {
                var json = JsonSerializer.Serialize(body, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(path, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    result.Result = JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"OnionOASIS POST {path} returned {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"OnionOASIS POST {path} error: {ex.Message}", ex); }
            return result;
        }

        private async Task<OASISResult<bool>> DeleteAsync(string path)
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _httpClient.DeleteAsync(path);
                if (response.IsSuccessStatusCode) { result.Result = true; result.IsError = false; }
                else OASISErrorHandling.HandleError(ref result,
                    $"OnionOASIS DELETE {path} returned {(int)response.StatusCode}.");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"OnionOASIS DELETE {path} error: {ex.Message}", ex); }
            return result;
        }

        #endregion

        #region Avatar operations

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = await GetAsync<Avatar>($"api/avatars/{id}");
            return new OASISResult<IAvatar> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = await PostAsync<Avatar>("api/avatars", avatar);
            return new OASISResult<IAvatar> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var r = await GetAsync<List<Avatar>>("api/avatars");
            return new OASISResult<IEnumerable<IAvatar>>
            {
                Result = r.Result?.Cast<IAvatar>().ToList(),
                IsError = r.IsError,
                Message = r.Message
            };
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => await DeleteAsync($"api/avatars/{id}?softDelete={softDelete}");

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
            => LoadAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var r = await GetAsync<Avatar>($"api/avatars/by-email/{Uri.EscapeDataString(email)}");
            return new OASISResult<IAvatar> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var r = await GetAsync<Avatar>($"api/avatars/by-username/{Uri.EscapeDataString(username)}");
            return new OASISResult<IAvatar> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var r = await GetAsync<Avatar>($"api/avatars/by-providerkey/{Uri.EscapeDataString(providerKey)}");
            return new OASISResult<IAvatar> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        #endregion

        #region AvatarDetail operations

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." });

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." });

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => new OASISResult<IEnumerable<IAvatarDetail>> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." });

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "AvatarDetail operations not yet implemented in OnionOASIS." });

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Delete by providerKey not yet implemented in OnionOASIS." };

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Delete by providerKey not yet implemented in OnionOASIS." });

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Not yet implemented in OnionOASIS." };

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Not yet implemented in OnionOASIS." });

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Not yet implemented in OnionOASIS." };

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Not yet implemented in OnionOASIS." });

        #endregion

        #region Holon operations

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = await GetAsync<Holon>($"api/holons/{id}");
            return new OASISResult<IHolon> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IHolon> { IsError = true, Message = "Load holon by providerKey not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Load holon by providerKey not yet implemented in OnionOASIS." });

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = await PostAsync<Holon>("api/holons", holon);
            return new OASISResult<IHolon> { Result = r.Result, IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = await GetAsync<List<Holon>>($"api/holons?type={type}");
            return new OASISResult<IEnumerable<IHolon>>
            {
                Result = r.Result?.Cast<IHolon>().ToList(),
                IsError = r.IsError,
                Message = r.Message
            };
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = await DeleteAsync($"api/holons/{id}?softDelete=true");
            return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message };
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => new OASISResult<IHolon> { IsError = true, Message = "Delete holon by providerKey not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Delete holon by providerKey not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsForParent not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsForParent not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsForParent not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsForParent not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "SaveHolons batch not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "SaveHolons batch not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsByMetaData not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsByMetaData not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsByMetaData not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "LoadHolonsByMetaData not yet implemented in OnionOASIS." });

        #endregion

        #region Search / Import / Export

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<ISearchResults> { IsError = true, Message = "Search not yet implemented in OnionOASIS." };

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(new OASISResult<ISearchResults> { IsError = true, Message = "Search not yet implemented in OnionOASIS." });

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => new OASISResult<bool> { IsError = true, Message = "Import not yet implemented in OnionOASIS." };

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Import not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Export not yet implemented in OnionOASIS." });

        #endregion
    }
}
