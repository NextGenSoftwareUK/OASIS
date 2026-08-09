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

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
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
}
