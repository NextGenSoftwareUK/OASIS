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
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS : Core.Interfaces.IOASISNativeFeeReceiptProvider
    {
        /// <summary>Read-only exact native gas evidence; not a quote, USD conversion or billing authorization.</summary>
        public async Task<OASISResult<JsonElement>> GetNativeFeeReceiptAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<JsonElement>();
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(transactionHash);
                var receipt = await InvokeSdkAsync("nativeFeeReceipt", new { transactionHash }, token);
                if (receipt.GetProperty("transactionHash").GetString() != transactionHash
                    || receipt.GetProperty("currency").GetString() != "SUI" || receipt.GetProperty("unit").GetString() != "MIST"
                    || string.IsNullOrWhiteSpace(receipt.GetProperty("payer").GetString())
                    || string.IsNullOrWhiteSpace(receipt.GetProperty("chainIdentifier").GetString()))
                    throw new InvalidOperationException("Sui native fee receipt identity, payer or denomination does not match.");
                _ = receipt.GetProperty("success").GetBoolean();
                var units = receipt.GetProperty("netUnits").GetString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(units ?? "", @"\A(?:0|-?[1-9][0-9]*)\z"))
                    throw new InvalidOperationException("Sui net fee must retain exact signed integral MIST units.");
                result.Result = receipt;
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
                ArgumentNullException.ThrowIfNull(request);
                var records = await InvokeSdkAsync("history", new { walletAddress = request.WalletAddress });
                var transactions = new List<IWalletTransaction>();
                foreach (var record in records.EnumerateArray())
                {
                    var digest = record.GetProperty("digest").GetString();
                    var units = System.Numerics.BigInteger.Parse(record.GetProperty("amountUnits").GetString(),
                        System.Globalization.CultureInfo.InvariantCulture);
                    var coinType = record.GetProperty("coinType").GetString();
                    var positive = units.Sign >= 0;
                    var recipients = record.GetProperty("recipients").EnumerateArray().Select(item => item.GetString()).ToArray();
                    transactions.Add(new WalletTransaction
                    {
                        TransactionId = CreateDeterministicGuid(digest + ":" + coinType),
                        NativeTransactionId = digest,
                        RawAmount = units.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        AmountUnit = coinType == null ? "non-monetary object change" : coinType + ":base-unit",
                        NativeOperation = "WalletNetBalanceChange",
                        NativeExecutionResult = record.GetProperty("success").GetBoolean() ? "Success" : "Failed",
                        FromWalletAddress = positive ? record.GetProperty("sender").GetString() : record.GetProperty("walletAddress").GetString(),
                        ToWalletAddress = positive ? record.GetProperty("walletAddress").GetString() : recipients.Length == 1 ? recipients[0] : null,
                        Amount = (double)System.Numerics.BigInteger.Abs(units) / Math.Pow(10, record.GetProperty("decimals").GetInt32()),
                        CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(record.GetProperty("timestampMs").GetInt64()).UtcDateTime,
                        TransactionType = positive ? TransactionType.Credit : TransactionType.Debit,
                        TransactionCategory = TransactionCategory.Other,
                        Description = $"Sui receipt {digest}; wallet net balance change (includes gas): {units} units of {coinType ?? "non-monetary object change"}; success={record.GetProperty("success").GetBoolean()}; recipients={string.Join(",", recipients)}"
                    });
                }
                result.Result = transactions;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
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
                var key = await InvokeSdkAsync("generateKey");
                result.Result = new KeyPairAndWallet
                {
                    PrivateKey = key.GetProperty("privateKey").GetString(),
                    PublicKey = key.GetProperty("publicKey").GetString(),
                    WalletAddressLegacy = key.GetProperty("address").GetString()
                };
                result.Message = "Official Sui SDK Ed25519 key pair generated.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input);

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }







        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                var balance = await InvokeSdkAsync("balance", new { walletAddress = accountAddress }, token);
                result.Result = decimal.Parse(balance.GetProperty("balance").GetString(),
                    System.Globalization.CultureInfo.InvariantCulture) / 1000000000m;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

    }
}
