using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

/// <summary>
/// Base OASIS Provider - extends Web3CoreOASISBaseProvider for Base blockchain (Coinbase's Layer 2)
/// All storage, NFT, and wallet logic is handled by Web3CoreOASISBaseProvider.
/// This class adds Base-specific SERV token integration.
/// </summary>
public sealed class BaseOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    private readonly string _hostURI;
    private Services.SERVService _servService;
    private Services.SERVService SERVService
    {
        get
        {
            if (_servService == null && !string.IsNullOrWhiteSpace(_hostURI))
            {
                _servService = new Services.SERVService(_hostURI);
            }
            return _servService;
        }
    }

    public BaseOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        _hostURI = hostUri;
        ProviderName = "BaseOASIS";
        ProviderDescription = "Base Provider (Coinbase Layer 2)";
        ProviderType = new(Core.Enums.ProviderType.BaseOASIS);
        ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }

    #region SERV Token Methods

    /// <summary>
    /// Get SERV token balance for a Base address
    /// </summary>
    public async Task<OASISResult<decimal>> GetSERVBalanceAsync(string address)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            return await SERVService.GetBalanceAsync(address);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting SERV balance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Get SERV token balance for an avatar
    /// </summary>
    public async Task<OASISResult<decimal>> GetSERVBalanceForAvatarAsync(Guid avatarId)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            // Get avatar's Base wallet
            var walletsResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, Core.Enums.ProviderType.BaseOASIS, Core.Enums.ProviderType.Default);
            if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(Core.Enums.ProviderType.BaseOASIS) || walletsResult.Result[Core.Enums.ProviderType.BaseOASIS].Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "No Base wallet found for avatar");
                return result;
            }

            var walletAddress = walletsResult.Result[Core.Enums.ProviderType.BaseOASIS][0].WalletAddress;
            return await SERVService.GetBalanceAsync(walletAddress);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting SERV balance for avatar: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Transfer SERV tokens from one address to another
    /// </summary>
    public async Task<OASISResult<string>> TransferSERVAsync(
        string fromPrivateKey,
        string toAddress,
        decimal amount)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            return await SERVService.TransferAsync(fromPrivateKey, toAddress, amount);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error transferring SERV: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Transfer SERV tokens between avatars
    /// </summary>
    public async Task<OASISResult<string>> TransferSERVBetweenAvatarsAsync(
        Guid fromAvatarId,
        Guid toAvatarId,
        decimal amount)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            // Get sender's private key
            var senderPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.BaseOASIS);
            if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "No private key found for sender");
                return result;
            }

            // Get recipient's wallet address
            var toWalletResult = await Core.Helpers.WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.BaseOASIS, toAvatarId);
            if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "No Base wallet found for recipient");
                return result;
            }

            return await SERVService.TransferAsync(senderPrivateKeysResult.Result[0], toWalletResult.Result, amount);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error transferring SERV between avatars: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Approve SERV token spending for a spender address
    /// </summary>
    public async Task<OASISResult<string>> ApproveSERVAsync(
        string ownerPrivateKey,
        string spenderAddress,
        decimal amount)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            return await SERVService.ApproveAsync(ownerPrivateKey, spenderAddress, amount);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error approving SERV: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Get SERV token allowance for a spender
    /// </summary>
    public async Task<OASISResult<decimal>> GetSERVAllowanceAsync(
        string ownerAddress,
        string spenderAddress)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || SERVService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated or RPC URL not configured");
                return result;
            }

            return await SERVService.GetAllowanceAsync(ownerAddress, spenderAddress);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting SERV allowance: {ex.Message}", ex);
            return result;
        }
    }

    #endregion
}
