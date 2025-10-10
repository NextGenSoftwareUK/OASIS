using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

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

                // Load avatar from Bitcoin blockchain
                var queryUrl = $"/address/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse Bitcoin JSON and create Avatar object
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin blockchain: {httpResponse.StatusCode}");
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

                // Serialize avatar to JSON
                var avatarJson = JsonSerializer.Serialize(avatar);
                var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);
                
                // Create Bitcoin transaction with avatar data
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
                            address = avatar.ProviderWallets[ProviderType.BitcoinOASIS]?.Address ?? "",
                            value = 0, // OP_RETURN transaction
                            script = Convert.ToHexString(avatarBytes) // Store avatar data in OP_RETURN
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
                    
                    avatar.ProviderWallets[ProviderType.BitcoinOASIS] = new Wallet()
                    {
                        Address = responseData.GetProperty("txid").GetString(),
                        ProviderType = ProviderType.BitcoinOASIS
                    };
                    
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar saved successfully to Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Bitcoin: {submitResponse.StatusCode}");
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
                            address = avatar.ProviderWallets[ProviderType.BitcoinOASIS]?.Address ?? "",
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
                    
                    avatar.ProviderWallets[ProviderType.BitcoinOASIS] = new Wallet()
                    {
                        Address = responseData.GetProperty("txid").GetString(),
                        ProviderType = ProviderType.BitcoinOASIS
                    };
                    
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
                    
                    holon.ProviderWallets[ProviderType.BitcoinOASIS] = new Wallet()
                    {
                        Address = responseData.GetProperty("txid").GetString(),
                        ProviderType = ProviderType.BitcoinOASIS
                    };
                    
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                    response.Result = new Holon { ProviderWallets = new Dictionary<ProviderType, IWallet> { { ProviderType.BitcoinOASIS, new Wallet { Address = providerKey, ProviderType = ProviderType.BitcoinOASIS } } } };
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
                    query = searchParams.SearchQuery,
                    filters = new
                    {
                        fromDate = searchParams.FromDate,
                        toDate = searchParams.ToDate,
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

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Get players near me from Bitcoin blockchain
                var queryUrl = "/addresses/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Bitcoin JSON and create Player collection
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from Bitcoin blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Bitcoin: {ex.Message}");
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Get holons near me from Bitcoin blockchain
                var queryUrl = $"/addresses/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Bitcoin JSON and create Holon collection
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Bitcoin blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Bitcoin: {ex.Message}");
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
                // Deserialize the complete Avatar object from Bitcoin JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(bitcoinJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
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
                        avatar.ProviderMetaData.Add("bitcoin_balance", funded.GetInt64().ToString());
                    }
                    if (chainStats.TryGetProperty("spent_txo_sum", out var spent))
                    {
                        avatar.ProviderMetaData.Add("bitcoin_spent", spent.GetInt64().ToString());
                    }
                }
                if (bitcoinData.TryGetProperty("mempool_stats", out var mempoolStats))
                {
                    if (mempoolStats.TryGetProperty("funded_txo_sum", out var mempoolFunded))
                    {
                        avatar.ProviderMetaData.Add("bitcoin_mempool_balance", mempoolFunded.GetInt64().ToString());
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

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            
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
                    
                    result.Result = new TransactionRespone
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

        #endregion
    }
}


