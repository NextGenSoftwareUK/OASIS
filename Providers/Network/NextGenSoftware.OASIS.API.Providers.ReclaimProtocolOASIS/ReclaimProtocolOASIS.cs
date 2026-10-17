using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Signer;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS
{
    public sealed class ReclaimVerification
    {
        public string Identifier { get; init; }
        public string Owner { get; init; }
        public string Provider { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        /// <summary>Recovered attestor addresses (lower-case) whose signatures are valid and registered.</summary>
        public IReadOnlyList<string> Attestors { get; init; }
        /// <summary>The signed claim context (JSON), including extractedParameters and providerHash.</summary>
        public JsonElement Context { get; init; }
    }

    /// <summary>
    /// Reclaim Protocol provider. Verifies Reclaim zkTLS proofs exactly as the official SDK's verifyProof does: the claim
    /// identifier is keccak256(provider \n parameters \n canonical(context)), the attestor signs (EIP-191) the lines
    /// identifier, owner, timestampS and epoch, and at least one recovered signer must be in the registered attestor set
    /// published at https://api.reclaimprotocol.org/api/attestors. Proof generation happens in the user's Reclaim
    /// app/extension, so this provider is the verifier side. Reclaim is not a data store, so this is an
    /// <see cref="OASISProvider"/>.
    /// </summary>
    public class ReclaimProtocolOASIS : OASISProvider
    {
        private readonly HttpClient _http;
        private static readonly EthereumMessageSigner Signer = new();

        public ReclaimProtocolOASIS(string backendUrl = "https://api.reclaimprotocol.org") : this(backendUrl, null) { }

        internal ReclaimProtocolOASIS(string backendUrl, HttpMessageHandler handler)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(backendUrl.TrimEnd('/') + "/");
            ProviderName = "ReclaimProtocolOASIS";
            ProviderDescription = "Reclaim Protocol zkTLS proof verifier.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ReclaimProtocolOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var attestors = await GetAttestorsAsync();
            var result = new OASISResult<bool>();
            if (attestors.IsError) { OASISErrorHandling.HandleError(ref result, attestors.Message); return result; }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "ReclaimProtocolOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "ReclaimProtocolOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>The registered attestor addresses (lower-case).</summary>
        public async Task<OASISResult<IReadOnlyList<string>>> GetAttestorsAsync(CancellationToken ct = default)
        {
            try
            {
                using var response = await _http.GetAsync("api/attestors", ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return Error<IReadOnlyList<string>>($"attestor list failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                using var doc = JsonDocument.Parse(body);
                var list = doc.RootElement.GetProperty("data").EnumerateArray()
                    .Select(a => a.GetProperty("address").GetString().ToLowerInvariant()).ToList();
                return list.Count == 0 ? Error<IReadOnlyList<string>>("attestor list is empty.") : new OASISResult<IReadOnlyList<string>>(list);
            }
            catch (Exception ex) { return Error<IReadOnlyList<string>>($"attestor list failed: {ex.Message}", ex); }
        }

        /// <summary>
        /// Verifies a Reclaim proof (the JSON object returned by the Reclaim SDK / callback). When
        /// <paramref name="expectedProviderHash"/> is given, the signed context's providerHash must equal it, binding the
        /// proof to the provider template the caller asked for.
        /// </summary>
        public async Task<OASISResult<ReclaimVerification>> VerifyProofAsync(string proofJson, string expectedProviderHash = null, CancellationToken ct = default)
        {
            ReclaimVerification verified;
            List<string> signers;
            try
            {
                using var doc = JsonDocument.Parse(proofJson);
                var proof = doc.RootElement;
                var claim = proof.GetProperty("claimData");
                var provider = claim.GetProperty("provider").GetString();
                var parameters = claim.GetProperty("parameters").GetString();
                var contextText = claim.TryGetProperty("context", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : "";
                var owner = claim.GetProperty("owner").GetString().ToLowerInvariant();
                var timestampS = claim.GetProperty("timestampS").GetInt64();
                var epoch = claim.GetProperty("epoch").GetInt64();

                JsonElement context = default;
                var canonicalContext = "";
                if (contextText.Length > 0)
                {
                    using var ctxDoc = JsonDocument.Parse(contextText);
                    context = ctxDoc.RootElement.Clone();
                    canonicalContext = JsonCanonicalizer.Canonicalize(context);
                }

                var identifier = "0x" + Convert.ToHexString(Sha3Keccack.Current.CalculateHash(
                    Encoding.UTF8.GetBytes($"{provider}\n{parameters}\n{canonicalContext}"))).ToLowerInvariant();
                if (claim.TryGetProperty("identifier", out var claimed) && !string.Equals(claimed.GetString(), identifier, StringComparison.OrdinalIgnoreCase))
                    return Error<ReclaimVerification>("claim identifier does not match its provider/parameters/context.");

                var signData = $"{identifier}\n{owner}\n{timestampS}\n{epoch}";
                signers = proof.GetProperty("signatures").EnumerateArray()
                    .Select(s => Signer.EncodeUTF8AndEcRecover(signData, s.GetString()).ToLowerInvariant()).ToList();

                if (expectedProviderHash != null &&
                    (context.ValueKind != JsonValueKind.Object || !context.TryGetProperty("providerHash", out var ph) ||
                     !string.Equals(ph.GetString(), expectedProviderHash, StringComparison.OrdinalIgnoreCase)))
                    return Error<ReclaimVerification>($"proof is not for provider hash {expectedProviderHash}.");

                verified = new ReclaimVerification
                {
                    Identifier = identifier,
                    Owner = owner,
                    Provider = provider,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampS),
                    Context = context
                };
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
            {
                return Error<ReclaimVerification>($"malformed proof: {ex.Message}", ex);
            }

            var attestors = await GetAttestorsAsync(ct);
            if (attestors.IsError) return Error<ReclaimVerification>(attestors.Message);
            var registered = signers.Where(attestors.Result.Contains).Distinct().ToList();
            if (registered.Count == 0)
                return Error<ReclaimVerification>("no signature is from a registered Reclaim attestor.");

            return new OASISResult<ReclaimVerification>(new ReclaimVerification
            {
                Identifier = verified.Identifier,
                Owner = verified.Owner,
                Provider = verified.Provider,
                Timestamp = verified.Timestamp,
                Context = verified.Context,
                Attestors = registered
            });
        }

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS: {message}", ex);
            return result;
        }
    }

    /// <summary>RFC 8785 JSON Canonicalization Scheme, matching the npm `canonicalize` package the Reclaim SDK uses.</summary>
    internal static class JsonCanonicalizer
    {
        public static string Canonicalize(JsonElement element)
        {
            var sb = new StringBuilder();
            Write(element, sb);
            return sb.ToString();
        }

        private static void Write(JsonElement e, StringBuilder sb)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object:
                    sb.Append('{');
                    var first = true;
                    foreach (var p in e.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(p.Name, sb);
                        sb.Append(':');
                        Write(p.Value, sb);
                    }
                    sb.Append('}');
                    break;
                case JsonValueKind.Array:
                    sb.Append('[');
                    var i = 0;
                    foreach (var item in e.EnumerateArray())
                    {
                        if (i++ > 0) sb.Append(',');
                        Write(item, sb);
                    }
                    sb.Append(']');
                    break;
                case JsonValueKind.String: WriteString(e.GetString(), sb); break;
                case JsonValueKind.Number: sb.Append(FormatNumber(e.GetDouble())); break;
                case JsonValueKind.True: sb.Append("true"); break;
                case JsonValueKind.False: sb.Append("false"); break;
                default: sb.Append("null"); break;
            }
        }

        /// <summary>JSON.stringify string escaping: quote, backslash and C0 controls only.</summary>
        private static void WriteString(string s, StringBuilder sb)
        {
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>ECMAScript Number.prototype.toString for finite doubles (shortest round-trip digits).</summary>
        internal static string FormatNumber(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) throw new FormatException("JSON numbers must be finite.");
            if (d == 0) return "0";
            // Shortest round-trip mantissa digits and decimal exponent.
            var shortest = d.ToString("R", CultureInfo.InvariantCulture);
            var neg = shortest.StartsWith('-');
            if (neg) shortest = shortest[1..];

            var ePos = shortest.IndexOfAny(new[] { 'E', 'e' });
            var mant = ePos >= 0 ? shortest[..ePos] : shortest;
            var e10 = ePos >= 0 ? int.Parse(shortest[(ePos + 1)..], CultureInfo.InvariantCulture) : 0;
            var dot = mant.IndexOf('.');
            var intPart = dot >= 0 ? mant[..dot] : mant;
            var frac = dot >= 0 ? mant[(dot + 1)..] : "";
            var digits = (intPart + frac).TrimStart('0');
            var leadingZeros = (intPart + frac).Length - (intPart + frac).TrimStart('0').Length;
            var exp = intPart.Length + e10 - leadingZeros; // position of decimal point relative to digits
            digits = digits.TrimEnd('0');
            var k = digits.Length;
            var n = exp;
            string body;
            if (k <= n && n <= 21) body = digits + new string('0', n - k);
            else if (0 < n && n <= 21) body = digits[..n] + "." + digits[n..];
            else if (-6 < n && n <= 0) body = "0." + new string('0', -n) + digits;
            else
            {
                var e = n - 1;
                var m = k == 1 ? digits : digits[..1] + "." + digits[1..];
                body = m + "e" + (e >= 0 ? "+" : "-") + Math.Abs(e).ToString(CultureInfo.InvariantCulture);
            }
            return neg ? "-" + body : body;
        }
    }
}
