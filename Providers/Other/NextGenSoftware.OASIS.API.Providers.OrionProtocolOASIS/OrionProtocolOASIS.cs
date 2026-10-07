using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS
{
    /// <summary>
    /// OrionProtocolOASIS Provider — integrates the Orion Protocol DEX aggregator with OASIS.
    /// Orion Protocol is a decentralised exchange aggregator and does NOT support avatar/holon storage.
    /// All storage methods return an explicit, actionable error directing callers to a storage provider.
    /// </summary>
    public class OrionProtocolOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private const string StorageNotSupported =
            "OrionProtocolOASIS is a DEX aggregator and does not support avatar/holon storage. " +
            "Use a storage provider such as MongoDBOASIS.";

        private readonly string _apiBaseUrl;
        private HttpClient _httpClient;

        public OrionProtocolOASIS(string apiBaseUrl = "https://trade.orionprotocol.io/api/v1/")
        {
            this.ProviderName = "OrionProtocolOASIS";
            this.ProviderDescription = "Orion Protocol DEX Aggregator Provider for OASIS";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OrionProtocolOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            _apiBaseUrl = apiBaseUrl.TrimEnd('/') + "/";
        }

        #region Activation

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (IsProviderActivated)
                {
                    result.Result = true;
                    return result;
                }

                _httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
                var response = await _httpClient.GetAsync("info");

                if (response.IsSuccessStatusCode)
                {
                    IsProviderActivated = true;
                    result.Result = true;
                    result.Message = "OrionProtocolOASIS activated successfully.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"OrionProtocol /info endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error activating OrionProtocolOASIS: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _httpClient?.Dispose();
            _httpClient = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            return await Task.FromResult(DeActivateProvider());
        }

        #endregion

        #region Orion API helpers

        public async Task<OASISResult<JsonDocument>> GetTickerAsync(string symbol = "ORN-USDT")
        {
            var result = new OASISResult<JsonDocument>();
            try
            {
                var response = await _httpClient.GetAsync($"ticker?symbols={symbol}");
                var json = await response.Content.ReadAsStringAsync();
                result.Result = JsonDocument.Parse(json);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error fetching Orion ticker: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<JsonDocument>> GetOrderBookAsync(string symbol = "ORN-USDT", int depth = 10)
        {
            var result = new OASISResult<JsonDocument>();
            try
            {
                var response = await _httpClient.GetAsync($"orderbook?symbol={symbol}&depth={depth}");
                var json = await response.Content.ReadAsStringAsync();
                result.Result = JsonDocument.Parse(json);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error fetching Orion order book: {ex.Message}", ex);
            }
            return result;
        }

        private async Task<OASISResult<IHolon>> LoadPairHolonAsync(string symbol)
        {
            var result = new OASISResult<IHolon>();
            if (!IsProviderActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError) { OASISErrorHandling.HandleError(ref result, activation.Message); return result; }
            }

            var ticker = await GetTickerAsync(symbol);
            if (ticker.IsError) { OASISErrorHandling.HandleError(ref result, ticker.Message); return result; }
            var orderBook = await GetOrderBookAsync(symbol);
            if (orderBook.IsError) { OASISErrorHandling.HandleError(ref result, orderBook.Message); return result; }

            var holon = new Holon { Name = symbol, Description = $"Orion Protocol market {symbol}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.OrionProtocolOASIS] = symbol;
            holon.MetaData["symbol"] = symbol;
            holon.MetaData["ticker"] = ticker.Result.RootElement.GetRawText();
            holon.MetaData["orderbook"] = orderBook.Result.RootElement.GetRawText();
            result.Result = holon;
            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadPairHolonsAsync(IEnumerable<string> symbols)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var holons = new List<IHolon>();
            foreach (var symbol in symbols)
            {
                var pair = await LoadPairHolonAsync(symbol);
                if (pair.IsError) { OASISErrorHandling.HandleError(ref result, pair.Message); return result; }
                holons.Add(pair.Result);
            }
            result.Result = holons;
            return result;
        }

        private static IEnumerable<string> SplitSymbols(string providerKey) =>
            providerKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        #endregion

        #region IOASISStorageProvider — market reads

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadPairHolonAsync(providerKey);
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadPairHolonAsync(providerKey).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadPairHolonsAsync(SplitSymbols(providerKey));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadPairHolonsAsync(SplitSymbols(providerKey)).GetAwaiter().GetResult();

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var symbols = searchParams?.SearchGroups?
                .OfType<ISearchTextGroup>()
                .Select(g => g.SearchQuery)
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .SelectMany(SplitSymbols)
                .ToList() ?? new List<string>();
            if (symbols.Count == 0) { OASISErrorHandling.HandleError(ref result, "OrionProtocolOASIS search requires a trading pair symbol such as ORN-USDT in a text search group."); return result; }

            var pairs = await LoadPairHolonsAsync(symbols);
            if (pairs.IsError) { OASISErrorHandling.HandleError(ref result, pairs.Message); return result; }
            result.Result = new SearchResults { SearchResultHolons = pairs.Result.ToList(), NumberOfResults = pairs.Result.Count() };
            return result;
        }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        #endregion

        #region IOASISStorageProvider — not supported

        private static OASISResult<T> NotSupported<T>() =>
            new OASISResult<T> { IsError = true, Message = StorageNotSupported };

        private static Task<OASISResult<T>> NotSupportedAsync<T>() =>
            Task.FromResult(NotSupported<T>());

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => NotSupported<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) => NotSupportedAsync<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => NotSupported<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0) => NotSupportedAsync<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => NotSupported<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0) => NotSupportedAsync<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NotSupported<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => NotSupportedAsync<IAvatar>();
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => NotSupported<IEnumerable<IAvatar>>();
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => NotSupportedAsync<IEnumerable<IAvatar>>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => NotSupported<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => NotSupportedAsync<IAvatarDetail>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => NotSupported<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0) => NotSupportedAsync<IAvatarDetail>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => NotSupported<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0) => NotSupportedAsync<IAvatarDetail>();
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NotSupported<IEnumerable<IAvatarDetail>>();
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => NotSupportedAsync<IEnumerable<IAvatarDetail>>();
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => NotSupported<IAvatar>();
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) => NotSupportedAsync<IAvatar>();
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => NotSupported<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) => NotSupportedAsync<IAvatarDetail>();
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NotSupported<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => NotSupportedAsync<bool>();
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NotSupported<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => NotSupportedAsync<bool>();
        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => NotSupported<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true) => NotSupportedAsync<bool>();
        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => NotSupported<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true) => NotSupportedAsync<bool>();
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IHolon>();
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IHolon>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupported<IHolon>();
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupportedAsync<IHolon>();
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IHolon> DeleteHolon(Guid id) => NotSupported<IHolon>();
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => NotSupportedAsync<IHolon>();
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotSupported<IHolon>();
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => NotSupportedAsync<IHolon>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotSupported<bool>();
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => NotSupportedAsync<bool>();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NotSupported<IEnumerable<IHolon>>();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>();

        #endregion
    }
}
