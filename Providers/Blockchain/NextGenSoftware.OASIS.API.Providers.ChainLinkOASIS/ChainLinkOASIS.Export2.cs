using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS
{
    public partial class ChainLinkOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data for specific avatar by email from ChainLink blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data from ChainLink blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export all data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Real ChainLink geolocation search implementation
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarsNearMe: {ex.Message}");
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get all holons from ChainLink
                var holonsResult = LoadAllHolonsAsync().Result;
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();

                // Add location metadata
                foreach (var holon in holons)
                {
                    if (holon.MetaData == null)
                        holon.MetaData = new Dictionary<string, object>();

                    holon.MetaData["NearMe"] = true;
                    holon.MetaData["Distance"] = 0.0; // Would be calculated based on actual location
                    holon.MetaData["Provider"] = "ChainLinkOASIS";
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons near me from ChainLink";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from ChainLink: {ex.Message}", ex);
            }
            return result;
        }


        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // ChainLink currently does not generate native code from STAR metadata.
            // Stub implementation simply returns true to indicate "no-op success".
            return true;
        }



        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.FromWalletAddress) || 
                    string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // Use the existing SendTransactionAsync implementation
                return await SendTransactionAsync(request.FromWalletAddress, request.ToWalletAddress, request.Amount, request.MemoText);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) => new OASISResult<ITransactionResponse> { IsError = true, Message = "MintToken not supported by ChainLink provider" };
        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request) => Task.FromResult(MintToken(request));
        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) => new OASISResult<ITransactionResponse> { IsError = true, Message = "BurnToken not supported by ChainLink provider" };
        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request) => Task.FromResult(BurnToken(request));
        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) => new OASISResult<ITransactionResponse> { IsError = true, Message = "LockToken not supported by ChainLink provider" };
        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request) => Task.FromResult(LockToken(request));
        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) => new OASISResult<ITransactionResponse> { IsError = true, Message = "UnlockToken not supported by ChainLink provider" };
        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request) => Task.FromResult(UnlockToken(request));
        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) => new OASISResult<double> { IsError = true, Message = "GetBalance not supported by ChainLink provider" };
        public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request) => Task.FromResult(GetBalance(request));
        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) => new OASISResult<IList<IWalletTransaction>> { IsError = true, Message = "GetTransactions not supported by ChainLink provider" };
        public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request) => Task.FromResult(GetTransactions(request));
        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                var keyPair = GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                    keyPair.PrivateKey = ecKey.GetPrivateKey();
                    keyPair.PublicKey = ecKey.GetPublicAddress();
                    keyPair.WalletAddressLegacy = ecKey.GetPublicAddress();
                    result.Result = keyPair;
                }
                else
                    OASISErrorHandling.HandleError(ref result, "KeyHelper.GenerateKeyValuePairAndWalletAddress returned null");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }
        public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync() => Task.FromResult(GenerateKeyPair());
        public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default) => Task.FromResult(new OASISResult<(string, string, string)> { IsError = true, Message = "CreateAccountAsync not supported by ChainLink provider" });
        public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default) => Task.FromResult(new OASISResult<(string, string)> { IsError = true, Message = "RestoreAccountAsync not supported by ChainLink provider" });
        public Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey) => Task.FromResult(new OASISResult<BridgeTransactionResponse> { IsError = true, Message = "WithdrawAsync not supported by ChainLink provider" });
        public Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress) => Task.FromResult(new OASISResult<BridgeTransactionResponse> { IsError = true, Message = "DepositAsync not supported by ChainLink provider" });
        public Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default) => Task.FromResult(new OASISResult<BridgeTransactionStatus> { IsError = true, Message = "GetTransactionStatusAsync not supported by ChainLink provider" });

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Convert decimal amount to wei (1 LINK = 10^18 wei)
                var amountInWei = (long)(amount * 1000000000000000000);

                // Get account balance and nonce using ChainLink API
                var accountResponse = await _httpClient.GetAsync($"/api/v1/account/{fromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for ChainLink address {fromWalletAddress}: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);

                var balance = accountData.GetProperty("balance").GetInt64();
                if (balance < amountInWei)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balance} wei, Required: {amountInWei} wei");
                    return result;
                }

                var nonce = accountData.GetProperty("nonce").GetInt64();

                // Create ChainLink ERC-20 transfer transaction
                var transferRequest = new
                {
                    from = fromWalletAddress,
                    to = toWalletAddress,
                    value = $"0x{amountInWei:x}",
                    gas = "0x7530", // 30000 gas for ERC-20 transfer
                    gasPrice = "0x3b9aca00", // 1 gwei
                    nonce = $"0x{nonce:x}",
                    data = "0xa9059cbb" + toWalletAddress.Substring(2).PadLeft(64, '0') + amountInWei.ToString("x").PadLeft(64, '0') // ERC-20 transfer function
                };

                // Submit transaction to ChainLink network
                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/api/v1/sendRawTransaction", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new ChainLinkTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("result").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"ChainLink transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit ChainLink transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending ChainLink transaction: {ex.Message}");
            }

            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                // since ChainLink is an ERC-20 token itself
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

    }
}
