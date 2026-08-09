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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS
{
    public partial class CardanoOASIS
    {
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from Cardano blockchain
                var queryUrl = $"/addresses/{id}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseCardanoToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Cardano successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Cardano JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Cardano blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Cardano: {ex.Message}");
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Cardano address by provider key using Blockfrost API
                var queryUrl = $"/addresses/{providerKey}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var addressData = JsonSerializer.Deserialize<JsonElement>(content);

                    var cardanoAddress = addressData.TryGetProperty("address", out var address) ? address.GetString() : providerKey;
                    var avatar = new Avatar
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:{cardanoAddress}"),
                        Username = providerKey,
                        Email = cardanoAddress,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };

                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded from Cardano address successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano address: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Cardano: {ex.Message}");
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Cardano metadata for avatar by email using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100"; // NFT metadata standard

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for metadata containing the email
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(avatarEmail))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatar loaded from Cardano by email successfully";
                                    return response;
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano metadata: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Cardano: {ex.Message}");
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Cardano metadata for avatar by username using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100"; // NFT metadata standard

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for metadata containing the username
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(avatarUsername))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatar loaded from Cardano by username successfully";
                                    return response;
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano metadata: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query all avatars from Cardano blockchain using Blockfrost API
                var queryUrl = "/metadata/txs/labels/721?count=100"; // NFT metadata standard

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Find first avatar metadata
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains("avatar"))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = new List<IAvatar> { avatar };
                                    response.IsError = false;
                                    response.Message = "Avatars loaded from Cardano successfully";
                                    return response;
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "No avatars found on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query avatar details from Cardano blockchain using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for metadata containing the avatar ID
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(id.ToString()))
                            {
                                var avatarDetail = ParseCardanoToAvatarDetail(metadataString);
                                if (avatarDetail != null && avatarDetail.Id == id)
                                {
                                    response.Result = avatarDetail;
                                    response.IsError = false;
                                    response.Message = "Avatar detail loaded from Cardano successfully";
                                    return response;
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cardano provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.CardanoOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                var walletAddress = walletResult.Result.WalletAddress;

                // Save avatar to Cardano blockchain using transaction with metadata via Blockfrost API
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Get UTXOs for the wallet address using Blockfrost API
                var utxosResponse = await _httpClient.GetAsync($"/addresses/{walletAddress}/utxos");
                if (!utxosResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get UTXOs for Cardano address: {utxosResponse.StatusCode}");
                    return response;
                }

                var utxosContent = await utxosResponse.Content.ReadAsStringAsync();
                var utxosData = JsonSerializer.Deserialize<JsonElement[]>(utxosContent);
                
                if (utxosData == null || utxosData.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref response, "No UTXOs available for transaction");
                    return response;
                }

                // Use first UTXO
                var utxo = utxosData[0];
                var txHash = utxo.TryGetProperty("tx_hash", out var txHashProp) ? txHashProp.GetString() : "";
                var outputIndex = utxo.TryGetProperty("output_index", out var indexProp) ? indexProp.GetInt32() : 0;

                // Get current slot for TTL
                var slotResponse = await _httpClient.GetAsync("/blocks/latest");
                long currentSlot = 0;
                if (slotResponse.IsSuccessStatusCode)
                {
                    var slotContent = await slotResponse.Content.ReadAsStringAsync();
                    var slotData = JsonSerializer.Deserialize<JsonElement>(slotContent);
                    if (slotData.TryGetProperty("slot", out var slotProp))
                    {
                        currentSlot = slotProp.GetInt64();
                    }
                }

                // Create Cardano transaction with metadata using Blockfrost API format
                var txRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = txHash,
                            output_index = outputIndex
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = walletAddress,
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "1000000"
                                }
                            }
                        }
                    },
                    metadata = new Dictionary<string, object>
                    {
                        ["721"] = new Dictionary<string, object>
                        {
                            [avatar.Id.ToString()] = new Dictionary<string, object>
                            {
                                ["avatar_data"] = avatarJson
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Submit transaction via Blockfrost API
                var httpResponse = await _httpClient.PostAsync("/tx/submit", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    string txId = null;
                    if (txResponse.TryGetProperty("tx_hash", out var txHashResult))
                    {
                        txId = txHashResult.GetString();
                    }
                    else if (txResponse.TryGetProperty("id", out var idProp))
                    {
                        txId = idProp.GetString();
                    }

                    if (!string.IsNullOrEmpty(txId))
                    {
                        // Store transaction hash in provider unique storage key
                        if (avatar.ProviderUniqueStorageKey == null)
                            avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                        avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CardanoOASIS] = txId;

                        response.Result = avatar;
                        response.IsError = false;
                        response.IsSaved = true;
                        response.Message = $"Avatar saved to Cardano blockchain successfully. Transaction ID: {txId}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Cardano blockchain - no transaction hash returned");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Cardano: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Cardano: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
{
    return SaveAvatarAsync(avatar).Result;
}

public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
{
    var response = new OASISResult<bool>();
    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Delete avatar from Cardano blockchain using transaction with deletion metadata
        var deleteData = JsonSerializer.Serialize(new { avatar_id = id.ToString(), deleted = true, soft_delete = softDelete });

            // Get real UTXOs for the wallet
            var utxosResult = await GetWalletUTXOsAsync();
            if (utxosResult.IsError || !utxosResult.Result.Any())
            {
                OASISErrorHandling.HandleError(ref response, "No UTXOs available for transaction");
                return response;
            }

            var utxo = utxosResult.Result.First();
            var walletAddress = await GetWalletAddressAsync();
            var fee = await CalculateTransactionFeeAsync(utxo, walletAddress, 1000000);

            var txRequest = new
            {
                tx = new
                {
                    body = new
                    {
                        inputs = new[]
                        {
                            new
                            {
                                tx_hash = utxo.TxHash,
                                index = utxo.Index
                            }
                        },
                        outputs = new[]
                        {
                            new
                            {
                                address = walletAddress,
                                amount = new
                                {
                                    quantity = 1000000,
                                    unit = "lovelace"
                                }
                            }
                        },
                        fee = fee.ToString(),
                        ttl = await GetCurrentSlotAsync() + 3600 // TTL: current slot + 1 hour
                    },
                    witness_set = new
                    {
                        vkey_witnesses = new[]
                        {
                            await CreateWitnessAsync(utxo, walletAddress)
                        }
                    },
                    metadata = new Dictionary<string, object>
                    {
                        ["721"] = new Dictionary<string, object>
                        {
                            ["avatar_deletion"] = deleteData
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(txRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("/tx/submit", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (txResponse.TryGetProperty("id", out var txId))
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Avatar deleted from Cardano blockchain successfully. Transaction ID: {txId.GetString()}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from Cardano blockchain");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from Cardano: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            response.Exception = ex;
            OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Cardano: {ex.Message}");
        }
            return response;
        }

    }
}
