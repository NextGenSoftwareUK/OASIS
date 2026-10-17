using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.deBridgeOASIS
{
    /// <summary>
    /// deBridge Liquidity Network (DLN) provider. Tracks cross-chain orders through the DLN stats API
    /// (https://stats-api.dln.trade). deBridge is a cross-chain trading network, not a data store, so this is an
    /// <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class deBridgeOASIS : OASISProvider, ICrossChainTracker
    {
        private const int MaxOrders = 100;

        // deBridge uses EVM chain ids for EVM chains and its own ids for others (7565164 = Solana).
        private static readonly Dictionary<string, string> ChainNames = new()
        {
            ["1"] = "ethereum", ["10"] = "optimism", ["56"] = "bsc", ["137"] = "polygon", ["8453"] = "base",
            ["42161"] = "arbitrum", ["43114"] = "avalanche", ["59144"] = "linea", ["7565164"] = "solana"
        };

        private readonly HttpClient _http;

        public deBridgeOASIS(string statsApiUrl = "https://stats-api.dln.trade") : this(statsApiUrl, null) { }

        internal deBridgeOASIS(string statsApiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(statsApiUrl.TrimEnd('/') + "/");
            ProviderName = "deBridgeOASIS";
            ProviderDescription = "deBridge DLN cross-chain order provider (DLN stats API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.deBridgeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public static string ChainName(string debridgeChainId) => debridgeChainId != null && ChainNames.TryGetValue(debridgeChainId, out var n) ? n : debridgeChainId == null ? null : $"debridge-chain-{debridgeChainId}";

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var probe = await SendAsync(HttpMethod.Post, "api/Orders/filteredList", JsonSerializer.Serialize(new { skip = 0, take = 1 }), CancellationToken.None);
            var result = new OASISResult<bool>();
            if (probe.IsError) { OASISErrorHandling.HandleError(ref result, probe.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "deBridgeOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "deBridgeOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceTxHash)) return Error<IReadOnlyList<CrossChainTransfer>>("A source transaction hash is required.");
            var ids = await SendAsync(HttpMethod.Get, $"api/Transaction/{Uri.EscapeDataString(sourceTxHash)}/orderIds", null, cancellationToken);
            if (ids.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(ids.Message);
            return await LoadOrdersAsync(ids.Result.GetProperty("orderIds").EnumerateArray().Select(o => o.GetProperty("stringValue").GetString()), cancellationToken);
        }

        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address)) return Error<IReadOnlyList<CrossChainTransfer>>("An address is required.");
            var list = await SendAsync(HttpMethod.Post, "api/Orders/filteredList",
                JsonSerializer.Serialize(new { maker = address, skip = 0, take = Math.Clamp(limit, 1, MaxOrders) }), cancellationToken);
            if (list.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(list.Message);
            return await LoadOrdersAsync(list.Result.GetProperty("orders").EnumerateArray().Select(o => o.GetProperty("orderId").GetProperty("stringValue").GetString()), cancellationToken);
        }

        /// <summary>Loads one DLN order by its order id.</summary>
        public async Task<OASISResult<CrossChainTransfer>> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var order = await SendAsync(HttpMethod.Get, $"api/Orders/{Uri.EscapeDataString(orderId)}", null, cancellationToken);
            return order.IsError ? Error<CrossChainTransfer>(order.Message) : new OASISResult<CrossChainTransfer>(Map(order.Result));
        }

        internal static CrossChainTransferStatus MapState(string state) => state switch
        {
            "Created" => CrossChainTransferStatus.Pending,
            "Fulfilled" or "SentUnlock" or "ClaimedUnlock" => CrossChainTransferStatus.Completed,
            "OrderCancelled" or "SentOrderCancel" or "ClaimedOrderCancel" => CrossChainTransferStatus.Refunded,
            _ => CrossChainTransferStatus.Unknown
        };

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> LoadOrdersAsync(IEnumerable<string> orderIds, CancellationToken ct)
        {
            var transfers = new List<CrossChainTransfer>();
            foreach (var id in orderIds.Take(MaxOrders))
            {
                var order = await GetOrderAsync(id, ct);
                if (order.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(order.Message);
                transfers.Add(order.Result);
            }
            return new OASISResult<IReadOnlyList<CrossChainTransfer>>(transfers);
        }

        private static CrossChainTransfer Map(JsonElement o)
        {
            static JsonElement Prop(JsonElement e, params string[] path)
            {
                foreach (var p in path)
                    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e)) return default;
                return e;
            }
            static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Number ? e.GetRawText() : null;

            var state = Str(Prop(o, "state"));
            var created = Prop(o, "createdSrcEventMetadata", "blockTimeStamp");
            return new CrossChainTransfer
            {
                Id = Str(Prop(o, "orderId", "stringValue")),
                Protocol = "debridge",
                Status = MapState(state),
                RawStatus = state,
                SourceChain = ChainName(Str(Prop(o, "giveOfferWithMetadata", "chainId", "stringValue"))),
                SourceTxHash = Str(Prop(o, "createdSrcEventMetadata", "transactionHash", "stringValue")),
                DestinationChain = ChainName(Str(Prop(o, "takeOfferWithMetadata", "chainId", "stringValue"))),
                DestinationTxHash = Str(Prop(o, "fulfilledDstEventMetadata", "transactionHash", "stringValue")),
                Sender = Str(Prop(o, "makerSrc", "stringValue")),
                Recipient = Str(Prop(o, "receiverDst", "stringValue")),
                Token = Str(Prop(o, "giveOfferWithMetadata", "metadata", "symbol")),
                Amount = Str(Prop(o, "giveOfferWithMetadata", "amount", "stringValue")),
                Timestamp = created.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(created.GetInt64()) : null
            };
        }

        private async Task<OASISResult<JsonElement>> SendAsync(HttpMethod method, string path, string jsonBody, CancellationToken ct)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (jsonBody != null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<JsonElement>($"DLN stats API {method} {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                return new OASISResult<JsonElement>(JsonDocument.Parse(body).RootElement.Clone());
            }
            catch (Exception ex) { return Error<JsonElement>($"DLN stats API {method} {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"deBridgeOASIS: {message}", ex);
            return result;
        }
    }
}
