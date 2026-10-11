using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private WalletManager _walletManager;
        private HttpClient _httpClient;
        private string _contractAddress;
        private readonly string _nftEscrowContractId;
        private readonly string _nftEscrowRuntimeCodeHash;
        private readonly long _nftEscrowGasLimit;
        private readonly long _nftEscrowMaxFeeTinybars;
        private readonly string _hederaNetwork;
        private readonly string _bridgePoolAccountId;
        private readonly Func<string> _bridgePoolSigningKey;
        private readonly Dictionary<string, string> _consensusNodes;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        public HashgraphOASIS(string rpcEndpoint = "https://mainnet-public.mirrornode.hedera.com/api/v1", string network = "mainnet", string chainId = "295", WalletManager walletManager = null, IDictionary<string, string> consensusNodes = null, HederaStorageConfiguration storageConfiguration = null, string bridgePoolAccountId = null, Func<string> bridgePoolSigningKey = null, string nftEscrowContractId = null, long nftEscrowGasLimit = 0, long nftEscrowMaxFeeTinybars = 0, string nftEscrowRuntimeCodeHash = null)
            : base(new ConfiguredHederaStorageBackend(network, consensusNodes, storageConfiguration))
        {
            if (network != "mainnet" && network != "testnet" && network != "previewnet" && network != "local")
                throw new ArgumentException("An explicit supported Hedera network is required.", nameof(network));
            if (network == "local" && (consensusNodes == null || consensusNodes.Count == 0))
                throw new ArgumentException("Local Hedera requires configured consensus nodes.", nameof(consensusNodes));
            if (network != "local" && consensusNodes != null && consensusNodes.Count != 0)
                throw new ArgumentException("Custom consensus nodes are restricted to the local network.", nameof(consensusNodes));
            _consensusNodes = consensusNodes == null ? null : new Dictionary<string, string>(consensusNodes);
            if (_consensusNodes != null)
                foreach (var node in _consensusNodes)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(node.Key, @"\A127\.0\.0\.1:[0-9]+\z")
                        || !Uri.TryCreate("http://" + node.Key, UriKind.Absolute, out var endpoint) || endpoint.Port <= 0
                        || !System.Text.RegularExpressions.Regex.IsMatch(node.Value ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                        throw new ArgumentException("Local consensus nodes require loopback endpoints and numeric account IDs.", nameof(consensusNodes));
                }
            _walletManager = walletManager;
            _bridgePoolAccountId = bridgePoolAccountId;
            _bridgePoolSigningKey = bridgePoolSigningKey;
            _nftEscrowContractId = nftEscrowContractId;
            _nftEscrowRuntimeCodeHash = nftEscrowRuntimeCodeHash;
            _nftEscrowGasLimit = nftEscrowGasLimit;
            _nftEscrowMaxFeeTinybars = nftEscrowMaxFeeTinybars;
            _hederaNetwork = network;
            this.ProviderName = "HashgraphOASIS";
            this.ProviderDescription = "Hashgraph Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                BaseAddress = NormalizeMirrorEndpoint(rpcEndpoint)
            };
        }

        internal static Uri NormalizeMirrorEndpoint(string endpoint)
        {
            var uri = new Uri(endpoint, UriKind.Absolute);
            if (uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new ArgumentException("Hedera mirror endpoint requires HTTPS or loopback HTTP, without credentials or query.");
            var path = uri.AbsolutePath.TrimEnd('/');
            if (path != "" && path != "/api/v1")
                throw new ArgumentException("Hedera mirror endpoint must be a host root or /api/v1.");
            return new UriBuilder(uri) { Path = "/api/v1/" }.Uri;
        }


    }

    /// <summary>
    /// REAL Hashgraph client for interacting with Hashgraph network
    /// </summary>
    public class HashgraphClient
    {
        private readonly string _networkUrl;
        private readonly string _accountId;
        private readonly string _privateKey;

        public HashgraphClient(string networkUrl = "https://mainnet-public.mirrornode.hedera.com", string accountId = "", string privateKey = "")
        {
            _networkUrl = networkUrl;
            _accountId = accountId;
            _privateKey = privateKey;
        }

        public async Task<string> ResolveAccountIdFromKeysAsync(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
                return string.Empty;

            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Try common mirror node query formats for public key.
                    var candidates = new[]
                    {
                        $"{_networkUrl}/api/v1/accounts?account.publickey={Uri.EscapeDataString(publicKey)}&limit=1",
                        $"{_networkUrl}/api/v1/accounts?publickey={Uri.EscapeDataString(publicKey)}&limit=1",
                        $"{_networkUrl}/api/v1/accounts?key={Uri.EscapeDataString(publicKey)}&limit=1"
                    };

                    foreach (var url in candidates)
                    {
                        var response = await httpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                            continue;

                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        if (accountData.TryGetProperty("accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array && accounts.GetArrayLength() > 0)
                        {
                            var first = accounts[0];
                            if (first.TryGetProperty("account", out var acctEl) && acctEl.ValueKind == JsonValueKind.String)
                                return acctEl.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // ignore and return empty
            }

            return string.Empty;
        }

        /// <summary>
        /// Get account information from Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> GetAccountInfoAsync(string accountId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/accounts/{accountId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        return new HashgraphAccountInfo
                        {
                            AccountId = accountData.TryGetProperty("account", out var account) &&
                                       account.TryGetProperty("account", out var accId) ? accId.GetString() : accountId,
                            Balance = accountData.TryGetProperty("account", out var acc) &&
                                     acc.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                            AutoRenewPeriod = accountData.TryGetProperty("account", out var acc2) &&
                                           acc2.TryGetProperty("auto_renew_period", out var period) ? period.GetInt64() : 0,
                            Expiry = accountData.TryGetProperty("account", out var acc3) &&
                                   acc3.TryGetProperty("expiry_timestamp", out var expiry) ? expiry.GetString() : ""
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Get account information by email from Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> GetAccountInfoByEmailAsync(string email)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Search for account by email in Hashgraph network
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/accounts?email={email}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        if (accountData.TryGetProperty("accounts", out var accounts) && accounts.GetArrayLength() > 0)
                        {
                            var firstAccount = accounts[0];
                            return new HashgraphAccountInfo
                            {
                                AccountId = firstAccount.TryGetProperty("account", out var account) ? account.GetString() : "",
                                Balance = firstAccount.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                                AutoRenewPeriod = firstAccount.TryGetProperty("auto_renew_period", out var period) ? period.GetInt64() : 0,
                                Expiry = firstAccount.TryGetProperty("expiry_timestamp", out var expiry) ? expiry.GetString() : ""
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Send transaction to Hashgraph network
        /// </summary>
        public async Task<HashgraphTransactionData> SendTransactionAsync(HashgraphTransactionData transactionData)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(transactionData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{_networkUrl}/api/v1/transactions", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var transactionResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                        var txId = transactionResponse.TryGetProperty("transaction_id", out var txIdProp)
                            ? txIdProp.GetString()
                            : string.Empty;

                        // If the mirror node did not return a transaction_id, treat as an error rather than inventing one.
                        if (string.IsNullOrWhiteSpace(txId))
                            return null;

                        return new HashgraphTransactionData
                        {
                            FromAddress = transactionData.FromAddress,
                            ToAddress = transactionData.ToAddress,
                            Amount = transactionData.Amount,
                            Memo = transactionData.Memo,
                            TransactionId = txId,
                            Status = "Success"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if transaction fails
            }
            return null;
        }

        /// <summary>
        /// Send transaction to Hashgraph network synchronously
        /// </summary>
        public HashgraphTransactionData SendTransaction(HashgraphTransactionData transactionData)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(transactionData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = httpClient.PostAsync($"{_networkUrl}/api/v1/transactions", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var transactionResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                        var txId = transactionResponse.TryGetProperty("transaction_id", out var txIdProp)
                            ? txIdProp.GetString()
                            : string.Empty;

                        if (string.IsNullOrWhiteSpace(txId))
                            return null;

                        return new HashgraphTransactionData
                        {
                            FromAddress = transactionData.FromAddress,
                            ToAddress = transactionData.ToAddress,
                            Amount = transactionData.Amount,
                            Memo = transactionData.Memo,
                            TransactionId = txId,
                            Status = "Success"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if transaction fails
            }
            return null;
        }

        /// <summary>
        /// Get NFT data from Hashgraph network
        /// </summary>
        public async Task<string> GetNFTData(string nftTokenAddress)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/tokens/{nftTokenAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Create a new account on Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> CreateAccountAsync()
        {
            try
            {
                // Create a new Hedera-compatible Ed25519 keypair and mnemonic.
                // NOTE: Creating the on-chain Hedera account requires a funded operator account; this method prepares keys + mnemonic.
                var mnemonic = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve);
                var seed = mnemonic.DeriveSeed();
                var edSeed = seed.Take(32).ToArray();

                byte[] publicKey;
                byte[] expandedPrivateKey;
                Chaos.NaCl.Ed25519.KeyPairFromSeed(out publicKey, out expandedPrivateKey, edSeed);

                var publicKeyB64 = Convert.ToBase64String(publicKey);
                var privateKeyB64 = Convert.ToBase64String(edSeed);

                var accountId = await ResolveAccountIdFromKeysAsync(publicKeyB64);

                return new HashgraphAccountInfo
                {
                    AccountId = accountId,
                    PublicKey = publicKeyB64,
                    PrivateKey = privateKeyB64,
                    SeedPhrase = mnemonic.ToString()
                };
            }
            catch (Exception)
            {
                // Return null if creation fails
            }
            return null;
        }

        /// <summary>
        /// Restore an account from seed phrase on Hashgraph network using Hedera SDK-compatible client.
        /// </summary>
        public async Task<HashgraphAccountInfo> RestoreAccountAsync(string seedPhrase)
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentNullException(nameof(seedPhrase), "Seed phrase is required to restore a Hashgraph account.");

            // Interpret seedPhrase as a BIP-39 mnemonic.
            var mnemonic = new NBitcoin.Mnemonic(seedPhrase);
            var seed = mnemonic.DeriveSeed();
            var edSeed = seed.Take(32).ToArray();

            byte[] publicKey;
            byte[] expandedPrivateKey;
            Chaos.NaCl.Ed25519.KeyPairFromSeed(out publicKey, out expandedPrivateKey, edSeed);

            var publicKeyB64 = Convert.ToBase64String(publicKey);
            var privateKeyB64 = Convert.ToBase64String(edSeed);
            var accountId = await ResolveAccountIdFromKeysAsync(publicKeyB64);

            return new HashgraphAccountInfo
            {
                AccountId = accountId,
                PublicKey = publicKeyB64,
                PrivateKey = privateKeyB64,
                SeedPhrase = mnemonic.ToString()
            };
        }

        /// <summary>
        /// Get transaction status from Hashgraph network
        /// </summary>
        public async Task<NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus> GetTransactionStatusAsync(string transactionId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/transactions/{transactionId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        var statusStr = txData.TryGetProperty("status", out var status) ? status.GetString() : "Unknown";
                        
                        // Convert string status to BridgeTransactionStatus enum
                        if (statusStr == "SUCCESS" || statusStr == "Completed")
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed;
                        else if (statusStr == "PENDING" || statusStr == "Pending")
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending;
                        else
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound;
                    }
                }
            }
            catch (Exception)
            {
                // Return NotFound if query fails
            }
            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound;
        }
    }

    /// <summary>
    /// Hashgraph account information
    /// </summary>
    public class HashgraphAccountInfo
    {
        public string AccountId { get; set; }
        public long? Balance { get; set; }
        public long? AutoRenewPeriod { get; set; }
        public string Expiry { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string SeedPhrase { get; set; }
    }

    /// <summary>
    /// Hashgraph transaction data
    /// </summary>
    public class HashgraphTransactionData
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string TokenId { get; set; }
    }

    /// <summary>
    /// Hashgraph transaction response
    /// </summary>
    public class TransactionResponse
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
    }
}
