using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS
{
    /// <summary>
    /// Hyperlane interchain messaging provider. Tracks messages through the Hyperlane Explorer GraphQL API
    /// (https://explorer4.hasura.app/v1/graphql, the API behind explorer.hyperlane.xyz). Hyperlane is a messaging
    /// protocol, not a data store, so this is an <see cref="OASISProvider"/> implementing <see cref="ICrossChainTracker"/>.
    /// </summary>
    public class HyperlaneOASIS : OASISProvider, ICrossChainTracker
    {
        private const string Fields = "msg_id is_delivered origin_tx_hash destination_tx_hash sender recipient origin_tx_sender origin_chain_id destination_chain_id origin_domain_id destination_domain_id send_occurred_at";

        private static readonly Dictionary<long, string> ChainNames = new()
        {
            [1] = "ethereum", [10] = "optimism", [56] = "bsc", [100] = "gnosis", [137] = "polygon", [8453] = "base",
            [42161] = "arbitrum", [43114] = "avalanche", [59144] = "linea"
        };

        private readonly HttpClient _http;
        private readonly string _graphQlPath;

        public HyperlaneOASIS(string graphQlUrl = "https://explorer4.hasura.app/v1/graphql") : this(graphQlUrl, null) { }

        internal HyperlaneOASIS(string graphQlUrl, HttpMessageHandler handler)
        {
            var uri = new Uri(graphQlUrl);
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/");
            _graphQlPath = uri.AbsolutePath.TrimStart('/');
            ProviderName = "HyperlaneOASIS";
            ProviderDescription = "Hyperlane interchain messaging provider (Hyperlane Explorer API).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HyperlaneOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var probe = await QueryAsync("{ message_view(limit: 1) { msg_id } }", null, CancellationToken.None);
            var result = new OASISResult<bool>();
            if (probe.IsError) { OASISErrorHandling.HandleError(ref result, probe.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "HyperlaneOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "HyperlaneOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default)
            => FilterAsync("origin_tx_hash", sourceTxHash, 50, cancellationToken);

        public Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default)
            => FilterAsync("origin_tx_sender", address, Math.Clamp(limit, 1, 100), cancellationToken);

        /// <summary>Loads a message by its Hyperlane message id.</summary>
        public async Task<OASISResult<CrossChainTransfer>> GetTransferByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var list = await FilterAsync("msg_id", messageId, 1, cancellationToken);
            if (list.IsError) return Error<CrossChainTransfer>(list.Message);
            return list.Result.Count == 0 ? Error<CrossChainTransfer>($"No Hyperlane message {messageId}.") : new OASISResult<CrossChainTransfer>(list.Result[0]);
        }

        /// <summary>Encodes a 0x-hex or base58 (e.g. Solana) value as a Postgres bytea literal, as the explorer expects.</summary>
        internal static string ToBytea(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A hash or address is required.");
            value = value.Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hex = value[2..];
                if (hex.Length % 2 != 0 || !hex.All(Uri.IsHexDigit)) throw new ArgumentException($"'{value}' is not valid hex.");
                return "\\x" + hex.ToLowerInvariant();
            }
            return "\\x" + Convert.ToHexString(Base58Decode(value)).ToLowerInvariant();
        }

        private static byte[] Base58Decode(string s)
        {
            const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            BigInteger n = 0;
            foreach (var c in s)
            {
                var digit = alphabet.IndexOf(c);
                if (digit < 0) throw new ArgumentException($"'{s}' is neither 0x-hex nor base58.");
                n = n * 58 + digit;
            }
            var bytes = n.IsZero ? Array.Empty<byte>() : n.ToByteArray(isUnsigned: true, isBigEndian: true);
            var leadingZeros = s.TakeWhile(c => c == '1').Count();
            return new byte[leadingZeros].Concat(bytes).ToArray();
        }

        private static string FromBytea(string value)
            => value == null ? null : value.StartsWith("\\x", StringComparison.Ordinal) ? "0x" + value[2..] : value;

        private async Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> FilterAsync(string column, string value, int limit, CancellationToken ct)
        {
            string bytea;
            try { bytea = ToBytea(value); }
            catch (ArgumentException ex) { return Error<IReadOnlyList<CrossChainTransfer>>(ex.Message); }

            var data = await QueryAsync($"query ($v: bytea!) {{ message_view(where: {{{column}: {{_eq: $v}}}}, limit: {limit}) {{ {Fields} }} }}", new { v = bytea }, ct);
            if (data.IsError) return Error<IReadOnlyList<CrossChainTransfer>>(data.Message);
            return new OASISResult<IReadOnlyList<CrossChainTransfer>>(data.Result.GetProperty("message_view").EnumerateArray().Select(Map).ToList());
        }

        private static CrossChainTransfer Map(JsonElement m)
        {
            static string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            static long? Num(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : null;
            static string Chain(long? id) => id == null ? null : ChainNames.TryGetValue(id.Value, out var n) ? n : $"chain-{id}";

            var delivered = m.TryGetProperty("is_delivered", out var d) && d.ValueKind == JsonValueKind.True;
            var sent = Str(m, "send_occurred_at");
            return new CrossChainTransfer
            {
                Id = FromBytea(Str(m, "msg_id")),
                Protocol = "hyperlane",
                Status = delivered ? CrossChainTransferStatus.Completed : CrossChainTransferStatus.Pending,
                RawStatus = delivered ? "delivered" : "pending",
                SourceChain = Chain(Num(m, "origin_chain_id") ?? Num(m, "origin_domain_id")),
                SourceTxHash = FromBytea(Str(m, "origin_tx_hash")),
                DestinationChain = Chain(Num(m, "destination_chain_id") ?? Num(m, "destination_domain_id")),
                DestinationTxHash = FromBytea(Str(m, "destination_tx_hash")),
                Sender = FromBytea(Str(m, "origin_tx_sender")) ?? FromBytea(Str(m, "sender")),
                Recipient = FromBytea(Str(m, "recipient")),
                // The explorer returns UTC timestamps without an offset.
                Timestamp = DateTime.TryParse(sent, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var ts) ? new DateTimeOffset(ts, TimeSpan.Zero) : null
            };
        }

        private async Task<OASISResult<JsonElement>> QueryAsync(string query, object variables, CancellationToken ct)
        {
            try
            {
                using var content = new StringContent(JsonSerializer.Serialize(new { query, variables }), Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync(_graphQlPath, content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<JsonElement>($"Hyperlane Explorer query failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("errors", out var errors))
                    return Error<JsonElement>($"Hyperlane Explorer query failed: {errors[0].GetProperty("message").GetString()}");
                return new OASISResult<JsonElement>(doc.RootElement.GetProperty("data").Clone());
            }
            catch (Exception ex) { return Error<JsonElement>($"Hyperlane Explorer query failed: {ex.Message}", ex); }
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"HyperlaneOASIS: {message}", ex);
            return result;
        }
    }
}
