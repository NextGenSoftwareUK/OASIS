using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {

        /// <summary>
        /// Load avatar data from Elrond smart contract
        /// </summary>
        private async Task<string> LoadAvatarFromElrondAsync(string avatarId, int version = 0)
        {
            try
            {
                // Query Elrond smart contract for avatar data
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatar",
                    args = new[] { avatarId, version.ToString() }
                };

                var response = await _httpClient.PostAsync("/vm/query", 
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                    var raw = result?.data?.returnData?.FirstOrDefault();
                    return DecodeElrondReturnData(raw);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading avatar from Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar detail data to Elrond smart contract (separate from Avatar).
        /// </summary>
        private async Task<string> SaveAvatarDetailToElrondAsync(IAvatarDetail avatarDetail)
        {
            try
            {
                var json = JsonSerializer.Serialize(avatarDetail, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var transactionData = new
                {
                    nonce = await GetAccountNonceAsync(),
                    value = "0",
                    receiver = GetOASISContractAddress(),
                    sender = await GetWalletAddressAsync(),
                    gasPrice = 1000000000,
                    gasLimit = 10000000,
                    data = $"saveAvatarDetail@{Convert.ToHexString(Encoding.UTF8.GetBytes(json))}"
                };
                var response = await _httpClient.PostAsync("/transaction/send",
                    new StringContent(JsonSerializer.Serialize(transactionData), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                    return result?.txHash;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving avatar detail to Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar data to Elrond smart contract
        /// </summary>
        private async Task<string> SaveAvatarToElrondAsync(IAvatar avatar)
        {
            try
            {
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var transactionData = new
                {
                    nonce = await GetAccountNonceAsync(),
                    value = "0",
                    receiver = GetOASISContractAddress(),
                    sender = await GetWalletAddressAsync(),
                    gasPrice = 1000000000,
                    gasLimit = 10000000,
                    data = $"saveAvatar@{Convert.ToHexString(Encoding.UTF8.GetBytes(avatarJson))}"
                };

                var response = await _httpClient.PostAsync("/transaction/send",
                    new StringContent(JsonSerializer.Serialize(transactionData), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                    return result?.txHash;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar to Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load avatar by provider key from Elrond smart contract (getAvatarByProviderKey view).
        /// </summary>
        private async Task<string> LoadAvatarByProviderKeyFromElrondAsync(string providerKey, int version = 0)
        {
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatarByProviderKey",
                    args = new[] { providerKey, version.ToString() }
                };
                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return null;
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                var raw = result?.data?.returnData?.FirstOrDefault();
                return DecodeElrondReturnData(raw);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Load avatar detail data from Elrond smart contract (separate from Avatar; stored as detail on chain).
        /// </summary>
        private async Task<string> LoadAvatarDetailFromElrondAsync(string avatarId, int version = 0)
        {
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatarDetail",
                    args = new[] { avatarId, version.ToString() }
                };

                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                    var raw = result?.data?.returnData?.FirstOrDefault();
                    return DecodeElrondReturnData(raw);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading avatar detail from Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolve avatar id by email from Elrond contract (getAvatarIdByEmail view).
        /// </summary>
        private async Task<Guid?> GetAvatarIdByEmailFromElrondAsync(string email)
        {
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatarIdByEmail",
                    args = new[] { email }
                };
                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return null;
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                var raw = result?.data?.returnData?.FirstOrDefault();
                var decoded = DecodeElrondReturnData(raw);
                return Guid.TryParse(decoded, out var id) ? id : (Guid?)null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Resolve avatar id by username from Elrond contract (getAvatarIdByUsername view).
        /// </summary>
        private async Task<Guid?> GetAvatarIdByUsernameFromElrondAsync(string username)
        {
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatarIdByUsername",
                    args = new[] { username }
                };
                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return null;
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                var raw = result?.data?.returnData?.FirstOrDefault();
                var decoded = DecodeElrondReturnData(raw);
                return Guid.TryParse(decoded, out var id) ? id : (Guid?)null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Get all avatar ids from Elrond contract (getAvatarIds view).
        /// </summary>
        private async Task<List<Guid>> GetAvatarIdsFromElrondAsync()
        {
            var list = new List<Guid>();
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getAvatarIds",
                    args = Array.Empty<string>()
                };
                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return list;
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                var returnData = result?.data?.returnData;
                if (returnData == null) return list;
                foreach (var raw in returnData)
                {
                    var decoded = DecodeElrondReturnData(raw);
                    if (Guid.TryParse(decoded, out var id))
                        list.Add(id);
                }
            }
            catch { /* ignore */ }
            return list;
        }

        /// <summary>
        /// Decode MultiversX/Elrond VM return data (base64) to UTF8 string.
        /// </summary>
        private static string DecodeElrondReturnData(string base64OrRaw)
        {
            if (string.IsNullOrEmpty(base64OrRaw)) return null;
            try
            {
                var bytes = Convert.FromBase64String(base64OrRaw);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64OrRaw;
            }
        }

        /// <summary>
        /// Parse JSON from Elrond chain into AvatarDetail (provider's own detail source, not built from Avatar).
        /// </summary>
        private static IAvatarDetail ParseElrondToAvatarDetail(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<AvatarDetail>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Load holon data from Elrond smart contract
        /// </summary>
        private async Task<string> LoadHolonFromElrondAsync(string holonId, int version = 0)
        {
            try
            {
                var queryData = new
                {
                    scAddress = GetOASISContractAddress(),
                    func = "getHolon",
                    args = new[] { holonId, version.ToString() }
                };

                var response = await _httpClient.PostAsync("/vm/query",
                    new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondQueryResult>(content);
                    var raw = result?.data?.returnData?.FirstOrDefault();
                    return DecodeElrondReturnData(raw);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading holon from Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save holon data to Elrond smart contract
        /// </summary>
        private async Task<string> SaveHolonToElrondAsync(IHolon holon)
        {
            try
            {
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var transactionData = new
                {
                    nonce = await GetAccountNonceAsync(),
                    value = "0",
                    receiver = GetOASISContractAddress(),
                    sender = await GetWalletAddressAsync(),
                    gasPrice = 1000000000,
                    gasLimit = 10000000,
                    data = $"saveHolon@{Convert.ToHexString(Encoding.UTF8.GetBytes(holonJson))}"
                };

                var response = await _httpClient.PostAsync("/transaction/send",
                    new StringContent(JsonSerializer.Serialize(transactionData), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                    return result?.txHash;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving holon to Elrond: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // This would be the deployed OASIS smart contract address on Elrond
            return "erd1qqqqqqqqqqqqqpgq7ykazrzd905zvnlr8dpfw0jp7r4q0v4s2zzqs0zp5s";
        }

        /// <summary>
        /// Get account nonce for transaction
        /// </summary>
        private async Task<long> GetAccountNonceAsync()
        {
            try
            {
                var address = await GetWalletAddressAsync();
                var response = await _httpClient.GetAsync($"/address/{address}/nonce");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ElrondAccountResult>(content);
                    return result?.nonce ?? 0;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting account nonce: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get wallet address for transactions using real WalletManager API
        /// </summary>
        private async Task<string> GetWalletAddressAsync(Guid? avatarId = null)
        {
            try
        {
                // If avatar ID is provided, get wallet for that avatar
                if (avatarId.HasValue && avatarId.Value != Guid.Empty)
                {
                    var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
                        WalletManager.Instance, 
                        Core.Enums.ProviderType.ElrondOASIS, 
                        avatarId.Value, 
                        _httpClient);
                    
                    if (!walletResult.IsError && !string.IsNullOrWhiteSpace(walletResult.Result))
                    {
                        return walletResult.Result;
                    }
                }
                
                // Fallback: no default wallet available; caller should provide avatarId or configure wallet
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error getting wallet address: {ex.Message}", ex);
            }
            
            // Final fallback: return empty string (caller should handle this)
            return "";
        }



    public class ElrondQueryResult
    {
        public ElrondQueryData data { get; set; }
    }

    public class ElrondQueryData
    {
        public string[] returnData { get; set; }
    }

    public class ElrondTransactionResult
    {
        public string txHash { get; set; }
    }

    public class ElrondAccountResult
    {
        public long nonce { get; set; }
    }


    }
}
