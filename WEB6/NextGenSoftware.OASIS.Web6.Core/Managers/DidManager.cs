using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Decentralised Identity (DID) and Verifiable Credential (VC) management for OASIS avatars.
    /// Supports DID creation, resolution, and VC issuance/verification.
    /// Priority 20 — DID/VCs.
    /// </summary>
    public class DidManager : OASISManager
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _resolverUrl;

        public DidManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna)
        {
            _resolverUrl = (Environment.GetEnvironmentVariable("DID_RESOLVER_URL") ?? "https://resolver.identity.foundation").TrimEnd('/');
        }

        /// <summary>
        /// Creates a new did:key DID for the avatar, returning the DID document.
        /// The DID is deterministically derived from the avatar's GUID so it's stable across calls.
        /// </summary>
        public OASISResult<DidDocument> CreateDid(Guid avatarId)
        {
            var result = new OASISResult<DidDocument>();
            try
            {
                // Derive a deterministic key pair from the avatarId
                byte[] seed = avatarId.ToByteArray();
                using var rsa = RSA.Create();
                // Use avatar bytes as PRNG seed (deterministic for same avatar)
                var rng = new PseudoRng(seed);
                byte[] keyBytes = rng.NextBytes(32);

                string didKey = $"did:key:z{Base58Encode(keyBytes)}";
                var doc = new DidDocument
                {
                    Id = didKey,
                    Context = new List<string> { "https://www.w3.org/ns/did/v1" },
                    VerificationMethod = new List<VerificationMethod>
                    {
                        new VerificationMethod
                        {
                            Id = $"{didKey}#key-1",
                            Type = "JsonWebKey2020",
                            Controller = didKey,
                            PublicKeyBase58 = Base58Encode(keyBytes)
                        }
                    },
                    Authentication = new List<string> { $"{didKey}#key-1" }
                };

                result.Result = doc;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Failed to create DID: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Resolves a DID document via the configured Universal Resolver.
        /// </summary>
        public async Task<OASISResult<DidDocument>> ResolveDid(string did)
        {
            var result = new OASISResult<DidDocument>();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{_resolverUrl}/1.0/identifiers/{Uri.EscapeDataString(did)}");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"DID resolution failed: {resp.StatusCode}";
                    return result;
                }
                string body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("didDocument", out var didDoc))
                    result.Result = JsonSerializer.Deserialize<DidDocument>(didDoc.GetRawText());
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"DID resolution threw: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Issues a Verifiable Credential claiming the avatar's OASIS identity attributes.
        /// </summary>
        public OASISResult<VerifiableCredential> IssueCredential(Guid avatarId, string subjectDid, Dictionary<string, object> claims)
        {
            var result = new OASISResult<VerifiableCredential>();
            try
            {
                var issuerDid = CreateDid(avatarId).Result?.Id ?? $"did:key:oasis:{avatarId}";
                var vc = new VerifiableCredential
                {
                    Context = new List<string> { "https://www.w3.org/2018/credentials/v1" },
                    Id = $"urn:uuid:{Guid.NewGuid()}",
                    Type = new List<string> { "VerifiableCredential", "OASISAvatarCredential" },
                    Issuer = issuerDid,
                    IssuanceDate = DateTime.UtcNow.ToString("O"),
                    CredentialSubject = new Dictionary<string, object>(claims) { ["id"] = subjectDid }
                };

                // Sign using a simple HMAC-SHA256 proof (replace with Ed25519/JWS for production)
                string payload = JsonSerializer.Serialize(vc.CredentialSubject);
                using var hmac = new HMACSHA256(avatarId.ToByteArray());
                byte[] sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                vc.Proof = new CredentialProof
                {
                    Type = "Hmac2025",
                    Created = DateTime.UtcNow.ToString("O"),
                    VerificationMethod = $"{issuerDid}#key-1",
                    ProofValue = Convert.ToBase64String(sig)
                };

                result.Result = vc;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Failed to issue credential: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Verifies a Verifiable Credential's proof (HMAC-SHA256 variant issued by this system).
        /// </summary>
        public OASISResult<bool> VerifyCredential(VerifiableCredential vc, Guid issuerAvatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                string payload = JsonSerializer.Serialize(vc.CredentialSubject);
                using var hmac = new HMACSHA256(issuerAvatarId.ToByteArray());
                byte[] expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                byte[] actual = Convert.FromBase64String(vc.Proof?.ProofValue ?? "");
                result.Result = CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Credential verification failed: {ex.Message}";
                result.Exception = ex;
                result.Result = false;
            }
            return result;
        }

        private static string Base58Encode(byte[] data)
        {
            const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            var sb = new StringBuilder();
            var n = new System.Numerics.BigInteger(data, isUnsigned: true, isBigEndian: true);
            while (n > 0) { n = System.Numerics.BigInteger.DivRem(n, 58, out var rem); sb.Insert(0, alphabet[(int)rem]); }
            foreach (var b in data) { if (b != 0) break; sb.Insert(0, '1'); }
            return sb.ToString();
        }

        private class PseudoRng
        {
            private readonly byte[] _seed;
            private int _pos;
            public PseudoRng(byte[] seed) { _seed = seed; }
            public byte[] NextBytes(int count)
            {
                using var sha = SHA256.Create();
                var result = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = sha.ComputeHash(_seed)[_pos++ % 32];
                }
                return result;
            }
        }
    }

    public class DidDocument
    {
        public string Id { get; set; }
        public List<string> Context { get; set; }
        public List<VerificationMethod> VerificationMethod { get; set; }
        public List<string> Authentication { get; set; }
    }

    public class VerificationMethod
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Controller { get; set; }
        public string PublicKeyBase58 { get; set; }
    }

    public class VerifiableCredential
    {
        public List<string> Context { get; set; }
        public string Id { get; set; }
        public List<string> Type { get; set; }
        public string Issuer { get; set; }
        public string IssuanceDate { get; set; }
        public Dictionary<string, object> CredentialSubject { get; set; }
        public CredentialProof Proof { get; set; }
    }

    public class CredentialProof
    {
        public string Type { get; set; }
        public string Created { get; set; }
        public string VerificationMethod { get; set; }
        public string ProofValue { get; set; }
    }
}
