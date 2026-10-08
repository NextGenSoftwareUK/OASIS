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
                {
                    var hash = tx.GetProperty("hash").GetString();
                    transactions.Add(new Core.Interfaces.Wallet.Response.WalletTransaction
                    {
                        TransactionId = CreateDeterministicGuid($"{ProviderType.Value}:tx:{hash}:{tx.GetProperty("messageIndex").GetInt32()}"),
                        FromWalletAddress = tx.GetProperty("from").GetString(),
                        ToWalletAddress = tx.GetProperty("to").GetString(),
                        Amount = (double)NativeDisplayAmount(tx.GetProperty("amountUnits").GetString()),
                        CreatedDate = DateTime.Parse(tx.GetProperty("time").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                        Description = $"Cosmos transaction {hash}: {tx.GetProperty("memo").GetString()}"
                    });
                }
                result.Result = transactions;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
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
                if (string.IsNullOrWhiteSpace(transactionHash)) throw new ArgumentException("Transaction hash is required.");
                var tx = await ((CosmosSdkBackend)Backend).InvokeAsync("transactionStatus", new { transactionHash }, token);
                if (tx.ValueKind == JsonValueKind.Null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.Message = "Transaction is not indexed on the configured chain.";
                }
                else result.Result = tx.GetProperty("code").GetInt32() == 0
                    ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Deterministic identity input is required.");
            return new Guid(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input)).Take(16).ToArray());
        }
    }
}
