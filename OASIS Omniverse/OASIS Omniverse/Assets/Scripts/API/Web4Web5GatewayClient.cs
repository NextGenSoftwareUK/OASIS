using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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

            var uri = $"{_web4Base}/inventoryitems/user/{_avatarId}";
            return await FetchInventoryAsync(uri);
        }

        public async Task<OASISResult<List<QuestItem>>> GetCrossGameQuestsAsync()
        {
            if (string.IsNullOrWhiteSpace(_avatarId))
            {
                return OASISResult<List<QuestItem>>.Error("AvatarId is empty. Set avatarId in omniverse_host_config.json.");
            }

            var uri = $"{_web5Base}/quests/avatar/{_avatarId}";
            try
            {
                var json = await _httpClient.GetStringAsync(uri);
                var root = JObject.Parse(json);
                var resultToken = root["Result"] ?? root["result"];

                var quests = new List<QuestItem>();
                if (resultToken is JArray arr)
                {
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
                }

                return OASISResult<List<QuestItem>>.Success(quests);
            }
            catch (Exception ex)
            {
                return OASISResult<List<QuestItem>>.Error($"Quest request failed: {ex.Message}");
            }
        }

        private async Task<OASISResult<List<InventoryItem>>> FetchInventoryAsync(string uri)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(uri);
                var root = JObject.Parse(json);
                var resultToken = root["Result"] ?? root["result"];

                var items = new List<InventoryItem>();
                if (resultToken is JArray arr)
                {
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
                }

                return OASISResult<List<InventoryItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return OASISResult<List<InventoryItem>>.Error($"Inventory request failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}

