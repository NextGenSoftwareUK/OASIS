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
                ArgumentNullException.ThrowIfNull(request);
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount), "An explicit positive burn amount is required.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.TokenAddress ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                    throw new ArgumentException("A numeric HTS token ID is required.");
                if (request.BurntByAvatarId == Guid.Empty)
                    throw new ArgumentException("The burning avatar's real treasury wallet is required.");
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId,
                    Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError || walletResult.Result == null)
                    throw new InvalidOperationException("Could not retrieve the burning avatar's Hedera wallet.");
                var wallet = walletResult.Result;
                var committed = await HederaSdkClient.InvokeAsync("tokenBurn", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress,
                    recipient = wallet.WalletAddress, tokenId = request.TokenAddress,
                    amount = request.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der",
                    transactionId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress)
                });
                if (committed.GetProperty("status").GetString() != "SUCCESS")
                    throw new InvalidOperationException("HTS burn did not return a successful consensus receipt.");
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                {
                    TransactionResult = committed.GetProperty("transactionId").GetString()
                };
                result.Message = "HTS burn confirmed by consensus receipt.";
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

                ArgumentNullException.ThrowIfNull(request);
                var accountId = request.WalletAddress;
                if (!System.Text.RegularExpressions.Regex.IsMatch(accountId ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                    throw new ArgumentException("A numeric Hedera wallet account ID is required.");
                var endpoint = _httpClient.BaseAddress;
                var cutoffTicks = DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;
                var cutoff = (cutoffTicks / TimeSpan.TicksPerSecond).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "." + ((cutoffTicks % TimeSpan.TicksPerSecond) * 100).ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
                var upperBound = "timestamp=lte:" + cutoff;
                var next = new Uri(endpoint, $"transactions?account.id={accountId}&limit=100&order=asc&timestamp=gte:0&{upperBound}");
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var identities = new HashSet<string>(StringComparer.Ordinal);
                var transactions = new List<IWalletTransaction>();
                  decimal? previousConsensusTimestamp = null;
                  var exactCutoff = decimal.Parse(cutoff, System.Globalization.CultureInfo.InvariantCulture);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                while (next != null)
                {
                    if (next.Scheme != endpoint.Scheme || next.Authority != endpoint.Authority
                        || next.AbsolutePath != new Uri(endpoint, "transactions").AbsolutePath
                        || !visited.Add(next.AbsoluteUri) || visited.Count > 5000)
                        throw new InvalidOperationException("Hedera history pagination is unsafe or exceeds the bounded page budget.");
                    var query = next.Query.TrimStart('?').Split('&').Select(Uri.UnescapeDataString).ToArray();
                    if (query.Count(value => value.StartsWith("account.id=", StringComparison.Ordinal)) != 1
                        || !query.Contains("account.id=" + accountId))
                        throw new InvalidOperationException("Hedera history pagination changed the requested account.");
                    using var response = await _httpClient.GetAsync(next, timeout.Token);
                    response.EnsureSuccessStatusCode();
                    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                    var txData = document.RootElement;
                    var txArray = txData.GetProperty("transactions");
                    foreach (var tx in txArray.EnumerateArray())
                    {
                        var walletTx = MapMirrorWalletTransaction(tx, accountId);
                          var consensusTimestamp = decimal.Parse(tx.GetProperty("consensus_timestamp").GetString(),
                              System.Globalization.CultureInfo.InvariantCulture);
                          if (consensusTimestamp > exactCutoff)
                            throw new InvalidOperationException("Hedera history record exceeds the fixed scan timestamp.");
                          if (previousConsensusTimestamp.HasValue && consensusTimestamp <= previousConsensusTimestamp.Value)
                              throw new InvalidOperationException("Hedera history did not preserve strictly ascending consensus timestamps.");
                          previousConsensusTimestamp = consensusTimestamp;
                        var identity = walletTx.NativeTransactionId + ":" + tx.GetProperty("consensus_timestamp").GetString();
                        if (!identities.Add(identity)) throw new InvalidOperationException("Hedera history returned a duplicate consensus record.");
                        transactions.Add(walletTx);
                    }
                    var link = txData.GetProperty("links").GetProperty("next");
                    next = link.ValueKind == JsonValueKind.Null ? null : new Uri(endpoint, link.GetString());
                    if (next != null)
                    {
                        var bounds = next.Query.TrimStart('?').Split('&').Select(Uri.UnescapeDataString)
                            .Where(value => value.StartsWith("timestamp=lte:", StringComparison.Ordinal)).ToArray();
                        if (bounds.Length > 1 || (bounds.Length == 1 && bounds[0] != upperBound))
                            throw new InvalidOperationException("Hedera history pagination changed the fixed scan timestamp.");
                        if (bounds.Length == 0)
                            next = new Uri(next.AbsoluteUri + (next.Query.Length == 0 ? "?" : "&") + upperBound);
                    }
                }
                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} indexed Hedera records; HBAR amounts are signed wallet deltas including fees. Mirror coverage is not a consensus completeness proof.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        private static WalletTransaction MapMirrorWalletTransaction(JsonElement tx, string accountId)
        {
            var id = tx.GetProperty("transaction_id").GetString();
            if (!System.Text.RegularExpressions.Regex.IsMatch(id ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+-[0-9]+-[0-9]+\z"))
                throw new InvalidOperationException("Hedera mirror record has no valid native transaction identity.");
            var timestamp = tx.GetProperty("consensus_timestamp").GetString();
            if (!System.Text.RegularExpressions.Regex.IsMatch(timestamp ?? "", @"\A[0-9]+\.[0-9]{9}\z"))
                throw new InvalidOperationException("Hedera mirror record has an invalid consensus timestamp.");
            var nativeResult = tx.GetProperty("result").GetString();
            var operation = tx.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(nativeResult) || string.IsNullOrWhiteSpace(operation))
                throw new InvalidOperationException("Hedera mirror record omits its operation or execution result.");
            long delta = 0;
            foreach (var transfer in tx.GetProperty("transfers").EnumerateArray())
                if (transfer.GetProperty("account").GetString() == accountId)
                    delta = checked(delta + transfer.GetProperty("amount").GetInt64());
            var seconds = decimal.Parse(timestamp, System.Globalization.CultureInfo.InvariantCulture);
            return new WalletTransaction
            {
                TransactionId = Guid.Empty, NativeTransactionId = id, NativeOperation = operation,
                NativeExecutionResult = nativeResult, NativeError = nativeResult == "SUCCESS" ? null : nativeResult,
                RawAmount = delta.ToString(System.Globalization.CultureInfo.InvariantCulture), AmountUnit = "tinybar",
                Amount = (double)((decimal)delta / 100000000m),
                CreatedDate = DateTime.UnixEpoch.AddTicks(checked((long)(seconds * TimeSpan.TicksPerSecond))),
                // A multi-party transaction plus fees cannot be represented as one invented sender/recipient pair.
                FromWalletAddress = null, ToWalletAddress = null,
                Description = $"Hedera {operation}: signed HBAR delta for {accountId}, including fees; {nativeResult}."
            };
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
                ArgumentNullException.ThrowIfNull(request);
                var asset = ParseHederaNftAddress(request.NFTTokenAddress);
                if (!string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("Recover an explicit Hedera signing key before burning an NFT.");
                if (request.BurntByAvatarId == Guid.Empty)
                    throw new ArgumentException("The burning avatar's real Hedera treasury wallet is required.");
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId,
                    Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError || walletResult.Result == null)
                    throw new InvalidOperationException("Could not retrieve the burning avatar's Hedera wallet.");
                var wallet = walletResult.Result;
                if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey)
                    && !string.Equals(request.OwnerPrivateKey, wallet.PrivateKey, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Supplied signing key does not match the avatar's treasury wallet.");
                var committed = await HederaSdkClient.InvokeAsync("nftBurn", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress,
                    recipient = wallet.WalletAddress, tokenId = asset.TokenId, serials = new[] { asset.Serial },
                    privateKey = wallet.PrivateKey,
                    publicKey = string.IsNullOrWhiteSpace(request.OwnerPublicKey) ? wallet.PublicKey : request.OwnerPublicKey,
                    keyType = "der", transactionId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress)
                });
                if (committed.GetProperty("status").GetString() != "SUCCESS")
                    throw new InvalidOperationException("HTS NFT burn did not return a successful consensus receipt.");
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                {
                    TransactionResult = committed.GetProperty("transactionId").GetString(),
                    Web3NFT = new Web3NFT { Id = request.Web3NFTId, NFTTokenAddress = request.NFTTokenAddress }
                };
                result.Message = "HTS NFT burn confirmed by consensus receipt.";
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

        private static (string TokenId, string Serial) ParseHederaNftAddress(string address)
        {
            var match = System.Text.RegularExpressions.Regex.Match(address ?? "",
                @"\A([0-9]+\.[0-9]+\.[0-9]+)/([1-9][0-9]*)\z");
            if (!match.Success || !long.TryParse(match.Groups[2].Value,
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
                throw new ArgumentException("A Hedera NFT address must identify token/serial, for example 0.0.1234/1; an OASIS GUID is not an HTS serial.");
            return (match.Groups[1].Value, match.Groups[2].Value);
        }


    }
}
