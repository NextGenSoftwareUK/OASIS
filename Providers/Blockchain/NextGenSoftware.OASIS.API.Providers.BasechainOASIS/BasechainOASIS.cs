using System;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.Providers.BasechainOASIS
{
    /// <summary>
    /// OASIS provider for Basechain (formerly Loom Network) — an EVM-compatible delegated-PoS sidechain
    /// (https://loomx.io / https://basechain.dappchains.com).
    /// Uses standard Ethereum JSON-RPC (same surface as EthereumOASIS).
    /// EVM addresses → OASIS Avatars; transactions → OASIS Holons.
    /// Set BASECHAIN_RPC_URL (default: https://basechain-mainnet.dappchains.com) env var.
    /// Avatar provider key = 0x Ethereum-compatible address.
    /// Holon provider key  = transaction hash (0x...).
    /// Write operations (SendRawTransaction) require the signed raw transaction to be provided by the caller
    /// — private keys must never leave the client.
    /// </summary>
    public class BasechainOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _rpcUrl;
        private int _rpcId;

        public BasechainOASIS(string rpcUrl = null)
        {
            _rpcUrl = rpcUrl ?? Environment.GetEnvironmentVariable("BASECHAIN_RPC_URL")
                      ?? "https://basechain-mainnet.dappchains.com";
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ProviderName = "BasechainOASIS";
            ProviderDescription = "Basechain (Loom Network) EVM-compatible sidechain provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BasechainOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Ethereum JSON-RPC ────────────────────────────────────────────────────

        private async Task<JsonElement?> EthCallAsync(string method, object[] @params)
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
                throw new Exception($"Basechain RPC error: {err.GetRawText()}");
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
                var chainId = await EthCallAsync("eth_chainId", Array.Empty<object>());
                ulong id = chainId.HasValue && chainId.Value.ValueKind == JsonValueKind.String
                    ? Convert.ToUInt64(chainId.Value.GetString(), 16) : 0;
                result.Result = true;
                result.Message = $"BasechainOASIS activated. Chain ID: {id} at {_rpcUrl}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BasechainOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() =>
            await Task.FromResult(new OASISResult<bool> { Result = true, Message = "BasechainOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "BasechainOASIS: Use LoadAvatarByProviderKeyAsync(0x...) to load an EVM account.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // Normalise to checksum address
                string address = providerKey.ToLowerInvariant().StartsWith("0x") ? providerKey : "0x" + providerKey;

                var balHex = await EthCallAsync("eth_getBalance", new object[] { address, "latest" });
                var nonceHex = await EthCallAsync("eth_getTransactionCount", new object[] { address, "latest" });

                string balStr = balHex?.GetString() ?? "0x0";
                string nonceStr = nonceHex?.GetString() ?? "0x0";
                ulong balWei = Convert.ToUInt64(balStr, 16);
                ulong nonce = Convert.ToUInt64(nonceStr, 16);

                var avatar = new Avatar
                {
                    Id = DeriveGuid(address),
                    Username = address,
                    Description = $"Basechain EVM account {address}",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BasechainOASIS] = address;
                avatar.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["address"] = address;
                avatar.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["balance_wei"] = balWei.ToString();
                avatar.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["nonce"] = nonce.ToString();
                result.Result = avatar;
                result.Message = $"BasechainOASIS: Loaded account '{address}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BasechainOASIS: Error loading avatar '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: LoadAllAvatars is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "BasechainOASIS: EVM accounts are derived from secp256k1 keys — no save operation.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: SaveAvatarDetail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: EVM accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: EVM accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: EVM accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: EVM accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Use LoadAvatarDetailByUsernameAsync(0x...) instead.");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Email not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: LoadAllAvatarDetails is not supported.");
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
                "BasechainOASIS: Use LoadHolonAsync(txHash) to load a transaction.");
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
            var result = new OASISResult<IHolon>();
            try
            {
                var tx = await EthCallAsync("eth_getTransactionByHash", new object[] { providerKey });
                if (!tx.HasValue || tx.Value.ValueKind == JsonValueKind.Null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"BasechainOASIS: Transaction '{providerKey}' not found.");
                    return result;
                }
                // Also get receipt for block timestamp
                var receipt = await EthCallAsync("eth_getTransactionReceipt", new object[] { providerKey });
                DateTime ts = DateTime.UtcNow;
                if (receipt.HasValue && receipt.Value.ValueKind != JsonValueKind.Null
                    && receipt.Value.TryGetProperty("blockNumber", out var bnHex))
                {
                    ulong blockNum = Convert.ToUInt64(bnHex.GetString(), 16);
                    var block = await EthCallAsync("eth_getBlockByNumber",
                        new object[] { "0x" + blockNum.ToString("x"), false });
                    if (block.HasValue && block.Value.TryGetProperty("timestamp", out var tsHex))
                        ts = DateTimeOffset.FromUnixTimeSeconds((long)Convert.ToUInt64(tsHex.GetString(), 16)).UtcDateTime;
                }
                result.Result = MapTxToHolon(tx.Value, providerKey, ts);
                result.Message = $"BasechainOASIS: Loaded transaction '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BasechainOASIS: Error loading transaction '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result,
                "BasechainOASIS: LoadAllHolons not supported — use LoadHolonsForParentAsync(0x...) for an account's transactions.");
            return await Task.FromResult(result);
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
            OASISErrorHandling.HandleError(ref result,
                "BasechainOASIS: Use LoadHolonsForParentAsync(0x...) instead.");
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
            // Scan blocks backwards from latest for transactions involving this address.
            // Standard JSON-RPC has no direct "get txs by address" call; we scan recent blocks.
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string address = providerKey.ToLowerInvariant();
                var latestHex = await EthCallAsync("eth_blockNumber", Array.Empty<object>());
                ulong latest = latestHex.HasValue ? Convert.ToUInt64(latestHex.Value.GetString(), 16) : 0;
                ulong scanFrom = latest > 10000 ? latest - 10000 : 0; // scan last 10,000 blocks

                var holons = new List<IHolon>();
                for (ulong i = latest; i >= scanFrom && holons.Count < 500; i--)
                {
                    var block = await EthCallAsync("eth_getBlockByNumber",
                        new object[] { "0x" + i.ToString("x"), true });
                    if (!block.HasValue || block.Value.ValueKind == JsonValueKind.Null) continue;

                    DateTime ts = DateTime.UtcNow;
                    if (block.Value.TryGetProperty("timestamp", out var tsHex))
                        ts = DateTimeOffset.FromUnixTimeSeconds((long)Convert.ToUInt64(tsHex.GetString(), 16)).UtcDateTime;

                    if (!block.Value.TryGetProperty("transactions", out var txns)
                        || txns.ValueKind != JsonValueKind.Array) continue;
                    foreach (var tx in txns.EnumerateArray())
                    {
                        string from = tx.TryGetProperty("from", out var f) ? (f.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;
                        string to = tx.TryGetProperty("to", out var t) ? (t.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;
                        if (from != address && to != address) continue;
                        string hash = tx.TryGetProperty("hash", out var h) ? h.GetString() ?? string.Empty : string.Empty;
                        if (!string.IsNullOrEmpty(hash))
                            holons.Add(MapTxToHolon(tx, hash, ts));
                    }
                }
                result.Result = holons;
                result.Message = $"BasechainOASIS: Loaded {holons.Count} transactions for '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BasechainOASIS: Error loading transactions for '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: LoadHolonsByMetaData is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: LoadHolonsByMetaData is not supported.");
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
            // Broadcast a pre-signed raw transaction. The caller must provide the raw signed tx
            // in holon.MetaData["SignedRawTxHex"] (0x-prefixed hex).
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.MetaData == null
                    || !holon.MetaData.ContainsKey("SignedRawTxHex")
                    || string.IsNullOrEmpty(holon.MetaData["SignedRawTxHex"]?.ToString()))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "BasechainOASIS: Set holon.MetaData[\"SignedRawTxHex\"] to the 0x-prefixed signed RLP transaction before saving.");
                    return result;
                }

                string rawTx = holon.MetaData["SignedRawTxHex"].ToString()!;
                var txHash = await EthCallAsync("eth_sendRawTransaction", new object[] { rawTx });
                string hash = txHash?.GetString() ?? string.Empty;
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BasechainOASIS] = hash;
                holon.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["tx_hash"] = hash;
                result.Result = holon;
                result.Message = $"BasechainOASIS: Transaction broadcast with hash '{hash}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BasechainOASIS: Error saving holon: {ex.Message}");
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
                foreach (var h in holons)
                {
                    var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null) saved.Add(r.Result);
                    else if (!continueOnError) { OASISErrorHandling.HandleError(ref result, r.Message); return result; }
                }
                result.Result = saved;
                result.Message = $"BasechainOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BasechainOASIS: Error saving holons: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Blockchain transactions are immutable and cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Blockchain transactions are immutable and cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            // Search by tx hash (direct lookup) or by address (scan recent blocks)
            var result = new OASISResult<ISearchResults>();
            try
            {
                string q = string.Empty;
                if (searchParams.SearchGroups != null)
                    foreach (var g in searchParams.SearchGroups)
                        if (g is NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup tg && !string.IsNullOrEmpty(tg.SearchQuery))
                        { q = tg.SearchQuery; break; }

                var searchResults = new SearchResults();
                if (!string.IsNullOrEmpty(q))
                {
                    if (q.StartsWith("0x") && q.Length == 66)
                    {
                        // Looks like a tx hash
                        var h = await LoadHolonAsync(q);
                        if (!h.IsError && h.Result != null) searchResults.SearchResultHolons.Add(h.Result);
                    }
                    else if (q.StartsWith("0x") && q.Length == 42)
                    {
                        // Looks like an address — load as avatar
                        var a = await LoadAvatarByProviderKeyAsync(q);
                        if (!a.IsError && a.Result != null) searchResults.SearchResultAvatars.Add(a.Result);
                    }
                }
                result.Result = searchResults;
                result.Message = $"BasechainOASIS: Search complete for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BasechainOASIS: Error searching: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Use ExportAllDataForAvatarByUsernameAsync(0x...).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadHolonsForParentAsync(avatarUsername, version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Email not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: ExportAll not supported — use ExportAllDataForAvatarByUsernameAsync(0x...).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BasechainOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static Holon MapTxToHolon(JsonElement tx, string txHash, DateTime ts)
        {
            string from = tx.TryGetProperty("from", out var f) ? f.GetString() ?? string.Empty : string.Empty;
            string to = tx.TryGetProperty("to", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            string value = tx.TryGetProperty("value", out var v) ? v.GetString() ?? "0x0" : "0x0";
            string input = tx.TryGetProperty("input", out var inp) ? inp.GetString() ?? "0x" : "0x";

            // Decode note from input data UTF-8 if present and non-empty beyond "0x"
            string note = string.Empty;
            if (input.Length > 2)
                try { note = Encoding.UTF8.GetString(Convert.FromHexString(input[2..])); } catch { }

            string name = string.IsNullOrEmpty(note)
                ? $"Basechain tx {txHash[..Math.Min(10, txHash.Length)]}..."
                : note;

            var h = new Holon
            {
                Id = DeriveGuid(txHash),
                Name = name,
                HolonType = HolonType.Holon,
                CreatedDate = ts,
                ModifiedDate = ts,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.BasechainOASIS] = txHash;
            h.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["tx_hash"] = txHash;
            h.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["from"] = from;
            h.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["to"] = to;
            ulong valWei = value.Length > 2 ? Convert.ToUInt64(value, 16) : 0;
            h.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["value_wei"] = valWei.ToString();
            h.ProviderMetaData[Core.Enums.ProviderType.BasechainOASIS]["input"] = input;
            return h;
        }

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }
    }
}
