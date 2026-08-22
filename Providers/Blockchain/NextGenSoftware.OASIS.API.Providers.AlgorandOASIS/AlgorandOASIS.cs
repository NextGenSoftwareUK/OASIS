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

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS
{
    /// <summary>
    /// OASIS provider for the Algorand pure-PoS blockchain (https://algorand.com).
    /// Uses the Algod v2 REST API via the public Algonode.io endpoint (no token required).
    /// Algorand accounts → OASIS Avatars; transactions → OASIS Holons; ASAs mapped as sub-holons.
    /// Set ALGORAND_ALGOD_URL (default: https://mainnet-api.algonode.cloud) and optionally
    /// ALGORAND_INDEXER_URL (default: https://mainnet-idx.algonode.cloud) for rich queries.
    /// Avatar provider key = Algorand account address (58-char Base32 string).
    /// Holon provider key  = transaction ID (Base32 string).
    /// </summary>
    public class AlgorandOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _algod;
        private readonly HttpClient _indexer;
        private readonly string _algodUrl;
        private readonly string _indexerUrl;
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public AlgorandOASIS(string algodUrl = null, string indexerUrl = null)
        {
            _algodUrl = (algodUrl ?? Environment.GetEnvironmentVariable("ALGORAND_ALGOD_URL")
                         ?? "https://mainnet-api.algonode.cloud").TrimEnd('/');
            _indexerUrl = (indexerUrl ?? Environment.GetEnvironmentVariable("ALGORAND_INDEXER_URL")
                           ?? "https://mainnet-idx.algonode.cloud").TrimEnd('/');
            _algod = new HttpClient { BaseAddress = new Uri(_algodUrl) };
            _algod.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _indexer = new HttpClient { BaseAddress = new Uri(_indexerUrl) };
            _indexer.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ProviderName = "AlgorandOASIS";
            ProviderDescription = "Algorand pure-PoS blockchain provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AlgorandOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var resp = await _algod.GetAsync("/v2/status");
                resp.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                ulong lastRound = doc.RootElement.TryGetProperty("last-round", out var lr) ? lr.GetUInt64() : 0;
                result.Result = true;
                result.Message = $"AlgorandOASIS activated. Last round: {lastRound} at {_algodUrl}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() =>
            await Task.FromResult(new OASISResult<bool> { Result = true, Message = "AlgorandOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "AlgorandOASIS: Use LoadAvatarByProviderKeyAsync(algorandAddress) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _algod.GetAsync($"/v2/accounts/{Uri.EscapeDataString(providerKey)}");
                resp.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                var acct = doc.RootElement.TryGetProperty("account", out var a) ? a : doc.RootElement;

                var avatar = new Avatar
                {
                    Id = DeriveGuid(providerKey),
                    Username = providerKey,
                    Description = $"Algorand account {providerKey}",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = providerKey;
                avatar.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["address"] = providerKey;
                if (acct.TryGetProperty("amount", out var bal))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["balance_microALGO"] = bal.GetUInt64().ToString();
                if (acct.TryGetProperty("amount-without-pending-rewards", out var nb))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["balance_without_rewards_microALGO"] = nb.GetUInt64().ToString();
                if (acct.TryGetProperty("status", out var st))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["status"] = st.GetString() ?? string.Empty;
                if (acct.TryGetProperty("total-assets-opted-in", out var asas))
                    avatar.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["total_assets_opted_in"] = asas.GetUInt32().ToString();
                result.Result = avatar;
                result.Message = $"AlgorandOASIS: Loaded account '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AlgorandOASIS: Error loading avatar '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: LoadAllAvatars is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "AlgorandOASIS: Algorand accounts are derived from Ed25519 keys — no save operation.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: SaveAvatarDetail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Accounts cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Use LoadAvatarDetailByUsernameAsync(address) instead.");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: LoadAllAvatarDetails is not supported.");
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
                "AlgorandOASIS: Use LoadHolonAsync(transactionId) to load an Algorand transaction.");
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
                // Use indexer for transaction lookup by ID
                var resp = await _indexer.GetAsync($"/v2/transactions/{Uri.EscapeDataString(providerKey)}");
                resp.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                var txn = doc.RootElement.TryGetProperty("transaction", out var t) ? t : doc.RootElement;
                result.Result = MapTransactionToHolon(txn, providerKey);
                result.Message = $"AlgorandOASIS: Loaded transaction '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AlgorandOASIS: Error loading transaction '{providerKey}': {ex.Message}");
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
                "AlgorandOASIS: LoadAllHolons is not supported — use LoadHolonsForParentAsync(accountAddress) to load an account's transactions.");
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
                "AlgorandOASIS: Use LoadHolonsForParentAsync(algorandAddress) instead.");
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
            // Load transaction history for an Algorand account via the indexer
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string nextToken = null;
                do
                {
                    string url = $"/v2/accounts/{Uri.EscapeDataString(providerKey)}/transactions?limit=100" +
                                 (nextToken != null ? $"&next={Uri.EscapeDataString(nextToken)}" : "");
                    var resp = await _indexer.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) break;
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("transactions", out var txns))
                        foreach (var t in txns.EnumerateArray())
                        {
                            string txId = t.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrEmpty(txId))
                                holons.Add(MapTransactionToHolon(t, txId));
                        }
                    nextToken = doc.RootElement.TryGetProperty("next-token", out var nt) ? nt.GetString() : null;
                }
                while (!string.IsNullOrEmpty(nextToken) && holons.Count < 2000);

                result.Result = holons;
                result.Message = $"AlgorandOASIS: Loaded {holons.Count} transactions for '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AlgorandOASIS: Error loading transactions for '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: LoadHolonsByMetaData is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: LoadHolonsByMetaData is not supported.");
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
            // Send an Algorand payment transaction with a note field containing the holon name.
            // Requires holon.MetaData["SenderAddress"], ["ReceiverAddress"], and ["SignedTxnBase64"].
            // The caller must sign the transaction offline (Ed25519) and supply the base64-encoded
            // signed msgpack. This is mandatory for blockchain transactions — private keys must never
            // leave the client.
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.MetaData == null
                    || !holon.MetaData.ContainsKey("SignedTxnBase64")
                    || string.IsNullOrEmpty(holon.MetaData["SignedTxnBase64"]?.ToString()))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "AlgorandOASIS: Set holon.MetaData[\"SignedTxnBase64\"] to the base64-encoded signed msgpack transaction before saving.");
                    return result;
                }

                byte[] signedTxn = Convert.FromBase64String(holon.MetaData["SignedTxnBase64"].ToString()!);
                using var content = new ByteArrayContent(signedTxn);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-binary");
                var resp = await _algod.PostAsync("/v2/transactions", content);
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    string txId = doc.RootElement.TryGetProperty("txId", out var tx) ? tx.GetString() ?? string.Empty : string.Empty;
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = txId;
                    holon.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["tx_id"] = txId;
                    result.Result = holon;
                    result.Message = $"AlgorandOASIS: Transaction submitted with ID '{txId}'.";
                }
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"AlgorandOASIS: Transaction submission failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error saving holon: {ex.Message}");
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
                result.Message = $"AlgorandOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error saving holons: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Blockchain transactions are immutable and cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Blockchain transactions are immutable and cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            // Search indexer for transactions by note field
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
                    // Indexer supports note-prefix search (base64)
                    string noteB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(q));
                    var resp = await _indexer.GetAsync($"/v2/transactions?note-prefix={Uri.EscapeDataString(noteB64)}&limit=100");
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                        if (doc.RootElement.TryGetProperty("transactions", out var txns))
                            foreach (var t in txns.EnumerateArray())
                            {
                                string txId = t.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
                                if (!string.IsNullOrEmpty(txId))
                                    searchResults.SearchResultHolons.Add(MapTransactionToHolon(t, txId));
                            }
                    }
                }
                result.Result = searchResults;
                result.Message = $"AlgorandOASIS: Found {searchResults.SearchResultHolons.Count} transactions for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error searching: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Use ExportAllDataForAvatarByUsernameAsync(address).");
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
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Email not supported — use ExportAllDataForAvatarByUsernameAsync(address).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: ExportAll not supported — use ExportAllDataForAvatarByUsernameAsync(address).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static Holon MapTransactionToHolon(JsonElement t, string txId)
        {
            string sender = t.TryGetProperty("sender", out var s) ? s.GetString() ?? string.Empty : string.Empty;
            string txType = t.TryGetProperty("tx-type", out var tt) ? tt.GetString() ?? "unknown" : "unknown";
            ulong roundTime = t.TryGetProperty("round-time", out var rt) ? rt.GetUInt64() : 0;
            DateTime ts = roundTime > 0 ? DateTimeOffset.FromUnixTimeSeconds((long)roundTime).UtcDateTime : DateTime.UtcNow;

            // Note field is base64-encoded bytes
            string note = string.Empty;
            if (t.TryGetProperty("note", out var n))
                try { note = Encoding.UTF8.GetString(Convert.FromBase64String(n.GetString() ?? string.Empty)); }
                catch { note = n.GetString() ?? string.Empty; }

            var h = new Holon
            {
                Id = DeriveGuid(txId),
                Name = string.IsNullOrEmpty(note) ? $"Algorand {txType} tx {txId[..Math.Min(8, txId.Length)]}..." : note,
                HolonType = HolonType.Holon,
                CreatedDate = ts,
                ModifiedDate = ts,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = txId;
            h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["tx_id"] = txId;
            h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["sender"] = sender;
            h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["tx_type"] = txType;
            h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["note"] = note;
            if (t.TryGetProperty("confirmed-round", out var cr))
                h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["confirmed_round"] = cr.GetUInt64().ToString();
            if (txType == "pay" && t.TryGetProperty("payment-transaction", out var pay))
            {
                if (pay.TryGetProperty("receiver", out var rec))
                    h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["receiver"] = rec.GetString() ?? string.Empty;
                if (pay.TryGetProperty("amount", out var amt))
                    h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["amount_microALGO"] = amt.GetUInt64().ToString();
            }
            else if (txType == "axfer" && t.TryGetProperty("asset-transfer-transaction", out var axfer))
            {
                if (axfer.TryGetProperty("asset-id", out var assetId))
                    h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["asa_id"] = assetId.GetUInt64().ToString();
                if (axfer.TryGetProperty("amount", out var asaAmt))
                    h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["asa_amount"] = asaAmt.GetUInt64().ToString();
                if (axfer.TryGetProperty("receiver", out var asaRec))
                    h.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS]["receiver"] = asaRec.GetString() ?? string.Empty;
            }
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
