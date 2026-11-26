extern alias StacksCrypto;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StacksCrypto::Org.BouncyCastle.Asn1.Sec;
using StacksCrypto::Org.BouncyCastle.Asn1.X9;
using StacksCrypto::Org.BouncyCastle.Crypto.Digests;
using StacksCrypto::Org.BouncyCastle.Crypto.Parameters;
using StacksCrypto::Org.BouncyCastle.Crypto.Signers;
using BigInteger = StacksCrypto::Org.BouncyCastle.Math.BigInteger;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    /// <summary>Gaia hub client for one storage bucket. Writes, deletes and listings carry a Gaia v1 auth token (an ES256K
    /// JWT signed with the bucket's secp256k1 key over the hub's challenge); reads use the hub's public read URL. Every hub
    /// failure is raised; only an absent file reads as null.</summary>
    public sealed class BlockStackClient : IDisposable
    {
        private static readonly X9ECParameters Curve = SecNamedCurves.GetByName("secp256k1");
        private static readonly ECDomainParameters Domain = new(Curve.Curve, Curve.G, Curve.N, Curve.H);
        private static readonly BigInteger HalfOrder = Curve.N.ShiftRight(1);
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
        private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        private readonly HttpClient _http;
        private readonly bool _ownsHttp;
        private readonly BigInteger _privateKey;
        private readonly SemaphoreSlim _authLock = new(1, 1);
        private HubInfo _hubInfo;
        private string _token;
        private DateTimeOffset _tokenIssuedAt;

        public string GaiaHubUrl { get; }
        public string AppDomain { get; set; } = "oasis";
        public string PublicKeyHex { get; }

        /// <summary>The bucket address: the P2PKH address of the bucket key, which the hub requires as the write path.</summary>
        public string BucketAddress { get; }

        public bool IsConfigured => _privateKey != null;

        public BlockStackClient(string gaiaHubUrl = null, string privateKeyHex = null, HttpClient httpClient = null)
        {
            GaiaHubUrl = (gaiaHubUrl ?? "https://hub.blockstack.org").TrimEnd('/');
            _ownsHttp = httpClient == null;
            _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            if (privateKeyHex == null) return;
            var hex = privateKeyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? privateKeyHex[2..] : privateKeyHex;
            // Stacks wallets append 01 to mark a compressed-key secret.
            if (hex.Length == 66 && hex.EndsWith("01", StringComparison.Ordinal)) hex = hex[..64];
            if (hex.Length != 64) throw new ArgumentException("A 32-byte secp256k1 Gaia bucket key (hex) is required.", nameof(privateKeyHex));
            var secret = new BigInteger(1, Convert.FromHexString(hex));
            if (secret.SignValue <= 0 || secret.CompareTo(Curve.N) >= 0)
                throw new ArgumentException("The Gaia bucket key is outside the secp256k1 range.", nameof(privateKeyHex));
            _privateKey = secret;
            var publicKey = Domain.G.Multiply(secret).Normalize().GetEncoded(true);
            PublicKeyHex = Convert.ToHexString(publicKey).ToLowerInvariant();
            BucketAddress = AddressForPublicKey(publicKey);
        }

        /// <summary>Bitcoin mainnet P2PKH address of a compressed secp256k1 public key (the Gaia bucket address).</summary>
        public static string AddressForPublicKey(byte[] compressedPublicKey)
        {
            if (compressedPublicKey?.Length != 33) throw new ArgumentException("A compressed secp256k1 public key is required.");
            var ripemd = new RipeMD160Digest();
            var sha = SHA256.HashData(compressedPublicKey);
            ripemd.BlockUpdate(sha, 0, sha.Length);
            var payload = new byte[21];
            ripemd.DoFinal(payload, 1);
            var checksum = SHA256.HashData(SHA256.HashData(payload));
            return Base58(payload.Concat(checksum.Take(4)).ToArray());
        }

        private static string Base58(byte[] data)
        {
            var value = new System.Numerics.BigInteger(data, isUnsigned: true, isBigEndian: true);
            var text = new StringBuilder();
            while (value > 0) { text.Insert(0, Base58Alphabet[(int)(value % 58)]); value /= 58; }
            foreach (var b in data) { if (b != 0) break; text.Insert(0, '1'); }
            return text.ToString();
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>Builds a Gaia v1 auth token: <c>v1:</c> + an ES256K JWT whose signature is the JOSE r||s pair with low s.</summary>
        internal string CreateAuthToken(string challengeText, DateTimeOffset issuedAt)
        {
            RequireKey();
            var header = Base64Url(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"ES256K\"}"));
            var claims = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["gaiaChallenge"] = challengeText,
                ["hubUrl"] = GaiaHubUrl,
                ["iss"] = PublicKeyHex,
                ["salt"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
                ["iat"] = issuedAt.ToUnixTimeSeconds(),
                ["exp"] = issuedAt.Add(TokenLifetime + TimeSpan.FromMinutes(5)).ToUnixTimeSeconds()
            });
            var signingInput = header + "." + Base64Url(Encoding.UTF8.GetBytes(claims));
            var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, new ECPrivateKeyParameters(_privateKey, Domain));
            var rs = signer.GenerateSignature(SHA256.HashData(Encoding.ASCII.GetBytes(signingInput)));
            var s = rs[1].CompareTo(HalfOrder) > 0 ? Curve.N.Subtract(rs[1]) : rs[1];
            var signature = new byte[64];
            Fixed32(rs[0]).CopyTo(signature, 0);
            Fixed32(s).CopyTo(signature, 32);
            return "v1:" + signingInput + "." + Base64Url(signature);
        }

        private static byte[] Fixed32(BigInteger value)
        {
            var raw = value.ToByteArrayUnsigned();
            var padded = new byte[32];
            raw.CopyTo(padded, 32 - raw.Length);
            return padded;
        }

        private void RequireKey()
        {
            if (_privateKey == null)
                throw new InvalidOperationException("Gaia storage requires an explicit hub URL and bucket key.");
        }

        private async Task<(HubInfo Info, string Token)> AuthorizeAsync(CancellationToken cancellationToken)
        {
            RequireKey();
            await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_hubInfo == null)
                {
                    using var response = await _http.GetAsync(GaiaHubUrl + "/hub_info", cancellationToken).ConfigureAwait(false);
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Gaia hub_info failed ({(int)response.StatusCode}): {content}");
                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;
                    var challenge = root.GetProperty("challenge_text").GetString();
                    var readPrefix = root.GetProperty("read_url_prefix").GetString();
                    var version = root.TryGetProperty("latest_auth_version", out var v) ? v.GetString() : "v1";
                    if (string.IsNullOrEmpty(challenge) || string.IsNullOrEmpty(readPrefix))
                        throw new InvalidDataException("Gaia hub_info omitted its challenge text or read URL prefix.");
                    if (version != "v1")
                        throw new NotSupportedException($"Gaia hub requires auth version {version}; this client signs v1 tokens.");
                    _hubInfo = new HubInfo(challenge, readPrefix.EndsWith('/') ? readPrefix : readPrefix + "/");
                }
                var now = DateTimeOffset.UtcNow;
                if (_token == null || now - _tokenIssuedAt >= TokenLifetime)
                {
                    _token = CreateAuthToken(_hubInfo.ChallengeText, now);
                    _tokenIssuedAt = now;
                }
                return (_hubInfo, _token);
            }
            finally { _authLock.Release(); }
        }

        private HttpRequestMessage Authorized(HttpMethod method, string url, string token)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", "bearer " + token);
            return request;
        }

        private static void RequireName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.StartsWith('/') || name.Split('/').Any(part => part is "" or "." or ".."))
                throw new ArgumentException("A relative Gaia file name is required.", nameof(name));
        }

        /// <summary>Reads a file from this bucket. Returns null only when the hub reports it absent (404).</summary>
        public async Task<string> GetTextAsync(string name, CancellationToken cancellationToken = default)
        {
            RequireName(name);
            var (info, _) = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            using var response = await _http.GetAsync(info.ReadUrlPrefix + BucketAddress + "/" + name, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia read of {name} failed ({(int)response.StatusCode}): {content}");
            return content;
        }

        /// <summary>Writes a file; throws unless the hub acknowledges the write.</summary>
        public async Task PutTextAsync(string name, string text, string contentType = "text/plain", CancellationToken cancellationToken = default)
        {
            RequireName(name);
            ArgumentNullException.ThrowIfNull(text);
            var (_, token) = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            using var request = Authorized(HttpMethod.Post, $"{GaiaHubUrl}/store/{BucketAddress}/{name}", token);
            request.Content = new StringContent(text, Encoding.UTF8, contentType);
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia write of {name} failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");
        }

        /// <summary>Deletes a file. Returns false when it was already absent (404); any other failure throws.</summary>
        public async Task<bool> DeleteFileAsync(string name, CancellationToken cancellationToken = default)
        {
            RequireName(name);
            var (_, token) = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            using var request = Authorized(HttpMethod.Delete, $"{GaiaHubUrl}/delete/{BucketAddress}/{name}", token);
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound) return false;
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia delete of {name} failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");
            return true;
        }

        /// <summary>Lists every file name in this bucket, following the hub's page tokens to the end.</summary>
        public async Task<IReadOnlyList<string>> ListFileNamesAsync(CancellationToken cancellationToken = default)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string page = null;
            do
            {
                var (_, token) = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
                using var request = Authorized(HttpMethod.Post, $"{GaiaHubUrl}/list-files/{BucketAddress}", token);
                request.Content = new StringContent(JsonSerializer.Serialize(new { page, stat = false }), Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Gaia listing failed ({(int)response.StatusCode}): {content}");
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                foreach (var entry in root.GetProperty("entries").EnumerateArray())
                    names.Add(entry.GetString() ?? throw new InvalidDataException("Gaia listing returned a null entry."));
                page = root.TryGetProperty("page", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
                if (page != null && !seen.Add(page)) throw new InvalidDataException("Gaia listing repeated a page token.");
            } while (page != null);
            return names;
        }

        /// <summary>Authenticated round trip: the hub must accept this bucket's token for a listing.</summary>
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var (_, token) = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            using var request = Authorized(HttpMethod.Post, $"{GaiaHubUrl}/list-files/{BucketAddress}", token);
            request.Content = new StringContent("{\"page\":null,\"stat\":false}", Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia hub refused this bucket's auth token ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");
        }

        /// <summary>Reads a JSON document. Returns null only when the file is absent.</summary>
        public async Task<Dictionary<string, object>> GetFileAsync(string filePath)
        {
            var content = await GetTextAsync(filePath).ConfigureAwait(false);
            if (content == null) return null;
            return JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException($"Gaia returned an empty document for {filePath}.");
        }

        /// <summary>Writes a JSON document; throws unless the hub acknowledges the write.</summary>
        public Task PutFileAsync(string filePath, Dictionary<string, object> data)
            => PutTextAsync(filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }), "application/json");

        /// <summary>Gaia hubs store per-address buckets and expose no listing of users, so this cannot be answered by the
        /// hub; it fails explicitly instead of returning invented directories.</summary>
        public Task<List<string>> ListUserDirectoriesAsync()
            => Task.FromException<List<string>>(new NotSupportedException(
                "Gaia hubs expose no user listing; look records up by their Gaia address instead."));

        public void Dispose()
        {
            _authLock.Dispose();
            if (_ownsHttp) _http.Dispose();
        }

        private sealed record HubInfo(string ChallengeText, string ReadUrlPrefix);
    }

    // Supporting classes for BlockStack API responses
    public class StacksAddressesResponse
    {
        public List<StacksAddress> Results { get; set; } = new List<StacksAddress>();
    }

    public class StacksAddress
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public string ScriptHash { get; set; }
    }
}
