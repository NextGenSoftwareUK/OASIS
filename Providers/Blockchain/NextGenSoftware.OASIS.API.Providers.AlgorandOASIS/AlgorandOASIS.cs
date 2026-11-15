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
            : this(algodUrl ?? Environment.GetEnvironmentVariable("ALGORAND_ALGOD_URL"),
                indexerUrl ?? Environment.GetEnvironmentVariable("ALGORAND_INDEXER_URL"),
                ReadConfiguredToken(algodUrl, "ALGORAND_ALGOD_URL", "ALGORAND_ALGOD_API_TOKEN"),
                ReadConfiguredToken(indexerUrl, "ALGORAND_INDEXER_URL", "ALGORAND_INDEXER_API_TOKEN"))
        {
        }

        public AlgorandOASIS(string algodUrl, string indexerUrl, string algodApiToken, string indexerApiToken)
        {
            _algodUrl = (algodUrl ?? "https://mainnet-api.algonode.cloud").TrimEnd('/');
            _indexerUrl = (indexerUrl ?? "https://mainnet-idx.algonode.cloud").TrimEnd('/');
            _algod = CreateClient(_algodUrl, "X-Algo-API-Token", algodApiToken);
            _indexer = CreateClient(_indexerUrl, "X-Indexer-API-Token", indexerApiToken);

            ProviderName = "AlgorandOASIS";
            ProviderDescription = "Algorand pure-PoS blockchain provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AlgorandOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        private static string ReadConfiguredToken(string suppliedUrl, string urlVariable, string tokenVariable) =>
            suppliedUrl == null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(urlVariable))
                ? Environment.GetEnvironmentVariable(tokenVariable)
                : null;

        private static HttpClient CreateClient(string url, string tokenHeader, string token)
        {
            var hasToken = !string.IsNullOrWhiteSpace(token);
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = !hasToken })
                { BaseAddress = new Uri(url) };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (hasToken)
                client.DefaultRequestHeaders.Add(tokenHeader, token);
            return client;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                using var response = await _algod.GetAsync("/v2/status").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(body).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("last-round", out var round) || !round.TryGetUInt64(out var lastRound))
                    throw new InvalidOperationException("Algod status did not provide a valid last round.");
                IsProviderActivated = true;
                result.Result = true;
                result.Message = $"AlgorandOASIS activated. Last round: {lastRound} at {_algodUrl}.";
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync() => Task.FromResult(DeActivateProvider());

        public override OASISResult<bool> DeActivateProvider()
        {
            IsProviderActivated = false;
            return new OASISResult<bool> { Result = true, Message = "AlgorandOASIS deactivated." };
        }

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
                var metadata = GetAlgorandMetadata(avatar);
                metadata["address"] = providerKey;
                if (acct.TryGetProperty("amount", out var bal))
                    metadata["balance_microALGO"] = bal.GetUInt64().ToString();
                if (acct.TryGetProperty("amount-without-pending-rewards", out var nb))
                    metadata["balance_without_rewards_microALGO"] = nb.GetUInt64().ToString();
                if (acct.TryGetProperty("status", out var st))
                    metadata["status"] = st.GetString() ?? string.Empty;
                if (acct.TryGetProperty("total-assets-opted-in", out var asas))
                    metadata["total_assets_opted_in"] = asas.GetUInt32().ToString();
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
                using var response = await _indexer.GetAsync($"/v2/transactions/{Uri.EscapeDataString(providerKey)}").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(body).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("transaction", out var transaction) ||
                    !transaction.TryGetProperty("id", out var id) ||
                    !string.Equals(id.GetString(), providerKey, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The indexer did not return the requested transaction.");

                result.Result = MapTransactionToHolon(transaction, id.GetString());
                result.IsLoaded = true;
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
            // Submit the caller-signed transaction. A submission receipt does not establish confirmation.
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
                using var response = await _algod.PostAsync("/v2/transactions", content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var rejected = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    OASISErrorHandling.HandleError(ref result,
                        $"AlgorandOASIS: Transaction submission failed ({response.StatusCode}): {rejected}");
                    return result;
                }

                using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(body).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("txId", out var id) || id.ValueKind != JsonValueKind.String ||
                    id.GetString() is not { Length: 52 } txId ||
                    !System.Text.RegularExpressions.Regex.IsMatch(txId, "^[A-Z2-7]{52}$"))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Algod acknowledged submission without a valid transaction ID. Reconcile the signed transaction before resubmitting.");
                    return result;
                }

                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = txId;
                GetAlgorandMetadata(holon)["tx_id"] = txId;
                result.Result = holon;
                result.Message = $"AlgorandOASIS: Transaction submitted with ID '{txId}'.";
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
            var submitted = new List<IHolon>();
            var result = new OASISResult<IEnumerable<IHolon>> { Result = submitted };
            try
            {
                var index = 0;
                foreach (var holon in holons)
                {
                    var response = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth,
                        continueOnError, saveChildrenOnProvider).ConfigureAwait(false);
                    if (!response.IsError && response.Result != null)
                    {
                        submitted.Add(response.Result);
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorCount++;
                        result.ErrorCode ??= response.ErrorCode;
                        result.Exception ??= response.Exception;
                        result.InnerMessages.Add($"Item {index}: {response.Message}");
                        if (!continueOnError)
                            break;
                    }
                    index++;
                }

                result.Message = result.IsError
                    ? $"AlgorandOASIS: Submitted {submitted.Count} transactions and rejected {result.ErrorCount}."
                    : $"AlgorandOASIS: Submitted {submitted.Count} transactions.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error submitting transactions: {ex.Message}");
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
            var metadata = GetAlgorandMetadata(h);
            metadata["tx_id"] = txId;
            metadata["sender"] = sender;
            metadata["tx_type"] = txType;
            metadata["note"] = note;
            if (t.TryGetProperty("confirmed-round", out var cr))
                metadata["confirmed_round"] = cr.GetUInt64().ToString();
            if (txType == "pay" && t.TryGetProperty("payment-transaction", out var pay))
            {
                if (pay.TryGetProperty("receiver", out var rec))
                    metadata["receiver"] = rec.GetString() ?? string.Empty;
                if (pay.TryGetProperty("amount", out var amt))
                    metadata["amount_microALGO"] = amt.GetUInt64().ToString();
            }
            else if (txType == "axfer" && t.TryGetProperty("asset-transfer-transaction", out var axfer))
            {
                if (axfer.TryGetProperty("asset-id", out var assetId))
                    metadata["asa_id"] = assetId.GetUInt64().ToString();
                if (axfer.TryGetProperty("amount", out var asaAmt))
                    metadata["asa_amount"] = asaAmt.GetUInt64().ToString();
                if (axfer.TryGetProperty("receiver", out var asaRec))
                    metadata["receiver"] = asaRec.GetString() ?? string.Empty;
            }
            return h;
        }

        private static Dictionary<string, string> GetAlgorandMetadata(IHolonBase holon)
        {
            holon.ProviderMetaData ??= new Dictionary<ProviderType, Dictionary<string, string>>();
            if (!holon.ProviderMetaData.TryGetValue(Core.Enums.ProviderType.AlgorandOASIS, out var metadata) || metadata == null)
            {
                metadata = new Dictionary<string, string>();
                holon.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS] = metadata;
            }
            return metadata;
        }

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }
    }
}
