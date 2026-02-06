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
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

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
        ProviderType = new EnumValue<ProviderType>(ProviderType.XRPLOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<ProviderCategory>(ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(ProviderCategory.Storage));

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
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, ProviderType.XRPLOASIS);
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

            avatar.ProviderUniqueStorageKey[ProviderType.XRPLOASIS] = submitResult.Result;
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

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
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
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, ProviderType.XRPLOASIS);
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

            holon.ProviderUniqueStorageKey[ProviderType.XRPLOASIS] = submitResult.Result;
            result.Result = holon;
            result.Message = "Holon saved to XRPL successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holon to XRPL: {ex.Message}", ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, int version = 0)
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

    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
    {
        var result = new OASISResult<ISearchResults>();

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

            var searchResults = new SearchResults();
            var query = searchParams?.SearchQuery?.ToLower() ?? "";

            foreach (var tx in transactions.Result)
            {
                if (searchParams?.SearchType == SearchType.Holon || searchParams?.SearchType == SearchType.All)
                {
                    var holon = TryParseHolonFromTransaction(tx);
                    if (holon != null && (string.IsNullOrEmpty(query) ||
                        (holon.Name?.ToLower().Contains(query) == true) ||
                        (holon.Description?.ToLower().Contains(query) == true)))
                    {
                        searchResults.Holons.Add(holon);
                    }
                }

                if (searchParams?.SearchType == SearchType.Avatar || searchParams?.SearchType == SearchType.All)
                {
                    var avatar = LoadAvatarFromTransaction(tx);
                    if (avatar != null && (string.IsNullOrEmpty(query) ||
                        (avatar.Username?.ToLower().Contains(query) == true) ||
                        (avatar.Email?.ToLower().Contains(query) == true)))
                    {
                        searchResults.Avatars.Add(avatar);
                    }
                }
            }

            searchResults.NumberOfResults = searchResults.Holons.Count + searchResults.Avatars.Count;
            result.Result = searchResults;
            result.Message = $"Search completed: {searchResults.NumberOfResults} results found";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching XRPL: {ex.Message}", ex);
        }

        return result;
    }

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
}

