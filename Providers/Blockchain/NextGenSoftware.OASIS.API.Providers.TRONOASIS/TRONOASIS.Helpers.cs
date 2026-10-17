using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // Return the contract address if set, otherwise use a default TRON contract address
            return _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
        }

        /// <summary>
        /// Parse TRON JSON response to Holon object
        /// </summary>
        private IHolon ParseTRONToHolon(string tronJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tronJson))
                    return null;

                // Parse JSON result from TRON contract call
                var holonData = JsonSerializer.Deserialize<JsonElement>(tronJson);
                
                // In production, map TRON data structure to IHolon
                // For now, create a basic Holon from the data
                var holon = new Holon();
                
                if (holonData.TryGetProperty("id", out var idProp))
                {
                    if (Guid.TryParse(idProp.GetString(), out var id))
                        holon.Id = id;
                }
                
                if (holonData.TryGetProperty("name", out var nameProp))
                    holon.Name = nameProp.GetString();
                
                if (holonData.TryGetProperty("description", out var descProp))
                    holon.Description = descProp.GetString();
                
                return holon;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse TRON JSON response to list of Holons
        /// </summary>
        private IEnumerable<IHolon> ParseTRONToHolons(string tronJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tronJson))
                    return new List<IHolon>();

                // Parse JSON result from TRON contract call
                var holonsData = JsonSerializer.Deserialize<JsonElement>(tronJson);
                var holons = new List<IHolon>();
                
                // Check if it's an array
                if (holonsData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var holonElement in holonsData.EnumerateArray())
                    {
                        var holon = ParseTRONToHolon(holonElement.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }
                else if (holonsData.TryGetProperty("holons", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = ParseTRONToHolon(holonElement.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }
                
                return holons;
            }
            catch
            {
                return new List<IHolon>();
            }
        }



        /// <summary>
        /// Parse TRON blockchain response to Avatar object
        /// </summary>
        private Avatar ParseTRONToAvatar(string tronJson)
        {
            try
            {
                // Deserialize the complete Avatar object from TRON JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromTRON(tronJson);
            }
        }

        /// <summary>
        /// Create Avatar from TRON response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromTRON(string tronJson)
        {
            try
            {
                // Extract basic information from TRON JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractTRONProperty(tronJson, "address") ?? "tron_user",
                    Email = ExtractTRONProperty(tronJson, "email") ?? "user@tron.example",
                    FirstName = ExtractTRONProperty(tronJson, "first_name"),
                    LastName = ExtractTRONProperty(tronJson, "last_name"),
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
        /// Extract property value from TRON JSON response
        /// </summary>
        private string ExtractTRONProperty(string tronJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for TRON properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(tronJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to TRON blockchain format
        /// </summary>
        private string ConvertAvatarToTRON(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with TRON blockchain structure
                var tronData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
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
        /// Convert Holon to TRON blockchain format
        /// </summary>
        private string ConvertHolonToTRON(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with TRON blockchain structure
                var tronData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
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


        /*

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error in LoadProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Use WalletManager to load provider wallets for the avatar
                var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id);
                
                if (walletsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
                    return result;
                }

                result.Result = walletsResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in SaveProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                if (providerWallets == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider wallets cannot be null");
                    return result;
                }

                // Use WalletManager to save provider wallets for the avatar
                var saveResult = await WalletManager.SaveProviderWalletsForAvatarByIdAsync(id, providerWallets);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveResult.Message}");
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        */


        /// <summary>
        /// Get wallet address for avatar using WalletHelper with fallback chain
        /// </summary>
        private async Task<OASISResult<string>> GetWalletAddressForAvatar(Guid avatarId)
        {
            return await WalletHelper.GetWalletAddressForAvatarAsync(
                WalletManager,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS,
                avatarId,
                _httpClient);
        }

        private string ConvertHexToTronAddress(string hexString)
        {
            try
            {
                var bytes = Convert.FromHexString(hexString);
                return "T" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 33);
            }
            catch
            {
                return "";
            }
        }

        private Avatar ParseTRONToAvatar(TRONAccountInfo accountInfo, Guid id)
        {
            try
            {
                var tronJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                if (avatar == null)
                {
                    avatar = new Avatar
                    {
                        Id = id,
                        Username = accountInfo?.Address ?? "tron_user",
                        Email = $"user@{accountInfo?.Address ?? "tron"}.com",
                        FirstName = "TRON",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = 1,
                        IsActive = true
                    };
                }

                if (accountInfo != null)
                {
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_address", accountInfo.Address ?? "");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_balance", accountInfo.Balance?.ToString() ?? "0");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_energy", accountInfo.Energy?.ToString() ?? "0");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_bandwidth", accountInfo.Bandwidth?.ToString() ?? "0");
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Call TRON smart contract method
        /// </summary>
        private async Task<OASISResult<string>> CallContractAsync(string contractAddress, string functionName, object[] parameters, string callerAddress = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(contractAddress) || string.IsNullOrWhiteSpace(functionName))
                {
                    OASISErrorHandling.HandleError(ref result, "Contract address and function name are required");
                    return result;
                }

                // Build function selector (first 4 bytes of keccak256 hash of function signature)
                // For simplicity, we'll use the function name directly - in production, use proper ABI encoding
                var functionSelector = functionName;
                
                // Encode parameters (simplified - in production use proper ABI encoding)
                var parameterHex = "";
                if (parameters != null && parameters.Length > 0)
                {
                    foreach (var param in parameters)
                    {
                        var paramStr = param?.ToString() ?? "";
                        // Convert to hex and pad to 64 characters
                        var paramBytes = Encoding.UTF8.GetBytes(paramStr);
                        parameterHex += Convert.ToHexString(paramBytes).PadLeft(64, '0').Substring(0, 64);
                    }
                }

                // Build TRON smart contract call payload
                var callPayload = new
                {
                    owner_address = callerAddress ?? _contractAddress ?? "T0000000000000000000000000000000000",
                    contract_address = contractAddress,
                    function_selector = functionSelector,
                    parameter = parameterHex,
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(callPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("result", out var resultProp))
                    {
                        result.Result = resultProp.GetRawText();
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Contract call failed: no result in response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Contract call failed: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error calling contract: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}
