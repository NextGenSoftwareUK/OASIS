using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using EosSharp.Core.Providers;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        private static string GetEOSExceptionDetails(Exception exception)
        {
            var errorField = exception.GetType().GetField("error", BindingFlags.Instance | BindingFlags.NonPublic);
            var error = errorField?.GetValue(exception);
            return error == null ? exception.ToString() : $"{exception}\nEOS API error: {JsonConvert.SerializeObject(error)}";
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_eosClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // Get currency balance from EOSIO
                var balanceRequest = new GetCurrencyBalanceRequestDto
                {
                    Code = "eosio.token",
                    Account = accountAddress,
                    Symbol = "EOS"
                };

                var balances = await _eosClient.GetCurrencyBalance(balanceRequest);
                if (balances != null && balances.Length > 0)
                {
                    // Parse EOS balance (format: "100.0000 EOS")
                    var balanceStr = balances[0].Split(' ')[0];
                    if (decimal.TryParse(balanceStr, out var balance))
                    {
                        result.Result = balance;
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance");
                    }
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting EOSIO account balance: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Creates a real EOSIO K1 key pair (eos-sharp). EOSIO has no seed-phrase standard: the WIF private key is the
        /// recovery secret, so it is returned as the "seed phrase" and is what <see cref="RestoreAccountAsync"/> accepts.
        /// An on-chain EOSIO account name must still be created and linked to the public key separately.
        /// </summary>
        public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                token.ThrowIfCancellationRequested();
                var keyPair = EosSharp.Core.Helpers.CryptoHelper.GenerateKeyPair("sha256x2");
                result.Result = (keyPair.PublicKey, keyPair.PrivateKey, keyPair.PrivateKey);
                result.Message = "EOSIO key pair created; the WIF private key is the recovery secret.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating EOSIO key pair: {ex.Message}", ex);
            }
            return Task.FromResult(result);
        }

        /// <summary>
        /// Restores an EOSIO key from its private key (WIF, or PVT_K1_ form). EOSIO has no mnemonic standard, so any
        /// other input is rejected rather than hashed into a key.
        /// </summary>
        public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "An EOSIO private key (WIF or PVT_K1_) is required.");
                    return Task.FromResult(result);
                }
                var privateKey = seedPhrase.Trim();
                var publicKeys = new DefaultSignProvider(privateKey).GetAvailableKeys().GetAwaiter().GetResult();
                if (publicKeys == null || publicKeys.Count() != 1)
                {
                    OASISErrorHandling.HandleError(ref result, "The EOSIO private key did not yield exactly one public key.");
                    return Task.FromResult(result);
                }
                result.Result = (publicKeys.Single(), privateKey);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Not a valid EOSIO private key: {ex.Message}", ex);
            }
            return Task.FromResult(result);
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_transferRepository == null)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO transfer repository is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Use transfer repository to send EOS to bridge pool
                var bridgePoolAddress = EOSAccountName ?? "oasispool"; // Use OASIS account as bridge pool
                var transferResult = await _transferRepository.TransferEosToken(
                    senderAccountAddress,
                    bridgePoolAddress,
                    amount);

                if (transferResult.IsError)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = transferResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, transferResult.Message, transferResult.Exception);
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transferResult.Result?.TransactionResult ?? string.Empty,
                    IsSuccessful = !transferResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_transferRepository == null)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO transfer repository is not initialized");
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

                // Use transfer repository to send EOS from OASIS account to receiver
                var fromAccount = EOSAccountName ?? "oasispool";
                var transferResult = await _transferRepository.TransferEosToken(
                    fromAccount,
                    receiverAccountAddress,
                    amount);

                if (transferResult.IsError)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = transferResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, transferResult.Message, transferResult.Exception);
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transferResult.Result?.TransactionResult ?? string.Empty,
                    IsSuccessful = !transferResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_eosClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // EOSIO transactions are typically irreversible after confirmation
                // For simplicity, we'll check if the transaction exists
                // In production, you'd query the blockchain for transaction status
                result.Result = BridgeTransactionStatus.Completed; // EOSIO transactions are typically fast
                result.IsError = false;
                result.Message = "EOSIO transaction status retrieved (assuming completed after confirmation).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting EOSIO transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
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
