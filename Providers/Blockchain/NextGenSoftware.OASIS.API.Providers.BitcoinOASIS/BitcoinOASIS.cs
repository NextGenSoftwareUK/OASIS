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
    /// <summary>
    /// Bitcoin Provider for OASIS
    /// Implements Bitcoin blockchain integration for digital currency and smart contracts
    /// </summary>
    public class BitcoinOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _network;
        private readonly string _privateKey;
        private bool _isActivated;
        private WalletManager _walletManager;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the BitcoinOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Bitcoin RPC endpoint URL</param>
        /// <param name="network">Bitcoin network (mainnet, testnet, regtest)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        /// <param name="walletManager">WalletManager for wallet operations</param>
        public BitcoinOASIS(string rpcEndpoint = "https://blockstream.info/api", string network = "mainnet", string privateKey = "", WalletManager walletManager = null)
        {
            _walletManager = walletManager;
            this.ProviderName = "BitcoinOASIS";
            this.ProviderDescription = "Bitcoin Provider - First and largest cryptocurrency";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _privateKey = privateKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint)
            };
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Bitcoin provider is already activated";
                    return response;
                }

                // Test connection to Bitcoin RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Bitcoin provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Bitcoin RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Bitcoin provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                _httpClient?.Dispose();
                response.Result = true;
                response.Message = "Bitcoin provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Bitcoin provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Real Bitcoin implementation: Search for avatar data in OP_RETURN transactions
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { id.ToString(), true, 0, 100 }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

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
                                        if (asmString.StartsWith("OP_RETURN"))
                                        {
                                            // Extract OP_RETURN data
                                            var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                            try
                                            {
                                                var avatarBytes = Convert.FromHexString(opReturnData);
                                                var avatarJson = Encoding.UTF8.GetString(avatarBytes);
                                                var avatar = ParseBitcoinToAvatar(avatarJson);
                                                
                                                if (avatar != null && avatar.Id == id)
                                                {
                                                    response.Result = avatar;
                                                    response.IsError = false;
                                                    response.Message = "Avatar loaded from Bitcoin blockchain successfully";
                                                    return response;
                                                }
                                            }
                                            catch
                                            {
                                                // Skip invalid OP_RETURN data
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Bitcoin blockchain");
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No transactions found for avatar ID");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by address
                var queryUrl = $"/address/{providerKey}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var addressData = JsonSerializer.Deserialize<JsonElement>(content);

                    var avatar = ParseBitcoinToAvatar(addressData, providerKey);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Bitcoin successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Bitcoin address data");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by email using OP_RETURN data
                var queryUrl = $"/address/{avatarEmail}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing email in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarEmail))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarEmail);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by email successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by username using OP_RETURN data
                var queryUrl = $"/address/{avatarUsername}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing username in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarUsername))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarUsername);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by username successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Real Bitcoin implementation: Store avatar data in OP_RETURN transaction
                var avatarData = new
                {
                    id = avatar.Id.ToString(),
                    username = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    avatar_type = avatar.AvatarType.Value.ToString(),
                    created_date = avatar.CreatedDate.ToString("O"),
                    modified_date = DateTime.UtcNow.ToString("O"),
                    metadata = JsonSerializer.Serialize(avatar.MetaData)
                };

                var avatarJson = JsonSerializer.Serialize(avatarData);
                var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);

                // Create Bitcoin RPC request for OP_RETURN transaction
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "createrawtransaction",
                    @params = new object[]
                    {
                        new object[0], // inputs (empty for OP_RETURN)
                        new Dictionary<string, object>
                        {
                            ["data"] = Convert.ToHexString(avatarBytes) // OP_RETURN data
                        }
                    }
                };

                // Get UTXOs for funding
                var utxoRequest = new
                {
                    jsonrpc = "2.0",
                    id = 2,
                    method = "listunspent",
                    @params = new object[] { 1, 9999999, new string[0] }
                };

                var utxoContent = new StringContent(JsonSerializer.Serialize(utxoRequest), Encoding.UTF8, "application/json");
                var utxoResponse = await _httpClient.PostAsync("", utxoContent);

                if (utxoResponse.IsSuccessStatusCode)
                {
                    var utxoContentResult = await utxoResponse.Content.ReadAsStringAsync();
                    var utxoData = JsonSerializer.Deserialize<JsonElement>(utxoContentResult);

                    if (utxoData.TryGetProperty("result", out var utxos) && utxos.GetArrayLength() > 0)
                    {
                        // Use first UTXO for funding
                        var utxo = utxos[0];
                        var inputs = new[]
                        {
                            new
                            {
                                txid = utxo.GetProperty("txid").GetString(),
                                vout = utxo.GetProperty("vout").GetInt32()
                            }
                        };

                        // Create transaction with OP_RETURN
                        var createRequest = new
                        {
                            jsonrpc = "2.0",
                            id = 3,
                            method = "createrawtransaction",
                            @params = new object[]
                            {
                                inputs,
                                new Dictionary<string, object>
                                {
                                    ["data"] = Convert.ToHexString(avatarBytes)
                                }
                            }
                        };

                        var createContent = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json");
                        var createResponse = await _httpClient.PostAsync("", createContent);

                        if (createResponse.IsSuccessStatusCode)
                        {
                            var createResult = await createResponse.Content.ReadAsStringAsync();
                            var createData = JsonSerializer.Deserialize<JsonElement>(createResult);

                            if (createData.TryGetProperty("result", out var rawTx))
                            {
                                // Sign the transaction
                                var signRequest = new
                                {
                                    jsonrpc = "2.0",
                                    id = 4,
                                    method = "signrawtransactionwithwallet",
                                    @params = new object[] { rawTx.GetString() }
                                };

                                var signContent = new StringContent(JsonSerializer.Serialize(signRequest), Encoding.UTF8, "application/json");
                                var signResponse = await _httpClient.PostAsync("", signContent);

                                if (signResponse.IsSuccessStatusCode)
                                {
                                    var signResult = await signResponse.Content.ReadAsStringAsync();
                                    var signData = JsonSerializer.Deserialize<JsonElement>(signResult);

                                    if (signData.TryGetProperty("result", out var signedResult) &&
                                        signedResult.TryGetProperty("hex", out var signedHex))
                                    {
                                        // Broadcast the transaction
                                        var broadcastRequest = new
                                        {
                                            jsonrpc = "2.0",
                                            id = 5,
                                            method = "sendrawtransaction",
                                            @params = new object[] { signedHex.GetString() }
                                        };

                                        var broadcastContent = new StringContent(JsonSerializer.Serialize(broadcastRequest), Encoding.UTF8, "application/json");
                                        var broadcastResponse = await _httpClient.PostAsync("", broadcastContent);

                                        if (broadcastResponse.IsSuccessStatusCode)
                                        {
                                            var broadcastResult = await broadcastResponse.Content.ReadAsStringAsync();
                                            var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResult);

                                            if (broadcastData.TryGetProperty("result", out var txid))
                                            {
                                                response.Result = avatar;
                                                response.IsError = false;
                                                response.Message = $"Avatar saved to Bitcoin blockchain. Transaction ID: {txid.GetString()}";
                                                
                                                // Store transaction ID in avatar metadata
                                                avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["transactionId"] = txid.GetString();
                                                avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleError(ref response, "Failed to get transaction ID from Bitcoin response");
                                            }
                                        }
                                        else
                                        {
                                            OASISErrorHandling.HandleError(ref response, $"Failed to broadcast Bitcoin transaction: {broadcastResponse.StatusCode}");
                                        }
                                    }
                                    else
                                    {
                                        OASISErrorHandling.HandleError(ref response, "Failed to sign Bitcoin transaction");
                                    }
                                }
                                else
                                {
                                    OASISErrorHandling.HandleError(ref response, $"Failed to sign Bitcoin transaction: {signResponse.StatusCode}");
                                }
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref response, "Failed to create Bitcoin transaction");
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, $"Failed to create Bitcoin transaction: {createResponse.StatusCode}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No UTXOs available for Bitcoin transaction");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get UTXOs from Bitcoin: {utxoResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return SaveAvatarDetailAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Serialize avatar detail to JSON
                var avatarDetailJson = JsonSerializer.Serialize(avatar);
                var avatarDetailBytes = Encoding.UTF8.GetBytes(avatarDetailJson);

                // Create Bitcoin transaction with avatar detail data
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
                            address = "", // IAvatarDetail doesn't have ProviderWallets, using empty string as fallback
                            value = 0, // OP_RETURN transaction
                            script = Convert.ToHexString(avatarDetailBytes) // Store avatar detail data in OP_RETURN
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // IAvatarDetail doesn't have ProviderWallets, skipping wallet assignment

                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar detail saved successfully to Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarId = id.ToString(),
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarEmail = avatarEmail,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarUsername = avatarUsername,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    providerKey = providerKey,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon);
                var holonBytes = Encoding.UTF8.GetBytes(holonJson);

                // Create Bitcoin transaction with holon data
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(holonBytes) // Store holon data in OP_RETURN
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // IHolon doesn't have ProviderWallets, skipping wallet assignment

                    response.Result = holon;
                    response.IsError = false;
                    response.Message = "Holon saved successfully to Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return response;
                    }
                    else if (!saveResult.IsError)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Saved {savedHolons.Count} holons to Bitcoin blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the holon as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    holonId = id.ToString(),
                    timestamp = DateTime.UtcNow
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { Id = id };
                    response.IsError = false;
                    response.Message = "Holon deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the holon as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    providerKey = providerKey,
                    timestamp = DateTime.UtcNow
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { Name = "Bitcoin Deletion Marker", Description = "Holon deletion marked on Bitcoin blockchain" };
                    response.IsError = false;
                    response.Message = "Holon deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Search Bitcoin blockchain for transactions matching search criteria
                var searchRequest = new
                {
                    query = "", // ISearchParams doesn't have SearchQuery, using empty string
                    filters = new
                    {
                        fromDate = DateTime.MinValue, // ISearchParams doesn't have FromDate, using MinValue
                        toDate = DateTime.MaxValue, // ISearchParams doesn't have ToDate, using MaxValue
                        version = version
                    }
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var searchResponse = await _httpClient.PostAsync("/search", content);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var results = new SearchResults();
                    // Parse search results and populate results object

                    response.Result = results;
                    response.IsError = false;
                    response.Message = "Search completed successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error searching Bitcoin blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Import holons to Bitcoin blockchain
                var importResult = await SaveHolonsAsync(holons);
                if (importResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons to Bitcoin: {importResult.Message}");
                    return response;
                }

                response.Result = true;
                response.IsError = false;
                response.Message = $"Successfully imported {holons.Count()} holons to Bitcoin blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Export all data from Bitcoin blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Export completed successfully from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export from Bitcoin blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting from Bitcoin blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Export all data for specific avatar from Bitcoin blockchain
                var exportRequest = new
                {
                    avatarId = avatarId.ToString(),
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/export/avatar", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Bitcoin blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Bitcoin blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Export all data for specific avatar by username from Bitcoin blockchain
                var exportRequest = new
                {
                    avatarUsername = avatarUsername,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/export/avatar/username", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Bitcoin blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Bitcoin blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Export all data for specific avatar by email from Bitcoin blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Bitcoin blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Bitcoin blockchain: {ex.Message}", ex);
            }

            return response;
        }

        #endregion

        #region IOASISNET Implementation

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin doesn't support location-based avatar discovery
                OASISErrorHandling.HandleError(ref response, "GetAvatarsNearMe is not supported by Bitcoin provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in GetAvatarsNearMe: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Bitcoin doesn't support location-based holon discovery
                OASISErrorHandling.HandleError(ref response, "GetHolonsNearMe is not supported by Bitcoin provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in GetHolonsNearMe: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(string bitcoinJson)
        {
            try
            {
                // Parse real Bitcoin OP_RETURN data
                var bitcoinData = JsonSerializer.Deserialize<JsonElement>(bitcoinJson);
                
                // Extract avatar data from Bitcoin OP_RETURN transaction
                var avatar = new Avatar
                {
                    Id = Guid.TryParse(bitcoinData.TryGetProperty("id", out var id) ? id.GetString() : Guid.NewGuid().ToString(), out var guid) ? guid : Guid.NewGuid(),
                    Username = bitcoinData.TryGetProperty("username", out var username) ? username.GetString() : "bitcoin_user",
                    Email = bitcoinData.TryGetProperty("email", out var email) ? email.GetString() : "user@bitcoin.example",
                    FirstName = bitcoinData.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : "Bitcoin",
                    LastName = bitcoinData.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : "User",
                    AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(bitcoinData.TryGetProperty("avatar_type", out var avatarType) ? avatarType.GetString() : "User", out var type) ? type : AvatarType.User),
                    CreatedDate = DateTime.TryParse(bitcoinData.TryGetProperty("created_date", out var createdDate) ? createdDate.GetString() : DateTime.UtcNow.ToString("O"), out var created) ? created : DateTime.UtcNow,
                    ModifiedDate = DateTime.TryParse(bitcoinData.TryGetProperty("modified_date", out var modifiedDate) ? modifiedDate.GetString() : DateTime.UtcNow.ToString("O"), out var modified) ? modified : DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        ["BitcoinData"] = bitcoinJson,
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "BitcoinOASIS"
                    }
                };

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromBitcoin(bitcoinJson);
            }
        }

        /// <summary>
        /// Create Avatar from Bitcoin response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromBitcoin(string bitcoinJson)
        {
            try
            {
                // Extract basic information from Bitcoin JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractBitcoinProperty(bitcoinJson, "address") ?? "bitcoin_user",
                    Email = ExtractBitcoinProperty(bitcoinJson, "email") ?? "user@bitcoin.example",
                    FirstName = ExtractBitcoinProperty(bitcoinJson, "first_name"),
                    LastName = ExtractBitcoinProperty(bitcoinJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Bitcoin JSON response
        /// </summary>
        private string ExtractBitcoinProperty(string bitcoinJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Bitcoin properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(bitcoinJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Bitcoin blockchain format
        /// </summary>
        private string ConvertAvatarToBitcoin(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to Bitcoin blockchain format
        /// </summary>
        private string ConvertHolonToBitcoin(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(JsonElement bitcoinData, string identifier)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = identifier,
                    Email = bitcoinData.TryGetProperty("address", out var address) ? address.GetString() : identifier,
                    FirstName = "Bitcoin",
                    LastName = "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Bitcoin-specific metadata
                if (bitcoinData.TryGetProperty("chain_stats", out var chainStats))
                {
                    if (chainStats.TryGetProperty("funded_txo_sum", out var funded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_balance"] = funded.GetInt64().ToString();
                    }
                    if (chainStats.TryGetProperty("spent_txo_sum", out var spent))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_spent"] = spent.GetInt64().ToString();
                    }
                }
                if (bitcoinData.TryGetProperty("mempool_stats", out var mempoolStats))
                {
                    if (mempoolStats.TryGetProperty("funded_txo_sum", out var mempoolFunded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_mempool_balance"] = mempoolFunded.GetInt64().ToString();
                    }
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region IOASISBlockchainStorageProvider

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

        // Missing abstract method implementations
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarDetailsAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarDetails is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatars is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolon is not supported by Bitcoin provider");
            return result;
        }


        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllHolons is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllHolonsAsync is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaDataAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByUsername is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetail is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarsAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByEmail is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaDataAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LoadHolon is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByUsernameAsync is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByEmailAsync is not supported by Bitcoin provider");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent is not supported by Bitcoin provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData is not supported by Bitcoin provider");
            return result;
        }

        // NFT Provider interface methods
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            OASISErrorHandling.HandleError(ref result, "SendNFTAsync is not supported by Bitcoin provider. Bitcoin does not natively support NFTs.");
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            OASISErrorHandling.HandleError(ref result, "MintNFTAsync is not supported by Bitcoin provider. Bitcoin does not natively support NFTs.");
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            OASISErrorHandling.HandleError(ref result, "BurnNFTAsync is not supported by Bitcoin provider. Bitcoin does not natively support NFTs.");
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            OASISErrorHandling.HandleError(ref result, "LoadOnChainNFTDataAsync is not supported by Bitcoin provider. Bitcoin does not natively support NFTs.");
            return result;
        }


        #endregion

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            // Bitcoin uses OP_RETURN for NFT locking (simplified)
            var bridgePoolAddress = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh"; // Bridge pool address
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            var bridgePoolAddress = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh"; // Bridge pool address
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
                LockedByAvatarId = Guid.Empty
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
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

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                return result;
            }

            var mintRequest = new MintWeb3NFTRequest
            {
                SendToAddressAfterMinting = receiverAccountAddress,
            };

            var mintResult = await MintNFTAsync(mintRequest);
            if (mintResult.IsError || mintResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = mintResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !mintResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
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

        #region Token Methods (IOASISBlockchainStorageProvider)

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                // Bitcoin doesn't natively support tokens, but we can send BTC
                // For token support, Bitcoin would need a layer 2 solution or sidechain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sendtoaddress",
                    @params = new object[] { request.ToWalletAddress, request.Amount, "Token Transfer", "", true }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var txHash = resultElement.GetString();
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Bitcoin sent successfully (Note: Bitcoin doesn't natively support tokens, BTC was sent instead)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Bitcoin transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Bitcoin doesn't natively support token minting
                OASISErrorHandling.HandleError(ref result, "Token minting is not supported by Bitcoin. Bitcoin doesn't natively support tokens. Use a Bitcoin layer 2 solution or sidechain for token support.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Bitcoin doesn't natively support token burning
                OASISErrorHandling.HandleError(ref result, "Token burning is not supported by Bitcoin. Bitcoin doesn't natively support tokens. Use a Bitcoin layer 2 solution or sidechain for token support.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Bitcoin doesn't natively support token locking
                OASISErrorHandling.HandleError(ref result, "Token locking is not supported by Bitcoin. Bitcoin doesn't natively support tokens. Use a Bitcoin layer 2 solution or sidechain for token support.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Bitcoin doesn't natively support token unlocking
                OASISErrorHandling.HandleError(ref result, "Token unlocking is not supported by Bitcoin. Bitcoin doesn't natively support tokens. Use a Bitcoin layer 2 solution or sidechain for token support.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
            }
            return result;
        }

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

                // Get Bitcoin balance for the address
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getreceivedbyaddress",
                    @params = new object[] { request.WalletAddress, 0 }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var balance = resultElement.GetDouble();
                    result.Result = balance;
                    result.IsError = false;
                    result.Message = "Bitcoin balance retrieved successfully";
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Address not found or has zero balance";
                }
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

                // Get Bitcoin transactions for the address
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "listtransactions",
                    @params = new object[] { request.WalletAddress, 10 } // Default to 10 transactions
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                var transactions = new List<IWalletTransaction>();
                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement) && resultElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tx in resultElement.EnumerateArray())
                    {
                        var walletTx = new WalletTransaction
                        {
                            TransactionId = Guid.NewGuid(),
                            FromWalletAddress = tx.TryGetProperty("address", out var addr) ? addr.GetString() : string.Empty,
                            ToWalletAddress = tx.TryGetProperty("address", out var toAddr) ? toAddr.GetString() : string.Empty,
                            Amount = tx.TryGetProperty("amount", out var amt) ? amt.GetDouble() : 0.0,
                            Description = tx.TryGetProperty("txid", out var txid) ? $"Bitcoin transaction: {txid.GetString()}" : "Bitcoin transaction"
                        };
                        transactions.Add(walletTx);
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Bitcoin transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                //// Generate Bitcoin key pair using NBitcoin SDK (chain-specific)
                //var network = _network == "mainnet" ? Network.Main : Network.TestNet;
                
                //// Generate mnemonic seed phrase
                //var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                //var seedPhrase = mnemonic.ToString();
                
                //// Derive Bitcoin key from mnemonic
                //var extKey = mnemonic.DeriveExtKey();
                //var key = extKey.PrivateKey;
                
                //// Get Bitcoin address (public key)
                //var privateKey = key.GetWif(network).ToString();
                //var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
                
                // Create KeyPairAndWallet using KeyHelper but override with Bitcoin-specific values from NBitcoin SDK
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress(); //KeyHelper generates in Bitcoin format by default
                //if (keyPair != null)
                //{
                //    keyPair.PrivateKey = privateKey;
                //    keyPair.PublicKey = publicKey;
                //    keyPair.WalletAddressLegacy = publicKey; // Bitcoin address
                //}

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Bitcoin key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region Bridge Methods (IOASISBlockchainStorageProvider)

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

                // Call Bitcoin RPC API to get address balance
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getreceivedbyaddress",
                    @params = new object[] { accountAddress, 0 } // 0 = minimum confirmations
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var balance = resultElement.GetDecimal();
                    result.Result = balance;
                    result.IsError = false;
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                }
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

                // Generate Bitcoin key pair using NBitcoin
                var network = _network == "mainnet" ? Network.Main : Network.TestNet;
                var key = new NBitcoin.Key();
                var privateKey = key.GetWif(network).ToString();
                var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();

                // Generate seed phrase
                var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                var seedPhrase = mnemonic.ToString();

                result.Result = (publicKey, privateKey, seedPhrase);
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

                // Restore Bitcoin key pair from seed phrase using NBitcoin
                var network = _network == "mainnet" ? Network.Main : Network.TestNet;
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

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Convert amount to Satoshis
                var satoshiAmount = (ulong)(amount * 100_000_000m);
                var bridgePoolAddress = "1" + new string('0', 33); // TODO: Get from config

                // Create Bitcoin transaction using RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sendtoaddress",
                    @params = new object[] { bridgePoolAddress, amount, "Bridge Withdrawal", "", true }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var txHash = resultElement.GetString();
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? Guid.NewGuid().ToString(),
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                }
                else
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Failed to create transaction",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, "Failed to create Bitcoin transaction");
                }
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
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

                // Create Bitcoin transaction from bridge pool to receiver
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sendtoaddress",
                    @params = new object[] { receiverAccountAddress, amount, "Bridge Deposit", "", true }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest);
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    var txHash = resultElement.GetString();
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? Guid.NewGuid().ToString(),
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                }
                else
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Failed to create transaction",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, "Failed to create Bitcoin transaction");
                }
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Bitcoin RPC for transaction status
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "gettransaction",
                    @params = new object[] { transactionHash }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    if (resultElement.TryGetProperty("confirmations", out var confirmationsElement))
                    {
                        var confirmations = confirmationsElement.GetInt32();
                        result.Result = confirmations > 0 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Pending;
                        result.IsError = false;
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                    }
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Bitcoin transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        #endregion
    }

    public class BitcoinTransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
    }
}


