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
                ObjectDisposedException.ThrowIf(_disposed,this);
                ArgumentNullException.ThrowIfNull(searchParams);
                if(version!=0)throw new NotSupportedException("Native social search does not expose historical OASIS versions.");
                if((!searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId!=Guid.Empty) || searchParams.ParentId!=Guid.Empty)
                    throw new NotSupportedException("Native status search cannot silently ignore an inactive avatar scope or parent scope.");
                if(searchParams.SearchGroups==null || searchParams.SearchGroups.Count==0)
                    throw new ArgumentException("Native status search requires text search groups.",nameof(searchParams));
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth);
                searchParams=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<SearchParams>(searchParams);
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth);
                var groups=searchParams.SearchGroups.ToArray();
                foreach(var group in groups){
                if(group is not ISearchTextGroup textGroup)throw new NotSupportedException("Native status search requires text groups.");
                if(textGroup.SearchAvatars || textGroup.SearchIds || textGroup.SearchProviderKeys || textGroup.AvatarSearchParams!=null || textGroup.HolonSearchParams!=null)
                    throw new NotSupportedException("Native status search does not implement account or structured identity field search.");
                var searchQuery=textGroup.SearchQuery;
                if(string.IsNullOrWhiteSpace(searchQuery))throw new ArgumentException("Search query cannot be empty.",nameof(searchParams));
                }
                if(string.IsNullOrWhiteSpace(_accessToken))throw new UnauthorizedAccessException("Complete native status search requires a user token with read:search authority.");
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,ISearchResults>(activateResult);
                    }
                }

                Dictionary<Guid,IHolon> combined=null;var totalPages=0;
                foreach(ISearchTextGroup group in groups){
                var searchQuery=group.SearchQuery;
                var holons=new List<IHolon>();var seen=new HashSet<Guid>();var exhausted=false;
                // Bounded exhaustive native search: never report the first page or a truncated prefix as complete.
                for(var page=0;page<100;page++){
                if(++totalPages>100)throw new System.IO.InvalidDataException("Native compound search exceeded the total 100-page safety bound.");
                var apiUrl = $"/api/v2/search?q={Uri.EscapeDataString(searchQuery)}&type=statuses&limit=40&offset="+(page*40).ToString(System.Globalization.CultureInfo.InvariantCulture);
                using var httpResponse = await _httpClient.GetAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if(searchResult.ValueKind!=JsonValueKind.Object || !searchResult.TryGetProperty("statuses",out var statuses) || statuses.ValueKind!=JsonValueKind.Array)
                        throw new System.IO.InvalidDataException("Native search response requires a statuses array.");
                    if(statuses.GetArrayLength()>40)throw new System.IO.InvalidDataException("Native search page exceeds requested limit.");
                    if(statuses.GetArrayLength()==0){
                        exhausted=true;break;
                    }
                    {
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var holon = ParseActivityPubToHolon(status);
                            if(holon==null || !seen.Add(holon.Id))throw new System.IO.InvalidDataException("Native search returned a missing or repeated status across pages.");
                            holons.Add(holon);
                        }
                    }
                }
                else
                {
                    httpResponse.EnsureSuccessStatusCode();
                }
                }
                if(!exhausted)throw new System.IO.InvalidDataException("Native search exceeded the 100-page safety bound; no partial result is published.");
                var matches=holons.ToDictionary(value=>value.Id);
                // Same left-to-right PreviousSearchGroupOperator semantics as StoredRecordSearch.Matches.
                if(combined==null)combined=matches;
                else if(group.PreviousSearchGroupOperator==SearchParamGroupOperator.And){foreach(var id in combined.Keys.Where(id=>!matches.ContainsKey(id)).ToArray())combined.Remove(id);}
                else{foreach(var entry in matches)combined.TryAdd(entry.Key,entry.Value);}
                }
                var selected=combined.Values.Where(value=>(!searchParams.SearchOnlyForCurrentAvatar || value.CreatedByAvatarId==searchParams.AvatarId) && NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(value.MetaData,searchParams.FilterByMetaData,searchParams.MetaKeyValuePairMatchMode)).ToList();
                if(loadChildren){
                    for(var index=0;index<selected.Count;index++)selected[index]=await ReadMastodonReplyGraphAsync((Holon)selected[index],recursive,maxChildDepth);
                }
                response.Result=new SearchResults{SearchResultHolons=selected,NumberOfResults=selected.Count};
                response.IsLoaded=true;
                response.Message="Native search exhausted its result pages; results are not a point-in-time snapshot.";
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
            => ProjectMastodonStatus(status);

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
