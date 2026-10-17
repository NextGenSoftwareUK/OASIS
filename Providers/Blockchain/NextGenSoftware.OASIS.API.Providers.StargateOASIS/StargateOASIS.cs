using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.StargateOASIS
{
    public sealed class StargateQuoteRequest
    {
        public string SrcChainKey { get; init; }
        public string SrcTokenAddress { get; init; }
        public string SrcWalletAddress { get; init; }
        public string DstChainKey { get; init; }
        public string DstTokenAddress { get; init; }
        public string DstWalletAddress { get; init; }
        /// <summary>Amount in the source token's smallest unit.</summary>
        public BigInteger Amount { get; init; }
    }

    public sealed class StargateQuote
    {
        public string Id { get; init; }
        public IReadOnlyList<string> RouteTypes { get; init; }
        public BigInteger SrcAmount { get; init; }
        public BigInteger DstAmount { get; init; }
        public BigInteger? DstAmountMin { get; init; }
        public string FeeUsd { get; init; }
        public string EstimatedDuration { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        /// <summary>Transactions/signatures the user must perform, exactly as returned by the API.</summary>
        public JsonElement UserSteps { get; init; }
    }

    public sealed class StargateTransferStatus
    {
        public CrossChainTransferStatus Status { get; init; }
        public string RawStatus { get; init; }
        public string ExplorerUrl { get; init; }
        public IReadOnlyList<(string Event, string ChainKey, string TxHash)> History { get; init; }
    }

    /// <summary>
    /// Stargate (LayerZero) bridge provider. Quotes and unsigned transfer steps come from the LayerZero Value Transfer
    /// API (https://transfer.layerzero-api.com/v1), which replaced the deprecated stargate.finance API; quotes require an
    /// API key. Transfers are tracked as Stargate-sent LayerZero messages via LayerZero Scan.
    /// </summary>
    public class StargateOASIS : OASISProvider, ICrossChainTracker
    {
        private readonly HttpClient _http;
        private readonly LayerZeroV2OASIS.LayerZeroV2OASIS _scan;
        private readonly bool _hasApiKey;

        public StargateOASIS(string apiKey = null, string apiUrl = "https://transfer.layerzero-api.com/v1", string scanApiUrl = "https://scan.layerzero-api.com/v1")
            : this(apiKey, apiUrl, new LayerZeroV2OASIS.LayerZeroV2OASIS(scanApiUrl), null) { }

        internal StargateOASIS(string apiKey, string apiUrl, LayerZeroV2OASIS.LayerZeroV2OASIS scan, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            _hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
            if (_hasApiKey) _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _scan = scan;
            ProviderName = "StargateOASIS";
            ProviderDescription = "Stargate bridge provider (LayerZero Value Transfer API + LayerZero Scan).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.StargateOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var chains = await GetChainsAsync();
            var result = new OASISResult<bool>();
            if (chains.IsError) { OASISErrorHandling.HandleError(ref result, chains.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = _hasApiKey ? "StargateOASIS activated." : "StargateOASIS activated without an API key: quotes and transfer building are unavailable.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "StargateOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Supported chains (chainKey, chainId, chainType, native currency).</summary>
        public Task<OASISResult<JsonElement>> GetChainsAsync(CancellationToken ct = default)
            => SendAsync(HttpMethod.Get, "chains", null, root => root.GetProperty("chains").Clone(), ct);

        /// <summary>Supported tokens, optionally only those transferable from a given chain/token.</summary>
        public Task<OASISResult<JsonElement>> GetTokensAsync(string transferrableFromChainKey = null, string transferrableFromTokenAddress = null, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(transferrableFromChainKey)) query.Add($"transferrableFromChainKey={Uri.EscapeDataString(transferrableFromChainKey)}");
            if (!string.IsNullOrWhiteSpace(transferrableFromTokenAddress)) query.Add($"transferrableFromTokenAddress={Uri.EscapeDataString(transferrableFromTokenAddress)}");
            return SendAsync(HttpMethod.Get, "tokens" + (query.Count == 0 ? "" : "?" + string.Join("&", query)), null, root => root.GetProperty("tokens").Clone(), ct);
        }

        /// <summary>Requests bridge quotes (requires an API key).</summary>
        public Task<OASISResult<IReadOnlyList<StargateQuote>>> GetQuotesAsync(StargateQuoteRequest request, CancellationToken ct = default)
        {
            if (!_hasApiKey) return Task.FromResult(Error<IReadOnlyList<StargateQuote>>("A LayerZero Value Transfer API key is required for quotes."));
            if (request == null || request.Amount <= 0) return Task.FromResult(Error<IReadOnlyList<StargateQuote>>("A quote request with a positive amount is required."));
            return SendAsync(HttpMethod.Post, "quotes", QuoteBody(request), root =>
            {
                if (root.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException(err.ToString());
                return (IReadOnlyList<StargateQuote>)root.GetProperty("quotes").EnumerateArray().Select(MapQuote).ToList();
            }, ct);
        }

        /// <summary>Builds the unsigned user steps (transactions/signatures) for a quote, for the chain providers to sign.</summary>
        public Task<OASISResult<JsonElement>> BuildUserStepsAsync(string quoteId, CancellationToken ct = default)
            => !_hasApiKey ? Task.FromResult(Error<JsonElement>("A LayerZero Value Transfer API key is required to build transfers."))
                : SendAsync(HttpMethod.Post, "build-user-steps", JsonSerializer.Serialize(new { quoteId }), root => root.GetProperty("userSteps").Clone(), ct);

        /// <summary>Status of a quoted transfer once its source transaction has been sent.</summary>
        public Task<OASISResult<StargateTransferStatus>> GetTransferStatusAsync(string quoteId, string sourceTxHash = null, CancellationToken ct = default)
            => SendAsync(HttpMethod.Get, $"status/{Uri.EscapeDataString(quoteId)}" + (string.IsNullOrWhiteSpace(sourceTxHash) ? "" : $"?txHash={Uri.EscapeDataString(sourceTxHash)}"), null, root =>
            {
                var raw = root.GetProperty("status").GetString();
                return new StargateTransferStatus
                {
                    RawStatus = raw,
                    Status = raw switch
                    {
                        "SUCCEEDED" => CrossChainTransferStatus.Completed,
                        "PENDING" or "PROCESSING" => CrossChainTransferStatus.Pending,
                        "FAILED" => CrossChainTransferStatus.Failed,
                        _ => CrossChainTransferStatus.Unknown
                    },
                    ExplorerUrl = root.TryGetProperty("explorerUrl", out var url) ? url.GetString() : null,
                    History = root.GetProperty("executionHistory").EnumerateArray()
                        .Select(h => (h.GetProperty("event").GetString(), h.GetProperty("transaction").GetProperty("chainKey").GetString(), h.GetProperty("transaction").GetProperty("hash").GetString()))
                        .ToList()
                };
            }, ct);

        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
            => OnlyStargate(await _scan.GetTransfersBySourceTxAsync(sourceTxHash, cancellationToken));

        public async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => OnlyStargate(await _scan.GetTransfersByAddressAsync(address, limit, cancellationToken));

        private static OASISResult<IReadOnlyList<CrossChainTransfer>> OnlyStargate(OASISResult<IReadOnlyList<CrossChainTransfer>> scan)
            => scan.IsError ? scan : new OASISResult<IReadOnlyList<CrossChainTransfer>>(
                scan.Result.Where(t => string.Equals(t.SenderApplication, "stargate", StringComparison.OrdinalIgnoreCase)).Select(t => t with { Protocol = "stargate" }).ToList());

        private static string QuoteBody(StargateQuoteRequest r)
        {
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                w.WriteStartObject();
                w.WriteString("srcChainKey", r.SrcChainKey);
                w.WriteString("srcTokenAddress", r.SrcTokenAddress);
                w.WriteString("srcWalletAddress", r.SrcWalletAddress);
                w.WriteString("dstChainKey", r.DstChainKey);
                w.WriteString("dstTokenAddress", r.DstTokenAddress);
                w.WriteString("dstWalletAddress", r.DstWalletAddress);
                w.WritePropertyName("amount");
                w.WriteRawValue(r.Amount.ToString()); // integer amounts can exceed 64 bits
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static StargateQuote MapQuote(JsonElement q)
        {
            static BigInteger Big(JsonElement e) => BigInteger.Parse(e.GetRawText().Trim('"'));
            return new StargateQuote
            {
                Id = q.GetProperty("id").GetString(),
                RouteTypes = q.GetProperty("routeSteps").EnumerateArray().Select(s => s.GetProperty("type").GetString()).ToList(),
                SrcAmount = Big(q.GetProperty("srcAmount")),
                DstAmount = Big(q.GetProperty("dstAmount")),
                DstAmountMin = q.TryGetProperty("dstAmountMin", out var min) && min.ValueKind != JsonValueKind.Null ? Big(min) : null,
                FeeUsd = q.TryGetProperty("feeUsd", out var fee) ? fee.GetString() : null,
                EstimatedDuration = q.GetProperty("duration").GetProperty("estimated").ValueKind == JsonValueKind.String ? q.GetProperty("duration").GetProperty("estimated").GetString() : null,
                ExpiresAt = q.TryGetProperty("expiresAt", out var exp) && DateTimeOffset.TryParse(exp.GetString(), out var at) ? at : null,
                UserSteps = q.TryGetProperty("userSteps", out var steps) ? steps.Clone() : default
            };
        }

        private async Task<OASISResult<T>> SendAsync<T>(HttpMethod method, string path, string jsonBody, Func<JsonElement, T> parse, CancellationToken ct)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (jsonBody != null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<T>($"Value Transfer API {method} {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                using var doc = JsonDocument.Parse(body);
                return new OASISResult<T>(parse(doc.RootElement));
            }
            catch (Exception ex) { return Error<T>($"Value Transfer API {method} {path} failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"StargateOASIS: {message}", ex);
            return result;
        }
    }
}
