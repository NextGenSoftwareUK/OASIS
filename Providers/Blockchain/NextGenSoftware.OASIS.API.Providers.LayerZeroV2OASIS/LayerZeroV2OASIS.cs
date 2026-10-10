using System;
using System.Collections.Generic;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS
{
    /// <summary>
    /// LayerZero V2 omnichain messaging provider. Tracks messages through the LayerZero Scan API
    /// (https://scan.layerzero-api.com/v1). LayerZero is a messaging protocol, not a data store, so this is an
    /// <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class LayerZeroV2OASIS : OASISProvider, ICrossChainTracker
    {
        private readonly HttpClient _http;

        public LayerZeroV2OASIS(string scanApiUrl = "https://scan.layerzero-api.com/v1") : this(scanApiUrl, null) { }

        internal LayerZeroV2OASIS(string scanApiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(scanApiUrl.TrimEnd('/') + "/");
            ProviderName = "LayerZeroV2OASIS";
            ProviderDescription = "LayerZero V2 omnichain messaging provider (LayerZero Scan API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LayerZeroV2OASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var probe = await QueryAsync("messages/latest?limit=1", CancellationToken.None);
            var result = new OASISResult<bool>();
            if (probe.IsError) { OASISErrorHandling.HandleError(ref result, probe.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "LayerZeroV2OASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "LayerZeroV2OASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(sourceTxHash)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("A source transaction hash is required."))
                : QueryAsync($"messages/tx/{Uri.EscapeDataString(sourceTxHash)}", cancellationToken);

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(address)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("An address is required."))
                : QueryAsync($"messages/wallet/{Uri.EscapeDataString(address)}?limit={Math.Clamp(limit, 1, 100)}", cancellationToken);

        /// <summary>Loads a message by its LayerZero GUID.</summary>
        public async Task<OASISResult<CrossChainTransfer>> GetTransferByGuidAsync(string guid, CancellationToken cancellationToken = default)
        {
            var list = await QueryAsync($"messages/guid/{Uri.EscapeDataString(guid)}", cancellationToken);
            if (list.IsError) return Error<CrossChainTransfer>(list.Message);
            return list.Result.Count == 0 ? Error<CrossChainTransfer>($"No message with GUID {guid}.") : new OASISResult<CrossChainTransfer>(list.Result[0]);
        }

        internal static CrossChainTransferStatus MapStatus(string name) => name switch
        {
            "DELIVERED" => CrossChainTransferStatus.Completed,
            "INFLIGHT" or "CONFIRMING" or "PAYLOAD_STORED" => CrossChainTransferStatus.Pending,
            "FAILED" or "BLOCKED" or "APPLICATION_BURNED" or "APPLICATION_SKIPPED" or "UNRESOLVABLE_COMMAND" or "MALFORMED_COMMAND" => CrossChainTransferStatus.Failed,
            _ => CrossChainTransferStatus.Unknown
        };

        private static CrossChainTransfer Map(JsonElement m)
        {
            static JsonElement Prop(JsonElement e, params string[] path)
            {
                foreach (var p in path)
                {
                    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e)) return default;
                }
                return e;
            }
            static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Number ? e.GetRawText() : null;

            var statusName = Str(Prop(m, "status", "name"));
            var created = Str(Prop(m, "created"));
            return new CrossChainTransfer
            {
                Id = Str(Prop(m, "guid")),
                Protocol = "layerzero-v2",
                Status = MapStatus(statusName),
                RawStatus = statusName,
                SourceChain = Str(Prop(m, "pathway", "sender", "chain")) ?? Str(Prop(m, "pathway", "srcEid")),
                SourceTxHash = Str(Prop(m, "source", "tx", "txHash")),
                DestinationChain = Str(Prop(m, "pathway", "receiver", "chain")) ?? Str(Prop(m, "pathway", "dstEid")),
                DestinationTxHash = Str(Prop(m, "destination", "tx", "txHash")),
                Sender = Str(Prop(m, "source", "tx", "from")) ?? Str(Prop(m, "pathway", "sender", "address")),
                Recipient = Str(Prop(m, "pathway", "receiver", "address")),
                Timestamp = DateTimeOffset.TryParse(created, out var ts) ? ts : null
            };
        }

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> QueryAsync(string path, CancellationToken ct)
        {
            try
            {
                using var response = await _http.GetAsync(path, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                // The scan API answers 404 when nothing matches a lookup.
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new OASISResult<IReadOnlyList<CrossChainTransfer>>(Array.Empty<CrossChainTransfer>()) { Message = "No LayerZero messages found." };
                if (!response.IsSuccessStatusCode)
                    return Error<IReadOnlyList<CrossChainTransfer>>($"LayerZero Scan {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                using var doc = JsonDocument.Parse(body);
                return new OASISResult<IReadOnlyList<CrossChainTransfer>>(doc.RootElement.GetProperty("data").EnumerateArray().Select(Map).ToList());
            }
            catch (Exception ex) { return Error<IReadOnlyList<CrossChainTransfer>>($"LayerZero Scan {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"LayerZeroV2OASIS: {message}", ex);
            return result;
        }
    }
}
