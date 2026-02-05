using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.GameIntegration
{
    /// <summary>
    /// Client for integrating games with the OASIS STAR API
    /// Enables cross-game item sharing, quest tracking, and NFT management
    /// </summary>
    public class GameIntegrationClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _oasisApiBaseUrl;
        private string _jwtToken;
        private string _refreshToken;
        private string _avatarId;
        private string _apiKey; // For direct API key auth (optional)

        public GameIntegrationClient(string baseUrl, string oasisApiBaseUrl = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _oasisApiBaseUrl = oasisApiBaseUrl?.TrimEnd('/') ?? baseUrl.Replace("/api", "");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Authenticate using avatar credentials (SSO)
        /// </summary>
        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                var authRequest = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(authRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_oasisApiBaseUrl}/api/avatar/authenticate", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<AvatarAuthResponse>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.IsError == false && result.Result != null)
                {
                    _jwtToken = result.Result.JwtToken;
                    _refreshToken = result.Result.RefreshToken;
                    _avatarId = result.Result.Id.ToString();

                    // Update authorization header
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Authenticate using API key (alternative method)
        /// </summary>
        public void SetApiKey(string apiKey, string avatarId)
        {
            _apiKey = apiKey;
            _avatarId = avatarId;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        /// <summary>
        /// Get current authenticated avatar
        /// </summary>
        public async Task<AvatarInfo> GetCurrentAvatarAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_oasisApiBaseUrl}/api/avatar/current");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<AvatarInfo>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.IsError == false && result.Result != null)
                {
                    _avatarId = result.Result.Id.ToString();
                    return result.Result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current avatar: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if the player has a specific item in their inventory
        /// </summary>
        public async Task<bool> HasItemAsync(string itemName)
        {
            try
            {
                var inventory = await GetInventoryAsync();
                return inventory.Exists(item => 
                    string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Description, itemName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for item '{itemName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all items in the player's inventory
        /// </summary>
        public async Task<List<GameItem>> GetInventoryAsync()
        {
            try
            {
                var avatarId = _avatarId ?? (await GetCurrentAvatarAsync())?.Id.ToString();
                if (string.IsNullOrEmpty(avatarId))
                {
                    throw new Exception("Avatar ID not available. Please authenticate first.");
                }

                var response = await _httpClient.GetAsync($"/api/inventoryitems/user/{avatarId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<List<InventoryItemResponse>>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.IsError == true)
                {
                    throw new Exception($"API Error: {result.Message}");
                }

                var items = new List<GameItem>();
                if (result?.Result != null)
                {
                    foreach (var item in result.Result)
                    {
                        items.Add(new GameItem
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Description = item.Description,
                            GameSource = ExtractGameSource(item),
                            ItemType = ExtractItemType(item)
                        });
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting inventory: {ex.Message}");
                return new List<GameItem>();
            }
        }

        /// <summary>
        /// Add an item to the player's inventory (e.g., when collected in-game)
        /// </summary>
        public async Task<bool> AddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem")
        {
            try
            {
                var item = new
                {
                    Name = itemName,
                    Description = $"{description} | Source: {gameSource}",
                    HolonType = "InventoryItem",
                    MetaData = new Dictionary<string, object>
                    {
                        ["GameSource"] = gameSource,
                        ["ItemType"] = itemType,
                        ["CollectedAt"] = DateTime.UtcNow.ToString("O"),
                        ["CrossGameItem"] = true
                    }
                };

                var json = JsonSerializer.Serialize(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/inventoryitems", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<InventoryItemResponse>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.IsError != true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding item '{itemName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Use an item (e.g., use a keycard to open a door)
        /// </summary>
        public async Task<bool> UseItemAsync(string itemName, string context = null)
        {
            try
            {
                // First, find the item in inventory
                var inventory = await GetInventoryAsync();
                var item = inventory.Find(i => 
                    string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));

                if (item == null)
                {
                    return false;
                }

                // Call the use item endpoint
                var useRequest = new
                {
                    Context = context ?? "game_use",
                    UsedAt = DateTime.UtcNow.ToString("O")
                };

                var json = JsonSerializer.Serialize(useRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/inventoryitems/{item.Id}/use", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using item '{itemName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a cross-game quest that spans multiple games
        /// </summary>
        public async Task<bool> CreateCrossGameQuestAsync(string questName, string description, List<QuestObjective> objectives)
        {
            try
            {
                var quest = new
                {
                    Name = questName,
                    Description = description,
                    Type = "CrossGame",
                    Objectives = objectives,
                    MetaData = new Dictionary<string, object>
                    {
                        ["CrossGameQuest"] = true,
                        ["Games"] = objectives.Select(o => o.GameSource).Distinct().ToList()
                    }
                };

                var json = JsonSerializer.Serialize(quest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/missions", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating quest '{questName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start a quest for the current player
        /// </summary>
        public async Task<bool> StartQuestAsync(string questId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/quests/{questId}/start", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting quest '{questId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Complete a quest objective
        /// </summary>
        public async Task<bool> CompleteQuestObjectiveAsync(string questId, string objectiveId, string gameSource)
        {
            try
            {
                var update = new
                {
                    objectiveId = objectiveId,
                    completed = true,
                    completedAt = DateTime.UtcNow.ToString("O"),
                    gameSource = gameSource
                };

                var json = JsonSerializer.Serialize(update);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/quests/{questId}/objectives/{objectiveId}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing quest objective: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Complete a quest and claim rewards
        /// </summary>
        public async Task<bool> CompleteQuestAsync(string questId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/quests/{questId}/complete", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing quest '{questId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get active quests for the current player
        /// </summary>
        public async Task<List<QuestInfo>> GetActiveQuestsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/quests?status=Active");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<QuestListResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Result?.Quests ?? new List<QuestInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active quests: {ex.Message}");
                return new List<QuestInfo>();
            }
        }

        /// <summary>
        /// Create an NFT for a defeated boss
        /// </summary>
        public async Task<string> CreateBossNFTAsync(string bossName, string description, string gameSource, Dictionary<string, object> bossStats, string modelData = null)
        {
            try
            {
                var nft = new
                {
                    Name = bossName,
                    Description = description,
                    Type = "Boss",
                    MetaData = new Dictionary<string, object>
                    {
                        ["GameSource"] = gameSource,
                        ["BossStats"] = bossStats,
                        ["DefeatedAt"] = DateTime.UtcNow.ToString("O"),
                        ["ModelData"] = modelData,
                        ["Deployable"] = true
                    }
                };

                var json = JsonSerializer.Serialize(nft);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/nfts", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<NFTResponse>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Result?.Id?.ToString() ?? null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating boss NFT: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get player's NFT collection
        /// </summary>
        public async Task<List<NFTInfo>> GetNFTCollectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/nfts/user/{_avatarId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OASISResult<List<NFTInfo>>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Result ?? new List<NFTInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting NFT collection: {ex.Message}");
                return new List<NFTInfo>();
            }
        }

        /// <summary>
        /// Deploy a boss NFT in a game
        /// </summary>
        public async Task<bool> DeployBossNFTAsync(string nftId, string targetGame, string location)
        {
            try
            {
                var deployment = new
                {
                    nftId = nftId,
                    targetGame = targetGame,
                    location = location,
                    deployedAt = DateTime.UtcNow.ToString("O")
                };

                var json = JsonSerializer.Serialize(deployment);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/nfts/{nftId}/deploy", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deploying boss NFT: {ex.Message}");
                return false;
            }
        }

        private string ExtractGameSource(InventoryItemResponse item)
        {
            // Extract game source from metadata or description
            if (item.MetaData?.TryGetValue("GameSource", out var source) == true)
            {
                return source?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }

        private string ExtractItemType(InventoryItemResponse item)
        {
            if (item.MetaData?.TryGetValue("ItemType", out var type) == true)
            {
                return type?.ToString() ?? "Miscellaneous";
            }
            return "Miscellaneous";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Response models
    public class OASISResult<T>
    {
        public T Result { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; }
    }

    public class InventoryItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }

    public class GameItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string GameSource { get; set; }
        public string ItemType { get; set; }
    }

    public class QuestObjective
    {
        public string Description { get; set; }
        public string GameSource { get; set; }
        public string ItemRequired { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class AvatarAuthResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public bool IsBeamedIn { get; set; }
        public DateTime LastBeamedIn { get; set; }
    }

    public class AvatarInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class QuestInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public List<QuestObjective> Objectives { get; set; }
    }

    public class QuestListResponse
    {
        public List<QuestInfo> Quests { get; set; }
    }

    public class NFTResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class NFTInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }
}

