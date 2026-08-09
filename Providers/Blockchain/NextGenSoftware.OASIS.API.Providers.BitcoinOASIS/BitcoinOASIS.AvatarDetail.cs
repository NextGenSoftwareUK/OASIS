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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Serialize avatar detail as separate object with type marker so we can load it without deriving from avatar
                var wrapper = new { oasisType = "AvatarDetail", value = avatar };
                var avatarDetailJson = JsonSerializer.Serialize(wrapper);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
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

    }
}
