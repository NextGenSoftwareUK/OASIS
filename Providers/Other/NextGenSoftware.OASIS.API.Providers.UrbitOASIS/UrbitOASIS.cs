using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS
{
    /// <summary>
    /// OASIS provider for Urbit — a peer-to-peer personal server OS.
    ///
    /// Key Urbit concepts mapped to OASIS:
    ///   @p ship name (e.g. ~sampel-palnet)   = avatar provider key / username
    ///   contact-store profile entry           = OASIS Avatar
    ///   graph-store notebook post             = OASIS Holon
    ///
    /// Connectivity uses Urbit's HTTP airlock API:
    ///   Scry (read)  : GET  http://&lt;host&gt;/~/scry/&lt;app&gt;/&lt;path&gt;.json
    ///   Poke (write) : PUT  http://&lt;host&gt;/~/channel/&lt;uid&gt;  (requires auth cookie)
    ///   Login        : POST http://&lt;host&gt;/~/login            (returns cookie)
    ///
    /// Pass the ship's web login code (+code in the dojo) to enable write operations.
    /// The default ship URL is http://localhost (Urbit runs on port 80 by default).
    /// </summary>
    public class UrbitOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        /// <summary>
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly string _shipUrl;
        private readonly string? _loginCode;
        private readonly HttpClient _http;
        private readonly CookieContainer _cookies;
        private bool _authenticated;

        public UrbitOASIS(string shipUrl = "http://localhost", string? loginCode = null)
        {
            _shipUrl = shipUrl.TrimEnd('/');
            _loginCode = loginCode;
            _cookies = new CookieContainer();
            _http = new HttpClient(new HttpClientHandler { CookieContainer = _cookies })
            {
                BaseAddress = new Uri(_shipUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            ProviderName = "UrbitOASIS";
            ProviderDescription = "Urbit peer-to-peer personal server OS provider (HTTP airlock)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.UrbitOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Auth ─────────────────────────────────────────────────────────────────

        private async Task<bool> EnsureAuthenticatedAsync()
        {
            if (_authenticated) return true;
            if (string.IsNullOrEmpty(_loginCode)) return false;

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("password", _loginCode)
            });
            var response = await _http.PostAsync("/~/login", form);
            _authenticated = response.IsSuccessStatusCode;
            return _authenticated;
        }

        // ─── Scry helper ──────────────────────────────────────────────────────────

        private async Task<JsonElement?> ScryAsync(string app, string path)
        {
            string url = $"/~/scry/{app}/{path.TrimStart('/')}.json";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonDocument.Parse(json).RootElement;
        }

        // ─── Poke helper ──────────────────────────────────────────────────────────

        private async Task<bool> PokeAsync(string ship, string app, string mark, object data)
        {
            if (!await EnsureAuthenticatedAsync()) return false;

            string uid = Guid.NewGuid().ToString("N");
            string channelUrl = $"/~/channel/{uid}";

            var pokeAction = new[]
            {
                new
                {
                    id = 1,
                    action = "poke",
                    ship,
                    app,
                    mark,
                    json = data
                }
            };

            string payload = JsonSerializer.Serialize(pokeAction);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(channelUrl, content);
            return response.IsSuccessStatusCode;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Scry the graph-store keys as a connectivity check (public, no auth required)
                var el = await ScryAsync("graph-store", "keys");
                if (el.HasValue)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"UrbitOASIS provider activated successfully — connected to ship at '{_shipUrl}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: Could not reach Urbit ship at '{_shipUrl}'. Ensure the ship is running and the URL is correct.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _authenticated = false;
            return await Task.FromResult(new OASISResult<bool>
            {
                Result = true,
                IsError = false,
                Message = "UrbitOASIS provider deactivated."
            });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        /// <summary>providerKey = @p ship name, e.g. ~sampel-palnet</summary>
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // Scry contact-store for this ship's profile
                var el = await ScryAsync("contact-store", $"contact/{providerKey}");
                if (!el.HasValue || el.Value.ValueKind == JsonValueKind.Null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: No contact-store profile found for ship '{providerKey}'.");
                    return result;
                }

                result.Result = MapContactToAvatar(providerKey, el.Value);
                result.IsError = false;
                result.Message = $"UrbitOASIS: Avatar loaded for ship '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error loading avatar for '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            // In Urbit the @p ship name IS the username
            return await LoadAvatarByProviderKeyAsync(username.StartsWith("~") ? username : $"~{username}", version);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: Urbit avatars are keyed by @p ship name; GUID lookup is not supported. Use LoadAvatarByProviderKey with the ship's @p address.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var el = await ScryAsync("contact-store", "all");
                if (!el.HasValue || el.Value.ValueKind == JsonValueKind.Null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        "UrbitOASIS: No contacts returned from contact-store.");
                    return result;
                }

                var avatars = new List<IAvatar>();

                // contact-store /all returns: {"contacts": {"~ship": {...profile...}, ...}}
                if (el.Value.TryGetProperty("contacts", out var contacts))
                {
                    foreach (var contact in contacts.EnumerateObject())
                    {
                        avatars.Add(MapContactToAvatar(contact.Name, contact.Value));
                    }
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"UrbitOASIS: Loaded {avatars.Count} avatar(s) from contact-store.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error loading all avatars: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!await EnsureAuthenticatedAsync())
                {
                    OASISErrorHandling.HandleError(ref result,
                        "UrbitOASIS: Authentication required to save an avatar. Provide the ship's +code login code.");
                    return result;
                }

                string ship = avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.UrbitOASIS)
                    ? avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.UrbitOASIS]
                    : $"~{avatar.Username}";

                // Poke contact-store to set-profile
                var profileData = new
                {
                    nickname = $"{avatar.FirstName} {avatar.LastName}".Trim(),
                    bio = avatar.Description ?? string.Empty,
                    status = string.Empty,
                    color = "0x0",
                    avatar = (string?)null,
                    cover = (string?)null,
                    groups = Array.Empty<string>()
                };

                bool ok = await PokeAsync(ship.TrimStart('~'), "contact-store", "contact-action",
                    new { edit = new { ship, edit = new { profileData } } });

                if (ok)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = $"UrbitOASIS: Avatar saved for ship '{ship}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: Poke to contact-store failed for ship '{ship}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: Deleting ship identities is not supported — Urbit ships are permanent cryptographic identities.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: Deleting ship identities is not supported — Urbit ships are permanent cryptographic identities.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: Deleting ship identities is not supported — Urbit ships are permanent cryptographic identities.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => DeleteAvatarByEmailAsync(email, softDelete).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        /// <summary>providerKey = "~ship/graph-name/index" e.g. "~sampel-palnet/my-notebook/1"</summary>
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // providerKey format: ~ship/graph-name/index
                var parts = providerKey.TrimStart('/').Split('/');
                if (parts.Length < 3)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: providerKey must be in format '~ship/graph-name/node-index'. Got: '{providerKey}'");
                    return result;
                }

                string ship = parts[0].StartsWith("~") ? parts[0] : $"~{parts[0]}";
                string graphName = parts[1];
                string nodeIndex = string.Join("/", parts, 2, parts.Length - 2);

                var el = await ScryAsync("graph-store", $"node/{ship}/{graphName}/{nodeIndex}");
                if (!el.HasValue || el.Value.ValueKind == JsonValueKind.Null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: No graph-store node found at '{providerKey}'.");
                    return result;
                }

                result.Result = MapNodeToHolon(providerKey, ship, graphName, el.Value);
                result.IsError = false;
                result.Message = $"UrbitOASIS: Holon loaded from graph-store node '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error loading holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: Holons are keyed by graph-store path (~ship/graph/index); GUID lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var keysEl = await ScryAsync("graph-store", "keys");
                if (!keysEl.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "UrbitOASIS: No graph-store keys found.");
                    return result;
                }

                var holons = new List<IHolon>();

                // keys returns: {"graph-update": {"keys": [{"ship": "~ship", "name": "graph-name"}, ...]}}
                JsonElement keysList = default;
                if (keysEl.Value.TryGetProperty("graph-update", out var gu) &&
                    gu.TryGetProperty("keys", out keysList))
                {
                    foreach (var key in keysList.EnumerateArray())
                    {
                        string ship = key.TryGetProperty("ship", out var s) ? s.GetString() ?? "" : "";
                        string name = key.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(ship) || string.IsNullOrEmpty(name)) continue;

                        // Load up to 20 nodes from each graph
                        var graphEl = await ScryAsync("graph-store", $"graph/{ship}/{name}");
                        if (!graphEl.HasValue) continue;

                        if (graphEl.Value.TryGetProperty("graph-update", out var graphUpdate) &&
                            graphUpdate.TryGetProperty("add-graph", out var addGraph) &&
                            addGraph.TryGetProperty("graph", out var graph))
                        {
                            int count = 0;
                            foreach (var node in graph.EnumerateObject())
                            {
                                if (count++ >= 20) break;
                                string providerKey = $"{ship}/{name}/{node.Name}";
                                holons.Add(MapNodeToHolon(providerKey, ship, name, node.Value));
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"UrbitOASIS: Loaded {holons.Count} holon(s) from graph-store.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: LoadHolonsForParent by GUID is not supported. Use the string overload with parent ship @p.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // providerKey = ~ship/graph-name  (loads all nodes in that graph)
                var parts = providerKey.TrimStart('/').Split('/');
                if (parts.Length < 2)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: providerKey must be '~ship/graph-name' for LoadHolonsForParent. Got: '{providerKey}'");
                    return result;
                }

                string ship = parts[0].StartsWith("~") ? parts[0] : $"~{parts[0]}";
                string graphName = parts[1];

                var graphEl = await ScryAsync("graph-store", $"graph/{ship}/{graphName}");
                var holons = new List<IHolon>();

                if (graphEl.HasValue &&
                    graphEl.Value.TryGetProperty("graph-update", out var gu) &&
                    gu.TryGetProperty("add-graph", out var ag) &&
                    ag.TryGetProperty("graph", out var graph))
                {
                    foreach (var node in graph.EnumerateObject())
                    {
                        string pk = $"{ship}/{graphName}/{node.Name}";
                        holons.Add(MapNodeToHolon(pk, ship, graphName, node.Value));
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"UrbitOASIS: Loaded {holons.Count} holon(s) from graph '{ship}/{graphName}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error loading holons for parent '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!await EnsureAuthenticatedAsync())
                {
                    OASISErrorHandling.HandleError(ref result,
                        "UrbitOASIS: Authentication required to save a holon. Provide the ship's +code login code.");
                    return result;
                }

                // Derive ship and graph-name from the holon's parent provider key or defaults
                string ship = "~";
                string graphName = "oasis-holons";

                if (holon.ParentHolonId != Guid.Empty &&
                    holon.MetaData != null &&
                    holon.MetaData.ContainsKey("UrbitGraph"))
                {
                    var graphPath = holon.MetaData["UrbitGraph"].ToString() ?? string.Empty;
                    var parts2 = graphPath.TrimStart('/').Split('/');
                    if (parts2.Length >= 2) { ship = parts2[0]; graphName = parts2[1]; }
                }

                long index = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var addNodesPayload = new
                {
                    @ref = new
                    {
                        term = "graph-update"
                    },
                    action = new
                    {
                        @add_nodes = new
                        {
                            resource = new { ship = ship.TrimStart('~'), name = graphName },
                            nodes = new Dictionary<string, object>
                            {
                                [$"/{index}"] = new
                                {
                                    post = new
                                    {
                                        author = ship,
                                        index = $"/{index}",
                                        time_sent = index,
                                        contents = new[]
                                        {
                                            new { text = $"{holon.Name}: {holon.Description}" }
                                        },
                                        hash = (string?)null,
                                        signatures = Array.Empty<object>()
                                    },
                                    children = (object?)null
                                }
                            }
                        }
                    }
                };

                bool ok = await PokeAsync(ship.TrimStart('~'), "graph-store", "graph-update", addNodesPayload);

                if (ok)
                {
                    string providerKey = $"{ship}/{graphName}/{index}";
                    if (holon.ProviderUniqueStorageKey == null)
                        holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.UrbitOASIS] = providerKey;

                    result.Result = holon;
                    result.IsError = false;
                    result.Message = $"UrbitOASIS: Holon saved to graph-store at '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: Poke to graph-store failed when saving holon '{holon.Name}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error saving holon '{holon.Name}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var errors = new List<string>();

            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError)
                    errors.Add(r.Message);
                else
                    saved.Add(r.Result!);
            }

            result.Result = saved;
            if (errors.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Join("; ", errors);
            }
            else
            {
                result.IsError = false;
                result.Message = $"UrbitOASIS: {saved.Count} holon(s) saved.";
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: DeleteHolon by GUID is not supported. Use the string overload with the graph-store path (~ship/graph/index).");
            return await Task.FromResult(result);
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
            => DeleteHolonSoftAsync(id, softDelete).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!await EnsureAuthenticatedAsync())
                {
                    OASISErrorHandling.HandleError(ref result,
                        "UrbitOASIS: Authentication required to delete a holon.");
                    return result;
                }

                // providerKey = ~ship/graph-name/index
                var parts = providerKey.TrimStart('/').Split('/');
                if (parts.Length < 3)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"UrbitOASIS: providerKey must be '~ship/graph-name/node-index' to delete. Got: '{providerKey}'");
                    return result;
                }

                string ship = parts[0].StartsWith("~") ? parts[0] : $"~{parts[0]}";
                string graphName = parts[1];
                string nodeIndex = "/" + string.Join("/", parts, 2, parts.Length - 2);

                var removeNodesPayload = new
                {
                    @ref = new { term = "graph-update" },
                    action = new
                    {
                        remove_nodes = new
                        {
                            resource = new { ship = ship.TrimStart('~'), name = graphName },
                            indices = new[] { nodeIndex }
                        }
                    }
                };

                bool ok = await PokeAsync(ship.TrimStart('~'), "graph-store", "graph-update", removeNodesPayload);
                result.Result = ok;
                result.IsError = !ok;
                result.Message = ok
                    ? $"UrbitOASIS: Holon deleted from graph-store node '{providerKey}'."
                    : $"UrbitOASIS: Poke to remove graph-store node '{providerKey}' failed.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"UrbitOASIS: Error deleting holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(string providerKey, bool softDelete = true)
            => DeleteHolonSoftAsync(providerKey, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────



        // ─── AvatarDetail (not applicable) ───────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: LoadAvatarDetail by GUID is not supported. Use LoadAvatarByProviderKey with the ship's @p address.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: LoadAvatarDetailByUsername is not supported. Use LoadAvatarByProviderKey with the ship's @p address.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: LoadAvatarDetailByEmail is not supported. Urbit does not use email addresses.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: LoadAllAvatarDetails is not supported. Use LoadAllAvatars which returns contact-store profiles.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "UrbitOASIS: SaveAvatarDetail is not supported. Use SaveAvatar to update the contact-store profile.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
            => SaveAvatarDetailAsync(avatarDetail).Result;

        // ─── Mapping helpers ──────────────────────────────────────────────────────

        private static Avatar MapContactToAvatar(string shipName, JsonElement contact)
        {
            var avatar = new Avatar
            {
                Username = shipName,
                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>
                {
                    [Core.Enums.ProviderType.UrbitOASIS] = shipName
                },
                MetaData = new Dictionary<string, object>
                {
                    ["UrbitShip"] = shipName
                }
            };

            // contact-store profile fields
            if (contact.TryGetProperty("nickname", out var nn) && nn.ValueKind == JsonValueKind.String)
            {
                string nickname = nn.GetString() ?? string.Empty;
                var nameParts = nickname.Split(' ', 2);
                avatar.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                avatar.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }

            if (contact.TryGetProperty("bio", out var bio) && bio.ValueKind == JsonValueKind.String)
                avatar.Description = bio.GetString();

            if (contact.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
                avatar.MetaData["UrbitStatus"] = status.GetString() ?? string.Empty;

            if (contact.TryGetProperty("color", out var color) && color.ValueKind == JsonValueKind.String)
                avatar.MetaData["UrbitColor"] = color.GetString() ?? string.Empty;

            return avatar;
        }

        private static Holon MapNodeToHolon(string providerKey, string ship, string graphName, JsonElement node)
        {
            var holon = new Holon
            {
                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>
                {
                    [Core.Enums.ProviderType.UrbitOASIS] = providerKey
                },
                MetaData = new Dictionary<string, object>
                {
                    ["UrbitShip"] = ship,
                    ["UrbitGraph"] = $"{ship}/{graphName}"
                }
            };

            // graph-store node → post
            if (node.TryGetProperty("post", out var post))
            {
                if (post.TryGetProperty("author", out var author))
                    holon.MetaData["UrbitAuthor"] = author.GetString() ?? string.Empty;

                if (post.TryGetProperty("time-sent", out var ts))
                    holon.CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(ts.GetInt64()).UtcDateTime;

                // contents is an array of content blocks; extract text blocks
                if (post.TryGetProperty("contents", out var contents))
                {
                    var texts = new List<string>();
                    foreach (var block in contents.EnumerateArray())
                    {
                        if (block.TryGetProperty("text", out var text))
                            texts.Add(text.GetString() ?? string.Empty);
                    }
                    string combined = string.Join("\n", texts);
                    // Use first 120 chars as Name, rest as Description
                    if (combined.Length <= 120)
                    {
                        holon.Name = combined;
                        holon.Description = combined;
                    }
                    else
                    {
                        holon.Name = combined.Substring(0, 120).TrimEnd() + "…";
                        holon.Description = combined;
                    }
                }
            }

            if (string.IsNullOrEmpty(holon.Name))
                holon.Name = providerKey;

            return holon;
        }

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            var avatar = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message);
            else
                result.Result = loaded.Result;

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData != null
                    && holon.MetaData.TryGetValue(metaKey, out var value)
                    && value?.ToString() == metaValue)
                    matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
            {
                result.Result = new List<IHolon>(all.Result);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData == null) continue;

                var matched = 0;
                foreach (var pair in metaKeyValuePairs)
                {
                    if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                        matched++;
                }

                var isMatch = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? matched == metaKeyValuePairs.Count
                    : matched > 0;

                if (isMatch) matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = true;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var owned = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.CreatedByAvatarId == avatarId)
                    owned.Add(holon);
            }

            result.Result = owned;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }


        // ─── Email lookup ────────────────────────────────────────────────────────
        // Urbit identity is the @p ship name; email is OASIS-side profile data held
        // in the contact-store entry, so this resolves by scanning the contact set.

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var all = await LoadAllAvatarsAsync(version);
                if (all.IsError || all.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, all.Message);
                    return result;
                }

                foreach (var avatar in all.Result)
                {
                    if (avatar != null && !avatar.IsDeleted
                        && string.Equals(avatar.Email, avatarEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Result = avatar;
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"UrbitOASIS: No avatar found with email '{avatarEmail}'.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"UrbitOASIS: LoadAvatarByEmailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        // ─── Delete holon by provider key ────────────────────────────────────────
        // The Urbit provider key is the graph-store resource path; resolve it to the
        // holon then reuse the id-based delete so the same poke path is taken.

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await LoadHolonAsync(providerKey);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, holon.Message);
                    return result;
                }
                return await DeleteHolonAsync(holon.Result.Id);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"UrbitOASIS: DeleteHolonAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"UrbitOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }

    }
}
