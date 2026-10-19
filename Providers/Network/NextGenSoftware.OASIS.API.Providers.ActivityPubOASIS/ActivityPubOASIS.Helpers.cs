using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS
{
    public partial class ActivityPubOASIS
    {

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Avatar
        /// </summary>
        private IAvatar ParseActivityPubToAvatar(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Avatar object to preserve all properties
                var avatar = JsonSerializer.Deserialize<Avatar>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Parse ActivityPub Actor/Person JSON to AvatarDetail. Tries full deserialize first; then maps ActivityPub fields (id, preferredUsername, name, summary).
        /// Avatar and AvatarDetail are separate; this does not build detail from an Avatar.
        /// </summary>
        private IAvatarDetail ParseActivityPubToAvatarDetail(string activityPubJson)
        {
            if (string.IsNullOrWhiteSpace(activityPubJson)) return null;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                var detail = JsonSerializer.Deserialize<AvatarDetail>(activityPubJson, options);
                if (detail != null && detail.Id != Guid.Empty) return detail;
            }
            catch { /* fallback to Actor mapping */ }
            try
            {
                using var doc = JsonDocument.Parse(activityPubJson);
                var root = doc.RootElement;
                var idStr = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var preferredUsername = root.TryGetProperty("preferredUsername", out var pu) ? pu.GetString() : null;
                var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
                var summary = root.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
                Guid id;
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out id)) { }
                else
                {
                    var input = idStr ?? preferredUsername ?? "activitypub";
                    using var sha = System.Security.Cryptography.SHA256.Create();
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                    var guidBytes = new byte[16];
                    Array.Copy(hash, 0, guidBytes, 0, 16);
                    id = new Guid(guidBytes);
                }
                return new AvatarDetail
                {
                    Id = id,
                    Username = preferredUsername ?? name ?? idStr ?? "",
                    Email = email ?? "",
                    FirstName = name ?? preferredUsername ?? "",
                    LastName = "",
                    Description = summary ?? "",
                    CreatedDate = default,
                    ModifiedDate = default
                };
            }
            catch { return null; }
        }

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Holon
        /// </summary>
        private IHolon ParseActivityPubToHolon(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Holon object to preserve all properties
                var holon = JsonSerializer.Deserialize<Holon>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return holon;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Holon collection
        /// </summary>
        private IEnumerable<IHolon> ParseActivityPubToHolons(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Holon collection to preserve all properties
                var holons = JsonSerializer.Deserialize<IEnumerable<Holon>>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return holons;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }



        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _isActivated = false;
            IsProviderActivated = false;
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }



        // Holon methods
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref response,"Use the native Mastodon status key for reply lookups; projection GUIDs are not native account or status IDs.",
                new NotSupportedException("An OASIS projection GUID has no reversible Mastodon native identity."));
            return Task.FromResult(response);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadMastodonChildrenAsync(providerKey,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single key-value pair to dictionary and use the main method
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Use ActivityPub search API to find holons by metadata
                // Build search query from metadata pairs
                var searchQuery = string.Join(" ", metaKeyValuePairs.Values);
                var apiUrl = $"/api/v2/search?q={Uri.EscapeDataString(searchQuery)}&type=statuses";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var holons = new List<IHolon>();
                    if (searchResult.TryGetProperty("statuses", out var statuses))
                    {
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var holon = ParseActivityPubToHolon(status);
                            if (holon != null)
                            {
                                // Filter by metadata match mode
                                bool matches = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                                    ? metaKeyValuePairs.All(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value)
                                    : metaKeyValuePairs.Any(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString()?.Contains(kvp.Value) == true);
                                
                                if (matches && (type == HolonType.All || holon.HolonType == type))
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {holons.Count} holons by metadata from ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search holons in ActivityPub: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => Task.FromResult(RejectPrivateRecordStorage<IEnumerable<IHolon>>());

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        // Search methods
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Extract search query from searchParams
                string searchQuery = "";
                if (searchParams is ISearchTextGroup textGroup)
                {
                    searchQuery = textGroup.SearchQuery ?? "";
                }

                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    OASISErrorHandling.HandleError(ref response, "Search query cannot be empty");
                    return response;
                }

                // Use ActivityPub search API
                var apiUrl = $"/api/v2/search?q={Uri.EscapeDataString(searchQuery)}&type=statuses";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var searchResults = new SearchResults();
                    
                    if (searchResult.TryGetProperty("statuses", out var statuses))
                    {
                        var holons = new List<IHolon>();
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var holon = ParseActivityPubToHolon(status);
                            if (holon != null)
                            {
                                holons.Add(holon);
                            }
                        }
                        searchResults.SearchResultHolons = holons;
                    }
                    
                    response.Result = searchResults;
                    response.IsError = false;
                    response.Message = "Search completed successfully in ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search in ActivityPub: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching in ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // Import/Export methods
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
            => Task.FromResult(RejectPrivateRecordStorage<bool>());

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            // Load all holons for the avatar (which are statuses in ActivityPub)
            return await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar by username to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load avatar by username: {avatarResult.Message}"
                };
            }

            // Then export all data for that avatar
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            // First load the avatar by email to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load avatar by email: {avatarResult.Message}"
                };
            }

            // Then export all data for that avatar
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Load all holons (statuses) from ActivityPub
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



        /// <summary>
        /// Parse ActivityPub status to Holon object
        /// </summary>
        private Holon ParseActivityPubToHolon(JsonElement status)
        {
            try
            {
                var statusId = status.TryGetProperty("id", out var id) ? id.GetString() : "";
                var holon = new Holon
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:holon:{statusId ?? "unknown"}"),
                    Name = status.TryGetProperty("content", out var content) ? content.GetString() : "ActivityPub Status",
                    Description = status.TryGetProperty("spoiler_text", out var spoiler) ? spoiler.GetString() : "",
                    HolonType = HolonType.Holon,
                    CreatedDate = status.TryGetProperty("created_at", out var createdAt) ? DateTime.Parse(createdAt.GetString()) : DateTime.UtcNow,
                    ModifiedDate = status.TryGetProperty("updated_at", out var updatedAt) ? DateTime.Parse(updatedAt.GetString()) : DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add metadata from ActivityPub status
                if (status.TryGetProperty("id", out var idProp))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_id"] = idProp.GetString() };
                }
                if (status.TryGetProperty("url", out var url))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_url"] = url.GetString() };
                }
                if (status.TryGetProperty("visibility", out var visibility))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_visibility"] = visibility.GetString() };
                }

                return holon;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
