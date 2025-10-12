using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public class Web3CoreOASISBaseProvider(string hostUri, string chainPrivateKey, string contractAddress) :
    OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private readonly string _hostURI = hostUri;
    private readonly string _chainPrivateKey = chainPrivateKey;
    private readonly string _contractAddress = contractAddress;

    private Web3CoreOASIS? _web3CoreOASIS;

    public bool IsVersionControlEnabled { get; set; }

    public override Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = ActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        OASISResult<bool> result = new();

        try
        {
            if (_hostURI is { Length: > 0 } &&
                _chainPrivateKey is { Length: > 0 })
            {
                _web3CoreOASIS = new(_chainPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi);
                this.IsProviderActivated = true;
            }
        }
        catch (Exception ex)
        {
            this.IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in {this.ProviderName} -> Web3CoreOASIS Provider. Reason: {ex}");
        }

        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = DeActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _web3CoreOASIS = null;
        IsProviderActivated = false;

        return new(value: true);
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        => DeleteAvatarAsync(id, softDelete).Result;

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in Web3CoreOASIS while deleting avatar. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());
            bool isDeleted = await _web3CoreOASIS.DeleteAvatarAsync((uint)avatarEntityId);

            result.Result = isDeleted;
            result.IsError = false;
            result.IsSaved = isDeleted;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in Web3CoreOASIS while deleting avatar by provider key. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await _web3CoreOASIS.DeleteAvatarAsync(providerKey, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarByEmailAsync method in Web3CoreOASIS while deleting avatar by email. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await _web3CoreOASIS.DeleteAvatarByEmailAsync(avatarEmail, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarByUsernameAsync method in Web3CoreOASIS while deleting avatar by username. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await _web3CoreOASIS.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in DeleteHolonAsync method in Web3CoreOASIS while deleting holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            OASISResult<IHolon> holonToDeleteResult = await LoadHolonAsync(id);
            if (holonToDeleteResult.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, holonToDeleteResult.Message), holonToDeleteResult.Exception);
                return result;
            }

            int holonEntityId = HashUtility.GetNumericHash(id.ToString());
            bool isDeleted = await _web3CoreOASIS.DeleteHolonAsync((uint)holonEntityId);

            result.Result = holonToDeleteResult.Result;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        return DeleteHolonByProviderKeyAsync(providerKey);
    }

    public async Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load holon by provider key first
            var holonResult = await LoadHolonAsync(providerKey);
            if (holonResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {holonResult.Message}");
                return result;
            }

            if (holonResult.Result != null)
            {
                // Delete holon by ID
                var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                    return result;
                }

                result.Result = holonResult.Result;
                result.IsError = false;
                result.Message = "Holon deleted successfully by provider key from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        return LoadAllHolonsAsync(version);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Export all holons for avatar from Web3Core
            var holonsResult = await LoadHolonsForParentAsync(avatarId);
            if (holonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                return result;
            }

            result.Result = holonsResult.Result;
            result.IsError = false;
            result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        throw new System.NotImplementedException();
    }

    public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
    {
        throw new System.NotImplementedException();
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in LoadAvatarAsync method in Web3CoreOASIS while loading an avatar. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS avatarInfo = await _web3CoreOASIS.GetAvatarByIdAsync((uint)avatarEntityId);

            result.Result = JsonSerializer.Deserialize<Avatar>(avatarInfo.Info)
                ?? throw new InvalidOperationException();
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in LoadAvatarDetailAsync method in Web3CoreOASIS while loading an avatar detail. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS detailInfo = await _web3CoreOASIS.GetAvatarDetailByIdAsync((uint)avatarDetailEntityId);

            result.IsError = false;
            result.IsLoaded = true;
            result.Result = JsonSerializer.Deserialize<AvatarDetail>(detailInfo.Info)
                ?? throw new InvalidOperationException();
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new Exception();
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in Web3CoreOASIS while loading holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int holonEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS holonInfo = await _web3CoreOASIS.GetHolonByIdAsync((uint)holonEntityId);

            result.Result = JsonSerializer.Deserialize<Holon>(holonInfo.Info)
                ?? throw new InvalidOperationException();
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public bool NativeCodeGenesis(ICelestialBody celestialBody)
    {
        throw new System.NotImplementedException();
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        OASISResult<IAvatar> result = new();

        try
        {
            Task<OASISResult<IAvatar>> saveAvatarTask = SaveAvatarAsync(avatar);
            saveAvatarTask.Wait();

            if (saveAvatarTask.IsCompletedSuccessfully)
                result = saveAvatarTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveAvatarTask.Exception?.Message ?? string.Empty, saveAvatarTask.Exception ?? new());
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in SaveAvatarAsync method in Web3CoreOASIS while saving avatar. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string avatarInfo = JsonSerializer.Serialize(avatar);
            int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
            string avatarId = avatar.AvatarId.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)avatarEntityId, Encoding.UTF8.GetBytes(avatarId), Encoding.UTF8.GetBytes(avatarInfo));

            result.Result = avatar;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
    {
        ArgumentNullException.ThrowIfNull(avatarDetail);

        OASISResult<IAvatarDetail> result = new();

        try
        {
            Task<OASISResult<IAvatarDetail>> saveAvatarDetailTask = SaveAvatarDetailAsync(avatarDetail);
            saveAvatarDetailTask.Wait();

            if (saveAvatarDetailTask.IsCompletedSuccessfully)
                result = saveAvatarDetailTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveAvatarDetailTask.Exception?.Message ?? string.Empty, saveAvatarDetailTask.Exception ?? new());
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
    {
        ArgumentNullException.ThrowIfNull(avatarDetail);

        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in SaveAvatarDetailAsync method in Web3CoreOASIS while saving and avatar detail. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string avatarDetailInfo = JsonSerializer.Serialize(avatarDetail);
            int avatarDetailEntityId = HashUtility.GetNumericHash(avatarDetail.Id.ToString());
            string avatarDetailId = avatarDetail.Id.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)avatarDetailEntityId, Encoding.UTF8.GetBytes(avatarDetailId), Encoding.UTF8.GetBytes(avatarDetailInfo));

            result.Result = avatarDetail;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        throw new NotImplementedException();
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holon);

        OASISResult<IHolon> result = new();
        string errorMessage = "Error in SaveHolonAsync method in Web3CoreOASIS while saving holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string holonInfo = JsonSerializer.Serialize(holon);
            int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
            string holonId = holon.Id.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)holonEntityId, Encoding.UTF8.GetBytes(holonId), Encoding.UTF8.GetBytes(holonInfo));

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holons);

        OASISResult<IEnumerable<IHolon>> result = new();
        string errorMessage = "Error in SaveHolonsAsync method in Web3CoreOASIS while saving holons. Reason: ";

        try
        {
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveHolonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, saveHolonResult.DetailedMessage));

                    if (!continueOnError) break;
                }
            }

            result.Result = holons;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            TransactionReceipt transactionResult = await _web3CoreOASIS.SendTransactionAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Web3CoreOASIS transaction performing failed! " +
                                 $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                 $"Reason: {transactionResult.Logs}");
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        => SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        OASISResult<IProviderWallet> senderAvatarPrivateKeysResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
        OASISResult<IProviderWallet> receiverAvatarAddressesResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionByUsernameAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
        OASISResult<List<string>> receiverAvatarAddressesResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    private async Task<OASISResult<ITransactionRespone>> SendTransactionBaseAsync(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionBaseAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            Web3CoreOASIS web3CoreInstance = new(senderAccountPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi);
            TransactionReceipt receipt = await web3CoreInstance.SendTransactionAsync(receiverAccountAddress, amount);

            if (receipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receipt.Logs));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transaction)
    {
        OASISResult<INFTTransactionRespone> result = new();
        string errorMessage = "Error in SendNFTAsync method in Web3CoreOASIS while sending nft. Reason: ";

        if (transaction.Amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string transactionHash = await _web3CoreOASIS.SendNFTAsync(
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                transaction.FromProvider.Value.ToString(),
                transaction.ToProvider.Value.ToString(),
                amount: new BigInteger(transaction.Amount),
                transaction.MemoText
            );

            INFTTransactionRespone response = new NFTTransactionRespone
            {
                OASISNFT = new OASISNFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Data), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        => MintNFTAsync(transation).Result;

    public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transaction)
    {
        OASISResult<INFTTransactionRespone> result = new();
        string errorMessage = "Error in MintNFTAsync method in Web3CoreOASIS while minting nft. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string metadataJson = JsonSerializer.Serialize(transaction);
            //string transactionHash = await _web3CoreOASIS.MintAsync(transaction.MintWalletAddress, metadataJson);
            string transactionHash = await _web3CoreOASIS.MintAsync(transaction.SendToAddressAfterMinting, metadataJson);

            INFTTransactionRespone response = new NFTTransactionRespone
            {
                OASISNFT = new OASISNFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Message), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        throw new NotImplementedException();
    }
}

