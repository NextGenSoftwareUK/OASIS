using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS
{
    /// <summary>
    /// OASIS provider for the Filecoin decentralised storage network (https://filecoin.io).
    /// Uses the Lotus JSON-RPC API via the public Glif.io endpoint.
    /// Filecoin addresses (f1/f3/t1) → OASIS Avatars; storage deals / CIDs → OASIS Holons.
    /// Set FILECOIN_RPC_URL (default: https://api.node.glif.io/rpc/v1) and optionally
    /// FILECOIN_TOKEN for authenticated calls.
    /// Holon provider key = CID string (e.g. "bafy..."); Avatar provider key = Filecoin address.
    /// </summary>
    public class FilecoinOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _rpcUrl;
        private readonly string _token;
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private int _rpcId;

        public FilecoinOASIS(string rpcUrl = null, string token = null)
        {
            _rpcUrl = rpcUrl ?? Environment.GetEnvironmentVariable("FILECOIN_RPC_URL")
                      ?? "https://api.node.glif.io/rpc/v1";
            _token = token ?? Environment.GetEnvironmentVariable("FILECOIN_TOKEN") ?? string.Empty;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (!string.IsNullOrEmpty(_token))
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            ProviderName = "FilecoinOASIS";
            ProviderDescription = "Filecoin decentralised storage network provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FilecoinOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Lotus JSON-RPC ───────────────────────────────────────────────────────

        private async Task<JsonElement?> LotusCallAsync(string method, object[] @params)
        {
            var req = new
            {
                jsonrpc = "2.0",
                method,
                @params,
                id = System.Threading.Interlocked.Increment(ref _rpcId)
            };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(_rpcUrl, content);
            resp.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err))
                throw new Exception($"Lotus RPC error: {err.GetRawText()}");
            if (root.TryGetProperty("result", out var result))
                return result.Clone();
            return null;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Ping: Filecoin.Version
                var ver = await LotusCallAsync("Filecoin.Version", Array.Empty<object>());
                result.Result = true;
                result.Message = $"FilecoinOASIS activated. Node version: {ver?.GetProperty("Version").GetString() ?? "unknown"} at {_rpcUrl}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FilecoinOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() =>
            await Task.FromResult(new OASISResult<bool> { Result = true, Message = "FilecoinOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "FilecoinOASIS: Use LoadAvatarByProviderKeyAsync(filecoinAddress) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // providerKey = Filecoin address, e.g. f1abc...
            var result = new OASISResult<IAvatar>();
            try
            {
                // Filecoin.StateLookupID resolves any address to its ID address
                var idAddr = await LotusCallAsync("Filecoin.StateLookupID",
                    new object[] { providerKey, new object[] { } });
                string canonicalId = idAddr?.GetString() ?? providerKey;

                // Filecoin.StateGetActor to get balance
                var actor = await LotusCallAsync("Filecoin.StateGetActor",
                    new object[] { providerKey, new object[] { } });

                var avatar = new Avatar
                {
                    Id = DeriveGuid(providerKey),
                    Username = providerKey,
                    Description = $"Filecoin address {providerKey} (ID: {canonicalId})",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.FilecoinOASIS] = providerKey;
                avatar.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["address"] = providerKey;
                avatar.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["id_address"] = canonicalId;
                if (actor.HasValue && actor.Value.TryGetProperty("Balance", out var bal))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["balance_attoFil"] = bal.GetString() ?? "0";
                if (actor.HasValue && actor.Value.TryGetProperty("Nonce", out var nonce))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["nonce"] = nonce.GetUInt64().ToString();
                result.Result = avatar;
                result.Message = $"FilecoinOASIS: Loaded actor for address '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FilecoinOASIS: Error loading avatar '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadAvatarByProviderKeyAsync(avatarUsername, version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: LoadAllAvatars is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "FilecoinOASIS: Filecoin addresses are cryptographic keys — there is no avatar save operation.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: SaveAvatarDetail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Addresses cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Addresses cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Addresses cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Addresses cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Use LoadAvatarDetailByUsernameAsync(address) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var r = await LoadAvatarByUsernameAsync(avatarUsername, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!r.IsError && r.Result != null)
                result.Result = new AvatarDetail { Id = r.Result.Id, Username = r.Result.Username, Description = r.Result.Description };
            else { result.IsError = r.IsError; result.Message = r.Message; result.Exception = r.Exception; }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: LoadAllAvatarDetails is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon: Load ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FilecoinOASIS: Use LoadHolonAsync(cidString) to load a Filecoin CID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            // providerKey = CID string; we look up the storage deal for it
            var result = new OASISResult<IHolon>();
            try
            {
                // Filecoin.ClientFindData: find deals by CID
                var cidObj = new Dictionary<string, string> { ["/"] = providerKey };
                var offers = await LotusCallAsync("Filecoin.ClientFindData",
                    new object[] { cidObj, null });

                var h = new Holon
                {
                    Id = DeriveGuid(providerKey),
                    Name = providerKey,
                    HolonType = HolonType.Holon,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                };
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.FilecoinOASIS] = providerKey;
                h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["cid"] = providerKey;
                if (offers.HasValue && offers.Value.ValueKind == System.Text.Json.JsonValueKind.Array
                    && offers.Value.GetArrayLength() > 0)
                {
                    var first = offers.Value[0];
                    if (first.TryGetProperty("MinPrice", out var price))
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["min_price_attoFil"] = price.GetString() ?? "0";
                    if (first.TryGetProperty("Size", out var sz))
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["size_bytes"] = sz.GetUInt64().ToString();
                    if (first.TryGetProperty("Miner", out var miner))
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["miner"] = miner.GetString() ?? string.Empty;
                }
                result.Result = h;
                result.Message = $"FilecoinOASIS: Loaded CID '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FilecoinOASIS: Error loading CID '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // List local imports from the node's import store
                var imports = await LotusCallAsync("Filecoin.ClientListImports", Array.Empty<object>());
                var holons = new List<IHolon>();
                if (imports.HasValue && imports.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in imports.Value.EnumerateArray())
                    {
                        string cid = string.Empty;
                        if (item.TryGetProperty("Root", out var rootEl)
                            && rootEl.TryGetProperty("/", out var cidEl))
                            cid = cidEl.GetString() ?? string.Empty;
                        if (string.IsNullOrEmpty(cid)) continue;
                        var h = new Holon
                        {
                            Id = DeriveGuid(cid),
                            Name = cid,
                            HolonType = HolonType.Holon,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                        };
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.FilecoinOASIS] = cid;
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["cid"] = cid;
                        if (item.TryGetProperty("FilePath", out var fp))
                            h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["file_path"] = fp.GetString() ?? string.Empty;
                        holons.Add(h);
                    }
                }
                result.Result = holons;
                result.Message = $"FilecoinOASIS: Loaded {holons.Count} imports from node.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FilecoinOASIS: Error loading holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
            LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Use LoadHolonsForParentAsync(filecoinAddress) to list deals for a miner.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            // providerKey = miner address; list deals where Miner == providerKey
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var deals = await LotusCallAsync("Filecoin.ClientListDeals", Array.Empty<object>());
                var holons = new List<IHolon>();
                if (deals.HasValue && deals.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var deal in deals.Value.EnumerateArray())
                    {
                        string miner = deal.TryGetProperty("Provider", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                        if (!string.IsNullOrEmpty(providerKey) && miner != providerKey) continue;
                        string cid = string.Empty;
                        if (deal.TryGetProperty("DataRef", out var dr)
                            && dr.TryGetProperty("Root", out var rootEl)
                            && rootEl.TryGetProperty("/", out var cidEl))
                            cid = cidEl.GetString() ?? string.Empty;
                        ulong dealId = deal.TryGetProperty("DealID", out var did) ? did.GetUInt64() : 0;
                        var h = new Holon
                        {
                            Id = DeriveGuid($"{miner}-{dealId}"),
                            Name = $"Deal {dealId} — {cid}",
                            HolonType = HolonType.Holon,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                        };
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.FilecoinOASIS] = cid.Length > 0 ? cid : dealId.ToString();
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["deal_id"] = dealId.ToString();
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["cid"] = cid;
                        h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["miner"] = miner;
                        if (deal.TryGetProperty("State", out var state))
                            h.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["state"] = state.GetUInt32().ToString();
                        holons.Add(h);
                    }
                }
                result.Result = holons;
                result.Message = $"FilecoinOASIS: Loaded {holons.Count} deals for '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FilecoinOASIS: Error loading deals for '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive,
                maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon: Save ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false)
        {
            // Start a storage deal for data referenced in holon.MetaData["FilePath"]
            var result = new OASISResult<IHolon>();
            try
            {
                string filePath = holon.MetaData != null && holon.MetaData.ContainsKey("FilePath")
                    ? holon.MetaData["FilePath"]?.ToString() ?? string.Empty : string.Empty;

                if (string.IsNullOrEmpty(filePath))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "FilecoinOASIS: Set holon.MetaData[\"FilePath\"] to the local file path before saving.");
                    return result;
                }

                // Step 1: import file to local Lotus node store
                var importResult = await LotusCallAsync("Filecoin.ClientImport",
                    new object[] { new { Path = filePath, IsCAR = false } });
                if (!importResult.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: ClientImport returned null.");
                    return result;
                }
                string cid = string.Empty;
                if (importResult.Value.TryGetProperty("Root", out var rootEl)
                    && rootEl.TryGetProperty("/", out var cidEl))
                    cid = cidEl.GetString() ?? string.Empty;

                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.FilecoinOASIS] = cid;
                holon.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["cid"] = cid;

                // Step 2: find available miners and start deal with cheapest
                var offers = await LotusCallAsync("Filecoin.ClientFindData",
                    new object[] { new Dictionary<string, string> { ["/"] = cid }, null });
                if (offers.HasValue && offers.Value.ValueKind == System.Text.Json.JsonValueKind.Array
                    && offers.Value.GetArrayLength() > 0)
                {
                    var best = offers.Value[0];
                    string miner = best.TryGetProperty("Miner", out var m) ? m.GetString() ?? string.Empty : string.Empty;
                    string price = best.TryGetProperty("MinPrice", out var p) ? p.GetString() ?? "0" : "0";
                    ulong size = best.TryGetProperty("Size", out var s) ? s.GetUInt64() : 0;

                    var dealParams = new
                    {
                        Data = new { TransferType = "graphsync", Root = new Dictionary<string, string> { ["/"] = cid }, PieceCid = (string)null, PieceSize = 0 },
                        Wallet = holon.MetaData != null && holon.MetaData.ContainsKey("WalletAddress")
                            ? holon.MetaData["WalletAddress"]?.ToString() ?? string.Empty : string.Empty,
                        Miner = miner,
                        EpochPrice = price,
                        MinBlocksDuration = 518400, // ~180 days
                        ProviderCollateral = "0",
                        DealStartEpoch = -1,
                        FastRetrieval = true,
                        VerifiedDeal = false,
                    };
                    var dealId = await LotusCallAsync("Filecoin.ClientStartDeal", new object[] { dealParams });
                    string dealCid = dealId.HasValue && dealId.Value.TryGetProperty("/", out var dc)
                        ? dc.GetString() ?? string.Empty : string.Empty;
                    holon.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["deal_proposal_cid"] = dealCid;
                    holon.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["miner"] = miner;
                    holon.ProviderMetaData[Core.Enums.ProviderType.FilecoinOASIS]["size_bytes"] = size.ToString();
                }

                result.Result = holon;
                result.Message = $"FilecoinOASIS: Imported and deal initiated for CID '{cid}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FilecoinOASIS: Error saving holon: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false) =>
            SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null) saved.Add(r.Result);
                    else if (!continueOnError) { OASISErrorHandling.HandleError(ref result, r.Message); return result; }
                }
                result.Result = saved;
                result.Message = $"FilecoinOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FilecoinOASIS: Error saving holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false) =>
            SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon: Delete ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FilecoinOASIS: Use DeleteHolonAsync(cidString) to remove a local import.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // Remove from local import store via Filecoin.ClientRemoveImport (import key, not CID)
            // For simplicity we look up imports by CID and remove the matching key
            var result = new OASISResult<IHolon>();
            try
            {
                var imports = await LotusCallAsync("Filecoin.ClientListImports", Array.Empty<object>());
                if (imports.HasValue && imports.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in imports.Value.EnumerateArray())
                    {
                        string cid = string.Empty;
                        if (item.TryGetProperty("Root", out var r) && r.TryGetProperty("/", out var c))
                            cid = c.GetString() ?? string.Empty;
                        if (cid != providerKey) continue;
                        ulong importKey = item.TryGetProperty("Key", out var k) ? k.GetUInt64() : 0;
                        await LotusCallAsync("Filecoin.ClientRemoveImport", new object[] { importKey });
                        result.Message = $"FilecoinOASIS: Import for CID '{providerKey}' removed.";
                        return result;
                    }
                }
                OASISErrorHandling.HandleError(ref result,
                    $"FilecoinOASIS: No local import found for CID '{providerKey}'.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FilecoinOASIS: Error deleting holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            // Search local deals/imports by CID
            var result = new OASISResult<ISearchResults>();
            try
            {
                string q = string.Empty;
                if (searchParams.SearchGroups != null)
                    foreach (var g in searchParams.SearchGroups)
                        if (g is NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup tg && !string.IsNullOrEmpty(tg.SearchQuery))
                        { q = tg.SearchQuery; break; }

                var searchResults = new SearchResults();
                var allHolons = await LoadAllHolonsAsync();
                if (!allHolons.IsError && allHolons.Result != null)
                    foreach (var h in allHolons.Result)
                        if (string.IsNullOrEmpty(q) || h.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                            searchResults.SearchResultHolons.Add(h);

                result.Result = searchResults;
                result.Message = $"FilecoinOASIS: Found {searchResults.SearchResultHolons.Count} matches for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FilecoinOASIS: Error searching: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0) =>
            SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Use SaveHolonAsync with MetaData[\"FilePath\"] set.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadHolonsForParentAsync(avatarUsername, version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FilecoinOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }
    }
}
