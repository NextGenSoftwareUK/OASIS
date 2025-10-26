using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Generic WalletHelper for all OASIS providers to share wallet lookup functionality
    /// Implements fallback chain: WalletManager -> Avatar.ProviderWallets -> HTTP client
    /// </summary>
    public static class WalletHelper
    {
        /// <summary>
        /// Get wallet address for avatar by ID using the fallback chain
        /// </summary>
        public static async Task<OASISResult<string>> GetWalletAddressForAvatarAsync(
            WalletManager walletManager, 
            ProviderType providerType, 
            Guid avatarId, 
            HttpClient httpClient = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // 1. Try WalletManager methods first (safest)
                if (walletManager != null)
                {
                    var walletResult = await GetWalletFromWalletManagerAsync(walletManager, providerType, avatarId);
                    if (!walletResult.IsError && !string.IsNullOrEmpty(walletResult.Result))
                    {
                        result.Result = walletResult.Result;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from WalletManager successfully";
                        return result;
                    }
                }

                // 2. Fallback to Avatar.ProviderWallets
                var avatarWalletResult = await GetWalletFromAvatarProviderWalletsAsync(walletManager, providerType, avatarId);
                if (!avatarWalletResult.IsError && !string.IsNullOrEmpty(avatarWalletResult.Result))
                {
                    result.Result = avatarWalletResult.Result;
                    result.IsError = false;
                    result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                    return result;
                }

                // 3. Final fallback to HTTP client
                if (httpClient != null)
                {
                    var httpResult = await GetWalletFromHttpClientAsync(httpClient, providerType, avatarId);
                    if (!string.IsNullOrEmpty(httpResult))
                    {
                        result.Result = httpResult;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from HTTP client successfully";
                        return result;
                    }
                }
                else
                {
                    // Create internal HttpClient if none provided
                    using (var internalHttpClient = new HttpClient())
                    {
                        var httpResult = await GetWalletFromHttpClientAsync(internalHttpClient, providerType, avatarId);
                        if (!string.IsNullOrEmpty(httpResult))
                        {
                            result.Result = httpResult;
                            result.IsError = false;
                            result.Message = "Wallet address retrieved from HTTP client successfully";
                            return result;
                        }
                    }
                }

                // If all methods fail
                OASISErrorHandling.HandleError(ref result, "Failed to retrieve wallet address from all methods");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet address: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Get wallet address for avatar by username using the fallback chain
        /// </summary>
        public static async Task<OASISResult<string>> GetWalletAddressForAvatarByUsernameAsync(
            WalletManager walletManager, 
            ProviderType providerType, 
            string username, 
            HttpClient httpClient = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // 1. Try WalletManager methods first (safest)
                if (walletManager != null)
                {
                    var walletResult = await GetWalletFromWalletManagerByUsernameAsync(walletManager, providerType, username);
                    if (!string.IsNullOrEmpty(walletResult))
                    {
                        result.Result = walletResult;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from WalletManager successfully";
                        return result;
                    }
                }

                // 2. Fallback to Avatar.ProviderWallets
                var avatarWalletResult = await GetWalletFromAvatarProviderWalletsByUsernameAsync(walletManager, providerType, username);
                if (!avatarWalletResult.IsError && !string.IsNullOrEmpty(avatarWalletResult.Result))
                {
                    result.Result = avatarWalletResult.Result;
                    result.IsError = false;
                    result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                    return result;
                }

                // 3. Final fallback to HTTP client
                if (httpClient != null)
                {
                    var httpResult = await GetWalletFromHttpClientByUsernameAsync(httpClient, providerType, username);
                    if (!string.IsNullOrEmpty(httpResult))
                    {
                        result.Result = httpResult;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from HTTP client successfully";
                        return result;
                    }
                }
                else
                {
                    // Create internal HttpClient if none provided
                    using (var internalHttpClient = new HttpClient())
                    {
                        var httpResult = await GetWalletFromHttpClientByUsernameAsync(internalHttpClient, providerType, username);
                        if (!string.IsNullOrEmpty(httpResult))
                        {
                            result.Result = httpResult;
                            result.IsError = false;
                            result.Message = "Wallet address retrieved from HTTP client successfully";
                            return result;
                        }
                    }
                }

                // If all methods fail
                OASISErrorHandling.HandleError(ref result, "Failed to retrieve wallet address from all methods");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet address: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Get wallet address for avatar by email using the fallback chain
        /// </summary>
        public static async Task<OASISResult<string>> GetWalletAddressForAvatarByEmailAsync(
            WalletManager walletManager, 
            ProviderType providerType, 
            string email, 
            HttpClient httpClient = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // 1. Try WalletManager methods first (safest)
                if (walletManager != null)
                {
                    var walletResult = await GetWalletFromWalletManagerByEmailAsync(walletManager, providerType, email);
                    if (!string.IsNullOrEmpty(walletResult))
                    {
                        result.Result = walletResult;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from WalletManager successfully";
                        return result;
                    }
                }

                // 2. Fallback to Avatar.ProviderWallets
                var avatarWalletResult = await GetWalletFromAvatarProviderWalletsByEmailAsync(walletManager, providerType, email);
                if (!avatarWalletResult.IsError && !string.IsNullOrEmpty(avatarWalletResult.Result))
                {
                    result.Result = avatarWalletResult.Result;
                    result.IsError = false;
                    result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                    return result;
                }

                // 3. Final fallback to HTTP client
                if (httpClient != null)
                {
                    var httpResult = await GetWalletFromHttpClientByEmailAsync(httpClient, providerType, email);
                    if (!string.IsNullOrEmpty(httpResult))
                    {
                        result.Result = httpResult;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from HTTP client successfully";
                        return result;
                    }
                }
                else
                {
                    // Create internal HttpClient if none provided
                    using (var internalHttpClient = new HttpClient())
                    {
                        var httpResult = await GetWalletFromHttpClientByEmailAsync(internalHttpClient, providerType, email);
                        if (!string.IsNullOrEmpty(httpResult))
                        {
                            result.Result = httpResult;
                            result.IsError = false;
                            result.Message = "Wallet address retrieved from HTTP client successfully";
                            return result;
                        }
                    }
                }

                // If all methods fail
                OASISErrorHandling.HandleError(ref result, "Failed to retrieve wallet address from all methods");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet address: {ex.Message}");
            }
            return result;
        }

        #region Private Helper Methods

        /// <summary>
        /// Try to get wallet from WalletManager methods (safest approach)
        /// </summary>
        private static async Task<OASISResult<string>> GetWalletFromWalletManagerAsync(WalletManager walletManager, ProviderType providerType, Guid avatarId)
        {
            var result = new OASISResult<string>();
            try
            {
                // Use the correct WalletManager method: LoadProviderWalletsForAvatarByIdAsync
                var walletsResult = await walletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, providerType);
                
                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(providerType))
                {
                    var providerWallets = walletsResult.Result[providerType];
                    if (providerWallets != null && providerWallets.Any())
                    {
                        result.Result = providerWallets.First().WalletAddress;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from WalletManager successfully";
                        return result;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "No wallet found for the specified provider type");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet from WalletManager: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Try to get wallet from Avatar.ProviderWallets (fallback)
        /// </summary>
        private static async Task<OASISResult<string>> GetWalletFromAvatarProviderWalletsAsync(WalletManager walletManager, ProviderType providerType, Guid avatarId)
        {
            var result = new OASISResult<string>();
            try
            {
                // Use AvatarManager.Instance to load avatar information
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var avatar = avatarResult.Result;
                    var wallets = avatar.ProviderWallets?.ContainsKey(providerType) == true ? avatar.ProviderWallets[providerType] : null;
                    if (wallets != null && wallets.Any())
                    {
                        result.Result = wallets.First().WalletAddress;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                        return result;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "No wallet found in Avatar.ProviderWallets for the specified provider type");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet from Avatar.ProviderWallets: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Try to get wallet from HTTP client (final fallback)
        /// </summary>
        private static async Task<string> GetWalletFromHttpClientAsync(HttpClient httpClient, ProviderType providerType, Guid avatarId)
        {
            try
            {
                var walletApiUrl = "https://api.oasis.network/wallet/avatar/{avatarId}/wallets";
                var requestUrl = walletApiUrl.Replace("{avatarId}", avatarId.ToString());
                
                var httpResponse = await httpClient.GetAsync(requestUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var walletData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (walletData.TryGetProperty("result", out var result) && 
                        result.TryGetProperty("value", out var value))
                    {
                        // Look for provider-specific wallet
                        var providerName = providerType.ToString();
                        if (value.TryGetProperty(providerName, out var providerWallets))
                        {
                            var wallets = providerWallets.EnumerateArray();
                            if (wallets.Any())
                            {
                                var firstWallet = wallets.First();
                                if (firstWallet.TryGetProperty("address", out var address))
                                {
                                    return address.GetString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return empty string if query fails
            }
            return "";
        }

        /// <summary>
        /// Try to get wallet from WalletManager methods by username
        /// </summary>
        private static async Task<string> GetWalletFromWalletManagerByUsernameAsync(WalletManager walletManager, ProviderType providerType, string username)
        {
            try
            {
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username);
                if (avatarResult != null && avatarResult.IsError == false && avatarResult.Result != null)
                {
                    var avatar = avatarResult.Result;
                    var wallets = avatar.ProviderWallets?.ContainsKey(providerType) == true ? avatar.ProviderWallets[providerType] : null;
                    if (wallets != null && wallets.Any())
                    {
                        return wallets.First().WalletAddress;
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Try to get wallet from Avatar.ProviderWallets by username
        /// </summary>
        private static async Task<OASISResult<string>> GetWalletFromAvatarProviderWalletsByUsernameAsync(WalletManager walletManager, ProviderType providerType, string username)
        {
            var result = new OASISResult<string>();
            try
            {
                // Use AvatarManager.Instance to load avatar information
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var avatar = avatarResult.Result;
                    var wallets = avatar.ProviderWallets?.ContainsKey(providerType) == true ? avatar.ProviderWallets[providerType] : null;
                    if (wallets != null && wallets.Any())
                    {
                        result.Result = wallets.First().WalletAddress;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                        return result;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "No wallet found in Avatar.ProviderWallets for the specified provider type");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet from Avatar.ProviderWallets: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Try to get wallet from HTTP client by username
        /// </summary>
        private static async Task<string> GetWalletFromHttpClientByUsernameAsync(HttpClient httpClient, ProviderType providerType, string username)
        {
            try
            {
                var walletApiUrl = "https://api.oasis.network/wallet/avatar/username/{username}/wallets";
                var requestUrl = walletApiUrl.Replace("{username}", username);
                
                var httpResponse = await httpClient.GetAsync(requestUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var walletData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (walletData.TryGetProperty("result", out var result) && 
                        result.TryGetProperty("value", out var value))
                    {
                        var providerName = providerType.ToString();
                        if (value.TryGetProperty(providerName, out var providerWallets))
                        {
                            var wallets = providerWallets.EnumerateArray();
                            if (wallets.Any())
                            {
                                var firstWallet = wallets.First();
                                if (firstWallet.TryGetProperty("address", out var address))
                                {
                                    return address.GetString();
                                }
                            }
                        }
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Try to get wallet from WalletManager methods by email
        /// </summary>
        private static async Task<string> GetWalletFromWalletManagerByEmailAsync(WalletManager walletManager, ProviderType providerType, string email)
        {
            try
            {
                var avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email);
                if (avatarResult != null && avatarResult.IsError == false && avatarResult.Result != null)
                {
                    var avatar = avatarResult.Result;
                    var wallets = avatar.ProviderWallets?.ContainsKey(providerType) == true ? avatar.ProviderWallets[providerType] : null;
                    if (wallets != null && wallets.Any())
                    {
                        return wallets.First().WalletAddress;
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Try to get wallet from Avatar.ProviderWallets by email
        /// </summary>
        private static async Task<OASISResult<string>> GetWalletFromAvatarProviderWalletsByEmailAsync(WalletManager walletManager, ProviderType providerType, string email)
        {
            var result = new OASISResult<string>();
            try
            {
                // Use AvatarManager.Instance to load avatar information
                var avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var avatar = avatarResult.Result;
                    var wallets = avatar.ProviderWallets?.ContainsKey(providerType) == true ? avatar.ProviderWallets[providerType] : null;
                    if (wallets != null && wallets.Any())
                    {
                        result.Result = wallets.First().WalletAddress;
                        result.IsError = false;
                        result.Message = "Wallet address retrieved from Avatar.ProviderWallets successfully";
                        return result;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "No wallet found in Avatar.ProviderWallets for the specified provider type");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving wallet from Avatar.ProviderWallets: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Try to get wallet from HTTP client by email
        /// </summary>
        private static async Task<string> GetWalletFromHttpClientByEmailAsync(HttpClient httpClient, ProviderType providerType, string email)
        {
            try
            {
                var walletApiUrl = "https://api.oasis.network/wallet/avatar/email/{email}/wallets";
                var requestUrl = walletApiUrl.Replace("{email}", email);
                
                var httpResponse = await httpClient.GetAsync(requestUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var walletData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (walletData.TryGetProperty("result", out var result) && 
                        result.TryGetProperty("value", out var value))
                    {
                        var providerName = providerType.ToString();
                        if (value.TryGetProperty(providerName, out var providerWallets))
                        {
                            var wallets = providerWallets.EnumerateArray();
                            if (wallets.Any())
                            {
                                var firstWallet = wallets.First();
                                if (firstWallet.TryGetProperty("address", out var address))
                                {
                                    return address.GetString();
                                }
                            }
                        }
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        #endregion
    }
}
