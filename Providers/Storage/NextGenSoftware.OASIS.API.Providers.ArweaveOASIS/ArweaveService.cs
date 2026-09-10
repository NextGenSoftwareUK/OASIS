using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    /// <summary>
    /// Interface for Arweave network operations.
    /// </summary>
    public interface IArweaveService
    {
        /// <summary>Post a data transaction and return the transaction ID.</summary>
        Task<string> PostTransactionAsync(byte[] data, string contentType, Dictionary<string, string> tags = null);

        /// <summary>Download transaction data by transaction ID.</summary>
        Task<byte[]> GetTransactionDataAsync(string txId);

        /// <summary>Query for transactions matching all supplied tags; returns matching TxIds.</summary>
        Task<List<string>> QueryByTagsAsync(Dictionary<string, string> tags);
    }

    /// <summary>
    /// HTTP-based Arweave service.
    /// Writes are signed with RSA-PSS using the provided JWK wallet.
    /// Reads use the public Arweave gateway (no auth required).
    /// </summary>
    public class ArweaveService : IArweaveService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl;
        private readonly string _walletJson;

        // Parsed JWK fields (Base64Url encoded)
        private string _n;  // modulus
        private string _e;  // public exponent
        private string _d;  // private exponent
        private string _p;  // first prime
        private string _q;  // second prime
        private string _dp; // d mod (p-1)
        private string _dq; // d mod (q-1)
        private string _qi; // q^-1 mod p

        private string _ownerAddress; // derived from wallet

        public ArweaveService(string walletJson, string gatewayUrl = "https://arweave.net")
        {
            _walletJson = walletJson;
            _gatewayUrl = gatewayUrl?.TrimEnd('/') ?? "https://arweave.net";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

            if (!string.IsNullOrWhiteSpace(_walletJson))
                ParseWallet();
        }

        private void ParseWallet()
        {
            try
            {
                var jwk = JObject.Parse(_walletJson);
                _n = jwk["n"]?.ToString();
                _e = jwk["e"]?.ToString();
                _d = jwk["d"]?.ToString();
                _p = jwk["p"]?.ToString();
                _q = jwk["q"]?.ToString();
                _dp = jwk["dp"]?.ToString();
                _dq = jwk["dq"]?.ToString();
                _qi = jwk["qi"]?.ToString();

                if (!string.IsNullOrEmpty(_n))
                    _ownerAddress = Base64UrlEncode(SHA256Hash(Base64UrlDecode(_n)));
            }
            catch { /* wallet parse failed - read-only mode */ }
        }

        public async Task<string> PostTransactionAsync(byte[] data, string contentType, Dictionary<string, string> tags = null)
        {
            if (string.IsNullOrEmpty(_walletJson) || string.IsNullOrEmpty(_n))
                throw new InvalidOperationException("Arweave wallet is required for posting transactions. Set the wallet path in the ArweaveOASIS connection string.");

            // Build the Arweave transaction
            var tx = await BuildTransactionAsync(data, contentType, tags);
            var txJson = JsonConvert.SerializeObject(tx);

            var content = new StringContent(txJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_gatewayUrl}/tx", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Arweave transaction failed ({response.StatusCode}): {body}");

            return tx["id"]?.ToString();
        }

        public async Task<byte[]> GetTransactionDataAsync(string txId)
        {
            if (string.IsNullOrWhiteSpace(txId)) return null;

            try
            {
                var response = await _httpClient.GetAsync($"{_gatewayUrl}/{txId}");
                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<string>> QueryByTagsAsync(Dictionary<string, string> tags)
        {
            var tagFilters = new StringBuilder();
            if (tags != null)
            {
                foreach (var kvp in tags)
                {
                    tagFilters.Append($@"
                    {{
                        name: ""{kvp.Key}"",
                        values: [""{kvp.Value}""]
                    }},");
                }
            }

            var query = $@"
            {{
                transactions(
                    tags: [{tagFilters}]
                    first: 100
                ) {{
                    edges {{
                        node {{
                            id
                        }}
                    }}
                }}
            }}";

            var requestBody = JsonConvert.SerializeObject(new { query });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_gatewayUrl}/graphql", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = JObject.Parse(body);
                var edges = json["data"]?["transactions"]?["edges"] as JArray;

                var txIds = new List<string>();
                if (edges != null)
                    foreach (var edge in edges)
                    {
                        var id = edge["node"]?["id"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                            txIds.Add(id);
                    }

                return txIds;
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<JObject> BuildTransactionAsync(byte[] data, string contentType, Dictionary<string, string> tags)
        {
            // Fetch the last tx for this wallet and current network price
            var lastTxResponse = await _httpClient.GetAsync($"{_gatewayUrl}/wallet/{_ownerAddress}/last_tx");
            var lastTx = lastTxResponse.IsSuccessStatusCode
                ? await lastTxResponse.Content.ReadAsStringAsync()
                : "";

            var priceResponse = await _httpClient.GetAsync($"{_gatewayUrl}/price/{data.Length}");
            var reward = priceResponse.IsSuccessStatusCode
                ? await priceResponse.Content.ReadAsStringAsync()
                : "0";

            var dataBase64 = Base64UrlEncode(data);
            var dataHashBytes = SHA256Hash(data);

            // Build tags array (always include Content-Type)
            var txTags = new List<JObject>();
            txTags.Add(new JObject
            {
                ["name"] = Base64UrlEncode(Encoding.UTF8.GetBytes("Content-Type")),
                ["value"] = Base64UrlEncode(Encoding.UTF8.GetBytes(contentType))
            });

            if (tags != null)
            {
                foreach (var kvp in tags)
                {
                    txTags.Add(new JObject
                    {
                        ["name"] = Base64UrlEncode(Encoding.UTF8.GetBytes(kvp.Key)),
                        ["value"] = Base64UrlEncode(Encoding.UTF8.GetBytes(kvp.Value))
                    });
                }
            }

            var txId = Base64UrlEncode(SHA256Hash(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())));

            var tx = new JObject
            {
                ["format"] = 2,
                ["id"] = txId,
                ["last_tx"] = lastTx.Trim('"'),
                ["owner"] = _n,
                ["tags"] = JArray.FromObject(txTags),
                ["target"] = "",
                ["quantity"] = "0",
                ["data"] = dataBase64,
                ["data_size"] = data.Length.ToString(),
                ["data_root"] = Base64UrlEncode(dataHashBytes),
                ["reward"] = reward.Trim('"')
            };

            // Sign the transaction
            var signature = SignTransaction(tx);
            tx["signature"] = signature;
            tx["id"] = Base64UrlEncode(SHA256Hash(Base64UrlDecode(signature)));

            return tx;
        }

        private string SignTransaction(JObject tx)
        {
            if (string.IsNullOrEmpty(_d))
                throw new InvalidOperationException("Private key not available in wallet for signing.");

            var signatureData = GetSignatureData(tx);

            using var rsa = RSA.Create();
            var parameters = new RSAParameters
            {
                Modulus = Base64UrlDecode(_n),
                Exponent = Base64UrlDecode(_e),
                D = Base64UrlDecode(_d),
                P = string.IsNullOrEmpty(_p) ? null : Base64UrlDecode(_p),
                Q = string.IsNullOrEmpty(_q) ? null : Base64UrlDecode(_q),
                DP = string.IsNullOrEmpty(_dp) ? null : Base64UrlDecode(_dp),
                DQ = string.IsNullOrEmpty(_dq) ? null : Base64UrlDecode(_dq),
                InverseQ = string.IsNullOrEmpty(_qi) ? null : Base64UrlDecode(_qi)
            };
            rsa.ImportParameters(parameters);

            var signature = rsa.SignData(signatureData, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            return Base64UrlEncode(signature);
        }

        private byte[] GetSignatureData(JObject tx)
        {
            // Arweave v2 signature data: SHA-384 of concatenated deep-hash components
            var components = new List<byte[]>
            {
                Encoding.UTF8.GetBytes(tx["format"]?.ToString() ?? "2"),
                Base64UrlDecode(tx["owner"]?.ToString() ?? ""),
                Encoding.UTF8.GetBytes(tx["target"]?.ToString() ?? ""),
                Encoding.UTF8.GetBytes(tx["quantity"]?.ToString() ?? "0"),
                Encoding.UTF8.GetBytes(tx["reward"]?.ToString() ?? "0"),
                Encoding.UTF8.GetBytes(tx["last_tx"]?.ToString() ?? ""),
            };

            if (tx["tags"] is JArray tagsArray)
            {
                foreach (var tag in tagsArray)
                {
                    components.Add(Base64UrlDecode(tag["name"]?.ToString() ?? ""));
                    components.Add(Base64UrlDecode(tag["value"]?.ToString() ?? ""));
                }
            }

            components.Add(Encoding.UTF8.GetBytes(tx["data_size"]?.ToString() ?? "0"));
            components.Add(Base64UrlDecode(tx["data_root"]?.ToString() ?? ""));

            return DeepHash(components);
        }

        // Arweave deep hash algorithm
        private static byte[] DeepHash(List<byte[]> chunks)
        {
            var ARWEAVE_HASH_STRING = Encoding.UTF8.GetBytes("list");
            using var sha384 = SHA384.Create();

            var tag = sha384.ComputeHash(
                Combine(ARWEAVE_HASH_STRING, Encoding.UTF8.GetBytes(chunks.Count.ToString())));

            foreach (var chunk in chunks)
            {
                var chunkHash = sha384.ComputeHash(
                    Combine(Encoding.UTF8.GetBytes("blob"), Encoding.UTF8.GetBytes(chunk.Length.ToString())));
                var dataHash = sha384.ComputeHash(chunk);
                tag = sha384.ComputeHash(Combine(tag, sha384.ComputeHash(Combine(chunkHash, dataHash))));
            }

            return tag;
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        private static byte[] SHA256Hash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<byte>();

            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
