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
                var actor = request.MintedByAvatarId;
                var amount = request.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var memo = request.MemoText;
                var hasRecipient = request.MetaData.TryGetValue("MintToWalletAddress", out var requestedRecipient);
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(actor,
                    Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,ITransactionResponse>(walletResult);
                if (walletResult.Result == null || walletResult.Result.AvatarId != actor)
                    throw new InvalidOperationException("Exact minting avatar wallet is required.");
                
                var wallet = walletResult.Result;
                if (string.IsNullOrWhiteSpace(wallet.PrivateKey))
                    throw new ArgumentException("The minting wallet has no signing key.");
                var recipient = hasRecipient ? requestedRecipient : wallet.WalletAddress;
                var committed = await HederaSdkClient.InvokeAsync("tokenMint", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress, recipient,
                    tokenId = tokenAddress, amount,
                    privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der", memo,
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
                if (!string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("Recover an explicit Hedera signing key before burning a token.");
                if (request.BurntByAvatarId == Guid.Empty)
                    throw new ArgumentException("The burning avatar's real treasury wallet is required.");
                var actor = request.BurntByAvatarId;
                var tokenId = request.TokenAddress;
                var amount = request.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var suppliedKey = request.OwnerPrivateKey;
                var publicKey = request.OwnerPublicKey;
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(actor,
                    Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,ITransactionResponse>(walletResult);
                if (walletResult.Result == null || walletResult.Result.AvatarId != actor)
                    throw new InvalidOperationException("Exact burning avatar wallet is required.");
                var wallet = walletResult.Result;
                if (!string.IsNullOrWhiteSpace(suppliedKey)
                    && !string.Equals(suppliedKey, wallet.PrivateKey, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Supplied signing key does not match the burning avatar's Hedera wallet.");
                var committed = await HederaSdkClient.InvokeAsync("tokenBurn", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress,
                    recipient = wallet.WalletAddress, tokenId, amount,
                    privateKey = wallet.PrivateKey, publicKey = string.IsNullOrWhiteSpace(publicKey) ? wallet.PublicKey : publicKey, keyType = "der",
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
            return await ExecuteAvatarTokenEscrowAsync("tokenEscrowLock", request?.LockedByAvatarId ?? Guid.Empty,
                request?.TokenAddress, request?.FromWalletAddress, request?.FromWalletPrivateKey,
                request?.Amount ?? 0, request?.LockId ?? Guid.Empty, request?.EscrowContractAddress);
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            return await ExecuteAvatarTokenEscrowAsync("tokenEscrowUnlock", request?.UnlockedByAvatarId ?? Guid.Empty,
                request?.TokenAddress, null, null, 0, request?.LockId ?? Guid.Empty, request?.EscrowContractAddress);
        }

        /// <summary>Separate funded association mutation; never implicitly runs during deposit.</summary>
        public Task<OASISResult<ITransactionResponse>> AssociateTokenWithEscrowAsync(Guid avatarId,string tokenAddress,string escrowContractId)
            =>ExecuteAvatarTokenEscrowAsync("tokenEscrowAssociate",avatarId,tokenAddress,null,null,0,Guid.Empty,escrowContractId);

        public OASISResult<ITransactionResponse> AssociateTokenWithEscrow(Guid avatarId,string tokenAddress,string escrowContractId)
            =>AssociateTokenWithEscrowAsync(avatarId,tokenAddress,escrowContractId).GetAwaiter().GetResult();

        private async Task<OASISResult<ITransactionResponse>> ExecuteAvatarTokenEscrowAsync(string action,
            Guid avatarId, string tokenAddress, string requestedWallet, string requestedKey,
            decimal requestedAmount, Guid requestedLockId, string requestedEscrow)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (_tokenEscrow == null) throw new InvalidOperationException("An explicit Hedera fungible escrow contract configuration is required.");
                var intent=action=="tokenEscrowAssociate"
                    ?HederaTokenEscrowIntent.CaptureAssociation(_tokenEscrow,tokenAddress,requestedEscrow)
                    :HederaTokenEscrowIntent.Capture(_tokenEscrow,action=="tokenEscrowLock",requestedLockId,requestedAmount,requestedEscrow);
                if (avatarId == Guid.Empty) throw new ArgumentException("An explicit requesting avatar identity is required for token escrow.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(tokenAddress ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new ArgumentException("Token escrow requires a nonzero numeric HTS token address.");
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if(walletResult.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,ITransactionResponse>(walletResult);
                if (walletResult.Result == null || walletResult.Result.AvatarId!=avatarId || string.IsNullOrWhiteSpace(walletResult.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the requesting avatar's real Hedera signing wallet.");
                var wallet = walletResult.Result;
                if ((!string.IsNullOrEmpty(requestedWallet) && requestedWallet != wallet.WalletAddress)
                    || (!string.IsNullOrEmpty(requestedKey) && requestedKey != wallet.PrivateKey))
                    throw new ArgumentException("Requested signing wallet does not match the requesting avatar's wallet.");
                var mutationId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var response = await _httpClient.GetAsync("accounts/" + wallet.WalletAddress, timeout.Token);
                response.EnsureSuccessStatusCode();
                using var accountDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var account = accountDocument.RootElement;
                if (account.GetProperty("account").GetString() != wallet.WalletAddress
                    || account.GetProperty("deleted").GetBoolean())
                    throw new InvalidOperationException("Mirror account identity does not match the requesting wallet.");
                var evmAddress = account.GetProperty("evm_address").GetString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(evmAddress ?? "", @"\A0x[0-9a-fA-F]{40}\z"))
                    throw new InvalidOperationException("Mirror account does not expose a valid EVM address.");
                var payload = new Dictionary<string,object>
                {
                    ["network"] = _hederaNetwork, ["nodes"] = _consensusNodes, ["sender"] = wallet.WalletAddress,
                    ["senderEvmAddress"] = evmAddress, ["privateKey"] = wallet.PrivateKey, ["publicKey"] = wallet.PublicKey, ["keyType"] = "der",
                    ["tokenId"] = tokenAddress, ["contractId"] = _tokenEscrow.ContractId, ["runtimeCodeHash"] = _tokenEscrow.RuntimeCodeHash,
                    ["gas"] = _tokenEscrow.GasLimit, ["maxFeeTinybars"] = _tokenEscrow.MaxFeeTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["transactionId"] = mutationId, ["approvalTransactionId"] = action == "tokenEscrowLock" ? HederaSdkClient.CreateTransactionId(wallet.WalletAddress) : null
                };
                intent.AppendTo(payload);
                var committed=await HederaSdkClient.InvokeAsync(action,payload);
                var approval=committed.TryGetProperty("approvalTransactionId",out var approvalValue)&&approvalValue.ValueKind==JsonValueKind.String?approvalValue.GetString():null;
                try {intent.ValidateReply(committed);}
                catch(Exception error){throw new HederaNativeMutationReconciliationException(
                    "Escrow reply does not establish original deposit effects; reconcile before retrying.",
                    new(mutationId,approval,mutationId,"provider-readback",null),error);}
                result.Result = intent.DepositId==null
                    ?new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = mutationId }
                    :new HederaIdBoundEscrowResponse(intent.LockId,mutationId,approval);
                result.IsSaved=true;result.SavedCount=1;
                result.Message = intent.AssociationTokenId!=null?"Hedera escrow token association confirmed by receipt and readback."
                    :"Hedera token escrow confirmed by consensus receipt and exact balance/deposit readback.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph token escrow: {ex.Message}", ex);
                if(ex is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
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
                ArgumentNullException.ThrowIfNull(request);
                result.Result = (double)((await ReadMirrorBalanceTinybarsAsync(request.WalletAddress, default)) / 100000000m);
                result.Message = "Native HBAR balance read from the supported Hedera mirror API.";
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

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountId, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                result.Result = (await ReadMirrorBalanceTinybarsAsync(accountId, token)) / 100000000m;
                result.Message = "Exact native HBAR balance read from the supported Hedera mirror API.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, "Hedera mirror balance failed: " + ex.Message, ex); }
            return result;
        }

        private async Task<long> ReadMirrorBalanceTinybarsAsync(string accountId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!System.Text.RegularExpressions.Regex.IsMatch(accountId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                throw new ArgumentException("A nonzero numeric Hedera account ID is required for mirror balance.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var response = await _httpClient.GetAsync("accounts/" + accountId, timeout.Token);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            var account = document.RootElement;
            if (account.GetProperty("account").GetString() != accountId || account.GetProperty("deleted").GetBoolean()
                || !account.GetProperty("balance").GetProperty("balance").TryGetInt64(out var balance) || balance < 0)
                throw new InvalidOperationException("Mirror response does not establish the requested active account's exact HBAR balance.");
            return balance;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var accountId = request.WalletAddress;
                var fromBlock=request.FromBlock;var toBlock=request.ToBlock;
                if(fromBlock.HasValue&&toBlock.HasValue&&fromBlock>toBlock)
                    throw new ArgumentOutOfRangeException(nameof(request),"Hedera mirror block range is reversed.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(accountId ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                    throw new ArgumentException("A numeric Hedera wallet account ID is required.");
                var endpoint = _httpClient.BaseAddress;
                var cutoffTicks = DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;
                var cutoff = (cutoffTicks / TimeSpan.TicksPerSecond).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "." + ((cutoffTicks % TimeSpan.TicksPerSecond) * 100).ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var range=await ResolveMirrorHistoryRangeAsync(endpoint,fromBlock,toBlock,decimal.Parse(cutoff,System.Globalization.CultureInfo.InvariantCulture),timeout.Token);
                if(range.Empty){result.Result=new List<IWalletTransaction>();result.Message="Requested range is beyond the latest indexed mirror block.";return result;}
                var upperBound = "timestamp=lte:" + range.Maximum.ToString("0.000000000",System.Globalization.CultureInfo.InvariantCulture);
                var lowerBound=range.Minimum==0?"0":range.Minimum.ToString("0.000000000",System.Globalization.CultureInfo.InvariantCulture);
                var next = new Uri(endpoint, $"transactions?account.id={accountId}&limit=100&order=asc&timestamp=gte:{lowerBound}&{upperBound}");
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var identities = new HashSet<string>(StringComparer.Ordinal);
                var transactions = new List<IWalletTransaction>();
                  decimal? previousConsensusTimestamp = null;
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
                          if (consensusTimestamp > range.Maximum||consensusTimestamp<range.Minimum)
                            throw new InvalidOperationException("Hedera history record exceeds the fixed scan timestamp or is outside the requested block range.");
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
                        next=ConstrainMirrorHistoryPage(next,range.Minimum,upperBound);
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
            var seconds=ParseMirrorHistoryTimestamp(timestamp);
            var nativeResult = tx.GetProperty("result").GetString();
            var operation = tx.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(nativeResult) || string.IsNullOrWhiteSpace(operation))
                throw new InvalidOperationException("Hedera mirror record omits its operation or execution result.");
            long delta = 0;
            foreach (var transfer in tx.GetProperty("transfers").EnumerateArray())
                if (transfer.GetProperty("account").GetString() == accountId)
                    delta = checked(delta + transfer.GetProperty("amount").GetInt64());
            var memo=Core.Helpers.NativeWalletMemo.FromBase64(tx.TryGetProperty("memo_base64",out var encodedMemo)?encodedMemo.GetString():null);
            return new WalletTransaction
            {
                TransactionId = Guid.Empty, NativeTransactionId = id, NativeOperation = operation,
                Token="HBAR",ProviderType=Core.Enums.ProviderType.HashgraphOASIS,MemoText=memo.Text,MemoBase64=memo.Base64,
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
