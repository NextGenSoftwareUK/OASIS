using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
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
        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var memo = Encoding.UTF8.GetBytes(memoText ?? string.Empty);
            return SendSignedTransactionAsync(fromWalletAddress, toWalletAddress, amount, memo, memoText, commitmentOnly: false);
        }

        private const string BitcoinTransactionIdMetadataKey = "BitcoinTransactionId";
        private const string BitcoinBroadcastOutcomeUnknown = "BITCOIN_BROADCAST_OUTCOME_UNKNOWN";

        private async Task<OASISResult<ITransactionResponse>> SendSignedTransactionAsync(
            string fromWalletAddress, string toWalletAddress, decimal amount, byte[] memo, string memoText, bool commitmentOnly,
            Func<uint256, Task> preparePublication = null)
        {
            var result = new OASISResult<ITransactionResponse>();
            uint256 submittedTransactionId = null;
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated.");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(_privateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "A Bitcoin wallet signing key is required.");
                    return result;
                }

                var satoshis = amount * 100_000_000m;
                if ((!commitmentOnly && satoshis <= 0) || satoshis > 2_100_000_000_000_000m || satoshis != decimal.Truncate(satoshis))
                {
                    OASISErrorHandling.HandleError(ref result, "The Bitcoin amount must be a positive whole number of satoshis within the supply limit.");
                    return result;
                }

                if (memo.Length > 80)
                {
                    OASISErrorHandling.HandleError(ref result, "A Bitcoin transaction data output may contain at most 80 bytes.");
                    return result;
                }

                var network = SigningNetwork;
                var source = BitcoinAddress.Create(fromWalletAddress, network);
                var destination = commitmentOnly ? null : BitcoinAddress.Create(toWalletAddress, network);
                var signer = new BitcoinSecret(_privateKey, network);
                var wrappedSegwit = source.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH));
                if (!wrappedSegwit && !source.Equals(signer.GetAddress(ScriptPubKeyType.Legacy)) &&
                    !source.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)))
                {
                    OASISErrorHandling.HandleError(ref result, "The source address must belong to the configured signing key.");
                    return result;
                }

                var genesis = (await _httpClient.GetStringAsync("block-height/0")).Trim();
                if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != network.GetGenesis().GetHash())
                {
                    OASISErrorHandling.HandleError(ref result, "Esplora is serving a different Bitcoin network than the signing wallet.");
                    return result;
                }

                var utxos = await _httpClient.GetFromJsonAsync<JsonElement[]>($"address/{source}/utxo");
                var tipText = await _httpClient.GetStringAsync("blocks/tip/height");
                var tip = long.Parse(tipText, System.Globalization.CultureInfo.InvariantCulture);
                var coins = new List<Coin>();
                foreach (var utxo in utxos)
                {
                    var txid = uint256.Parse(utxo.GetProperty("txid").GetString());
                    var index = utxo.GetProperty("vout").GetUInt32();
                    var value = Money.Satoshis(utxo.GetProperty("value").GetInt64());
                    var status = utxo.GetProperty("status");
                    var confirmed = status.GetProperty("confirmed").GetBoolean();

                    // Old confirmed outputs are mature regardless of whether they came from mining.
                    var mature = confirmed && tip - status.GetProperty("block_height").GetInt64() + 1 >= 100;
                    if (!mature)
                    {
                        var funding = await ReadVerifiedTransactionAsync(txid);
                        if (index >= funding.Outputs.Count ||
                            funding.Outputs[(int)index].ScriptPubKey != source.ScriptPubKey ||
                            funding.Outputs[(int)index].Value != value)
                            throw new InvalidOperationException("Esplora returned inconsistent funding output details.");

                        if (funding.IsCoinBase)
                            continue;
                    }

                    var outpoint = new OutPoint(txid, index);
                    var output = new TxOut(value, source.ScriptPubKey);
                    coins.Add(wrappedSegwit
                        ? new ScriptCoin(outpoint, output, signer.PrivateKey.PubKey.WitHash.ScriptPubKey)
                        : new Coin(outpoint, output));
                }

                decimal feeRate;
                if (_paymentFeeRateSatPerVByte is decimal configuredFeeRate)
                {
                    feeRate = configuredFeeRate;
                }
                else
                {
                    var estimates = await _httpClient.GetFromJsonAsync<Dictionary<string, decimal>>("fee-estimates");
                    if (!estimates.TryGetValue("6", out feeRate) || feeRate <= 0)
                    {
                        OASISErrorHandling.HandleError(ref result, "Esplora did not provide a fee estimate for six confirmations.");
                        return result;
                    }
                }

                var payment = Money.Satoshis((long)satoshis);
                var builder = network.CreateTransactionBuilder()
                    .AddCoins(coins.ToArray())
                    .AddKeys(signer.PrivateKey)
                    .SetChange(source)
                    .SendEstimatedFees(new FeeRate(Money.Satoshis((long)decimal.Ceiling(feeRate * 1000m))));
                if (destination != null)
                    builder.Send(destination, payment);

                Script memoScript = null;
                if (memo.Length > 0)
                {
                    memoScript = TxNullDataTemplate.Instance.GenerateScriptPubKey(memo);
                    // The builder applies a dust threshold to OP_RETURN. Final policy verification exempts it.
                    builder.DustPrevention = false;
                    builder.Send(memoScript, Money.Zero);
                }

                var transaction = builder.BuildTransaction(true);
                if ((destination != null && !transaction.Outputs.Any(output => output.ScriptPubKey == destination.ScriptPubKey && output.Value >= payment)) ||
                    (memoScript != null && !transaction.Outputs.Any(output => output.ScriptPubKey == memoScript && output.Value == Money.Zero)) ||
                    !builder.Verify(transaction))
                {
                    OASISErrorHandling.HandleError(ref result, "The signed transaction failed payment or policy validation.");
                    return result;
                }

                var expectedId = transaction.GetHash();
                using var body = new StringContent(transaction.ToHex(), Encoding.ASCII, "text/plain");
                if (preparePublication != null)
                    await preparePublication(expectedId);
                submittedTransactionId = expectedId;
                result.MetaData[BitcoinTransactionIdMetadataKey] = expectedId.ToString();
                using var response = await _httpClient.PostAsync("tx", body);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    {
                        result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
                        OASISErrorHandling.HandleError(ref result,
                            $"Esplora did not confirm Bitcoin transaction {expectedId}: {response.StatusCode}.");
                        return result;
                    }

                    var rejection = await response.Content.ReadAsStringAsync();
                    rejection = System.Text.RegularExpressions.Regex.Replace(rejection, "[a-fA-F0-9]{64,}", "[redacted]");
                    rejection = rejection.Replace('\r', ' ').Replace('\n', ' ').Trim();
                    if (rejection.Length > 512)
                        rejection = rejection[..512];
                    OASISErrorHandling.HandleError(ref result, $"Esplora rejected Bitcoin transaction {expectedId}: {response.StatusCode}. {rejection}");
                    return result;
                }

                var receipt = (await response.Content.ReadAsStringAsync()).Trim();
                if (!uint256.TryParse(receipt, out var transactionId) || transactionId != expectedId)
                {
                    result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
                    OASISErrorHandling.HandleError(ref result, $"Esplora did not acknowledge the expected Bitcoin transaction {expectedId}.");
                    return result;
                }

                result.Result = new BitcoinTransactionResponse
                {
                    TransactionResult = transactionId.ToString(),
                    MemoText = memoText
                };
            }
            catch (Exception ex)
            {
                if (submittedTransactionId != null)
                {
                    result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
                    OASISErrorHandling.HandleError(ref result,
                        $"Esplora did not confirm Bitcoin transaction {submittedTransactionId} ({ex.GetType().Name}).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Bitcoin transaction failed ({ex.GetType().Name}).");
            }

            return result;
        }

        // LoadAllAvatarDetailsAsync is already implemented above (around line 2240)

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                // Load all avatar details as separate objects by scanning blocks for OP_RETURN (same approach as LoadAllHolonsAsync)
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }
                var avatarDetails = new List<IAvatarDetail>();
                var seenIds = new HashSet<Guid>();
                var blockHeightRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getblockcount",
                    @params = new object[0]
                };
                var blockHeightContent = new StringContent(JsonSerializer.Serialize(blockHeightRequest), Encoding.UTF8, "application/json");
                var blockHeightResponse = await _httpClient.PostAsync("", blockHeightContent);
                if (blockHeightResponse.IsSuccessStatusCode)
                {
                    var blockHeightResult = await blockHeightResponse.Content.ReadAsStringAsync();
                    var blockHeightData = JsonSerializer.Deserialize<JsonElement>(blockHeightResult);
                    if (blockHeightData.TryGetProperty("result", out var currentHeight))
                    {
                        var startHeight = Math.Max(0, currentHeight.GetInt32() - 100);
                        for (int height = currentHeight.GetInt32(); height >= startHeight; height--)
                        {
                            var blockHashRequest = new
                            {
                                jsonrpc = "2.0",
                                id = 1,
                                method = "getblockhash",
                                @params = new object[] { height }
                            };
                            var blockHashContent = new StringContent(JsonSerializer.Serialize(blockHashRequest), Encoding.UTF8, "application/json");
                            var blockHashResponse = await _httpClient.PostAsync("", blockHashContent);
                            if (!blockHashResponse.IsSuccessStatusCode) continue;
                            var blockHashResult = await blockHashResponse.Content.ReadAsStringAsync();
                            var blockHashData = JsonSerializer.Deserialize<JsonElement>(blockHashResult);
                            if (!blockHashData.TryGetProperty("result", out var blockHash)) continue;
                            var blockRequest = new
                            {
                                jsonrpc = "2.0",
                                id = 1,
                                method = "getblock",
                                @params = new object[] { blockHash.GetString(), 2 }
                            };
                            var blockContent = new StringContent(JsonSerializer.Serialize(blockRequest), Encoding.UTF8, "application/json");
                            var blockResponse = await _httpClient.PostAsync("", blockContent);
                            if (!blockResponse.IsSuccessStatusCode) continue;
                            var blockResult = await blockResponse.Content.ReadAsStringAsync();
                            var blockData = JsonSerializer.Deserialize<JsonElement>(blockResult);
                            if (!blockData.TryGetProperty("result", out var block) || !block.TryGetProperty("tx", out var transactions)) continue;
                            foreach (var tx in transactions.EnumerateArray())
                            {
                                if (!tx.TryGetProperty("vout", out var vouts)) continue;
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (!vout.TryGetProperty("scriptPubKey", out var scriptPubKey) || !scriptPubKey.TryGetProperty("asm", out var asm)) continue;
                                    var asmString = asm.GetString();
                                    if (asmString == null || !asmString.StartsWith("OP_RETURN")) continue;
                                    try
                                    {
                                        var detailBytes = Convert.FromHexString(asmString.Substring("OP_RETURN ".Length));
                                        var detailJson = Encoding.UTF8.GetString(detailBytes);
                                        var avatarDetail = ParseBitcoinToAvatarDetail(detailJson);
                                        if (avatarDetail != null && seenIds.Add(avatarDetail.Id))
                                            avatarDetails.Add(avatarDetail);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Bitcoin blockchain (scanned last 100 blocks)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for holons with ParentHolonId in OP_RETURN transactions
                // Bitcoin doesn't natively support queries, so we search transactions containing the parent ID
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { parentId.ToString(), true, 0, 1000 }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    var holons = new List<IHolon>();
                    if (searchData.TryGetProperty("result", out var transactions))
                    {
                        foreach (var transaction in transactions.EnumerateArray())
                        {
                            if (transaction.TryGetProperty("vout", out var vouts))
                            {
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                        scriptPubKey.TryGetProperty("asm", out var asm))
                                    {
                                        var asmString = asm.GetString();
                                        if (asmString != null && asmString.StartsWith("OP_RETURN"))
                                        {
                                            try
                                            {
                                                var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                                var holonBytes = Convert.FromHexString(opReturnData);
                                                var holonJson = Encoding.UTF8.GetString(holonBytes);
                                                var holon = ParseBitcoinToHolon(holonJson);
                                                if (holon != null && holon.ParentHolonId == parentId &&
                                                    (holonType == HolonType.All || holon.HolonType == holonType))
                                                {
                                                    holons.Add(holon);
                                                }
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Loaded {holons.Count} holons for parent from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!uint256.TryParse(providerKey, out var transactionId))
                {
                    OASISErrorHandling.HandleError(ref result, "A Bitcoin transaction ID is required.");
                    return result;
                }

                var transaction = await ReadVerifiedTransactionAsync(transactionId);

                Holon holon = null;
                foreach (var output in transaction.Outputs)
                {
                    var pushes = new TxNullDataTemplate(output.ScriptPubKey.Length).ExtractScriptPubKeyParameters(output.ScriptPubKey);
                    if (pushes?.Length != 1)
                        continue;

                    var decoded = BitcoinRecordBlobStore.IsCommitment(pushes[0])
                        ? await ReadEncryptedHolonAsync(pushes[0])
                        : ParseBitcoinToHolon(Encoding.UTF8.GetString(pushes[0]));
                    if (decoded == null)
                        continue;

                    if (holon != null)
                    {
                        OASISErrorHandling.HandleError(ref result, "The Bitcoin transaction contains multiple holon records.");
                        return result;
                    }
                    holon = decoded;
                }

                if (holon == null || (version > 0 && holon.Version != version))
                {
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    OASISErrorHandling.HandleError(ref result, "The Bitcoin transaction does not contain the requested holon version.");
                    return result;
                }

                if (loadChildren && (recursive || maxChildDepth > 0))
                {
                    var children = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, continueOnErrorRecursive, version);
                    if (children.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, children.Message, children.Exception);
                        return result;
                    }

                    holon.Children = children.Result.ToList();
                }

                holon.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = transactionId.ToString();
                result.Result = holon;
            }
            catch (BitcoinTransactionNotFoundException ex)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                result.Result = await ReadWalletAvatarsAsync(version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Bitcoin avatar listing failed ({ex.GetType().Name}).");
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }


    }
}
