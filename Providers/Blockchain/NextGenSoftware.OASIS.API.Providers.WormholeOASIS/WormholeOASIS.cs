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

namespace NextGenSoftware.OASIS.API.Providers.WormholeOASIS
{
    /// <summary>
    /// Wormhole cross-chain messaging provider. Tracks Wormhole operations (VAAs and their redemptions) through the
    /// Wormholescan API (https://api.wormholescan.io). Wormhole is a messaging protocol, not a data store, so this is an
    /// <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class WormholeOASIS : OASISProvider, ICrossChainTracker
    {
        private static readonly Dictionary<int, string> ChainNames = new()
        {
            [1] = "solana", [2] = "ethereum", [4] = "bsc", [5] = "polygon", [6] = "avalanche", [8] = "algorand",
            [10] = "fantom", [14] = "celo", [15] = "near", [16] = "moonbeam", [21] = "sui", [22] = "aptos",
            [23] = "arbitrum", [24] = "optimism", [30] = "base", [34] = "scroll", [38] = "linea"
        };

        private readonly HttpClient _http;

        public WormholeOASIS(string apiUrl = "https://api.wormholescan.io") : this(apiUrl, null) { }

        internal WormholeOASIS(string apiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            ProviderName = "WormholeOASIS";
            ProviderDescription = "Wormhole cross-chain messaging provider (Wormholescan API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.WormholeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public static string ChainName(int wormholeChainId) => ChainNames.TryGetValue(wormholeChainId, out var n) ? n : $"wormhole-chain-{wormholeChainId}";

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = await GetJsonAsync("api/v1/operations?pageSize=1", CancellationToken.None);
            var activation = new OASISResult<bool>();
            if (result.IsError) { OASISErrorHandling.HandleError(ref activation, result.Message); return activation; }
            IsProviderActivated = true;
            activation.Result = true;
            activation.Message = "WormholeOASIS activated.";
            return activation;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "WormholeOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(sourceTxHash)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("A source transaction hash is required."))
                : ListOperationsAsync($"api/v1/operations?txHash={Uri.EscapeDataString(sourceTxHash)}", cancellationToken);

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => string.IsNullOrWhiteSpace(address)
                ? Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("An address is required."))
                : ListOperationsAsync($"api/v1/operations?address={Uri.EscapeDataString(address)}&pageSize={Math.Clamp(limit, 1, 100)}", cancellationToken);

        /// <summary>Loads one operation by its VAA id components.</summary>
        public async Task<OASISResult<CrossChainTransfer>> GetTransferAsync(int emitterChain, string emitterAddress, string sequence, CancellationToken cancellationToken = default)
        {
            var json = await GetJsonAsync($"api/v1/operations/{emitterChain}/{Uri.EscapeDataString(emitterAddress)}/{Uri.EscapeDataString(sequence)}", cancellationToken);
            if (json.IsError) return Error<CrossChainTransfer>(json.Message);
            return new OASISResult<CrossChainTransfer>(Map(json.Result));
        }

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> ListOperationsAsync(string path, CancellationToken ct)
        {
            var json = await GetJsonAsync(path, ct);
            if (json.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(json.Message);
            // The live API wraps results in {"operations": [...]}; the published swagger documents a bare array.
            var array = json.Result.ValueKind == JsonValueKind.Array ? json.Result : json.Result.GetProperty("operations");
            return new OASISResult<IReadOnlyList<CrossChainTransfer>>(array.EnumerateArray().Select(Map).ToList());
        }

        private static CrossChainTransfer Map(JsonElement op)
        {
            op.TryGetProperty("sourceChain", out var source);
            op.TryGetProperty("targetChain", out var target);
            JsonElement std = default;
            if (op.TryGetProperty("content", out var content)) content.TryGetProperty("standarizedProperties", out std);

            string Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() != "" ? v.GetString() : null;
            int Int(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            string Tx(JsonElement chain) => chain.ValueKind == JsonValueKind.Object && chain.TryGetProperty("transaction", out var t) ? Str(t, "txHash") : null;

            var targetStatus = Str(target, "status");
            var sourceStatus = Str(source, "status");
            var status = targetStatus == "completed" ? CrossChainTransferStatus.Completed
                : targetStatus == "failed" || sourceStatus == "failed" ? CrossChainTransferStatus.Failed
                : sourceStatus != null ? CrossChainTransferStatus.Pending
                : CrossChainTransferStatus.Unknown;

            var fromChain = Int(std, "fromChain") is var fc && fc != 0 ? fc : Int(source, "chainId");
            var toChain = Int(std, "toChain") is var tc && tc != 0 ? tc : Int(target, "chainId");
            DateTimeOffset? ts = DateTimeOffset.TryParse(Str(source, "timestamp"), out var parsed) ? parsed : null;

            return new CrossChainTransfer
            {
                Id = Str(op, "id"),
                Protocol = "wormhole",
                Status = status,
                RawStatus = targetStatus ?? sourceStatus,
                SourceChain = fromChain == 0 ? null : ChainName(fromChain),
                SourceTxHash = Tx(source),
                DestinationChain = toChain == 0 ? null : ChainName(toChain),
                DestinationTxHash = Tx(target),
                Sender = Str(std, "fromAddress") ?? Str(source, "from"),
                Recipient = Str(std, "toAddress") ?? Str(target, "to"),
                Token = Str(std, "tokenAddress"),
                Amount = Str(std, "amount"),
                Timestamp = ts
            };
        }

        private async Task<OASISResult<JsonElement>> GetJsonAsync(string path, CancellationToken ct)
        {
            try
            {
                using var response = await _http.GetAsync(path, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<JsonElement>($"Wormholescan {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                return new OASISResult<JsonElement>(JsonDocument.Parse(body).RootElement.Clone());
            }
            catch (Exception ex) { return Error<JsonElement>($"Wormholescan {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"WormholeOASIS: {message}", ex);
            return result;
        }
    }
}