file static class Web3CoreOASISBaseProviderHelper
{
    public const string ProviderNotActivatedError = "Provider is not activated. Please activate provider and try again.";
    public const string InvalidAmountError = "Amount value must be greater than zero.";
    public const string Abi = @"
[
    {
      ""inputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""constructor""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721IncorrectOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721InsufficientApproval"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""approver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidApprover"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOperator"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""receiver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidReceiver"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidSender"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721NonexistentToken"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableInvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""account"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableUnauthorizedAccount"",
      ""type"": ""error""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""approved"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Approval"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""indexed"": false,
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""ApprovalForAll"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""previousOwner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnershipTransferred"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Transfer"",
      ""type"": ""event""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""approve"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""balanceOf"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""holonId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createHolon"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getApproved"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarDetailById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarDetailsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getHolonById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getHolonsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getTransferHistory"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""address"",
              ""name"": ""fromWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""address"",
              ""name"": ""toWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""fromProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""toProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""uint256"",
              ""name"": ""amount"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""memoText"",
              ""type"": ""string""
            }
          ],
          ""internalType"": ""struct NFTTransfer[]"",
          ""name"": """",
          ""type"": ""tuple[]""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""isApprovedForAll"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""name"": ""mint"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""name"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""nextTokenId"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftMetadata"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftTransfers"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""owner"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ownerOf"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""renounceOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""data"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""name"": ""sendNFT"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""setApprovalForAll"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""bytes4"",
          ""name"": ""interfaceId"",
          ""type"": ""bytes4""
        }
      ],
      ""name"": ""supportsInterface"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""symbol"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""tokenURI"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""transferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""transferOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    }
  ]
";
}
