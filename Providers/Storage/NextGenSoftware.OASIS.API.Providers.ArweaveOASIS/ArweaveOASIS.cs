using System;
using System.Collections.Generic;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    /// <summary>
    /// OASIS storage provider backed by Arweave — a permanent, decentralised storage network.
    /// Data is stored as immutable Arweave transactions tagged with OASIS metadata.
    /// Because Arweave is immutable, "save" always appends a new transaction; "load" retrieves
    /// the most-recent transaction for a given OASIS ID via the Arweave GraphQL endpoint.
    /// Delete operations are unsupported by design.
    /// </summary>
    public class ArweaveOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        // ── Constants ───────────────────────────────────────────────────────────────
        private const string GraphQlEndpoint    = "https://arweave.net/graphql";
        private const string TurboUploadEndpoint = "https://uploader.ardrive.io/v1/tx";
        private const string TagApp     = "OASIS";
        private const string TagHolon   = "Holon";
        private const string TagAvatar  = "Avatar";
        private const string TagAvatarDetail = "AvatarDetail";

        // ── Fields ──────────────────────────────────────────────────────────────────
        private readonly HttpClient _httpClient;
        private readonly string     _gatewayUrl;
        private readonly string?    _walletJwk;
        private bool                _isActivated;

        // ── Constructor ─────────────────────────────────────────────────────────────
        public ArweaveOASIS(string? walletJwk = null, string gatewayUrl = "https://arweave.net")
        {
            _walletJwk   = walletJwk;
            _gatewayUrl  = gatewayUrl.TrimEnd('/');
            _httpClient  = new HttpClient();

            ProviderName        = "ArweaveOASIS";
            ProviderDescription = "Arweave permanent decentralised storage provider for OASIS";
            ProviderType        = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArweaveOASIS);
            ProviderCategory    = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private static string ToBase64Url(string s) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
                   .TrimEnd('=')
                   .Replace('+', '-')
                   .Replace('/', '_');

        /// <summary>Upload JSON data to Arweave via Turbo and return the transaction ID.</summary>
        private async Task<string> UploadJsonAsync(string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            // Turbo unsigned upload API — no wallet signing required for small payloads
            var response = await _httpClient.PostAsync(TurboUploadEndpoint, content);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement.GetProperty("id").GetString()
                   ?? throw new InvalidOperationException("Arweave upload response did not contain an 'id' field.");
        }

        /// <summary>Execute a GraphQL query against the Arweave network and return the root element.</summary>
        private async Task<JsonElement> GraphQlAsync(string query)
        {
            var body    = JsonSerializer.Serialize(new { query });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(GraphQlEndpoint, content);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            // Return a clone so the document lifetime doesn't matter
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Query GraphQL for the most-recent Arweave TX with the given OASIS tags and return
        /// the TX id, or null if nothing was found.
        /// </summary>
        private async Task<string?> FindLatestTxIdAsync(string type, string oasisId)
        {
            var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",     values: [""{TagApp}""]  }},
      {{ name: ""Type"",    values: [""{type}""]    }},
      {{ name: ""OasisId"", values: [""{oasisId}""] }}
    ],
    first: 1,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";

            var root  = await GraphQlAsync(query);
            var edges = root.GetProperty("data")
                            .GetProperty("transactions")
                            .GetProperty("edges");

            if (edges.GetArrayLength() == 0)
                return null;

            return edges[0].GetProperty("node").GetProperty("id").GetString();
        }

        /// <summary>Download and deserialise the data stored in a given Arweave TX.</summary>
        private async Task<T?> FetchTxDataAsync<T>(string txId)
        {
            var response = await _httpClient.GetAsync($"{_gatewayUrl}/{txId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>Build a GraphQL query for multiple IDs of the same type and return all TX ids.</summary>
        private async Task<IEnumerable<string>> FindAllTxIdsForTypeAsync(string type)
        {
            var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",  values: [""{TagApp}""] }},
      {{ name: ""Type"", values: [""{type}""]  }}
    ],
    first: 100,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id tags {{ name value }} }} }}
  }}
}}";

            var root  = await GraphQlAsync(query);
            var edges = root.GetProperty("data")
                            .GetProperty("transactions")
                            .GetProperty("edges");

            var ids   = new List<string>();
            foreach (var edge in edges.EnumerateArray())
            {
                var id = edge.GetProperty("node").GetProperty("id").GetString();
                if (id != null) ids.Add(id);
            }
            return ids;
        }

        // ── Activate / Deactivate ───────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated)
                {
                    result.Result  = true;
                    result.Message = "ArweaveOASIS provider is already activated.";
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_gatewayUrl}/info");
                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"ArweaveOASIS: failed to reach gateway {_gatewayUrl}/info — HTTP {(int)response.StatusCode}.");
                    return result;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc    = await JsonDocument.ParseAsync(stream);
                var network = doc.RootElement.TryGetProperty("network", out var netProp)
                              ? netProp.GetString() ?? string.Empty
                              : string.Empty;

                if (!network.Contains("arweave", StringComparison.OrdinalIgnoreCase))
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"ArweaveOASIS: gateway did not identify as Arweave (network='{network}').");
                    return result;
                }

                _isActivated   = true;
                IsProviderActivated = true;
                result.Result  = true;
                result.Message = $"ArweaveOASIS activated successfully (network='{network}').";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS: exception during activation — {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() =>
            ActivateProviderAsync().GetAwaiter().GetResult();

        public Task<OASISResult<bool>> DeactivateProviderAsync()
        {
            _isActivated        = false;
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "ArweaveOASIS deactivated." });
        }

        public OASISResult<bool> DeactivateProvider() =>
            DeactivateProviderAsync().GetAwaiter().GetResult();

        // ── Holons ──────────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(
            IHolon holon,
            bool saveChildren         = true,
            bool recursive            = true,
            int  maxChildDepth        = 0,
            bool continueOnError      = true,
            bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var json  = JsonSerializer.Serialize(holon);
                var txId  = await UploadJsonAsync(json);

                // Store the Arweave TX id as provider key so callers can reference it
                holon.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = txId;

                result.Result  = holon;
                result.IsSaved = true;
                result.Message = $"Holon saved to Arweave. TxId={txId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.SaveHolonAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(
            IHolon holon,
            bool saveChildren         = true,
            bool recursive            = true,
            int  maxChildDepth        = 0,
            bool continueOnError      = true,
            bool saveChildrenOnProvider = false) =>
            SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(
            IEnumerable<IHolon> holons,
            bool saveChildren         = true,
            bool recursive            = true,
            int  maxChildDepth        = 0,
            int  curentChildDepth     = 0,
            bool continueOnError      = true,
            bool saveChildrenOnProvider = false)
        {
            var result  = new OASISResult<IEnumerable<IHolon>>();
            var saved   = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth,
                                                 continueOnError, saveChildrenOnProvider);
                    if (r.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"ArweaveOASIS.SaveHolonsAsync: error saving holon {holon.Id} — {r.Message}");
                        return result;
                    }
                    if (!r.IsError && r.Result != null)
                        saved.Add(r.Result);
                }
                result.Result  = saved;
                result.IsSaved = true;
                result.Message = $"Saved {saved.Count} holon(s) to Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.SaveHolonsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(
            IEnumerable<IHolon> holons,
            bool saveChildren         = true,
            bool recursive            = true,
            int  maxChildDepth        = 0,
            int  curentChildDepth     = 0,
            bool continueOnError      = true,
            bool saveChildrenOnProvider = false) =>
            SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth,
                            curentChildDepth, continueOnError, saveChildrenOnProvider)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(
            Guid id,
            bool loadChildren           = true,
            bool recursive              = true,
            int  maxChildDepth          = 0,
            bool continueOnError        = true,
            bool loadChildrenFromProvider = false,
            int  version                = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txId = await FindLatestTxIdAsync(TagHolon, id.ToString());
                if (txId == null)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for Holon id={id}.";
                    return result;
                }

                var holon = await FetchTxDataAsync<Holon>(txId);
                result.Result   = holon;
                result.IsLoaded = holon != null;
                result.Message  = holon != null
                    ? $"Holon loaded from Arweave TxId={txId}."
                    : "Holon data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadHolonAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(
            Guid id,
            bool loadChildren           = true,
            bool recursive              = true,
            int  maxChildDepth          = 0,
            bool continueOnError        = true,
            bool loadChildrenFromProvider = false,
            int  version                = 0) =>
            LoadHolonAsync(id, loadChildren, recursive, maxChildDepth,
                           continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(
            string providerKey,
            bool loadChildren           = true,
            bool recursive              = true,
            int  maxChildDepth          = 0,
            bool continueOnError        = true,
            bool loadChildrenFromProvider = false,
            int  version                = 0)
        {
            // providerKey is the Arweave TX id when stored via this provider
            var result = new OASISResult<IHolon>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var holon = await FetchTxDataAsync<Holon>(providerKey);
                result.Result   = holon;
                result.IsLoaded = holon != null;
                result.Message  = holon != null
                    ? $"Holon loaded from Arweave TxId={providerKey}."
                    : "Holon data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadHolonAsync(providerKey): {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(
            string providerKey,
            bool loadChildren           = true,
            bool recursive              = true,
            int  maxChildDepth          = 0,
            bool continueOnError        = true,
            bool loadChildrenFromProvider = false,
            int  version                = 0) =>
            LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth,
                           continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txIds  = await FindAllTxIdsForTypeAsync(TagHolon);
                var holons = new List<IHolon>();

                foreach (var txId in txIds)
                {
                    var holon = await FetchTxDataAsync<Holon>(txId);
                    if (holon == null) continue;
                    if (type == HolonType.All || holon.HolonType == type)
                        holons.Add(holon);
                }

                result.Result   = holons;
                result.IsLoaded = true;
                result.Message  = $"Loaded {holons.Count} holon(s) from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAllHolonsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0) =>
            LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth,
                               curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(
            Guid      id,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",      values: [""{TagApp}""]         }},
      {{ name: ""Type"",     values: [""{TagHolon}""]       }},
      {{ name: ""ParentId"", values: [""{id}""]             }}
    ],
    first: 100,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                var holons = new List<IHolon>();
                foreach (var edge in edges.EnumerateArray())
                {
                    var txId  = edge.GetProperty("node").GetProperty("id").GetString();
                    if (txId == null) continue;
                    var holon = await FetchTxDataAsync<Holon>(txId);
                    if (holon == null) continue;
                    if (type == HolonType.All || holon.HolonType == type)
                        holons.Add(holon);
                }

                result.Result   = holons;
                result.IsLoaded = true;
                result.Message  = $"Loaded {holons.Count} child holon(s) for parent {id} from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadHolonsForParentAsync(Guid): {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(
            Guid      id,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0) =>
            LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth,
                                     curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(
            string    providerKey,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0)
        {
            if (Guid.TryParse(providerKey, out var guid))
                return LoadHolonsForParentAsync(guid, type, loadChildren, recursive, maxChildDepth,
                                                curentChildDepth, continueOnError,
                                                loadChildrenFromProvider, version);

            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true,
                Message = "ArweaveOASIS.LoadHolonsForParent(providerKey): providerKey must be a valid GUID."
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(
            string    providerKey,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                                     curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            string    metaKey,
            string    metaValue,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",    values: [""{TagApp}""]    }},
      {{ name: ""Type"",   values: [""{TagHolon}""]  }},
      {{ name: ""{metaKey}"", values: [""{metaValue}""] }}
    ],
    first: 100,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                var holons = new List<IHolon>();
                foreach (var edge in edges.EnumerateArray())
                {
                    var txId  = edge.GetProperty("node").GetProperty("id").GetString();
                    if (txId == null) continue;
                    var holon = await FetchTxDataAsync<Holon>(txId);
                    if (holon == null) continue;
                    if (type == HolonType.All || holon.HolonType == type)
                        holons.Add(holon);
                }

                result.Result   = holons;
                result.IsLoaded = true;
                result.Message  = $"Loaded {holons.Count} holon(s) matching {metaKey}={metaValue} from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadHolonsByMetaDataAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            string    metaKey,
            string    metaValue,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0) =>
            LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth,
                                      curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode  metaKeyValuePairMatchMode,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0)
        {
            // For simplicity, run each key/value as a separate query and intersect/union as requested
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!metaKeyValuePairs.Any())
                {
                    result.Result   = Enumerable.Empty<IHolon>();
                    result.IsLoaded = true;
                    result.Message  = "No meta key/value pairs supplied.";
                    return result;
                }

                List<IHolon>? accumulated = null;

                foreach (var (key, value) in metaKeyValuePairs)
                {
                    var r = await LoadHolonsByMetaDataAsync(key, value, type, loadChildren, recursive,
                                                            maxChildDepth, curentChildDepth,
                                                            continueOnError, loadChildrenFromProvider, version);
                    if (r.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"ArweaveOASIS.LoadHolonsByMetaData: {r.Message}");
                        return result;
                    }

                    var current = r.Result?.ToList() ?? new List<IHolon>();

                    if (accumulated == null)
                    {
                        accumulated = current;
                    }
                    else if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                    {
                        var currentIds = current.Select(h => h.Id).ToHashSet();
                        accumulated = accumulated.Where(h => currentIds.Contains(h.Id)).ToList();
                    }
                    else // Or
                    {
                        var existingIds = accumulated.Select(h => h.Id).ToHashSet();
                        accumulated.AddRange(current.Where(h => !existingIds.Contains(h.Id)));
                    }
                }

                result.Result   = accumulated ?? new List<IHolon>();
                result.IsLoaded = true;
                result.Message  = $"Loaded {(accumulated?.Count ?? 0)} holon(s) from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadHolonsByMetaDataAsync(dict): {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode  metaKeyValuePairMatchMode,
            HolonType type              = HolonType.All,
            bool      loadChildren      = true,
            bool      recursive         = true,
            int       maxChildDepth     = 0,
            int       curentChildDepth  = 0,
            bool      continueOnError   = true,
            bool      loadChildrenFromProvider = false,
            int       version           = 0) =>
            LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type,
                                      loadChildren, recursive, maxChildDepth, curentChildDepth,
                                      continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        // Arweave is immutable — delete is not supported
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) =>
            Task.FromResult(ImmutableDeleteResult<IHolon>());

        public override OASISResult<IHolon> DeleteHolon(Guid id) =>
            ImmutableDeleteResult<IHolon>();

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) =>
            Task.FromResult(ImmutableDeleteResult<IHolon>());

        public override OASISResult<IHolon> DeleteHolon(string providerKey) =>
            ImmutableDeleteResult<IHolon>();

        // ── Avatars ─────────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var json = JsonSerializer.Serialize(avatar);
                var txId = await UploadJsonAsync(json);

                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = txId;

                result.Result  = avatar;
                result.IsSaved = true;
                result.Message = $"Avatar saved to Arweave. TxId={txId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.SaveAvatarAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) =>
            SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var json = JsonSerializer.Serialize(avatarDetail);
                var txId = await UploadJsonAsync(json);

                avatarDetail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = txId;

                result.Result  = avatarDetail;
                result.IsSaved = true;
                result.Message = $"AvatarDetail saved to Arweave. TxId={txId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.SaveAvatarDetailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txId = await FindLatestTxIdAsync(TagAvatar, id.ToString());
                if (txId == null)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for Avatar id={id}.";
                    return result;
                }

                var avatar = await FetchTxDataAsync<Avatar>(txId);
                result.Result   = avatar;
                result.IsLoaded = avatar != null;
                result.Message  = avatar != null
                    ? $"Avatar loaded from Arweave TxId={txId}."
                    : "Avatar data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(
            string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txId = await FindLatestTxIdAsync("AvatarByUsername",
                    ToBase64Url(avatarUsername));
                if (txId == null)
                {
                    // Fall back: GraphQL tag query by Username value
                    var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",      values: [""{TagApp}""]      }},
      {{ name: ""Type"",     values: [""{TagAvatar}""]   }},
      {{ name: ""Username"", values: [""{avatarUsername}""] }}
    ],
    first: 1,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                    var root  = await GraphQlAsync(query);
                    var edges = root.GetProperty("data")
                                    .GetProperty("transactions")
                                    .GetProperty("edges");
                    txId = edges.GetArrayLength() > 0
                        ? edges[0].GetProperty("node").GetProperty("id").GetString()
                        : null;
                }

                if (txId == null)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for Avatar username={avatarUsername}.";
                    return result;
                }

                var avatar = await FetchTxDataAsync<Avatar>(txId);
                result.Result   = avatar;
                result.IsLoaded = avatar != null;
                result.Message  = avatar != null
                    ? $"Avatar loaded from Arweave TxId={txId}."
                    : "Avatar data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(
            string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(
            string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",   values: [""{TagApp}""]     }},
      {{ name: ""Type"",  values: [""{TagAvatar}""]  }},
      {{ name: ""Email"", values: [""{avatarEmail}""] }}
    ],
    first: 1,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                if (edges.GetArrayLength() == 0)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for Avatar email={avatarEmail}.";
                    return result;
                }

                var txId   = edges[0].GetProperty("node").GetProperty("id").GetString()!;
                var avatar = await FetchTxDataAsync<Avatar>(txId);
                result.Result   = avatar;
                result.IsLoaded = avatar != null;
                result.Message  = avatar != null
                    ? $"Avatar loaded from Arweave TxId={txId}."
                    : "Avatar data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarByEmailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(
            string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(
            string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var avatar = await FetchTxDataAsync<Avatar>(providerKey);
                result.Result   = avatar;
                result.IsLoaded = avatar != null;
                result.Message  = avatar != null
                    ? $"Avatar loaded from Arweave TxId={providerKey}."
                    : "Avatar data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarByProviderKeyAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(
            string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txIds   = await FindAllTxIdsForTypeAsync(TagAvatar);
                var avatars = new List<IAvatar>();

                foreach (var txId in txIds)
                {
                    var avatar = await FetchTxDataAsync<Avatar>(txId);
                    if (avatar != null) avatars.Add(avatar);
                }

                result.Result   = avatars;
                result.IsLoaded = true;
                result.Message  = $"Loaded {avatars.Count} avatar(s) from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAllAvatarsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        // ── AvatarDetail ────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(
            Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txId = await FindLatestTxIdAsync(TagAvatarDetail, id.ToString());
                if (txId == null)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for AvatarDetail id={id}.";
                    return result;
                }

                var detail = await FetchTxDataAsync<AvatarDetail>(txId);
                result.Result   = detail;
                result.IsLoaded = detail != null;
                result.Message  = detail != null
                    ? $"AvatarDetail loaded from Arweave TxId={txId}."
                    : "AvatarDetail data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarDetailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(
            string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",   values: [""{TagApp}""]         }},
      {{ name: ""Type"",  values: [""{TagAvatarDetail}""] }},
      {{ name: ""Email"", values: [""{avatarEmail}""]     }}
    ],
    first: 1,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                if (edges.GetArrayLength() == 0)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for AvatarDetail email={avatarEmail}.";
                    return result;
                }

                var txId   = edges[0].GetProperty("node").GetProperty("id").GetString()!;
                var detail = await FetchTxDataAsync<AvatarDetail>(txId);
                result.Result   = detail;
                result.IsLoaded = detail != null;
                result.Message  = detail != null
                    ? $"AvatarDetail loaded from Arweave TxId={txId}."
                    : "AvatarDetail data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarDetailByEmailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(
            string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(
            string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",      values: [""{TagApp}""]         }},
      {{ name: ""Type"",     values: [""{TagAvatarDetail}""] }},
      {{ name: ""Username"", values: [""{avatarUsername}""]  }}
    ],
    first: 1,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                if (edges.GetArrayLength() == 0)
                {
                    result.IsLoaded = false;
                    result.Message  = $"No Arweave transaction found for AvatarDetail username={avatarUsername}.";
                    return result;
                }

                var txId   = edges[0].GetProperty("node").GetProperty("id").GetString()!;
                var detail = await FetchTxDataAsync<AvatarDetail>(txId);
                result.Result   = detail;
                result.IsLoaded = detail != null;
                result.Message  = detail != null
                    ? $"AvatarDetail loaded from Arweave TxId={txId}."
                    : "AvatarDetail data could not be deserialised.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAvatarDetailByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(
            string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(
            int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var txIds   = await FindAllTxIdsForTypeAsync(TagAvatarDetail);
                var details = new List<IAvatarDetail>();

                foreach (var txId in txIds)
                {
                    var detail = await FetchTxDataAsync<AvatarDetail>(txId);
                    if (detail != null) details.Add(detail);
                }

                result.Result   = details;
                result.IsLoaded = true;
                result.Message  = $"Loaded {details.Count} avatar detail(s) from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.LoadAllAvatarDetailsAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(
            int version = 0) =>
            LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        // ── Delete (immutable — not supported) ──────────────────────────────────────

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) =>
            Task.FromResult(ImmutableDeleteResult<bool>());

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            ImmutableDeleteResult<bool>();

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) =>
            Task.FromResult(ImmutableDeleteResult<bool>());

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            ImmutableDeleteResult<bool>();

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) =>
            Task.FromResult(ImmutableDeleteResult<bool>());

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            ImmutableDeleteResult<bool>();

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) =>
            Task.FromResult(ImmutableDeleteResult<bool>());

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            ImmutableDeleteResult<bool>();

        // ── Search ──────────────────────────────────────────────────────────────────

        public override Task<OASISResult<ISearchResults>> SearchAsync(
            ISearchParams searchParams,
            bool loadChildren      = true,
            bool recursive         = true,
            int  maxChildDepth     = 0,
            bool continueOnError   = true,
            int  version           = 0)
        {
            return Task.FromResult(new OASISResult<ISearchResults>
            {
                IsError = true,
                Message = "ArweaveOASIS: full-text search is not supported. Use GraphQL tag queries via LoadHolonsByMetaDataAsync."
            });
        }

        public override OASISResult<ISearchResults> Search(
            ISearchParams searchParams,
            bool loadChildren      = true,
            bool recursive         = true,
            int  maxChildDepth     = 0,
            bool continueOnError   = true,
            int  version           = 0) =>
            SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version)
                .GetAwaiter().GetResult();

        // ── Import / Export ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                var r = await SaveHolonsAsync(holons);
                result.Result  = !r.IsError;
                result.IsError = r.IsError;
                result.Message = r.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.ImportAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) =>
            ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(
            Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var _activateError = await EnsureActivatedAsync();
                if (_activateError != null) { OASISErrorHandling.HandleError(ref result, _activateError); return result; }

                var query = $@"
{{
  transactions(
    tags: [
      {{ name: ""App"",      values: [""{TagApp}""]     }},
      {{ name: ""AvatarId"", values: [""{avatarId}""]   }}
    ],
    first: 100,
    sort: HEIGHT_DESC
  ) {{
    edges {{ node {{ id }} }}
  }}
}}";
                var root  = await GraphQlAsync(query);
                var edges = root.GetProperty("data")
                                .GetProperty("transactions")
                                .GetProperty("edges");

                var holons = new List<IHolon>();
                foreach (var edge in edges.EnumerateArray())
                {
                    var txId  = edge.GetProperty("node").GetProperty("id").GetString();
                    if (txId == null) continue;
                    var holon = await FetchTxDataAsync<Holon>(txId);
                    if (holon != null) holons.Add(holon);
                }

                result.Result   = holons;
                result.IsLoaded = true;
                result.Message  = $"Exported {holons.Count} record(s) for avatar {avatarId} from Arweave.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.ExportAllDataForAvatarByIdAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(
            Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(
            string avatarUsername, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true,
                Message = "ArweaveOASIS.ExportAllDataForAvatarByUsername: use ExportAllDataForAvatarById instead."
            });
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(
            string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version)
                .GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(
            string avatarEmailAddress, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true,
                Message = "ArweaveOASIS.ExportAllDataForAvatarByEmail: use ExportAllDataForAvatarById instead."
            });
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(
            string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holonResult = await LoadAllHolonsAsync();
                result.Result   = holonResult.Result;
                result.IsError  = holonResult.IsError;
                result.IsLoaded = holonResult.IsLoaded;
                result.Message  = holonResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"ArweaveOASIS.ExportAllAsync: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) =>
            ExportAllAsync(version).GetAwaiter().GetResult();

        // ── Private helpers ─────────────────────────────────────────────────────────

        private static OASISResult<T> ImmutableDeleteResult<T>() =>
            new OASISResult<T>
            {
                IsError = true,
                Message = "Arweave is a permanent storage network — data cannot be deleted by design."
            };

        /// <summary>
        /// Attempts to activate the provider and returns an error message if it fails,
        /// or null on success. Callers check the return value and set their result accordingly.
        /// </summary>
        private async Task<string?> EnsureActivatedAsync()
        {
            var activateResult = await ActivateProviderAsync();
            return activateResult.IsError
                ? $"ArweaveOASIS: failed to activate provider — {activateResult.Message}"
                : null;
        }
    }
}
