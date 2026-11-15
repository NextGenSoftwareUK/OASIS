using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    public partial class AlgorandOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
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
            : this(algodUrl, indexerUrl, algodApiToken, indexerApiToken, null)
        {
        }

        public AlgorandOASIS(string algodUrl, string indexerUrl, string algodApiToken, string indexerApiToken,
            AlgorandRecordBoxSettings? recordBoxes)
        {
            _algodUrl = (algodUrl ?? "https://mainnet-api.algonode.cloud").TrimEnd('/');
            _indexerUrl = (indexerUrl ?? "https://mainnet-idx.algonode.cloud").TrimEnd('/');
            _algod = CreateClient(_algodUrl, "X-Algo-API-Token", algodApiToken);
            _indexer = CreateClient(_indexerUrl, "X-Indexer-API-Token", indexerApiToken);
            _recordBoxes = recordBoxes;

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
                var algod = ReadAlgodRoundAsync();
                var versions = ReadAlgodNetworkAsync();
                var indexer = ReadIndexerStatusAsync();
                await Task.WhenAll(algod, versions, indexer).ConfigureAwait(false);
                var algodRound = await algod.ConfigureAwait(false);
                var algodNetwork = await versions.ConfigureAwait(false);
                var index = await indexer.ConfigureAwait(false);
                if (algodNetwork.Id != index.GenesisId || algodNetwork.Hash != index.GenesisHash)
                    throw new InvalidOperationException("Algod and Indexer identify different Algorand networks.");
                if (_recordBoxes != null)
                    await VerifyRecordApplicationAsync(_recordBoxes, algodNetwork.Hash).ConfigureAwait(false);
                IsProviderActivated = true;
                result.Result = true;
                result.Message = $"AlgorandOASIS activated. Algod round {algodRound} at {_algodUrl}. Indexed round {index.Round} at {_indexerUrl}.";
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        private async Task<ulong> ReadAlgodRoundAsync()
        {
            using var document = await ReadJsonAsync(_algod, "/v2/status").ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("last-round", out var round) || !round.TryGetUInt64(out var lastRound))
                throw new InvalidOperationException("Algod status did not provide a valid last round.");
            return lastRound;
        }

        private async Task<(string Id, string Hash)> ReadAlgodNetworkAsync()
        {
            using var document = await ReadJsonAsync(_algod, "/versions").ConfigureAwait(false);
            var root = document.RootElement;
            return ReadNetworkIdentity(root, "genesis_id", "genesis_hash_b64");
        }

        private async Task<(ulong Round, string GenesisId, string GenesisHash)> ReadIndexerStatusAsync()
        {
            using var document = await ReadJsonAsync(_indexer, "/health").ConfigureAwait(false);
            var health = document.RootElement;
            if (!health.TryGetProperty("db-available", out var database) || database.ValueKind != JsonValueKind.True ||
                !health.TryGetProperty("is-migrating", out var migrating) || migrating.ValueKind != JsonValueKind.False ||
                !health.TryGetProperty("round", out var round) || !round.TryGetUInt64(out var indexedRound))
                throw new InvalidOperationException("Indexer health did not report an available database and a valid round.");

            using var block = await ReadJsonAsync(_indexer, $"/v2/blocks/{indexedRound.ToString(CultureInfo.InvariantCulture)}?header-only=true").ConfigureAwait(false);
            var header = block.RootElement;
            if (header.GetProperty("round").GetUInt64() != indexedRound)
                throw new InvalidOperationException("Indexer did not return the requested network header.");
            var network = ReadNetworkIdentity(header, "genesis-id", "genesis-hash");
            return (indexedRound, network.Id, network.Hash);
        }

        private static (string Id, string Hash) ReadNetworkIdentity(JsonElement document, string idField, string hashField)
        {
            var id = document.GetProperty(idField).GetString();
            var hash = document.GetProperty(hashField).GetString();
            if (string.IsNullOrWhiteSpace(id) || hash == null || Convert.FromBase64String(hash).Length != 32)
                throw new InvalidOperationException("Algorand did not return a valid genesis identity.");
            return (id, hash);
        }

        private static async Task<JsonDocument> ReadJsonAsync(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonDocument.ParseAsync(body).ConfigureAwait(false);
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync() => Task.FromResult(DeActivateProvider());

        public override OASISResult<bool> DeActivateProvider()
        {
            IsProviderActivated = false;
            return new OASISResult<bool> { Result = true, Message = "AlgorandOASIS deactivated." };
        }

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => LoadStoredRecordAsync<Avatar, IAvatar>(id, version, "avatar");

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => _recordBoxes != null
                ? Guid.TryParseExact(providerKey, "D", out var id) && id != Guid.Empty
                    ? await LoadAvatarAsync(id, version).ConfigureAwait(false)
                    : new OASISResult<IAvatar> { IsError = true, Message = "An enrolled Algorand avatar key must be an OASIS ID." }
                : await LoadPublicAccountAsync(providerKey, version).ConfigureAwait(false);

        private async Task<OASISResult<IAvatar>> LoadPublicAccountAsync(string providerKey, int version)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (version != 0)
                    throw new ArgumentOutOfRangeException(nameof(version), "Algod account lookup returns the current account only.");
                AlgorandPublicAccountMetadataSource.ValidateAddress(providerKey);
                using var deadline = new CancellationTokenSource(_algod.Timeout);
                using var resp = await _algod.GetAsync($"/v2/accounts/{Uri.EscapeDataString(providerKey)}?exclude=all",
                    HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token).ConfigureAwait(false);
                var account = doc.RootElement;
                string address = account.GetProperty("address").GetString();
                if (!string.Equals(address, providerKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("Algod returned a different account address.");
                string status = account.GetProperty("status").GetString();
                if (status is not ("Online" or "Offline" or "NotParticipating"))
                    throw new InvalidOperationException("Algod returned an unrecognized account status.");

                var avatar = new Avatar
                {
                    Id = DeriveGuid(address),
                    Username = address,
                    Description = $"Algorand account {address}"
                };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = address;
                var metadata = GetAlgorandMetadata(avatar);
                metadata["address"] = address;
                metadata["balance_microALGO"] = account.GetProperty("amount").GetUInt64().ToString(CultureInfo.InvariantCulture);
                metadata["balance_without_rewards_microALGO"] = account.GetProperty("amount-without-pending-rewards").GetUInt64().ToString(CultureInfo.InvariantCulture);
                metadata["status"] = status;
                metadata["total_assets_opted_in"] = account.GetProperty("total-assets-opted-in").GetUInt64().ToString(CultureInfo.InvariantCulture);
                metadata["round"] = account.GetProperty("round").GetUInt64().ToString(CultureInfo.InvariantCulture);
                result.Result = avatar;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"AlgorandOASIS: Loaded account '{address}'.";
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
            _recordBoxes == null ? await LoadPublicAccountAsync(avatarUsername, version).ConfigureAwait(false) :
                new OASISResult<IAvatar> { IsError = true, Message = "Algorand enrolled avatar lookup requires the OASIS ID." };

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

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => LoadEnrolledAvatarRecordsAsync<Avatar, IAvatar>("avatar", version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => SaveStoredRecordAsync<Avatar, IAvatar>(avatar, avatar is { Version: > 0 } ? avatar.Version - 1 : 0, "avatar");

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => SaveStoredRecordAsync<AvatarDetail, IAvatarDetail>(avatarDetail,
                avatarDetail is { Version: > 0 } ? avatarDetail.Version - 1 : 0, "avatar-detail");

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

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => LoadStoredRecordAsync<AvatarDetail, IAvatarDetail>(id, version, "avatar-detail");

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

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => LoadEnrolledAvatarRecordsAsync<AvatarDetail, IAvatarDetail>("avatar-detail", version);

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon: Load ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            if (_recordBoxes != null)
                return await LoadEnrolledHolonAsync(id, loadChildren, recursive, maxChildDepth,
                    loadChildrenFromProvider, version).ConfigureAwait(false);
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
            if (_recordBoxes != null && Guid.TryParseExact(providerKey, "D", out var storedId))
                return await LoadEnrolledHolonAsync(storedId, loadChildren, recursive, maxChildDepth,
                    loadChildrenFromProvider, version).ConfigureAwait(false);
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
            if (_recordBoxes != null)
                return await LoadEnrolledCollectionAsync(null, type, loadChildren, recursive, maxChildDepth,
                    curentChildDepth, continueOnError, loadChildrenFromProvider, version).ConfigureAwait(false);
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
            if (_recordBoxes != null)
                return await LoadEnrolledCollectionAsync(id, type, loadChildren, recursive, maxChildDepth,
                    curentChildDepth, continueOnError, loadChildrenFromProvider, version).ConfigureAwait(false);
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
            if (_recordBoxes != null && Guid.TryParseExact(providerKey, "D", out var parentId))
                return await LoadEnrolledCollectionAsync(parentId, type, loadChildren, recursive, maxChildDepth,
                    curentChildDepth, continueOnError, loadChildrenFromProvider, version).ConfigureAwait(false);
            if (type != HolonType.All && type != HolonType.Holon)
                return new OASISResult<IEnumerable<IHolon>>
                {
                    Result = Array.Empty<IHolon>(),
                    IsLoaded = true,
                    Message = $"AlgorandOASIS: Account history has no {type} holons."
                };

            // Load transaction history for an Algorand account via the indexer
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var (holons, hasMore) = await ReadIndexedTransactionsAsync(
                    $"/v2/accounts/{Uri.EscapeDataString(providerKey)}/transactions?").ConfigureAwait(false);

                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
                result.IsWarning = hasMore;
                result.WarningCount = result.IsWarning ? 1 : 0;
                result.Message = result.IsWarning
                    ? $"AlgorandOASIS: Returned {holons.Count} transactions for '{providerKey}'. More indexed history is available."
                    : $"AlgorandOASIS: Loaded {holons.Count} transactions for '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AlgorandOASIS: Error loading transactions for '{providerKey}': {ex.Message}");
            }
            return result;
        }

        private const int IndexerTransactionLimit = 2000;

        private async Task<(List<IHolon> Transactions, bool HasMore)> ReadIndexedTransactionsAsync(string query)
        {
            // Use the existing client timeout for the complete lookup, including all pages and response bodies.
            using var deadline = new CancellationTokenSource(_indexer.Timeout);
            var holons = new List<IHolon>();
            var transactionIds = new HashSet<string>(StringComparer.Ordinal);
            var pageTokens = new HashSet<string>(StringComparer.Ordinal);
            string nextToken = null;
            do
            {
                var pageSize = Math.Min(100, IndexerTransactionLimit - holons.Count);
                var url = query + $"limit={pageSize}" + (nextToken == null ? "" : $"&next={Uri.EscapeDataString(nextToken)}");
                using var response = await _indexer.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,
                    deadline.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var body = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(body, cancellationToken: deadline.Token).ConfigureAwait(false);
                foreach (var transaction in document.RootElement.GetProperty("transactions").EnumerateArray())
                {
                    var transactionId = transaction.GetProperty("id").GetString();
                    if (string.IsNullOrWhiteSpace(transactionId) || !transactionIds.Add(transactionId))
                        throw new InvalidOperationException("The indexer returned a missing or repeated transaction identity.");
                    holons.Add(MapTransactionToHolon(transaction, transactionId));
                }
                nextToken = document.RootElement.TryGetProperty("next-token", out var next) ? next.GetString() : null;
                if (!string.IsNullOrEmpty(nextToken) && !pageTokens.Add(nextToken))
                    throw new InvalidOperationException("The indexer repeated the transaction page token.");
            }
            while (!string.IsNullOrEmpty(nextToken) && holons.Count < IndexerTransactionLimit);
            return (holons, !string.IsNullOrEmpty(nextToken));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(new Dictionary<string, string> { [metaKey ?? string.Empty] = metaValue },
                MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version);

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
            try
            {
                if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
                    throw new ArgumentException("At least one indexed Algorand transaction field is required.");
                if (!Enum.IsDefined(typeof(MetaKeyValuePairMatchMode), metaKeyValuePairMatchMode))
                    throw new ArgumentOutOfRangeException(nameof(metaKeyValuePairMatchMode));

                var filters = metaKeyValuePairs.Select(pair => (Field: pair, Query: AlgorandTransactionFilter(pair.Key, pair.Value))).ToArray();
                // The Indexer accepts one address-role per query. For an All query with both participants,
                // push the sender and all non-address fields down, then check both mapped participants.
                var queries = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? new[] { (Query: string.Join("&", filters.Where(filter =>
                        filter.Field.Key != "receiver" || !metaKeyValuePairs.ContainsKey("sender")).Select(filter => filter.Query)),
                        Fields: filters.Select(filter => filter.Field).ToArray()) }
                    : filters.Select(filter => (filter.Query, Fields: new[] { filter.Field })).ToArray();
                if (type != HolonType.All && type != HolonType.Holon)
                    queries = Array.Empty<(string Query, KeyValuePair<string, string>[] Fields)>();

                var pages = await Task.WhenAll(queries.Select(async query =>
                {
                    var page = await ReadIndexedTransactionsAsync($"/v2/transactions?{query.Query}&").ConfigureAwait(false);
                    var exactMatches = page.Transactions.Where(holon => MatchesProjectedMetadata(holon, query.Fields)).ToList();
                    return (Transactions: exactMatches, page.HasMore);
                })).ConfigureAwait(false);
                var matches = pages.SelectMany(page => page.Transactions)
                    .DistinctBy(holon => holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS])
                    .Take(IndexerTransactionLimit + 1).ToList();
                result.IsWarning = matches.Count > IndexerTransactionLimit || pages.Any(page => page.HasMore);
                result.WarningCount = result.IsWarning ? 1 : 0;
                result.Result = matches.Take(IndexerTransactionLimit).ToList();
                result.IsLoaded = true;
                result.LoadedCount = Math.Min(matches.Count, IndexerTransactionLimit);
                result.Message = result.IsWarning
                    ? $"AlgorandOASIS: Returned {result.LoadedCount} matches. More indexed results are available."
                    : $"AlgorandOASIS: Returned {result.LoadedCount} matches.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AlgorandOASIS: Transaction metadata lookup failed: {ex.Message}");
            }
            return result;
        }

        private static string AlgorandTransactionFilter(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"The metadata field '{key}' requires a value.");
            string encoded = Uri.EscapeDataString(value);
            return key switch
            {
                "tx_id" => $"txid={encoded}",
                "tx_type" => $"tx-type={encoded}",
                "confirmed_round" => $"round={encoded}",
                "sender" => $"address={encoded}&address-role=sender",
                "receiver" => $"address={encoded}&address-role=receiver&exclude-close-to=true",
                "asa_id" => $"asset-id={encoded}",
                "note" => $"note-prefix={Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)))}",
                _ => throw new ArgumentException($"The metadata field '{key}' is not indexed by this provider.")
            };
        }

        private static bool MatchesProjectedMetadata(IHolon holon, IEnumerable<KeyValuePair<string, string>> fields)
        {
            var metadata = holon.ProviderMetaData[Core.Enums.ProviderType.AlgorandOASIS];
            return fields.Where(field => field.Key is "sender" or "receiver" or "asa_id" or "note").All(field =>
                metadata.TryGetValue(field.Key, out var value) && StringComparer.Ordinal.Equals(value, field.Value));
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
            if (_recordBoxes != null)
                return await SaveEnrolledHolonAsync(holon, saveChildren).ConfigureAwait(false);
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
            if (_recordBoxes != null)
                return await DeleteStoredHolonAsync(id).ConfigureAwait(false);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "AlgorandOASIS: Blockchain transactions are immutable and cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (_recordBoxes != null && Guid.TryParseExact(providerKey, "D", out var id))
                return await DeleteStoredHolonAsync(id).ConfigureAwait(false);
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
                    var (transactions, hasMore) = await ReadIndexedTransactionsAsync(
                        $"/v2/transactions?note-prefix={Uri.EscapeDataString(noteB64)}&").ConfigureAwait(false);
                    searchResults.SearchResultHolons = transactions;
                    result.IsWarning = hasMore;
                    result.WarningCount = hasMore ? 1 : 0;
                }
                result.Result = searchResults;
                result.IsLoaded = true;
                result.LoadedCount = searchResults.SearchResultHolons.Count;
                result.Message = result.IsWarning
                    ? $"AlgorandOASIS: Returned {result.LoadedCount} transactions for '{q}'. More indexed matches are available."
                    : $"AlgorandOASIS: Found {result.LoadedCount} transactions for '{q}'.";
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
            DateTime timestamp = t.TryGetProperty("round-time", out var roundTime)
                ? DateTimeOffset.FromUnixTimeSeconds(roundTime.GetInt64()).UtcDateTime
                : default;

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
                CreatedDate = timestamp,
                ModifiedDate = timestamp,
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
