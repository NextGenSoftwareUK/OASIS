using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Bitcoin balance for the address
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getreceivedbyaddress",
                    @params = new object[] { request.WalletAddress, 0 }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var balance = resultElement.GetDouble();
                    result.Result = balance;
                    result.IsError = false;
                    result.Message = "Bitcoin balance retrieved successfully";
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Address not found or has zero balance";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Bitcoin transactions for the address
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "listtransactions",
                    @params = new object[] { request.WalletAddress, 10 } // Default to 10 transactions
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                var transactions = new List<IWalletTransaction>();
                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement) && resultElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tx in resultElement.EnumerateArray())
                    {
                        // Extract transaction ID for deterministic GUID
                        var txId = tx.TryGetProperty("txid", out var txidProp) ? txidProp.GetString() : null;
                        Guid txGuid;
                        if (!string.IsNullOrWhiteSpace(txId))
                        {
                            using var sha256 = System.Security.Cryptography.SHA256.Create();
                            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(txId));
                            txGuid = new Guid(hashBytes.Take(16).ToArray());
                        }
                        else
                        {
                            // Fallback: use deterministic GUID from transaction data
                            var txData = $"{request.WalletAddress}:{tx.GetRawText()}";
                            txGuid = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txData}");
                        }
                        
                        var walletTx = new WalletTransaction
                        {
                            TransactionId = txGuid,
                            FromWalletAddress = tx.TryGetProperty("address", out var addr) ? addr.GetString() : string.Empty,
                            ToWalletAddress = tx.TryGetProperty("address", out var toAddr) ? toAddr.GetString() : string.Empty,
                            Amount = tx.TryGetProperty("amount", out var amt) ? amt.GetDouble() : 0.0,
                            Description = txId != null ? $"Bitcoin transaction: {txId}" : "Bitcoin transaction"
                        };
                        transactions.Add(walletTx);
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Bitcoin transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                //// Generate Bitcoin key pair using NBitcoin SDK (chain-specific)
                //var network = _network == "mainnet" ? Network.Main : Network.TestNet;
                
                //// Generate mnemonic seed phrase
                //var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                //var seedPhrase = mnemonic.ToString();
                
                //// Derive Bitcoin key from mnemonic
                //var extKey = mnemonic.DeriveExtKey();
                //var key = extKey.PrivateKey;
                
                //// Get Bitcoin address (public key)
                //var privateKey = key.GetWif(network).ToString();
                //var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
                
                // Create KeyPairAndWallet using KeyHelper but override with Bitcoin-specific values from NBitcoin SDK
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress(); //KeyHelper generates in Bitcoin format by default
                //if (keyPair != null)
                //{
                //    keyPair.PrivateKey = privateKey;
                //    keyPair.PublicKey = publicKey;
                //    keyPair.WalletAddressLegacy = publicKey; // Bitcoin address
                //}

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Bitcoin key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }



        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // Call Bitcoin RPC API to get address balance
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getreceivedbyaddress",
                    @params = new object[] { accountAddress, 0 } // 0 = minimum confirmations
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var balance = resultElement.GetDecimal();
                    result.Result = balance;
                    result.IsError = false;
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Bitcoin account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Generate Bitcoin key pair using NBitcoin
                var network = _network == "mainnet" ? Network.Main : Network.TestNet;
                var key = new NBitcoin.Key();
                var privateKey = key.GetWif(network).ToString();
                var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();

                // Generate seed phrase
                var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                var seedPhrase = mnemonic.ToString();

                result.Result = (publicKey, privateKey, seedPhrase);
                result.IsError = false;
                result.Message = "Bitcoin account created successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Bitcoin account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Restore Bitcoin key pair from seed phrase using NBitcoin
                var network = _network == "mainnet" ? Network.Main : Network.TestNet;
                var mnemonic = new Mnemonic(seedPhrase);
                var extKey = mnemonic.DeriveExtKey();
                var key = extKey.PrivateKey;
                var privateKey = key.GetWif(network).ToString();
                var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();

                result.Result = (publicKey, privateKey);
                result.IsError = false;
                result.Message = "Bitcoin account restored successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Bitcoin account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Convert amount to Satoshis
                var satoshiAmount = (ulong)(amount * 100_000_000m);
                var bridgePoolAddress = "1" + new string('0', 33);

                // Create Bitcoin transaction using RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sendtoaddress",
                    @params = new object[] { bridgePoolAddress, amount, "Bridge Withdrawal", "", true }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var txHash = resultElement.GetString();
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? CreateDeterministicGuid($"{ProviderType.Value}:withdraw:{senderAccountAddress}:{amount}:{DateTime.UtcNow.Ticks}").ToString("N"),
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                }
                else
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Failed to create transaction",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, "Failed to create Bitcoin transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Create Bitcoin transaction from bridge pool to receiver
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sendtoaddress",
                    @params = new object[] { receiverAccountAddress, amount, "Bridge Deposit", "", true }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var txHash = resultElement.GetString();
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? CreateDeterministicGuid($"{ProviderType.Value}:deposit:{receiverAccountAddress}:{amount}:{DateTime.UtcNow.Ticks}").ToString("N"),
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                }
                else
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Failed to create transaction",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, "Failed to create Bitcoin transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Bitcoin RPC for transaction status
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "gettransaction",
                    @params = new object[] { transactionHash }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    if (resultElement.TryGetProperty("confirmations", out var confirmationsElement))
                    {
                        var confirmations = confirmationsElement.GetInt32();
                        result.Result = confirmations > 0 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Pending;
                        result.IsError = false;
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                    }
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Bitcoin transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
