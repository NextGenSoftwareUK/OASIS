using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;

namespace OASIS.Omniverse.UnityHost.API
{
    public sealed class Web4Web5GatewayClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _web4Base;
        private readonly string _web5Base;
        private readonly string _avatarId;

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
                return OASISResult<List<InventoryItem>>.Error(fetch.Message);
            }

            return ParseInventory(fetch.Result);
        }

        public async Task<OASISResult<List<QuestItem>>> GetCrossGameQuestsAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<List<QuestItem>>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web5Base}/api/quests/by-avatar/{_avatarId}",
                $"{_web5Base}/api/quests/load-all-for-avatar",
                $"{_web5Base}/api/quests");

            if (fetch.IsError)
            {
                return OASISResult<List<QuestItem>>.Error(fetch.Message);
            }

            return ParseQuests(fetch.Result);
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
                return OASISResult<List<NftAssetItem>>.Error(fetch.Message);
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
                return OASISResult<AvatarProfileItem>.Error(fetch.Message);
            }

            try
            {
                var profile = ExtractObject(fetch.Result);
                return OASISResult<AvatarProfileItem>.Success(new AvatarProfileItem
                {
                    id = profile.Value<string>("Id") ?? profile.Value<string>("id"),
                    username = profile.Value<string>("Username") ?? profile.Value<string>("username"),
                    email = profile.Value<string>("Email") ?? profile.Value<string>("email"),
                    firstName = profile.Value<string>("FirstName") ?? profile.Value<string>("firstName"),
                    lastName = profile.Value<string>("LastName") ?? profile.Value<string>("lastName"),
                    title = profile.Value<string>("Title") ?? profile.Value<string>("title")
                });
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
                return OASISResult<KarmaOverview>.Error($"Karma request failed. Total: {totalFetch.Message}. History: {historyFetch.Message}");
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
                        amount = ParseFloat(row, "Karma", "Amount", "amount", "value"),
                        karmaType = row.Value<string>("KarmaType") ?? row.Value<string>("Type") ?? row.Value<string>("type"),
                        createdDate = row.Value<string>("CreatedDate") ?? row.Value<string>("CreatedOn") ?? row.Value<string>("createdDate")
                    });
                }
            }

            return OASISResult<KarmaOverview>.Success(overview);
        }

        public async Task<OASISResult<OmniverseGlobalSettings>> GetGlobalPreferencesAsync()
        {
            var fetch = await GetJsonFromCandidatesAsync(
                $"{_web4Base}/api/settings/user/preferences",
                $"{_web4Base}/api/settings/user");

            if (fetch.IsError)
            {
                return OASISResult<OmniverseGlobalSettings>.Error(fetch.Message);
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

            return save;
        }

        private async Task<OASISResult<string>> GetJsonFromCandidatesAsync(params string[] uris)
        {
            var errors = new List<string>();
            foreach (var uri in uris)
            {
                if (string.IsNullOrWhiteSpace(uri))
                {
                    continue;
                }

                try
                {
                    var response = await _httpClient.GetAsync(uri);
                    if (!response.IsSuccessStatusCode)
                    {
                        errors.Add($"{uri} -> {(int)response.StatusCode} {response.ReasonPhrase}");
                        continue;
                    }

                    return OASISResult<string>.Success(await response.Content.ReadAsStringAsync());
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
            foreach (var uri in uris)
            {
                if (string.IsNullOrWhiteSpace(uri))
                {
                    continue;
                }

                try
                {
                    using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync(uri, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return OASISResult<bool>.Success(true);
                    }

                    if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        using var postContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        var postResponse = await _httpClient.PostAsync(uri, postContent);
                        if (postResponse.IsSuccessStatusCode)
                        {
                            return OASISResult<bool>.Success(true);
                        }

                        errors.Add($"{uri} (POST fallback) -> {(int)postResponse.StatusCode} {postResponse.ReasonPhrase}");
                    }
                    else
                    {
                        errors.Add($"{uri} -> {(int)response.StatusCode} {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{uri} -> {ex.Message}");
                }
            }

            return OASISResult<bool>.Error(string.Join(" | ", errors));
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
                    quests.Add(new QuestItem
                    {
                        id = q.Value<string>("Id") ?? q.Value<string>("id"),
                        name = q.Value<string>("Name") ?? q.Value<string>("name"),
                        description = q.Value<string>("Description") ?? q.Value<string>("description"),
                        status = q.Value<string>("Status") ?? q.Value<string>("status")
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

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}

