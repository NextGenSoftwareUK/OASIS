using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

namespace NextGenSoftware.OASIS.API.Providers.SynapseOASIS
{
    /// <summary>
    /// Synapse bridge provider on the Synapse REST API (https://api.synapseprotocol.com): bridge quotes with
    /// ready-to-sign call data, and FastBridge (RFQ) transfer tracking by origin transaction hash or transaction id.
    /// Synapse is a bridge, not a data store, so this is an <see cref="OASISProvider"/> implementing
    /// <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class SynapseOASIS : OASISProvider, ICrossChainTracker
    {
        private readonly HttpClient _http;

        public SynapseOASIS(string apiUrl = "https://api.synapseprotocol.com") : this(apiUrl, null) { }

        internal SynapseOASIS(string apiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            ProviderName = "SynapseOASIS";
            ProviderDescription = "Synapse bridge provider (Synapse REST API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SynapseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var info = await GetJsonAsync(string.Empty, CancellationToken.None);
            var result = new OASISResult<bool>();
            if (info.IsError) { OASISErrorHandling.HandleError(ref result, info.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "SynapseOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "SynapseOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Bridge routes (with call data to sign) from /bridge/v2. Amounts are in the token's smallest unit.</summary>
        public Task<OASISResult<JsonElement>> GetBridgeQuotesAsync(long fromChainId, string fromToken, string fromAmount, long toChainId, string toToken,
            string fromSender = null, string toRecipient = null, double? slippagePercent = null, CancellationToken ct = default)
        {
            var query = $"bridge/v2?fromChainId={fromChainId}&fromToken={Uri.EscapeDataString(fromToken)}&fromAmount={Uri.EscapeDataString(fromAmount)}" +
                        $"&toChainId={toChainId}&toToken={Uri.EscapeDataString(toToken)}" +
                        (fromSender == null ? "" : $"&fromSender={Uri.EscapeDataString(fromSender)}") +
                        (toRecipient == null ? "" : $"&toRecipient={Uri.EscapeDataString(toRecipient)}") +
                        (slippagePercent == null ? "" : $"&slippage={slippagePercent.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return GetJsonAsync(query, ct);
        }

        /// <summary>FastBridge transfer by origin transaction hash or Synapse transaction id.</summary>
        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceTxHash)) return Error<IReadOnlyList<CrossChainTransfer>>("A source transaction hash is required.");
            var json = await GetJsonAsync($"transaction-id/{Uri.EscapeDataString(sourceTxHash)}", cancellationToken);
            if (json.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(json.Message);
            // A miss is HTTP 200 with {"message":"Transaction not found"}.
            if (!json.Result.TryGetProperty("Bridge", out _))
                return new OASISResult<IReadOnlyList<CrossChainTransfer>>(Array.Empty<CrossChainTransfer>()) { Message = "No Synapse transfer found." };
            return new OASISResult<IReadOnlyList<CrossChainTransfer>>(new[] { Map(json.Result) });
        }

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("The Synapse API has no transfer lookup by address; look transfers up by origin transaction hash."));

        internal static CrossChainTransfer Map(JsonElement t)
        {
            static JsonElement Prop(JsonElement e, params string[] path)
            {
                foreach (var p in path)
                    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e) || e.ValueKind == JsonValueKind.Null) return default;
                return e;
            }
            static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Number ? e.GetRawText() : null;

            var relayed = Prop(t, "BridgeRelay").ValueKind == JsonValueKind.Object;
            var refunded = Prop(t, "BridgeRefund").ValueKind == JsonValueKind.Object;
            var requested = Prop(t, "BridgeRequest", "blockTimestamp");
            return new CrossChainTransfer
            {
                Id = Str(Prop(t, "Bridge", "transactionId")),
                Protocol = "synapse",
                Status = refunded ? CrossChainTransferStatus.Refunded : relayed ? CrossChainTransferStatus.Completed : CrossChainTransferStatus.Pending,
                RawStatus = refunded ? "refunded" : relayed ? "relayed" : "requested",
                SourceChain = Str(Prop(t, "Bridge", "originChain")),
                SourceTxHash = Str(Prop(t, "BridgeRequest", "transactionHash")),
                DestinationChain = Str(Prop(t, "Bridge", "destChain")),
                DestinationTxHash = Str(Prop(t, "BridgeRelay", "transactionHash")),
                Sender = Str(Prop(t, "Bridge", "sender")),
                Recipient = Str(Prop(t, "BridgeRelay", "to")),
                Token = Str(Prop(t, "Bridge", "originToken")),
                Amount = Str(Prop(t, "Bridge", "originAmountFormatted")),
                Timestamp = requested.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(requested.GetInt64()) : null
            };
        }

        private async Task<OASISResult<JsonElement>> GetJsonAsync(string path, CancellationToken ct)
        {
            try
            {
                using var response = await _http.GetAsync(path, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<JsonElement>($"Synapse API {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                return new OASISResult<JsonElement>(JsonDocument.Parse(body).RootElement.Clone());
            }
            catch (Exception ex) { return Error<JsonElement>($"Synapse API {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"SynapseOASIS: {message}", ex);
            return result;
        }
    }
}
