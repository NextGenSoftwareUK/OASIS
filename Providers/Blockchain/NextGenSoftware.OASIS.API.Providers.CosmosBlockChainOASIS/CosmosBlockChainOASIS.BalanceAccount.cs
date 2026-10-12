using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using KeyPairAndWallet = NextGenSoftware.Utilities.KeyHelper.KeyPairAndWallet;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        private decimal NativeDisplayAmount(string units)
        {
            decimal scale = 1;
            for (var i = 0; i < _nativeDecimals; i++) scale *= 10;
            return decimal.Parse(units, NumberStyles.None, CultureInfo.InvariantCulture) / scale;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "Wallet balance request is required.");
                return result;
            }
            var balance = await GetAccountBalanceAsync(request.WalletAddress);
            if (balance.IsError)
                OASISErrorHandling.HandleError(ref result, balance.Message, balance.Exception);
            else result.Result = (double)balance.Result;
            return result;
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (string.IsNullOrWhiteSpace(accountAddress)) throw new ArgumentException("Account address is required.");
                var coin = await ((CosmosSdkBackend)Backend).InvokeAsync("balance",
                    new { walletAddress = accountAddress, denom = _nativeDenom }, token);
                if (coin.GetProperty("denom").GetString() != _nativeDenom)
                    throw new InvalidOperationException("SDK returned a different asset denomination.");
                result.Result = NativeDisplayAmount(coin.GetProperty("amount").GetString());
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
            => GetTransactionsAsync(request).GetAwaiter().GetResult();

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                    throw new ArgumentException("Wallet address is required.");
                var indexed = await ((CosmosSdkBackend)Backend).InvokeAsync("transactions",
                    new { walletAddress = request.WalletAddress, denom = _nativeDenom });
                var transactions = new List<IWalletTransaction>();
                foreach (var tx in indexed.EnumerateArray())
                    transactions.Add(MapIndexedWalletTransaction(tx, request.WalletAddress));
                result.Result = transactions;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        private IWalletTransaction MapIndexedWalletTransaction(System.Text.Json.JsonElement tx, string walletAddress)
        {
                {
                    var hash = tx.GetProperty("hash").GetString();
                    var rawAmount = tx.GetProperty("amountUnits").GetString();
                    var from = tx.GetProperty("from").GetString();
                    var to = tx.GetProperty("to").GetString();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(hash ?? "", "\\A[0-9a-fA-F]{64}\\z")
                        || !System.Text.RegularExpressions.Regex.IsMatch(rawAmount ?? "", "\\A(?:0|[1-9][0-9]*)\\z")
                        || tx.GetProperty("denom").GetString() != _nativeDenom
                        || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)
                        || (from != walletAddress && to != walletAddress))
                        throw new InvalidOperationException("Cosmos indexed transfer identity, amount, denomination or wallet participation is invalid.");
                    var executionCode = tx.GetProperty("code").GetUInt32();
                    var operation = tx.GetProperty("operation").GetString();
                    var transferIndex = tx.GetProperty("transferIndex").GetInt32();
                    var messageIndex = tx.GetProperty("messageIndex").GetInt32();
                    if (messageIndex < 0 || transferIndex < 0
                        || (operation == "/cosmos.bank.v1beta1.MsgSend" && transferIndex != 0)
                        || (operation != "/cosmos.bank.v1beta1.MsgSend" && operation != "/cosmos.bank.v1beta1.MsgMultiSend"))
                        throw new InvalidOperationException("Invalid Cosmos bank history operation or output index.");
                    var rowIdentity = $"{ProviderType.Value}:tx:{hash}:{messageIndex}";
                    if (operation == "/cosmos.bank.v1beta1.MsgMultiSend") rowIdentity += ":" + transferIndex;
                    return new Core.Interfaces.Wallet.Response.WalletTransaction
                    {
                        TransactionId = CreateDeterministicGuid(rowIdentity),
                        NativeTransactionId = hash,
                        RawAmount = rawAmount,
                        AmountUnit = tx.GetProperty("denom").GetString(),
                        NativeOperation = operation,
                        NativeExecutionResult = executionCode == 0 ? "Success" : "Failed",
                        NativeError = executionCode == 0 ? null : "Cosmos execution code " + executionCode.ToString(CultureInfo.InvariantCulture),
                        TransactionType = tx.GetProperty("from").GetString() == walletAddress
                            ? Core.Enums.TransactionType.Debit : Core.Enums.TransactionType.Credit,
                        FromWalletAddress = tx.GetProperty("from").GetString(),
                        ToWalletAddress = tx.GetProperty("to").GetString(),
                        Amount = (double)NativeDisplayAmount(tx.GetProperty("amountUnits").GetString()),
                        CreatedDate = DateTime.Parse(tx.GetProperty("time").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                        Description = $"Cosmos transaction {hash} (requested amount; execution code {executionCode}): {tx.GetProperty("memo").GetString()}"
                    };
                }
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
            => GenerateKeyPairAsync().GetAwaiter().GetResult();

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                var key = await ((CosmosSdkBackend)Backend).InvokeAsync("generateKey");
                result.Result = new KeyPairAndWallet
                {
                    PublicKey = key.GetProperty("publicKey").GetString(),
                    PrivateKey = key.GetProperty("mnemonic").GetString(),
                    WalletAddressLegacy = key.GetProperty("address").GetString()
                };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                var key = await ((CosmosSdkBackend)Backend).InvokeAsync("generateKey", cancellationToken: token);
                var mnemonic = key.GetProperty("mnemonic").GetString();
                result.Result = (key.GetProperty("publicKey").GetString(), mnemonic, mnemonic);
                result.Message = "Recoverable Cosmos BIP39 wallet created; funding creates its on-chain account.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (string.IsNullOrWhiteSpace(seedPhrase)) throw new ArgumentException("Mnemonic or hex signing key is required.");
                var key = await ((CosmosSdkBackend)Backend).InvokeAsync("restoreKey", new { privateKey = seedPhrase }, token);
                result.Result = (key.GetProperty("publicKey").GetString(), seedPhrase);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        // The configured signer owns the native-asset bridge pool. A storage contract address
        // is not a signing account and must never be substituted as the bank sender.
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                var pool = await ((CosmosSdkBackend)Backend).InvokeAsync("restoreKey");
                var transfer = await SendBankTransferAsync(senderAccountAddress, pool.GetProperty("address").GetString(),
                    amount, "OASIS bridge withdrawal", _nativeDenom, senderPrivateKey);
                if (transfer.IsError) throw transfer.Exception ?? new InvalidOperationException(transfer.Message);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transfer.Result.TransactionResult,
                    IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                var pool = await ((CosmosSdkBackend)Backend).InvokeAsync("restoreKey");
                var transfer = await SendBankTransferAsync(pool.GetProperty("address").GetString(), receiverAccountAddress,
                    amount, "OASIS bridge deposit", _nativeDenom, _privateKey);
                if (transfer.IsError) throw transfer.Exception ?? new InvalidOperationException(transfer.Message);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transfer.Result.TransactionResult,
                    IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(transactionHash ?? "", @"\A[0-9a-fA-F]{64}\z"))
                    throw new ArgumentException("An exact 64-hex Cosmos transaction hash is required.");
                var tx = await ((CosmosSdkBackend)Backend).InvokeAsync("transactionStatus", new { transactionHash }, token);
                if (tx.ValueKind == JsonValueKind.Null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.Message = "Transaction is not indexed on the configured chain.";
                }
                else result.Result = MapTransactionStatus(tx, transactionHash);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Deterministic identity input is required.");
            return new Guid(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input)).Take(16).ToArray());
        }
        internal static BridgeTransactionStatus MapExecutionCode(int code)
        {
            if (code < 0) throw new ArgumentOutOfRangeException(nameof(code), "Native execution codes must be nonnegative.");
            return code == 0 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Failed;
        }

        internal static BridgeTransactionStatus MapTransactionStatus(JsonElement tx, string requestedHash)
        {
            if (tx.ValueKind != JsonValueKind.Object || !tx.TryGetProperty("hash", out var hash)
                || hash.ValueKind != JsonValueKind.String || !string.Equals(hash.GetString(), requestedHash, StringComparison.OrdinalIgnoreCase)
                || !tx.TryGetProperty("height", out var height) || !height.TryGetInt64(out var blockHeight) || blockHeight <= 0)
                throw new InvalidOperationException("Cosmos transaction status does not establish the requested hash and committed block height.");
            return MapExecutionCode(tx.GetProperty("code").GetInt32());
        }
    }
}
