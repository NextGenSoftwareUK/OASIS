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
        /// <summary>
        /// Create Holon from Bitcoin response when JSON deserialization fails
        /// </summary>
        private Holon CreateHolonFromBitcoin(string bitcoinJson)
        {
            try
            {
                // Extract basic information from Bitcoin JSON response
                var holon = new Holon
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{ExtractBitcoinProperty(bitcoinJson, "name") ?? "bitcoin_holon"}"),
                    Name = ExtractBitcoinProperty(bitcoinJson, "name") ?? "Bitcoin Holon",
                    Description = ExtractBitcoinProperty(bitcoinJson, "description") ?? "",
                    HolonType = HolonType.Holon,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                return holon;
            }
            catch (Exception)
            {
                return null;
            }
        }



        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Convert decimal amount to satoshis (1 BTC = 100,000,000 satoshis)
                var amountInSatoshis = (long)(amount * 100000000);

                // Create Bitcoin transaction using Blockstream API
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = toWalletAddress,
                            value = amountInSatoshis
                        }
                    }
                };

                // First, get UTXOs for the from address
                var utxoResponse = await _httpClient.GetAsync($"/address/{fromWalletAddress}/utxo");
                if (!utxoResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get UTXOs for address {fromWalletAddress}: {utxoResponse.StatusCode}");
                    return result;
                }

                var utxoContent = await utxoResponse.Content.ReadAsStringAsync();
                var utxos = JsonSerializer.Deserialize<JsonElement[]>(utxoContent);

                if (utxos == null || utxos.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No UTXOs found for address {fromWalletAddress}");
                    return result;
                }

                // Find sufficient UTXOs
                long totalValue = 0;
                var selectedUtxos = new List<object>();

                foreach (var utxo in utxos)
                {
                    var value = utxo.GetProperty("value").GetInt64();
                    totalValue += value;
                    selectedUtxos.Add(new
                    {
                        txid = utxo.GetProperty("txid").GetString(),
                        vout = utxo.GetProperty("vout").GetInt32()
                    });

                    if (totalValue >= amountInSatoshis)
                        break;
                }

                if (totalValue < amountInSatoshis)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient funds. Available: {totalValue} satoshis, Required: {amountInSatoshis} satoshis");
                    return result;
                }

                // Create transaction with selected UTXOs
                var finalTransaction = new
                {
                    inputs = selectedUtxos,
                    outputs = new[]
                    {
                        new
                        {
                            address = toWalletAddress,
                            value = amountInSatoshis
                        }
                    }
                };

                // Broadcast transaction
                var jsonContent = JsonSerializer.Serialize(finalTransaction);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var broadcastResponse = await _httpClient.PostAsync("/tx", content);
                if (broadcastResponse.IsSuccessStatusCode)
                {
                    var responseContent = await broadcastResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BitcoinTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("txid").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Bitcoin transaction sent successfully. TXID: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to broadcast Bitcoin transaction: {broadcastResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Bitcoin transaction: {ex.Message}");
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
                                                var holonData = JsonSerializer.Deserialize<JsonElement>(holonJson);
                                                
                                                // Check if this holon has the matching parent ID
                                                if (holonData.TryGetProperty("parent_holon_id", out var parentIdProp) &&
                                                    parentIdProp.GetString() == parentId.ToString())
                                                {
                                                    var holon = ParseBitcoinToHolon(holonJson);
                                                    if (holon != null && (holonType == HolonType.All || holon.HolonType == holonType))
                                                    {
                                                        holons.Add(holon);
                                                    }
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

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key cannot be null or empty");
                    return result;
                }

                // Search for holon by provider key (transaction hash) in OP_RETURN transactions
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getrawtransaction",
                    @params = new object[] { providerKey, true }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    if (searchData.TryGetProperty("result", out var transaction))
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
                                            
                                            if (holon != null)
                                            {
                                                // Load children if requested
                                                if (loadChildren && (recursive || maxChildDepth > 0))
                                                {
                                                    var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, continueOnErrorRecursive, version);
                                                    if (!childrenResult.IsError && childrenResult.Result != null)
                                                    {
                                                        holon.Children = childrenResult.Result.ToList();
                                                    }
                                                }
                                                
                                                result.Result = holon;
                                                result.IsError = false;
                                                result.Message = "Holon loaded from Bitcoin blockchain successfully";
                                                return result;
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
                    
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Bitcoin: {searchResponse.StatusCode}");
                }
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Bitcoin does not have a native index of avatars; return empty set (avatars are discovered via LoadAvatar by providerKey/email/username)
                result.Result = new List<IAvatar>();
                result.IsError = false;
                result.Message = "Bitcoin provider does not support LoadAllAvatars; returning empty list.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }


    }
}
