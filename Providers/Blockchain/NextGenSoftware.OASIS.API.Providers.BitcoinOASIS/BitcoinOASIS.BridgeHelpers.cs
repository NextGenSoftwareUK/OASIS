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

                var balance = await GetAccountBalanceAsync(request.WalletAddress);
                if (balance.IsError)
                    OASISErrorHandling.HandleError(ref result, balance.Message, balance.Exception);
                else
                    result.Result = (double)balance.Result;
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

                if (request.FromBlock > request.ToBlock)
                    throw new ArgumentException("FromBlock must be at or before ToBlock.");

                var address = BitcoinAddress.Create(request.WalletAddress, SigningNetwork).ToString();
                var transactions = new List<IWalletTransaction>();
                var bounded = request.FromBlock.HasValue || request.ToBlock.HasValue;
                var endpoint = $"address/{address}/txs";
                var path = bounded ? $"{endpoint}/chain" : endpoint;
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var seenTransactions = new HashSet<string>(StringComparer.Ordinal);

                while (true)
                {
                    var page = await ReadPageAsync(path);
                    if (page.Length == 0)
                        break;

                    var belowRange = false;
                    var confirmedCount = 0;
                    string lastConfirmed = null;
                    foreach (var transaction in page)
                    {
                        var status = transaction.GetProperty("status");
                        var confirmed = status.GetProperty("confirmed").GetBoolean();
                        var txid = uint256.Parse(transaction.GetProperty("txid").GetString()).ToString();
                        if (!seenTransactions.Add(txid))
                            throw new InvalidOperationException("Esplora repeated a transaction in Bitcoin wallet history.");
                        if (confirmed)
                        {
                            confirmedCount++;
                            lastConfirmed = txid;
                        }
                        ulong? height = confirmed ? status.GetProperty("block_height").GetUInt64() : null;
                        if (bounded && (!height.HasValue || height > request.ToBlock))
                            continue;
                        if (height < request.FromBlock)
                        {
                            belowRange = true;
                            break;
                        }

                        var received = transaction.GetProperty("vout").EnumerateArray()
                            .Where(output => PaysAddress(output, address))
                            .Sum(output => output.GetProperty("value").GetInt64());
                        var spent = transaction.GetProperty("vin").EnumerateArray()
                            .Where(input => input.TryGetProperty("prevout", out var previous) &&
                                previous.ValueKind == JsonValueKind.Object && PaysAddress(previous, address))
                            .Sum(input => input.GetProperty("prevout").GetProperty("value").GetInt64());
                        var net = checked(received - spent);

                        transactions.Add(new WalletTransaction
                        {
                            TransactionId = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txid}"),
                            NativeTransactionId = txid,
                            ProviderType = ProviderType.Value,
                            NativeBlockNumber = height,
                            NativeBlockHash = confirmed ? status.GetProperty("block_hash").GetString() : null,
                            CreatedDate = confirmed ? DateTimeOffset.FromUnixTimeSeconds(status.GetProperty("block_time").GetInt64()).UtcDateTime : default,
                            FromWalletAddress = spent > 0 ? address : null,
                            ToWalletAddress = received > 0 ? address : null,
                            RawAmount = net.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            AmountUnit = "satoshi",
                            AmountEvidence = "Matching output value minus matching previous output value",
                            Amount = (double)Money.Satoshis(net).ToDecimal(MoneyUnit.BTC),
                            Description = $"Bitcoin transaction: {txid}"
                        });
                    }

                    if (belowRange || confirmedCount < 25)
                        break;

                    if (confirmedCount != 25 || !visited.Add(lastConfirmed))
                        throw new InvalidOperationException("Esplora returned an invalid Bitcoin history cursor.");
                    path = $"{endpoint}/chain/{lastConfirmed}";
                }

                result.Result = transactions;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        private static bool PaysAddress(JsonElement output, string address) =>
            output.TryGetProperty("scriptpubkey_address", out var recipient) && recipient.GetString() == address;

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

                var network = SigningNetwork;
                using var key = new NBitcoin.Key();
                result.Result = new KeyHelper.KeyPairAndWallet
                {
                    PrivateKey = key.GetWif(network).ToString(),
                    PublicKey = key.PubKey.ToHex(),
                    WalletAddressLegacy = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString(),
                    WalletAddressSegwitP2SH = key.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, network).ToString()
                };
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

                var address = BitcoinAddress.Create(accountAddress, SigningNetwork);
                var stats = await _httpClient.GetFromJsonAsync<JsonElement>($"address/{address}", token);
                var confirmed = stats.GetProperty("chain_stats");
                var pending = stats.GetProperty("mempool_stats");
                var satoshis = checked(confirmed.GetProperty("funded_txo_sum").GetInt64()
                    - confirmed.GetProperty("spent_txo_sum").GetInt64()
                    + pending.GetProperty("funded_txo_sum").GetInt64()
                    - pending.GetProperty("spent_txo_sum").GetInt64());

                if (satoshis < 0)
                    throw new InvalidOperationException("Esplora returned a negative Bitcoin balance.");

                result.Result = Money.Satoshis(satoshis).ToDecimal(MoneyUnit.BTC);
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

                token.ThrowIfCancellationRequested();
                var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                var key = mnemonic.DeriveExtKey().PrivateKey;
                var network = SigningNetwork;
                var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network);
                result.Result = (address.ToString(), key.GetWif(network).ToString(), mnemonic.ToString());
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

                token.ThrowIfCancellationRequested();
                var network = SigningNetwork;
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

        public Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
            => SubmitBridgePaymentAsync(amount, senderAccountAddress, null, senderPrivateKey, fromPool: false);

        public Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
            => SubmitBridgePaymentAsync(amount, null, receiverAccountAddress, _privateKey, fromPool: true);

        private async Task<OASISResult<BridgeTransactionResponse>> SubmitBridgePaymentAsync(
            decimal amount, string sender, string receiver, string signingKey, bool fromPool)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            var configuredPool = Environment.GetEnvironmentVariable("OASIS_BITCOIN_BRIDGE_POOL_ADDRESS");
            if (string.IsNullOrWhiteSpace(configuredPool))
            {
                OASISErrorHandling.HandleError(ref result, "A Bitcoin bridge pool address must be configured.");
                return result;
            }

            BitcoinAddress pool;
            try
            {
                pool = BitcoinAddress.Create(configuredPool, SigningNetwork);
            }
            catch (FormatException)
            {
                OASISErrorHandling.HandleError(ref result, "The Bitcoin bridge pool address is invalid for the configured network.");
                return result;
            }

            var payment = await SendSignedTransactionAsync(
                fromPool ? pool.ToString() : sender,
                fromPool ? receiver : pool.ToString(),
                amount, Array.Empty<byte>(), null, signingKey, commitmentOnly: false);
            result.ErrorCode = payment.ErrorCode;
            result.MetaData = payment.MetaData;
            if (payment.IsError)
            {
                OASISErrorHandling.HandleError(ref result, payment.Message, payment.Exception);
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = payment.Result.TransactionResult,
                IsSuccessful = true,
                Status = BridgeTransactionStatus.Pending
            };
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

                if (!uint256.TryParse(transactionHash, out var transactionId))
                {
                    OASISErrorHandling.HandleError(ref result, "A Bitcoin transaction ID is required.");
                    return result;
                }

                using var response = await _httpClient.GetAsync($"tx/{transactionId}/status", token);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    OASISErrorHandling.HandleError(ref result, "The Bitcoin transaction was not found by Esplora.");
                    return result;
                }

                response.EnsureSuccessStatusCode();
                var status = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
                result.Result = status.GetProperty("confirmed").GetBoolean()
                    ? BridgeTransactionStatus.Completed
                    : BridgeTransactionStatus.Pending;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Bitcoin transaction status: {ex.Message}", ex);
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
