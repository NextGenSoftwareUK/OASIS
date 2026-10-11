using System;
using System.Collections.Generic;
using System.Net;
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

namespace NextGenSoftware.OASIS.API.Providers.ChainflipOASIS
{
    /// <summary>
    /// Chainflip cross-chain swap provider on the Chainflip swap backend (https://chainflip-swap.chainflip.io): swap quotes
    /// (/v2/quote) and swap tracking (/v2/swaps/{swapId | deposit channel id | deposit tx}). Chainflip is a cross-chain
    /// AMM, not a data store, so this is an <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class ChainflipOASIS : OASISProvider, ICrossChainTracker
    {
        private readonly HttpClient _http;

        public ChainflipOASIS(string apiUrl = "https://chainflip-swap.chainflip.io") : this(apiUrl, null) { }

        internal ChainflipOASIS(string apiUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            ProviderName = "ChainflipOASIS";
            ProviderDescription = "Chainflip cross-chain swap provider (Chainflip swap API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ChainflipOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                using var response = await _http.GetAsync("healthcheck");
                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"ChainflipOASIS: healthcheck failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ChainflipOASIS: healthcheck failed: {ex.Message}", ex);
                return result;
            }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "ChainflipOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "ChainflipOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Swap quotes from /v2/quote. Chains and assets use Chainflip names (e.g. Ethereum/ETH, Bitcoin/BTC); amount is in base units.</summary>
        public Task<OASISResult<JsonElement>> GetQuoteAsync(string srcChain, string srcAsset, string destChain, string destAsset, string amount, CancellationToken ct = default)
            => GetJsonAsync($"v2/quote?srcChain={Uri.EscapeDataString(srcChain)}&srcAsset={Uri.EscapeDataString(srcAsset)}" +
                            $"&destChain={Uri.EscapeDataString(destChain)}&destAsset={Uri.EscapeDataString(destAsset)}&amount={Uri.EscapeDataString(amount)}", ct);

        /// <summary>Swap by deposit transaction hash, swap id or deposit channel id.</summary>
        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceTxHash)) return Error<IReadOnlyList<CrossChainTransfer>>("A deposit transaction hash or swap id is required.");
            var path = $"v2/swaps/{Uri.EscapeDataString(sourceTxHash)}";
            try
            {
                using var response = await _http.GetAsync(path, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return new OASISResult<IReadOnlyList<CrossChainTransfer>>(Array.Empty<CrossChainTransfer>()) { Message = "No Chainflip swap found." };
                if (!response.IsSuccessStatusCode)
                    return Error<IReadOnlyList<CrossChainTransfer>>($"Chainflip API {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                return new OASISResult<IReadOnlyList<CrossChainTransfer>>(new[] { Map(JsonDocument.Parse(body).RootElement) });
            }
            catch (Exception ex) { return Error<IReadOnlyList<CrossChainTransfer>>($"Chainflip API {path} failed: {ex.Message}", ex); }
        }

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(Error<IReadOnlyList<CrossChainTransfer>>("The Chainflip swap API has no lookup by address; look swaps up by deposit transaction hash, swap id or deposit channel id."));

        internal static CrossChainTransfer Map(JsonElement s)
        {
            static JsonElement Prop(JsonElement e, params string[] path)
            {
                foreach (var p in path)
                    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out e) || e.ValueKind == JsonValueKind.Null) return default;
                return e;
            }
            static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ValueKind == JsonValueKind.Number ? e.GetRawText() : null;

            var state = Str(Prop(s, "state"));
            var refunded = Prop(s, "refundEgress").ValueKind == JsonValueKind.Object;
            var status = state switch
            {
                "COMPLETED" => refunded ? CrossChainTransferStatus.Refunded : CrossChainTransferStatus.Completed,
                "FAILED" => CrossChainTransferStatus.Failed,
                "WAITING" or "RECEIVING" or "SWAPPING" or "SENDING" or "SENT" => CrossChainTransferStatus.Pending,
                _ => CrossChainTransferStatus.Unknown
            };
            var witnessed = Prop(s, "deposit", "witnessedAt");
            return new CrossChainTransfer
            {
                Id = Str(Prop(s, "swapId")),
                Protocol = "chainflip",
                Status = status,
                RawStatus = state,
                SourceChain = Str(Prop(s, "srcChain")),
                SourceTxHash = Str(Prop(s, "deposit", "txRef")),
                DestinationChain = Str(Prop(s, "destChain")),
                DestinationTxHash = Str(Prop(s, refunded ? "refundEgress" : "swapEgress", "txRef")),
                Sender = Str(Prop(s, "depositChannel", "depositAddress")),
                Recipient = Str(Prop(s, "destAddress")),
                Token = Str(Prop(s, "srcAsset")),
                Amount = Str(Prop(s, "deposit", "amount")),
                Timestamp = witnessed.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeMilliseconds(witnessed.GetInt64()) : null
            };
        }

        private async Task<OASISResult<JsonElement>> GetJsonAsync(string path, CancellationToken ct)
        {
            try
            {
                using var response = await _http.GetAsync(path, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<JsonElement>($"Chainflip API {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                return new OASISResult<JsonElement>(JsonDocument.Parse(body).RootElement.Clone());
            }
            catch (Exception ex) { return Error<JsonElement>($"Chainflip API {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"ChainflipOASIS: {message}", ex);
            return result;
        }
    }
}
