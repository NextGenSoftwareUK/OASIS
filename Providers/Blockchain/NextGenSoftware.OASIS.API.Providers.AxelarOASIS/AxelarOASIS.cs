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

namespace NextGenSoftware.OASIS.API.Providers.AxelarOASIS
{
    /// <summary>
    /// Axelar General Message Passing (GMP) provider. Tracks GMP calls and token transfers through the Axelarscan API
    /// (https://api.axelarscan.io/gmp/searchGMP). Axelar is a cross-chain network, not a data store, so this is an
    /// <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class AxelarOASIS : OASISProvider, ICrossChainTracker
    {
        private const int MaxPageSize = 25; // searchGMP rejects size > 25
        private readonly HttpClient _http;

        public AxelarOASIS(string apiUrl = "https://api.axelarscan.io") : this(apiUrl, null) { }

        internal AxelarOASIS(string apiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            ProviderName = "AxelarOASIS";
            ProviderDescription = "Axelar GMP cross-chain provider (Axelarscan API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AxelarOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var probe = await SearchAsync(new Dictionary<string, object> { ["size"] = 1 }, CancellationToken.None);
            var result = new OASISResult<bool>();
            if (probe.IsError) { OASISErrorHandling.HandleError(ref result, probe.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "AxelarOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "AxelarOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(sourceTxHash)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("A source transaction hash is required."))
                : SearchAsync(new Dictionary<string, object> { ["txHash"] = sourceTxHash }, cancellationToken);

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(address)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("An address is required."))
                : SearchAllAsync(new Dictionary<string, object> { ["senderAddress"] = address }, Math.Max(1, limit), cancellationToken);

        /// <summary>Loads a GMP message by its Axelar message id.</summary>
        public async Task<OASISResult<CrossChainTransfer>> GetTransferByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var found = await SearchAsync(new Dictionary<string, object> { ["messageId"] = messageId }, cancellationToken);
            if (found.IsError) return Error<CrossChainTransfer>(found.Message);
            return found.Result.Count == 0 ? Error<CrossChainTransfer>($"No GMP message with id {messageId}.") : new OASISResult<CrossChainTransfer>(found.Result[0]);
        }

        internal static CrossChainTransferStatus MapStatus(string status) => status switch
        {
            "executed" or "express_executed" => CrossChainTransferStatus.Completed,
            "error" or "cannot_execute" or "cannot_execute_message" => CrossChainTransferStatus.Failed,
            "called" or "confirming" or "confirmed" or "approving" or "approved" or "executing" or "insufficient_fee" or "not_enough_gas_to_execute" => CrossChainTransferStatus.Pending,
            _ => CrossChainTransferStatus.Unknown
        };

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> SearchAllAsync(Dictionary<string, object> filter, int limit, CancellationToken ct)
        {
            var all = new List<CrossChainTransfer>();
            while (all.Count < limit)
            {
                var page = new Dictionary<string, object>(filter) { ["size"] = Math.Min(MaxPageSize, limit - all.Count), ["from"] = all.Count };
                var result = await SearchAsync(page, ct);
                if (result.IsError) return result;
                all.AddRange(result.Result);
                if (result.Result.Count < (int)page["size"]) break;
            }
            return new OASISResult<IReadOnlyList<CrossChainTransfer>>(all);
        }

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> SearchAsync(Dictionary<string, object> body, CancellationToken ct)
        {
            try
            {
                using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync("gmp/searchGMP", content, ct);
                var text = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                // Axelarscan reports validation errors as {"error": true, "message": ...}, sometimes with HTTP 200.
                if (!response.IsSuccessStatusCode || (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.True))
                    return Error<IReadOnlyList<CrossChainTransfer>>($"searchGMP failed: {(int)response.StatusCode} {(root.TryGetProperty("message", out var m) ? m.GetString() : text)}");
                return new OASISResult<IReadOnlyList<CrossChainTransfer>>(root.GetProperty("data").EnumerateArray().Select(Map).ToList());
            }
            catch (Exception ex) { return Error<IReadOnlyList<CrossChainTransfer>>($"searchGMP failed: {ex.Message}", ex); }
        }

        private static CrossChainTransfer Map(JsonElement g)
        {
            static JsonElement Prop(JsonElement e, params string[] path)
            {
                foreach (var p in path)
                    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e)) return default;
                return e;
            }
            static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Number ? e.GetRawText() : null;

            var status = Str(Prop(g, "status"));
            var seconds = Prop(g, "call", "block_timestamp");
            return new CrossChainTransfer
            {
                Id = Str(Prop(g, "message_id")) ?? Str(Prop(g, "call", "id")),
                Protocol = "axelar",
                Status = MapStatus(status),
                RawStatus = status,
                SourceChain = Str(Prop(g, "call", "chain")),
                SourceTxHash = Str(Prop(g, "call", "transactionHash")),
                DestinationChain = Str(Prop(g, "call", "returnValues", "destinationChain"))?.ToLowerInvariant(),
                DestinationTxHash = Str(Prop(g, "executed", "transactionHash")),
                Sender = Str(Prop(g, "call", "returnValues", "sender")),
                Recipient = Str(Prop(g, "call", "returnValues", "destinationContractAddress")),
                Token = Str(Prop(g, "call", "returnValues", "symbol")),
                Amount = Str(Prop(g, "call", "returnValues", "amount")),
                Timestamp = seconds.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(seconds.GetInt64()) : null
            };
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"AxelarOASIS: {message}", ex);
            return result;
        }
    }
}
