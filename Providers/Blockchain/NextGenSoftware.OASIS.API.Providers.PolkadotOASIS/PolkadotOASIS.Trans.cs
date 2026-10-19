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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {



        /// <summary>
        /// Legacy storage calls have no reviewed deployed contract protocol/SDK encoder.
        /// Native wallet and asset operations use their separate real SDK owners.
        /// </summary>
        private Task<string> CreatePolkadotTransaction(string method, string data)
            => Task.FromException<string>(new NotSupportedException(
                "Polkadot contract storage is not implemented: these legacy calls have no reviewed deployed ABI or real SDK call encoder. Native wallet and asset SDK operations are separate."));

        /// <summary>
        /// Parse Polkadot blockchain response to Avatar object
        /// </summary>
        private Avatar ParsePolkadotToAvatar(string polkadotJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Polkadot JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(polkadotJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromPolkadot(polkadotJson);
            }
        }

        /// <summary>
        /// Create Avatar from Polkadot response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromPolkadot(string polkadotJson)
        {
            try
            {
                // Extract basic information from Polkadot JSON response
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{ExtractPolkadotProperty(polkadotJson, "address") ?? "polkadot_user"}"),
                    Username = ExtractPolkadotProperty(polkadotJson, "address") ?? "polkadot_user",
                    Email = ExtractPolkadotProperty(polkadotJson, "email") ?? "user@polkadot.example",
                    FirstName = ExtractPolkadotProperty(polkadotJson, "first_name"),
                    LastName = ExtractPolkadotProperty(polkadotJson, "last_name"),
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
        /// Extract property value from Polkadot JSON response
        /// </summary>
        private string ExtractPolkadotProperty(string polkadotJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Polkadot properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(polkadotJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Polkadot blockchain format
        /// </summary>
        private string ConvertAvatarToPolkadot(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Polkadot blockchain structure
                var polkadotData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(polkadotData, new JsonSerializerOptions
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
        /// Convert Holon to Polkadot blockchain format
        /// </summary>
        private string ConvertAvatarDetailToPolkadot(IAvatarDetail avatarDetail)
        {
            try
            {
                // Serialize AvatarDetail to JSON with Polkadot blockchain structure (includes inventory with Quantity/Stack for contract support)
                var polkadotData = new
                {
                    avatar_id = avatarDetail.Id.ToString(),
                    username = avatarDetail.Username,
                    email = avatarDetail.Email,
                    created = avatarDetail.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatarDetail.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    inventory = avatarDetail.Inventory
                };

                return System.Text.Json.JsonSerializer.Serialize(polkadotData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error converting AvatarDetail to Polkadot format: {ex.Message}", ex);
                return "{}";
            }
        }

        private string ConvertHolonToPolkadot(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Polkadot blockchain structure
                var polkadotData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(polkadotData, new JsonSerializerOptions
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
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                // Convert decimal amount to planck (1 DOT = 10^10 planck)
                var amountInPlanck = (long)(amount * 10000000000);

                // Get account info for balance check
                var accountResponse = await _httpClient.GetAsync($"/accounts/{fromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Polkadot address {fromWalletAddress}: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);

                var balance = accountData.GetProperty("data").GetProperty("free").GetInt64();
                if (balance < amountInPlanck)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balance} planck, Required: {amountInPlanck} planck");
                    return result;
                }

                // Create Polkadot transfer transaction
                var transferRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = "Balances",
                                method = "transfer",
                                args = new
                                {
                                    dest = toWalletAddress,
                                    value = amountInPlanck
                                }
                            },
                            signature = new
                            {
                                signer = fromWalletAddress,
                                signature = "", // Would be filled by actual signing
                                era = new
                                {
                                    immortal = true
                                },
                                nonce = 0,
                                tip = 0
                            }
                        }
                    }
                };

                // Submit transaction to Polkadot network
                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new PolkadotTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("result").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Polkadot transaction sent successfully. Extrinsic Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Polkadot transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Polkadot transaction: {ex.Message}");
            }

            return result;
        }



        public void Dispose()
        {
            _lifecycleGate.Wait();
            try
            {
                if(_isDisposed)return;
                _isDisposed=true;_isActivated=false;IsProviderActivated=false;
                ReportedChainName=null;ReportedGenesisHash=null;_httpClient.Dispose();
            }
            finally{_lifecycleGate.Release();}
        }



        public override OASISResult<IEnumerable<IHolon>> ExportAll(int maxChildDepth = 0)
        {
            return ExportAllAsync(maxChildDepth).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int maxChildDepth = 0)
        {
            // Export all holons - delegate to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, maxChildDepth, 0, true, false, 0);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            return LoadHolonAsync(id, loadChildren, continueOnError, maxChildren, recurseChildren, loadDetail, maxDepth).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - delegate to ProviderManager as fallback
                    return await HolonManager.Instance.LoadHolonAsync(id, loadChildren, recurseChildren, maxChildren, continueOnError, loadDetail, HolonType.All, maxDepth, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                }

                // Query holon from Polkadot blockchain using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getHolon",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"id\":\"{id}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && !string.IsNullOrEmpty(result.GetString()))
                    {
                        var holonData = JsonSerializer.Deserialize<Holon>(result.GetString());
                        response.Result = holonData;
                        response.IsError = false;
                        response.Message = "Holon loaded from Polkadot successfully";

                        // Load children if requested
                        if (loadChildren && holonData != null && recurseChildren && maxDepth > 0)
                        {
                            var childrenResult = await LoadHolonsForParentAsync(id, HolonType.All, loadChildren, recurseChildren, maxDepth - 1, 0, continueOnError, false, 0);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holonData.Children = childrenResult.Result.ToList();
                            }
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, continueOnError, maxChildren, recurseChildren, loadDetail, maxDepth).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - delegate to ProviderManager as fallback
                    return await HolonManager.Instance.LoadHolonAsync(providerKey, loadChildren, recurseChildren, maxChildren, continueOnError, loadDetail, HolonType.All, maxDepth, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                }

                // Query holon from Polkadot blockchain using smart contract call by provider key
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getHolonByProviderKey",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"provider_key\":\"{providerKey}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && !string.IsNullOrEmpty(result.GetString()))
                    {
                        var holonData = JsonSerializer.Deserialize<Holon>(result.GetString());
                        response.Result = holonData;
                        response.IsError = false;
                        response.Message = "Holon loaded from Polkadot by provider key successfully";

                        // Load children if requested
                        if (loadChildren && holonData != null && recurseChildren && maxDepth > 0)
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holonData.Id, HolonType.All, loadChildren, recurseChildren, maxDepth - 1, 0, continueOnError, false, 0);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holonData.Children = childrenResult.Result.ToList();
                            }
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
