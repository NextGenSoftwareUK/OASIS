using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.Runtime;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.API
{
    [Serializable]
    public class GatewayHealthSnapshot
    {
        public bool circuitOpen;
        public int consecutiveFailures;
        public string lastError;
        public bool lastResultFromCache;
        public bool authExpired;
        public long lastLatencyMs;
        public string lastSuccessUtc;
    }

    public sealed class Web4Web5GatewayClient : IDisposable
    {
        [Serializable]
        private class ApiCacheEnvelope
        {
            public string savedUtc;
            public string payload;
        }

        private const int MaxRetries = 3;
        private const int RequestTimeoutSeconds = 8;
        private const int CircuitFailureThreshold = 6;
        private const int CircuitCooldownSeconds = 20;
        private const float CacheTtlSeconds = 180f;
        private const string CachePrefix = "OASIS_OMNI_API_CACHE_";
        private const string CacheInventory = CachePrefix + "inventory";
        private const string CacheQuests = CachePrefix + "quests";
        private const string CacheNfts = CachePrefix + "nfts";
        private const string CacheAvatar = CachePrefix + "avatar";
        private const string CacheKarma = CachePrefix + "karma";
        private const string CacheSettings = CachePrefix + "settings";

        private readonly HttpClient _httpClient;
        private readonly string _web4Base;
        private readonly string _web5Base;
        private readonly string _avatarId;
        private int _consecutiveFailures;
        private DateTime _circuitOpenUntilUtc = DateTime.MinValue;
        private string _lastError = "none";
        private bool _lastResultFromCache;
        private bool _authExpired;
        private long _lastLatencyMs;
        private DateTime _lastSuccessUtc = DateTime.MinValue;

        public Web4Web5GatewayClient(string web4BaseUrl, string web5BaseUrl, string apiKey, string avatarId)
        {
            _web4Base = web4BaseUrl.TrimEnd('/');
            _web5Base = web5BaseUrl.TrimEnd('/');
            _avatarId = avatarId;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        public async Task<OASISResult<List<InventoryItem>>> GetSharedInventoryAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<List<InventoryItem>>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web5Base}/api/inventoryitems/user/{_avatarId}",
                $"{_web5Base}/api/inventoryitems/by-avatar/{_avatarId}",
                $"{_web4Base}/api/inventoryitems/user/{_avatarId}");

            if (fetch.IsError)
            {
                return TryLoadCache<List<InventoryItem>>(CacheInventory, "Inventory", fetch.Message);
            }

            var parsed = ParseInventory(fetch.Result);
            if (!parsed.IsError)
            {
                SaveCache(CacheInventory, parsed.Result);
            }

            return parsed;
        }

        public async Task<OASISResult<List<QuestItem>>> GetCrossGameQuestsAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<List<QuestItem>>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            // Try GET endpoints - load-all-for-avatar uses the authenticated user's avatarId from the token
            UnityEngine.Debug.Log($"[Web4Web5GatewayClient] Fetching quests for avatarId: {_avatarId}, hasToken: {_httpClient.DefaultRequestHeaders.Authorization != null}");
            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web5Base}/api/quests/load-all-for-avatar",
                $"{_web5Base}/api/quests/by-avatar/{_avatarId}",
                $"{_web5Base}/api/quests");

            if (fetch.IsError)
            {
                UnityEngine.Debug.LogError($"[Web4Web5GatewayClient] Failed to fetch quests: {fetch.Message}");
                return TryLoadCache<List<QuestItem>>(CacheQuests, "Quests", fetch.Message);
            }

            var parsed = ParseQuests(fetch.Result);
            if (!parsed.IsError)
            {
                SaveCache(CacheQuests, parsed.Result);
            }

            return parsed;
        }

        public async Task<OASISResult<List<NftAssetItem>>> GetCrossGameNftsAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<List<NftAssetItem>>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web5Base}/api/nfts/load-all-for-avatar",
                $"{_web5Base}/api/nfts",
                $"{_web4Base}/api/nft/load-all-nfts-for-avatar/{_avatarId}");

            if (fetch.IsError)
            {
                return TryLoadCache<List<NftAssetItem>>(CacheNfts, "NFT assets", fetch.Message);
            }

            try
            {
                var arr = ExtractArray(fetch.Result);
                var items = new List<NftAssetItem>();
                foreach (var nft in arr)
                {
                    var meta = nft["MetaData"] ?? nft["metaData"];
                    items.Add(new NftAssetItem
                    {
                        id = nft.Value<string>("Id") ?? nft.Value<string>("id"),
                        name = nft.Value<string>("Name") ?? nft.Value<string>("name"),
                        description = nft.Value<string>("Description") ?? nft.Value<string>("description"),
                        type = nft.Value<string>("Type") ?? nft.Value<string>("type") ?? "NFT",
                        source = meta?.Value<string>("GameSource") ?? "Omniverse"
                    });
                }

                SaveCache(CacheNfts, items);
                return OASISResult<List<NftAssetItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return OASISResult<List<NftAssetItem>>.Error($"NFT request parse failed: {ex.Message}");
            }
        }

        public async Task<OASISResult<AvatarProfileItem>> GetAvatarProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<AvatarProfileItem>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web4Base}/api/avatar/get-by-id/{_avatarId}",
                $"{_web4Base}/api/avatar/{_avatarId}",
                $"{_web5Base}/api/avatar/{_avatarId}",
                $"{_web5Base}/api/avatar/current");

            if (fetch.IsError)
            {
                return TryLoadCache<AvatarProfileItem>(CacheAvatar, "Avatar profile", fetch.Message);
            }

            try
            {
                var profile = ExtractObject(fetch.Result);
                var profileItem = new AvatarProfileItem
                {
                    id = profile.Value<string>("Id") ?? profile.Value<string>("id"),
                    username = profile.Value<string>("Username") ?? profile.Value<string>("username"),
                    email = profile.Value<string>("Email") ?? profile.Value<string>("email"),
                    firstName = profile.Value<string>("FirstName") ?? profile.Value<string>("firstName"),
                    lastName = profile.Value<string>("LastName") ?? profile.Value<string>("lastName"),
                    title = profile.Value<string>("Title") ?? profile.Value<string>("title")
                };

                SaveCache(CacheAvatar, profileItem);
                return OASISResult<AvatarProfileItem>.Success(profileItem);
            }
            catch (Exception ex)
            {
                return OASISResult<AvatarProfileItem>.Error($"Avatar profile parse failed: {ex.Message}");
            }
        }

        public async Task<OASISResult<KarmaOverview>> GetKarmaOverviewAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<KarmaOverview>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var totalFetch = await GetJsonFromCandidatesAsync($"{_web4Base}/api/karma/get-karma-for-avatar/{_avatarId}");
            var historyFetch = await GetJsonFromCandidatesAsync(
                $"{_web4Base}/api/karma/get-karma-history/{_avatarId}?limit=100&offset=0",
                $"{_web4Base}/api/karma/get-karma-akashic-records-for-avatar/{_avatarId}");

            if (totalFetch.IsError && historyFetch.IsError)
            {
                return TryLoadCache<KarmaOverview>(CacheKarma, "Karma", $"Karma request failed. Total: {totalFetch.Message}. History: {historyFetch.Message}");
            }

            var overview = new KarmaOverview();
            if (!totalFetch.IsError)
            {
                var totalToken = ExtractDataToken(totalFetch.Result);
                if (totalToken.Type == JTokenType.Float || totalToken.Type == JTokenType.Integer || totalToken.Type == JTokenType.String)
                {
                    float.TryParse(totalToken.ToString(), out overview.totalKarma);
                }
                else if (totalToken is JObject totalObj)
                {
                    float.TryParse((totalObj.Value<string>("TotalKarma") ?? totalObj.Value<string>("totalKarma") ?? totalObj.Value<string>("Karma")), out overview.totalKarma);
                }
            }

            if (!historyFetch.IsError)
            {
                var arr = ExtractArray(historyFetch.Result);
                foreach (var row in arr)
                {
                    overview.history.Add(new KarmaEntry
                    {
                        id = row.Value<string>("Id") ?? row.Value<string>("id"),
                        source = row.Value<string>("KarmaSourceTitle") ?? row.Value<string>("Source") ?? row.Value<string>("source"),
                        reason = row.Value<string>("KarmaSourceDesc") ?? row.Value<string>("Reason") ?? row.Value<string>("reason"),
                        amount = row is JObject rowObj ? ParseFloat(rowObj, "Karma", "Amount", "amount", "value") : 0f,
                        karmaType = row.Value<string>("KarmaType") ?? row.Value<string>("Type") ?? row.Value<string>("type"),
                        createdDate = row.Value<string>("CreatedDate") ?? row.Value<string>("CreatedOn") ?? row.Value<string>("createdDate")
                    });
                }
            }

            SaveCache(CacheKarma, overview);
            return OASISResult<KarmaOverview>.Success(overview);
        }

        public async Task<OASISResult<OmniverseGlobalSettings>> GetGlobalPreferencesAsync()
        {
            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web4Base}/api/settings/user/preferences",
                $"{_web4Base}/api/settings/user");

            if (fetch.IsError)
            {
                return TryLoadCache<OmniverseGlobalSettings>(CacheSettings, "Global preferences", fetch.Message);
            }

            try
            {
                var token = ExtractDataToken(fetch.Result);
                if (token is JObject obj && obj["preferences"] is JObject pref)
                {
                    token = pref;
                }

                var json = token.ToString(Formatting.None);
                var settings = JsonConvert.DeserializeObject<OmniverseGlobalSettings>(json);
                if (settings == null)
                {
                    return OASISResult<OmniverseGlobalSettings>.Error("Global preferences could not be deserialized.");
                }

                SaveCache(CacheSettings, settings);
                return OASISResult<OmniverseGlobalSettings>.Success(settings);
            }
            catch (Exception ex)
            {
                return OASISResult<OmniverseGlobalSettings>.Error($"Global preferences parse failed: {ex.Message}");
            }
        }

        public async Task<OASISResult<bool>> SaveGlobalPreferencesAsync(OmniverseGlobalSettings settings)
        {
            var body = new JObject
            {
                ["preferences"] = JObject.FromObject(settings),
                ["key"] = "user_preferences"
            };

            var save = await PutJsonToCandidatesAsync(body.ToString(Formatting.None),
                $"{_web4Base}/api/settings/user/preferences",
                $"{_web4Base}/api/settings/user");

            if (!save.IsError)
            {
                SaveCache(CacheSettings, settings);
            }

            return save;
        }

        private async Task<OASISResult<string>> GetJsonFromCandidatesAsync(params string[] uris)
        {
            var errors = new List<string>();
            if (IsCircuitOpen())
            {
                return OASISResult<string>.Error($"API circuit temporarily open ({Mathf.CeilToInt((float)(_circuitOpenUntilUtc - DateTime.UtcNow).TotalSeconds)}s cooldown). Last error: {_lastError}");
            }

            foreach (var uri in uris)
            {
                if (string.IsNullOrWhiteSpace(uri))
                {
                    continue;
                }

                try
                {
                    var result = await ExecuteGetWithResilienceAsync(uri);
                    if (!result.IsError)
                    {
                        return result;
                    }

                    errors.Add($"{uri} -> {result.Message}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{uri} -> {ex.Message}");
                }
            }

            return OASISResult<string>.Error(string.Join(" | ", errors));
        }

        private async Task<OASISResult<bool>> PutJsonToCandidatesAsync(string jsonBody, params string[] uris)
        {
            var errors = new List<string>();
            if (IsCircuitOpen())
            {
                return OASISResult<bool>.Error($"API circuit temporarily open ({Mathf.CeilToInt((float)(_circuitOpenUntilUtc - DateTime.UtcNow).TotalSeconds)}s cooldown). Last error: {_lastError}");
            }

            foreach (var uri in uris)
            {
                if (string.IsNullOrWhiteSpace(uri))
                {
                    continue;
                }

                try
                {
                    var response = await ExecuteSendWithResilienceAsync(uri, HttpMethod.Put, jsonBody);
                    if (response.IsSuccessStatusCode)
                    {
                        RegisterRequestSuccess(0);
                        return OASISResult<bool>.Success(true);
                    }

                    if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        var postResponse = await ExecuteSendWithResilienceAsync(uri, HttpMethod.Post, jsonBody);
                        if (postResponse.IsSuccessStatusCode)
                        {
                            RegisterRequestSuccess(0);
                            return OASISResult<bool>.Success(true);
                        }

                        var mapped = MapStatusToFriendlyMessage(postResponse.StatusCode, uri, postResponse.ReasonPhrase);
                        RegisterRequestFailure(mapped, postResponse.StatusCode);
                        errors.Add($"{uri} (POST fallback) -> {mapped}");
                    }
                    else
                    {
                        var mapped = MapStatusToFriendlyMessage(response.StatusCode, uri, response.ReasonPhrase);
                        RegisterRequestFailure(mapped, response.StatusCode);
                        errors.Add($"{uri} -> {mapped}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{uri} -> {ex.Message}");
                }
            }

            return OASISResult<bool>.Error(string.Join(" | ", errors));
        }

        [Serializable]
        public class AuthenticateRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        public class AuthenticateResponse
        {
            public string id;
            public string username;
            public string email;
            public string jwtToken;
            public string refreshToken;
            public bool isBeamedIn;
            public string lastBeamedIn;
        }

        public async Task<OASISResult<AuthenticateResponse>> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return OASISResult<AuthenticateResponse>.Error("Username is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return OASISResult<AuthenticateResponse>.Error("Password is required.");
            }

            var request = new AuthenticateRequest
            {
                username = username,
                password = password
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var uri = $"{_web4Base}/api/avatar/authenticate";
            UnityEngine.Debug.Log($"[Web4Web5GatewayClient] Authenticating to URI: {uri}");
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await _httpClient.PostAsync(uri, content);
                sw.Stop();
                _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);

                var responseBody = await response.Content.ReadAsStringAsync();
                UnityEngine.Debug.Log($"[Web4Web5GatewayClient] Authentication response status: {response.StatusCode}, body length: {responseBody?.Length ?? 0}");

                if (response.IsSuccessStatusCode)
                {
                    RegisterRequestSuccess(_lastLatencyMs);
                    
                    try
                    {
                        var jsonObj = JObject.Parse(responseBody);
                        UnityEngine.Debug.Log($"[Web4Web5GatewayClient] Full response: {responseBody}");
                        
                        var result = jsonObj["result"];
                        if (result == null)
                        {
                            return OASISResult<AuthenticateResponse>.Error("Authentication response missing 'result' field.");
                        }

                        // The API can return nested result structure: result.result
                        JToken avatarObj = result["result"] ?? result;
                        
                        // Try multiple response structures
                        JToken data = result["data"] ?? result["Data"];
                        JToken user = data?["user"] ?? data?["User"];
                        
                        // Try to get avatar ID from various locations
                        // First check if avatarObj (result.result or result) has the Id directly
                        string avatarId = avatarObj.Value<string>("Id") ?? avatarObj.Value<string>("id") 
                            ?? avatarObj.Value<string>("AvatarId") ?? avatarObj.Value<string>("avatarId");
                        
                        // If not found, check if it's in providerWallets (extract from first wallet)
                        if (string.IsNullOrEmpty(avatarId))
                        {
                            var providerWallets = avatarObj["providerWallets"];
                            if (providerWallets != null)
                            {
                                // Try ArbitrumOASIS first, then SolanaOASIS, then any first array
                                var arbitrumWallets = providerWallets["ArbitrumOASIS"] as JArray;
                                if (arbitrumWallets != null && arbitrumWallets.Count > 0)
                                {
                                    avatarId = arbitrumWallets[0].Value<string>("avatarId") ?? arbitrumWallets[0].Value<string>("AvatarId");
                                }
                                
                                if (string.IsNullOrEmpty(avatarId))
                                {
                                    var solanaWallets = providerWallets["SolanaOASIS"] as JArray;
                                    if (solanaWallets != null && solanaWallets.Count > 0)
                                    {
                                        avatarId = solanaWallets[0].Value<string>("avatarId") ?? solanaWallets[0].Value<string>("AvatarId");
                                    }
                                }
                                
                                // Try any first property that's an array
                                if (string.IsNullOrEmpty(avatarId))
                                {
                                    foreach (var prop in providerWallets.Children<JProperty>())
                                    {
                                        if (prop.Value is JArray array && array.Count > 0)
                                        {
                                            avatarId = array[0].Value<string>("avatarId") ?? array[0].Value<string>("AvatarId");
                                            if (!string.IsNullOrEmpty(avatarId)) break;
                                        }
                                    }
                                }
                            }
                        }
                        
                        // If still not found, check nested structures
                        if (string.IsNullOrEmpty(avatarId) && user != null)
                        {
                            avatarId = user.Value<string>("id") ?? user.Value<string>("Id") ?? user.Value<string>("avatarId") ?? user.Value<string>("AvatarId");
                        }
                        if (string.IsNullOrEmpty(avatarId) && data != null)
                        {
                            avatarId = data.Value<string>("id") ?? data.Value<string>("Id") ?? data.Value<string>("avatarId") ?? data.Value<string>("AvatarId");
                        }
                        if (string.IsNullOrEmpty(avatarId))
                        {
                            avatarId = result.Value<string>("id") ?? result.Value<string>("Id") ?? result.Value<string>("avatarId") ?? result.Value<string>("AvatarId");
                        }

                        // Try to get JWT token from various locations
                        string jwtToken = null;
                        if (data != null)
                        {
                            jwtToken = data.Value<string>("token") ?? data.Value<string>("Token") ?? data.Value<string>("jwtToken") ?? data.Value<string>("JwtToken");
                        }
                        if (string.IsNullOrEmpty(jwtToken))
                        {
                            jwtToken = result.Value<string>("token") ?? result.Value<string>("Token") ?? result.Value<string>("jwtToken") ?? result.Value<string>("JwtToken");
                        }

                        // Try to get refresh token
                        string refreshToken = null;
                        if (data != null)
                        {
                            refreshToken = data.Value<string>("refreshToken") ?? data.Value<string>("RefreshToken");
                        }
                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            refreshToken = result.Value<string>("refreshToken") ?? result.Value<string>("RefreshToken");
                        }

                        // Get username/email
                        string avatarUsername = null;
                        string avatarEmail = null;
                        if (user != null)
                        {
                            avatarUsername = user.Value<string>("username") ?? user.Value<string>("Username");
                            avatarEmail = user.Value<string>("email") ?? user.Value<string>("Email");
                        }
                        if (string.IsNullOrEmpty(avatarUsername))
                        {
                            avatarUsername = result.Value<string>("username") ?? result.Value<string>("Username");
                        }
                        if (string.IsNullOrEmpty(avatarEmail))
                        {
                            avatarEmail = result.Value<string>("email") ?? result.Value<string>("Email");
                        }

                        if (string.IsNullOrEmpty(avatarId))
                        {
                            UnityEngine.Debug.LogError($"[Web4Web5GatewayClient] Could not find avatarId in response. Response structure: {jsonObj.ToString(Newtonsoft.Json.Formatting.Indented)}");
                            return OASISResult<AuthenticateResponse>.Error("Authentication response missing avatar ID. Check console for full response structure.");
                        }

                        var authResponse = new AuthenticateResponse
                        {
                            id = avatarId,
                            username = avatarUsername,
                            email = avatarEmail,
                            jwtToken = jwtToken,
                            refreshToken = refreshToken,
                            isBeamedIn = result.Value<bool?>("isBeamedIn") ?? result.Value<bool?>("IsBeamedIn") ?? false,
                            lastBeamedIn = result.Value<string>("lastBeamedIn") ?? result.Value<string>("LastBeamedIn")
                        };

                        UnityEngine.Debug.Log($"[Web4Web5GatewayClient] Parsed avatarId: {avatarId}, jwtToken present: {!string.IsNullOrEmpty(jwtToken)}");
                        return OASISResult<AuthenticateResponse>.Success(authResponse);
                    }
                    catch (Exception ex)
                    {
                        return OASISResult<AuthenticateResponse>.Error($"Failed to parse authentication response: {ex.Message}");
                    }
                }

                var message = MapStatusToFriendlyMessage(response.StatusCode, uri, response.ReasonPhrase);
                UnityEngine.Debug.LogError($"[Web4Web5GatewayClient] Authentication failed - Status: {response.StatusCode}, URI: {uri}, Message: {message}, Response: {responseBody}");
                RegisterRequestFailure(message, response.StatusCode);
                return OASISResult<AuthenticateResponse>.Error($"{message} (URI: {uri})");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                var errorMessage = $"Authentication failed: {ex.Message}";
                UnityEngine.Debug.LogError($"[Web4Web5GatewayClient] Authentication exception - URI: {uri}, Exception: {ex}");
                RegisterRequestFailure(errorMessage, 0);
                return OASISResult<AuthenticateResponse>.Error($"{errorMessage} (URI: {uri})");
            }
        }

        public GatewayHealthSnapshot GetHealthSnapshot()
        {
            return new GatewayHealthSnapshot
            {
                circuitOpen = IsCircuitOpen(),
                consecutiveFailures = _consecutiveFailures,
                lastError = _lastError,
                lastResultFromCache = _lastResultFromCache,
                authExpired = _authExpired,
                lastLatencyMs = _lastLatencyMs,
                lastSuccessUtc = _lastSuccessUtc == DateTime.MinValue ? "never" : _lastSuccessUtc.ToString("u")
            };
        }

        private bool IsCircuitOpen()
        {
            return DateTime.UtcNow < _circuitOpenUntilUtc;
        }

        private async Task<OASISResult<string>> ExecuteGetWithResilienceAsync(string uri)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
                var sw = Stopwatch.StartNew();
                try
                {
                    var response = await _httpClient.GetAsync(uri, cts.Token);
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);

                    if (response.IsSuccessStatusCode)
                    {
                        var payload = await response.Content.ReadAsStringAsync();
                        RegisterRequestSuccess(_lastLatencyMs);
                        return OASISResult<string>.Success(payload);
                    }

                    var message = MapStatusToFriendlyMessage(response.StatusCode, uri, response.ReasonPhrase);
                    if (IsUnauthorized(response.StatusCode))
                    {
                        RegisterRequestFailure(message, response.StatusCode);
                        return OASISResult<string>.Error(message);
                    }

                    if (IsTransientStatus(response.StatusCode) && attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    RegisterRequestFailure(message, response.StatusCode);
                    return OASISResult<string>.Error(message);
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    var timeoutMessage = $"GET {uri} timed out after {RequestTimeoutSeconds}s.";
                    RegisterRequestFailure(timeoutMessage, 0);
                    return OASISResult<string>.Error(timeoutMessage);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    var errorMessage = $"GET {uri} failed: {ex.Message}";
                    RegisterRequestFailure(errorMessage, 0);
                    return OASISResult<string>.Error(errorMessage);
                }
            }

            var fallback = $"GET {uri} failed after {MaxRetries} retries.";
            RegisterRequestFailure(fallback, 0);
            return OASISResult<string>.Error(fallback);
        }

        private async Task<HttpResponseMessage> ExecuteSendWithResilienceAsync(string uri, HttpMethod method, string jsonBody)
        {
            HttpResponseMessage lastResponse = null;
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
                var sw = Stopwatch.StartNew();
                try
                {
                    using var request = new HttpRequestMessage(method, uri)
                    {
                        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                    };

                    var response = await _httpClient.SendAsync(request, cts.Token);
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    if (IsUnauthorized(response.StatusCode))
                    {
                        RegisterRequestFailure(MapStatusToFriendlyMessage(response.StatusCode, uri, response.ReasonPhrase), response.StatusCode);
                        return response;
                    }

                    lastResponse = response;
                    if (IsTransientStatus(response.StatusCode) && attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    return response;
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    RegisterRequestFailure($"{method} {uri} timed out after {RequestTimeoutSeconds}s.", 0);
                    return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                    {
                        RequestMessage = new HttpRequestMessage(method, uri),
                        ReasonPhrase = "Timed out"
                    };
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _lastLatencyMs = Math.Max(0, sw.ElapsedMilliseconds);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(GetRetryDelayMs(attempt));
                        continue;
                    }

                    RegisterRequestFailure($"{method} {uri} failed: {ex.Message}", 0);
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        RequestMessage = new HttpRequestMessage(method, uri),
                        ReasonPhrase = ex.Message
                    };
                }
            }

            return lastResponse ?? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = new HttpRequestMessage(method, uri),
                ReasonPhrase = "Failed after retries"
            };
        }

        private static bool IsUnauthorized(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden;
        }

        private static bool IsTransientStatus(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == HttpStatusCode.TooManyRequests ||
                   (int)statusCode >= 500;
        }

        private static int GetRetryDelayMs(int attempt)
        {
            // 250, 500, 1000...
            var clamped = Math.Max(0, Math.Min(6, attempt - 1));
            return (int)(250 * Math.Pow(2, clamped));
        }

        private void RegisterRequestSuccess(long latencyMs)
        {
            var recovered = _consecutiveFailures > 0;
            _consecutiveFailures = 0;
            _circuitOpenUntilUtc = DateTime.MinValue;
            _lastError = "none";
            _lastResultFromCache = false;
            _authExpired = false;
            _lastLatencyMs = latencyMs;
            _lastSuccessUtc = DateTime.UtcNow;
            if (recovered)
            {
                RuntimeDiagnosticsLog.Write("API", $"Recovered after failures. Latest latency: {latencyMs}ms");
            }
        }

        private void RegisterRequestFailure(string message, HttpStatusCode statusCode)
        {
            _consecutiveFailures++;
            _lastError = message;
            _authExpired = IsUnauthorized(statusCode);
            RuntimeDiagnosticsLog.Write("API", $"Failure #{_consecutiveFailures}: {message}");
            if (_consecutiveFailures >= CircuitFailureThreshold)
            {
                _circuitOpenUntilUtc = DateTime.UtcNow.AddSeconds(CircuitCooldownSeconds);
                RuntimeDiagnosticsLog.Write("API", $"Circuit opened for {CircuitCooldownSeconds}s due to repeated failures.");
            }
        }

        private static string MapStatusToFriendlyMessage(HttpStatusCode statusCode, string uri, string reasonPhrase)
        {
            if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            {
                return $"Auth expired/invalid for {uri}. Please refresh WEB4/WEB5 API credentials.";
            }

            if (statusCode == HttpStatusCode.TooManyRequests)
            {
                return $"Rate limited by API at {uri}. Retrying with backoff.";
            }

            if (statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
            {
                return $"API temporarily unavailable at {uri}: {(int)statusCode} {reasonPhrase}";
            }

            return $"{uri} -> {(int)statusCode} {reasonPhrase}";
        }

        private OASISResult<T> TryLoadCache<T>(string key, string friendlyName, string fetchError)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return OASISResult<T>.Error(fetchError);
            }

            try
            {
                var envelopeJson = PlayerPrefs.GetString(key);
                var envelope = JsonConvert.DeserializeObject<ApiCacheEnvelope>(envelopeJson);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.payload))
                {
                    return OASISResult<T>.Error(fetchError);
                }

                if (DateTime.TryParse(envelope.savedUtc, out var savedUtc))
                {
                    if ((DateTime.UtcNow - savedUtc).TotalSeconds > CacheTtlSeconds)
                    {
                        return OASISResult<T>.Error(fetchError);
                    }
                }

                var cached = JsonConvert.DeserializeObject<T>(envelope.payload);
                if (cached == null)
                {
                    return OASISResult<T>.Error(fetchError);
                }

                _lastResultFromCache = true;
                return OASISResult<T>.Success(cached, $"{friendlyName} loaded from cache (API unavailable).");
            }
            catch
            {
                return OASISResult<T>.Error(fetchError);
            }
        }

        private static void SaveCache<T>(string key, T value)
        {
            try
            {
                var envelope = new ApiCacheEnvelope
                {
                    savedUtc = DateTime.UtcNow.ToString("u"),
                    payload = JsonConvert.SerializeObject(value)
                };

                PlayerPrefs.SetString(key, JsonConvert.SerializeObject(envelope));
                PlayerPrefs.Save();
            }
            catch
            {
                // best-effort cache
            }
        }

        private static OASISResult<List<InventoryItem>> ParseInventory(string json)
        {
            try
            {
                var arr = ExtractArray(json);
                var items = new List<InventoryItem>();
                foreach (var item in arr)
                {
                    var metaData = item["MetaData"] ?? item["metaData"];
                    items.Add(new InventoryItem
                    {
                        id = item.Value<string>("Id") ?? item.Value<string>("id"),
                        name = item.Value<string>("Name") ?? item.Value<string>("name"),
                        description = item.Value<string>("Description") ?? item.Value<string>("description"),
                        source = metaData?.Value<string>("GameSource") ?? "Unknown",
                        itemType = metaData?.Value<string>("ItemType") ?? "Unknown"
                    });
                }

                return OASISResult<List<InventoryItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return OASISResult<List<InventoryItem>>.Error($"Inventory parse failed: {ex.Message}");
            }
        }

        private static OASISResult<List<QuestItem>> ParseQuests(string json)
        {
            try
            {
                var arr = ExtractArray(json);
                var quests = new List<QuestItem>();
                foreach (var q in arr)
                {
                    var objectivesTotal = ParseIntFromToken(q["ObjectivesCount"] ?? q["TotalObjectives"] ?? q["objectivesCount"] ?? q["totalObjectives"]);
                    var objectivesCompleted = ParseIntFromToken(q["ObjectivesCompleted"] ?? q["CompletedObjectives"] ?? q["objectivesCompleted"] ?? q["completedObjectives"]);
                    var progress = ParseFloatFromToken(q["Progress"] ?? q["progress"] ?? q["Completion"] ?? q["completion"]);
                    if (progress <= 0f && objectivesTotal > 0)
                    {
                        progress = Mathf.Clamp01(objectivesCompleted / (float)objectivesTotal);
                    }

                    quests.Add(new QuestItem
                    {
                        id = q.Value<string>("Id") ?? q.Value<string>("id"),
                        name = q.Value<string>("Name") ?? q.Value<string>("name"),
                        description = q.Value<string>("Description") ?? q.Value<string>("description"),
                        status = q.Value<string>("Status") ?? q.Value<string>("status"),
                        gameSource = q.Value<string>("GameSource") ?? q.Value<string>("gameSource"),
                        priority = q.Value<string>("Priority") ?? q.Value<string>("priority"),
                        progress = progress,
                        objectivesCompleted = objectivesCompleted,
                        objectivesTotal = objectivesTotal
                    });
                }

                return OASISResult<List<QuestItem>>.Success(quests);
            }
            catch (Exception ex)
            {
                return OASISResult<List<QuestItem>>.Error($"Quest parse failed: {ex.Message}");
            }
        }

        private static JArray ExtractArray(string json)
        {
            var token = ExtractDataToken(json);
            if (token is JArray arr)
            {
                return arr;
            }

            if (token is JObject obj)
            {
                if (obj["Items"] is JArray items)
                {
                    return items;
                }

                if (obj["Quests"] is JArray quests)
                {
                    return quests;
                }

                if (obj["History"] is JArray history)
                {
                    return history;
                }
            }

            return new JArray();
        }

        private static JObject ExtractObject(string json)
        {
            var token = ExtractDataToken(json);
            if (token is JObject obj)
            {
                return obj;
            }

            if (token is JArray arr && arr.Count > 0 && arr[0] is JObject first)
            {
                return first;
            }

            return new JObject();
        }

        private static JToken ExtractDataToken(string json)
        {
            var token = JToken.Parse(json);
            if (token is JObject obj)
            {
                if (obj.TryGetValue("Result", out var result))
                {
                    return result;
                }

                if (obj.TryGetValue("result", out var resultLower))
                {
                    return resultLower;
                }
            }

            return token;
        }

        private static float ParseFloat(JObject obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    if (float.TryParse(token.ToString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }

            return 0f;
        }

        private static int ParseIntFromToken(JToken token)
        {
            if (token == null)
            {
                return 0;
            }

            return int.TryParse(token.ToString(), out var value) ? value : 0;
        }

        private static float ParseFloatFromToken(JToken token)
        {
            if (token == null)
            {
                return 0f;
            }

            return float.TryParse(token.ToString(), out var value) ? value : 0f;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}

