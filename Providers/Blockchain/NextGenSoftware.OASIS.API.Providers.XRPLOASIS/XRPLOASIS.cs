using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS;

/// <summary>
/// XRP Ledger (XRPL) provider for OASIS
/// Stores Avatar and Holon data in XRPL transaction memos
/// </summary>
public sealed class XRPLOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _rpcEndpoint;
    private readonly string _archiveAccount;
    private bool _isActivated;

    public XRPLOASIS(
        string rpcEndpoint = "https://s1.ripple.com:51234",
        string archiveAccount = "rXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX")
    {
        ProviderName = "XRPLOASIS";
        ProviderDescription = "XRP Ledger provider for OASIS - stores data in transaction memos";
        ProviderType = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.Storage));

        _rpcEndpoint = rpcEndpoint;
        _archiveAccount = archiveAccount;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_rpcEndpoint),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result = new OASISResult<bool>();

        try
        {
            if (_isActivated)
            {
                result.Result = true;
                result.Message = "XRPL provider already activated";
                return result;
            }

            // Test connection to XRPL JSON-RPC
            var testRequest = new
            {
                method = "server_info",
                @params = new object[] { }
            };

            var jsonContent = JsonSerializer.Serialize(testRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content);

            if (response.IsSuccessStatusCode)
            {
                _isActivated = true;
                result.Result = true;
                result.Message = "XRPL provider activated successfully";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to connect to XRPL endpoint: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error activating XRPL provider: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().Result;
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        var result = new OASISResult<bool>();

        try
        {
            _isActivated = false;
            _httpClient?.Dispose();
            result.Result = true;
            result.Message = "XRPL provider deactivated successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deactivating XRPL provider: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        return DeActivateProviderAsync().Result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get wallet address from avatar
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS);
            if (walletResult.IsError || string.IsNullOrEmpty(walletResult.Result?.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Avatar wallet address not found for XRPL");
                return result;
            }

            var walletAddress = walletResult.Result.WalletAddress;

            // Serialize avatar to JSON
            var avatarJson = JsonSerializer.Serialize(avatar);
            var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);
            var memoData = Convert.ToHexString(avatarBytes).ToLower();

            // Create Payment transaction with memo
            var transaction = new
            {
                TransactionType = "Payment",
                Account = walletAddress,
                Destination = _archiveAccount,
                Amount = "0",
                Memos = new[]
                {
                    new
                    {
                        Memo = new
                        {
                            MemoType = Convert.ToHexString(Encoding.UTF8.GetBytes("OASIS_AVATAR")).ToLower(),
                            MemoData = memoData
                        }
                    }
                }
            };

            // Submit transaction
            var submitResult = await SignAndSubmitTransactionAsync(transaction);
            if (submitResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit XRPL transaction: {submitResult.Message}");
                return result;
            }

            avatar.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS] = submitResult.Result;
            result.Result = avatar;
            result.Message = "Avatar saved to XRPL successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving avatar to XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get transactions from archive account
            var transactions = await GetArchiveTransactionsAsync();
            if (transactions.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get XRPL transactions: {transactions.Message}");
                return result;
            }

            // Search for avatar in memos
            foreach (var tx in transactions.Result)
            {
                var avatar = LoadAvatarFromTransaction(tx, id);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.Message = "Avatar loaded from XRPL successfully";
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found in XRPL");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar from XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            var transactions = await GetArchiveTransactionsAsync();
            if (transactions.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get XRPL transactions: {transactions.Message}");
                return result;
            }

            foreach (var tx in transactions.Result)
            {
                var avatar = LoadAvatarFromTransaction(tx, email: avatarEmail);
                if (avatar != null && avatar.Email == avatarEmail)
                {
                    result.Result = avatar;
                    result.Message = "Avatar loaded from XRPL by email successfully";
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmail} not found in XRPL");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            var transactions = await GetArchiveTransactionsAsync();
            if (transactions.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get XRPL transactions: {transactions.Message}");
                return result;
            }

            foreach (var tx in transactions.Result)
            {
                var avatar = LoadAvatarFromTransaction(tx, username: avatarUsername);
                if (avatar != null && avatar.Username == avatarUsername)
                {
                    result.Result = avatar;
                    result.Message = "Avatar loaded from XRPL by username successfully";
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in XRPL");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get wallet address from holon metadata or parent avatar
            string walletAddress = null;
            if (holon.MetaData != null && holon.MetaData.ContainsKey("avatarId"))
            {
                var avatarId = Guid.Parse(holon.MetaData["avatarId"].ToString());
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS);
                if (!walletResult.IsError && !string.IsNullOrEmpty(walletResult.Result?.WalletAddress))
                {
                    walletAddress = walletResult.Result.WalletAddress;
                }
            }

            if (string.IsNullOrEmpty(walletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address not found for holon");
                return result;
            }

            // Serialize holon to JSON
            var holonJson = JsonSerializer.Serialize(holon);
            var holonBytes = Encoding.UTF8.GetBytes(holonJson);
            var memoData = Convert.ToHexString(holonBytes).ToLower();

            // Create Payment transaction with memo
            var transaction = new
            {
                TransactionType = "Payment",
                Account = walletAddress,
                Destination = _archiveAccount,
                Amount = "0",
                Memos = new[]
                {
                    new
                    {
                        Memo = new
                        {
                            MemoType = Convert.ToHexString(Encoding.UTF8.GetBytes("OASIS_HOLON")).ToLower(),
                            MemoData = memoData
                        }
                    }
                }
            };

            // Submit transaction
            var submitResult = await SignAndSubmitTransactionAsync(transaction);
            if (submitResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit XRPL transaction: {submitResult.Message}");
                return result;
            }

            holon.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS] = submitResult.Result;
            result.Result = holon;
            result.Message = "Holon saved to XRPL successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holon to XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();

        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate XRPL provider: {activateResult.Message}");
                    return result;
                }
            }

            var transactions = await GetArchiveTransactionsAsync();
            if (transactions.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get XRPL transactions: {transactions.Message}");
                return result;
            }

            foreach (var tx in transactions.Result)
            {
                var holon = TryParseHolonFromTransaction(tx, id);
                if (holon != null)
                {
                    result.Result = holon;
                    result.Message = "Holon loaded from XRPL successfully";
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in XRPL");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon from XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) { var r = new OASISResult<ISearchResults>(); OASISErrorHandling.HandleError(ref r, "SearchAsync is not supported by XRPLOASIS."); return Task.FromResult(r); }

    #region Helper Methods

    private async Task<OASISResult<string>> SignAndSubmitTransactionAsync(object transaction)
    {
        var result = new OASISResult<string>();

        try
        {
            // In production, this would use XRPL SDK to sign the transaction
            // For now, we'll submit unsigned (XRPL node will auto-fill and sign if configured)
            var request = new
            {
                method = "submit",
                @params = new[]
                {
                    new
                    {
                        tx_json = transaction,
                        secret = "" // In production, use wallet manager to get secret
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                if (responseJson.RootElement.TryGetProperty("result", out var resultEl) &&
                    resultEl.TryGetProperty("tx_json", out var txJson) &&
                    txJson.TryGetProperty("hash", out var hash))
                {
                    result.Result = hash.GetString();
                    result.Message = "Transaction submitted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse transaction hash from XRPL response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"XRPL submit failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error submitting XRPL transaction: {ex.Message}", ex);
        }

        return result;
    }

    private async Task<OASISResult<List<JsonElement>>> GetArchiveTransactionsAsync()
    {
        var result = new OASISResult<List<JsonElement>>();

        try
        {
            var request = new
            {
                method = "account_tx",
                @params = new[]
                {
                    new
                    {
                        account = _archiveAccount,
                        ledger_index_min = -1,
                        ledger_index_max = -1,
                        limit = 1000
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                var transactions = new List<JsonElement>();
                if (responseJson.RootElement.TryGetProperty("result", out var resultEl) &&
                    resultEl.TryGetProperty("transactions", out var txs))
                {
                    foreach (var tx in txs.EnumerateArray())
                    {
                        if (tx.TryGetProperty("tx", out var txData))
                        {
                            transactions.Add(txData);
                        }
                    }
                }

                result.Result = transactions;
                result.Message = $"Retrieved {transactions.Count} transactions";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"XRPL account_tx failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting XRPL transactions: {ex.Message}", ex);
        }

        return result;
    }

    private IAvatar LoadAvatarFromTransaction(JsonElement tx, Guid? id = null, string email = null, string username = null)
    {
        try
        {
            if (!tx.TryGetProperty("Memos", out var memos))
                return null;

            foreach (var memo in memos.EnumerateArray())
            {
                if (!memo.TryGetProperty("Memo", out var memoData))
                    continue;

                if (!memoData.TryGetProperty("MemoType", out var memoType) ||
                    !memoData.TryGetProperty("MemoData", out var memoDataHex))
                    continue;

                var memoTypeStr = Encoding.UTF8.GetString(Convert.FromHexString(memoType.GetString()));
                if (memoTypeStr != "OASIS_AVATAR")
                    continue;

                var jsonBytes = Convert.FromHexString(memoDataHex.GetString());
                var json = Encoding.UTF8.GetString(jsonBytes);
                var avatar = JsonSerializer.Deserialize<Avatar>(json);

                if (avatar != null)
                {
                    if (id.HasValue && avatar.Id == id.Value)
                        return avatar;
                    if (!string.IsNullOrEmpty(email) && avatar.Email == email)
                        return avatar;
                    if (!string.IsNullOrEmpty(username) && avatar.Username == username)
                        return avatar;
                    if (!id.HasValue && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(username))
                        return avatar;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private IHolon TryParseHolonFromTransaction(JsonElement tx, Guid? id = null)
    {
        try
        {
            if (!tx.TryGetProperty("Memos", out var memos))
                return null;

            foreach (var memo in memos.EnumerateArray())
            {
                if (!memo.TryGetProperty("Memo", out var memoData))
                    continue;

                if (!memoData.TryGetProperty("MemoType", out var memoType) ||
                    !memoData.TryGetProperty("MemoData", out var memoDataHex))
                    continue;

                var memoTypeStr = Encoding.UTF8.GetString(Convert.FromHexString(memoType.GetString()));
                if (memoTypeStr != "OASIS_HOLON")
                    continue;

                var jsonBytes = Convert.FromHexString(memoDataHex.GetString());
                var json = Encoding.UTF8.GetString(jsonBytes);
                var holon = JsonSerializer.Deserialize<Holon>(json);

                if (holon != null)
                {
                    if (id.HasValue && holon.Id == id.Value)
                        return holon;
                    if (!id.HasValue)
                        return holon;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    #endregion

    #region Unsupported storage/search members — XRPLOASIS is a transaction layer, not a general storage provider

    private static OASISResult<T> NotSupported<T>(string method)
    {
        var r = new OASISResult<T>();
        OASISErrorHandling.HandleError(ref r, $"{method} is not supported by XRPLOASIS. Use a full OASIS storage provider (e.g. MongoDBOASIS) for avatar/holon persistence.");
        return r;
    }
    private static Task<OASISResult<T>> NotSupportedAsync<T>(string method) => Task.FromResult(NotSupported<T>(method));

    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => NotSupportedAsync<IEnumerable<IAvatar>>(nameof(LoadAllAvatarsAsync));
    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => NotSupported<IEnumerable<IAvatar>>(nameof(LoadAllAvatars));
    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => NotSupported<IAvatar>(nameof(LoadAvatar));
    public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => NotSupportedAsync<IAvatar>(nameof(LoadAvatarByProviderKeyAsync));
    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NotSupported<IAvatar>(nameof(LoadAvatarByProviderKey));
    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => NotSupported<IAvatar>(nameof(LoadAvatarByUsername));
    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => NotSupported<IAvatar>(nameof(LoadAvatarByEmail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => NotSupportedAsync<IAvatarDetail>(nameof(LoadAvatarDetailAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => NotSupported<IAvatarDetail>(nameof(LoadAvatarDetail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => NotSupportedAsync<IAvatarDetail>(nameof(LoadAvatarDetailByEmailAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => NotSupported<IAvatarDetail>(nameof(LoadAvatarDetailByEmail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => NotSupportedAsync<IAvatarDetail>(nameof(LoadAvatarDetailByUsernameAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => NotSupported<IAvatarDetail>(nameof(LoadAvatarDetailByUsername));
    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NotSupported<IEnumerable<IAvatarDetail>>(nameof(LoadAllAvatarDetails));
    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => NotSupportedAsync<IEnumerable<IAvatarDetail>>(nameof(LoadAllAvatarDetailsAsync));
    public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => NotSupported<IAvatar>(nameof(SaveAvatar));
    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => NotSupported<IAvatarDetail>(nameof(SaveAvatarDetail));
    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar) => NotSupportedAsync<IAvatarDetail>(nameof(SaveAvatarDetailAsync));
    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => NotSupportedAsync<bool>(nameof(DeleteAvatarAsync));
    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NotSupported<bool>(nameof(DeleteAvatar));
    public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => NotSupportedAsync<bool>(nameof(DeleteAvatarAsync));
    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NotSupported<bool>(nameof(DeleteAvatar));
    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NotSupported<bool>(nameof(DeleteAvatarByEmail));
    public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => NotSupportedAsync<bool>(nameof(DeleteAvatarByEmailAsync));
    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => NotSupported<bool>(nameof(DeleteAvatarByUsername));
    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => NotSupportedAsync<bool>(nameof(DeleteAvatarByUsernameAsync));
    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => NotSupported<ISearchResults>(nameof(Search));
    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IHolon>(nameof(LoadHolon));
    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IHolon>(nameof(LoadHolon));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(LoadHolonsForParent));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsForParentAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(LoadHolonsForParent));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsForParentAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaData));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaDataAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaData));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaDataAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(LoadAllHolons));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(LoadAllHolonsAsync));
    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupported<IHolon>(nameof(SaveHolon));
    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupported<IEnumerable<IHolon>>(nameof(SaveHolons));
    public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(SaveHolonsAsync));
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => NotSupportedAsync<IHolon>(nameof(DeleteHolonAsync));
    public override OASISResult<IHolon> DeleteHolon(Guid id) => NotSupported<IHolon>(nameof(DeleteHolon));
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => NotSupportedAsync<IHolon>(nameof(DeleteHolonAsync));
    public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotSupported<IHolon>(nameof(DeleteHolon));
    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => NotSupportedAsync<bool>(nameof(ImportAsync));
    public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotSupported<bool>(nameof(Import));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByIdAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarById));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsername));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsernameAsync));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmailAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmail));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => NotSupportedAsync<IEnumerable<IHolon>>(nameof(ExportAllAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotSupported<IEnumerable<IHolon>>(nameof(ExportAll));
    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedAsync<IHolon>(nameof(LoadHolonAsync));
    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters) => NotSupported<IEnumerable<IAvatar>>(nameof(GetAvatarsNearMe));
    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type) => NotSupported<IEnumerable<IHolon>>(nameof(GetHolonsNearMe));
    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(SendToken));
    public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(SendTokenAsync));
    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(MintToken));
    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(MintTokenAsync));
    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(BurnToken));
    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(BurnTokenAsync));
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(LockToken));
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(LockTokenAsync));
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(UnlockToken));
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(UnlockTokenAsync));
    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) => NotSupported<double>(nameof(GetBalance));
    public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request) => NotSupportedAsync<double>(nameof(GetBalanceAsync));
    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) => NotSupported<IList<IWalletTransaction>>(nameof(GetTransactions));
    public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request) => NotSupportedAsync<IList<IWalletTransaction>>(nameof(GetTransactionsAsync));
    public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => NotSupported<IKeyPairAndWallet>(nameof(GenerateKeyPair));
    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync() => NotSupportedAsync<IKeyPairAndWallet>(nameof(GenerateKeyPairAsync));
    public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default) => NotSupportedAsync<(string, string, string)>(nameof(CreateAccountAsync));
    public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default) => NotSupportedAsync<(string, string)>(nameof(RestoreAccountAsync));
    public Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey) => NotSupportedAsync<BridgeTransactionResponse>(nameof(WithdrawAsync));
    public Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress) => NotSupportedAsync<BridgeTransactionResponse>(nameof(DepositAsync));
    public Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default) => NotSupportedAsync<BridgeTransactionStatus>(nameof(GetTransactionStatusAsync));

    #endregion
}

