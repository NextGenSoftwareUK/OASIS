using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Save holon to Aptos blockchain using real Aptos RPC
                var sequenceNumber = await GetSequenceNumber();
                var request = new
                {
                    sender = APTOS_ACCOUNT_ADDRESS,
                    sequence_number = sequenceNumber.ToString(),
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = ((DateTimeOffset)DateTime.UtcNow.AddMinutes(10)).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = $"{APTOS_CONTRACT_ADDRESS}::oasis::save_holon",
                        arguments = new object[]
                        {
                            holon.Id.ToString(),
                            holon.Name ?? "",
                            holon.Description ?? "",
                            (int)holon.HolonType,
                            holon.ParentHolonId.ToString(),
                            holon.ParentOmniverseId.ToString(),
                            holon.ParentMultiverseId.ToString(),
                            holon.ParentUniverseId.ToString(),
                            holon.ParentDimensionId.ToString(),
                            holon.ParentGalaxyClusterId.ToString(),
                            holon.ParentGalaxyId.ToString(),
                            holon.ParentSolarSystemId.ToString(),
                            holon.ParentPlanetId.ToString(),
                            holon.ParentMoonId.ToString(),
                            holon.ParentStarId.ToString(),
                            holon.ParentZomeId.ToString(),
                            holon.MetaData != null ? System.Text.Json.JsonSerializer.Serialize(holon.MetaData) : "",
                            ((DateTimeOffset)holon.CreatedDate).ToUnixTimeSeconds(),
                            ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                            holon.IsActive
                        },
                        type_arguments = new object[0]
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = holon;
                    response.IsError = false;
                    response.Message = "Holon saved to Aptos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"SaveHolonsAsync failed on holon {holon.Id}: {saveResult.Message}");
                            return result;
                        }
                    }
                    else
                        saved.Add(saveResult.Result);
                }
                result.Result = saved;
                result.IsSaved = true;
                result.Message = $"{saved.Count} holons saved to Aptos successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveHolonsAsync: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonAsync(id.ToString());
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // Load holon first to get its ID
                var loadResult = await LoadHolonAsync(providerKey);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon not found for provider key: {providerKey}");
                    return result;
                }
                
                // Delete holon using Aptos Move smart contract
                var deletePayload = new
                {
                    type = "entry_function_payload",
                    function = "0x1::oasis::delete_holon",
                    type_arguments = new string[0],
                    arguments = new[] { providerKey }
                };
                
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(deletePayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/transactions", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    result.Result = loadResult.Result;
                    result.Message = "Holon deleted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                var saveResult = await SaveHolonsAsync(holons);
                if (saveResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"ImportAsync failed: {saveResult.Message}");
                else
                {
                    result.Result = true;
                    result.Message = saveResult.Message;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in ImportAsync: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Export all holons - delegate to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            // Query Aptos for holons created by this avatar
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_holons_by_avatar",
                    arguments = new object[] { id.ToString() },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null)
                                holons.Add(holon);
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count} holons for avatar {id} from Aptos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found for avatar on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export data from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting data from Aptos: {ex.Message}");
            }
            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0) => ExportAllDataForAvatarByIdAsync(id, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {username} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0) => ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {email} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0) => ExportAllDataForAvatarByEmailAsync(email, version).Result;



        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                var transactionPayload = new
                {
                    sender = fromWalletAddress,
                    sequence_number = "0",
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { toWalletAddress, amount.ToString() }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    response.Result = new TransactionResponse
                    {
                        TransactionResult = $"Transaction sent successfully. Hash: {transactionResult.TransactionHash}"
                    };
                    response.IsError = false;
                    response.Message = "Transaction sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Implement real Aptos transaction
                if (string.IsNullOrEmpty(_privateKey))
                {
                    OASISErrorHandling.HandleError(ref response, "Private key not configured for Aptos transactions");
                    return response;
                }

                try
                {
                    // Create transaction payload for Aptos
                    var transactionPayload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { toAvatarId.ToString(), amount.ToString() }
                    };

                    // Submit REAL transaction to Aptos network
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                        response.Result = new TransactionResponse
                        {
                            TransactionResult = $"Transaction submitted successfully. Hash: {transactionResult.TransactionHash}, Version: {transactionResult.Version}"
                        };
                        response.IsError = false;
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Aptos transaction failed: {httpResponse.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error creating Aptos transaction: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending Aptos transaction: {ex.Message}");
            }

            return response;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by avatar IDs
                var task = SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, memo);
                response = task.Result;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction by avatar IDs to Aptos: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by avatar IDs
                var transactionPayload = new
                {
                    sender = $"0x{fromAvatarId.ToString("N")}",
                    sequence_number = "0",
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { $"0x{toAvatarId.ToString("N")}", amount.ToString() }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    response.Result = new TransactionResponse
                    {
                        TransactionResult = $"Transaction sent successfully. Hash: {transactionResult.TransactionHash}"
                    };
                    response.IsError = false;
                    response.Message = "Transaction sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromUsername, string toUsername, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by usernames
                var task = SendTransactionByUsernameAsync(fromUsername, toUsername, amount);
                response = task.Result;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction by usernames to Aptos: {ex.Message}");
            }
            return response;
        }

    }
}
