using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result=new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if(version != 0) throw new NotSupportedException("Solana collection reads currently require latest version (0).");
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var records=await _solanaRepository.GetAllAsync<SolanaAvatarDetailDto>();
            var mapped=new List<IAvatarDetail>();
            foreach(var entry in records)
            {
                var record=entry.Record.GetAvatarDetail();
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=entry.Signature;
                mapped.Add(record);
            }
            result.Result=mapped;
            result.IsLoaded=true;
        }
        catch(Exception cause) { OASISErrorHandling.HandleError(ref result,"Solana finalized collection read failed: "+cause.Message,cause); }
        return result;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        return SaveAvatarAsync(avatar).Result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            string transactionHash;
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarSolanaHash))
            {
                var persistedAvatar = await _solanaRepository.GetAsync<SolanaAvatarDto>(avatarSolanaHash);
                if (persistedAvatar == null || persistedAvatar.Id != avatar.Id)
                    throw new InvalidOperationException("Solana avatar storage reference does not match the requested identity.");
                var solanaAvatarDto = avatar.GetSolanaAvatarDto();
                solanaAvatarDto.Version = persistedAvatar.Version;
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                var solanaAvatarDto = avatar.GetSolanaAvatarDto();
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            else
                OASISErrorHandling.HandleError(ref result,
                    "Error Occured In SolanaOASIS.SaveAvatarAsync. Transaction processing failed!");
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
    {
        return SaveAvatarDetailAsync(avatar).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            string transactionHash;
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarDetailSolanaHash))
            {
                var persistedDetail =
                    await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                if (persistedDetail == null || persistedDetail.Id != avatar.Id)
                    throw new InvalidOperationException("Solana avatar detail storage reference does not match the requested identity.");
                var solanaAvatarDetailDto = avatar.GetSolanaAvatarDetailDto();
                solanaAvatarDetailDto.Version = persistedDetail.Version;
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                var solanaAvatarDetailDto = avatar.GetSolanaAvatarDetailDto();
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            else
                OASISErrorHandling.HandleError(ref result,
                    "Error Occured In SolanaOASIS.SaveAvatarAsync. Transaction processing failed!");
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar from Solana program
            // Real Solana implementation: Load avatar first to get provider key, then delete
            var avatarResult = await LoadAvatarAsync(id, 0);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Avatar not found for deletion: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var providerKey))
            {
                var deleteSuccess = await _solanaRepository.DeleteAsync(providerKey);
                if (deleteSuccess)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from Solana";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete avatar from Solana repository");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar does not have a Solana provider key (transaction hash)");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by email from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by username from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Solana: {ex.Message}", ex);
        }
        return result;
    }



    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public OASISResult<ITransactionResponse> SendTransaction(string fromAddress, string toAddress, decimal amount, string memo)
    {
        return SendTransactionAsync(fromAddress, toAddress, amount, memo).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            var deleteResult = await _solanaRepository.DeleteAsync(providerKey);

            result.IsError = !deleteResult;
            result.IsSaved = deleteResult;
            result.Result = deleteResult;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if(string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("A Solana storage signature is required.",nameof(providerKey));
            if(maxChildDepth<0 || version<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
            if(version!=0 && loadChildrenFromProvider && loadChildren)
                throw new NotSupportedException("Solana historical provider-child graph reads require a versioned index.");
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var solanaHolonDto = await _solanaRepository.GetAsync<SolanaHolonDto>(providerKey);
            if(version!=0 && solanaHolonDto.Version!=version) throw new KeyNotFoundException("Solana signature does not reference the requested record version.");
            var holon=solanaHolonDto.GetHolon();
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=providerKey;
            var graphRecords=new List<IHolon> {holon};
            if(loadChildren && loadChildrenFromProvider)
            {
                var stored=await _solanaRepository.GetAllAsync<SolanaHolonDto>();
                foreach(var entry in stored)
                {
                    if(entry.Record.Id==holon.Id) continue;
                    var child=entry.Record.GetHolon();
                    child.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=entry.Signature;
                    graphRecords.Add(child);
                }
            }
            var project=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(graphRecords,loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider,
                source=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Core.Holons.Holon>(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(source)));
            result.Result=project(holon);
            result.IsLoaded = true;
            result.IsError = false;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }//

    public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
        => FindStoredIdentityAsync(id!=Guid.Empty,()=>LoadAllHolonsAsync(Core.Enums.HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,loadChildrenFromProvider,version),row=>row.Id==id);

}
