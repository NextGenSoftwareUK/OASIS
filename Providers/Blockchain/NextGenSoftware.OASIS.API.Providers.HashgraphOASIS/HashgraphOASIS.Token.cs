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
    public partial class HashgraphOASIS
    {
        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                    throw new ArgumentException("A real Hedera sender signing key is required.");
                if (!string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("Use an explicitly recovered Hedera key, not an ambiguous seed-and-key request.");
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
                var native = string.IsNullOrWhiteSpace(request.FromTokenAddress) || request.FromTokenAddress == "HBAR";
                // HTS precision is obtained from the token ledger, not HBAR's eight decimals.
                var tinybars = native ? checked(request.Amount * 100000000m) : 0m;
                if (native && (tinybars != decimal.Truncate(tinybars) || tinybars > long.MaxValue))
                    throw new ArgumentOutOfRangeException(nameof(request.Amount), "HBAR amount must fit exact int64 tinybars.");
                var committed = await HederaSdkClient.InvokeAsync(native ? "transfer" : "tokenTransfer", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = request.FromWalletAddress, recipient = request.ToWalletAddress,
                    privateKey = request.OwnerPrivateKey, keyType = "der", memo = request.MemoText,
                    publicKey = request.OwnerPublicKey, transactionId = HederaSdkClient.CreateTransactionId(request.FromWalletAddress),
                    tinybars = native ? ((long)tinybars).ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                    tokenId = request.FromTokenAddress, amount = request.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
                if (committed.GetProperty("status").GetString() != "SUCCESS")
                    throw new InvalidOperationException("Hedera SDK did not return a successful consensus receipt.");
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                {
                    TransactionResult = committed.GetProperty("transactionId").GetString()
                };
                result.Message = "Hedera transfer confirmed by consensus receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
                if (request.MetaData == null || !request.MetaData.TryGetValue("TokenAddress", out var tokenAddress)
                    || !System.Text.RegularExpressions.Regex.IsMatch(tokenAddress ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                    throw new ArgumentException("TokenAddress metadata must identify an existing HTS token.");
                if (request.MintedByAvatarId == Guid.Empty)
                    throw new ArgumentException("The minting avatar's real Hedera wallet is required.");
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.MintedByAvatarId,
                    Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var wallet = walletResult.Result;
                if (string.IsNullOrWhiteSpace(wallet.PrivateKey))
                    throw new ArgumentException("The minting wallet has no signing key.");
                var recipient = request.MetaData.TryGetValue("MintToWalletAddress", out var target) ? target : wallet.WalletAddress;
                var committed = await HederaSdkClient.InvokeAsync("tokenMint", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress, recipient,
                    tokenId = tokenAddress, amount = request.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der", memo = request.MemoText,
                    transactionId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress)
                });
                if (committed.GetProperty("status").GetString() != "SUCCESS")
                    throw new InvalidOperationException("HTS mint did not return a successful consensus receipt.");
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                {
                    TransactionResult = committed.GetProperty("transactionId").GetString()
                };
                result.Message = "HTS mint confirmed by consensus receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var burnFromAddress = walletResult.Result.WalletAddress;
                var burnAmount = 1m; // Default amount
                
                // Hedera Token Service (HTS) burn using Mirror Node API
                var tokenBurnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.TokenAddress}/burn";
                var burnData = new
                {
                    account_id = burnFromAddress,
                    amount = (long)(burnAmount * 100000000) // Convert to tinybars
                };

                var content = new StringContent(JsonSerializer.Serialize(burnData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenBurnUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Lock token by transferring to a lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = request.FromWalletAddress,
                    ToWalletAddress = lockAccount,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token locked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.UnlockedByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                // Unlock token by transferring from lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = lockAccount,
                    ToWalletAddress = walletResult.Result.WalletAddress,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get account balance from Hedera Mirror Node API
                var accountId = request.WalletAddress;
                if (!System.Text.RegularExpressions.Regex.IsMatch(accountId ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                    throw new ArgumentException("A numeric Hedera account ID is required for this balance query.");
                using var response = await _httpClient.GetAsync("accounts/" + accountId);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (accountData.TryGetProperty("balance", out var balances) && balances.ValueKind == JsonValueKind.Object
                        && balances.TryGetProperty("balance", out var balance) && balance.TryGetInt64(out var tinybars)
                        && tinybars >= 0)
                    {
                        // Hedera balances are in tinybars (1 HBAR = 100,000,000 tinybars)
                        result.Result = tinybars / 100000000.0;
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully from Hashgraph";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var accountId = request.WalletAddress;
                var transactionsUrl = $"{_httpClient.BaseAddress}/api/v1/accounts/{accountId}/transactions?limit=100";
                var response = await _httpClient.GetAsync(transactionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    var transactions = new List<IWalletTransaction>();
                    if (txData.TryGetProperty("transactions", out var txArray))
                    {
                        foreach (var tx in txArray.EnumerateArray())
                        {
                            var txId = tx.TryGetProperty("transaction_id", out var txIdEl) && txIdEl.ValueKind == JsonValueKind.String
                                ? txIdEl.GetString()
                                : string.Empty;

                            var walletTx = new WalletTransaction
                            {
                                TransactionId = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txId}"),
                                FromWalletAddress = accountId,
                                ToWalletAddress = string.Empty,
                                Amount = 0,
                                Description = string.IsNullOrWhiteSpace(txId) ? "Hedera transaction" : $"Hedera transaction: {txId}"
                            };
                            transactions.Add(walletTx);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions from Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                var generated = await HederaSdkClient.InvokeAsync("generateKey", new { keyType = "ed25519" });
                result.Result = new NextGenSoftware.Utilities.KeyHelper.KeyPairAndWallet
                {
                    PrivateKey = generated.GetProperty("privateKey").GetString(),
                    PublicKey = generated.GetProperty("publicKey").GetString(),
                    WalletAddressLegacy = null
                };
                result.Message = "Hedera SDK key pair generated. A ledger account must be created or linked separately.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Burn NFT on Hedera using HTS
                var burnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.NFTTokenAddress}/nfts/{request.Web3NFTId}/burn";
                var response = await _httpClient.PostAsync(burnUrl, new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph NFT burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }


    }
}
