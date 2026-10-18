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
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var records=await _solanaRepository.GetAllAsync<SolanaAvatarDetailDto>(version);
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
            var solanaAvatarDto = avatar.GetSolanaAvatarDto();
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarSolanaHash))
            {
                var persistedAvatar = await _solanaRepository.GetAsync<SolanaAvatarDto>(avatarSolanaHash);
                if (persistedAvatar == null || persistedAvatar.Id != avatar.Id)
                    throw new InvalidOperationException("Solana avatar storage reference does not match the requested identity.");
                solanaAvatarDto.Version = persistedAvatar.Version;
                RetainSolanaCreationOrigin(solanaAvatarDto,persistedAvatar);
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;
                PublishSolanaCreationOrigin(avatar,solanaAvatarDto);

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
            if(e is SolanaOriginValidationException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
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
            var solanaAvatarDetailDto = avatar.GetSolanaAvatarDetailDto();
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarDetailSolanaHash))
            {
                var persistedDetail =
                    await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                if (persistedDetail == null || persistedDetail.Id != avatar.Id)
                    throw new InvalidOperationException("Solana avatar detail storage reference does not match the requested identity.");
                solanaAvatarDetailDto.Version = persistedDetail.Version;
                RetainSolanaCreationOrigin(solanaAvatarDetailDto,persistedDetail);
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;
                PublishSolanaCreationOrigin(avatar,solanaAvatarDetailDto);

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
            if(e is SolanaOriginValidationException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        => DeleteSelectedAvatarAsync(()=>LoadAvatarAsync(id),softDelete);

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        => DeleteSelectedAvatarAsync(()=>LoadAvatarByEmailAsync(avatarEmail),softDelete);

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        => DeleteSelectedAvatarAsync(()=>LoadAvatarByUsernameAsync(avatarUsername),softDelete);

    private async Task<OASISResult<bool>> DeleteSelectedAvatarAsync(Func<Task<OASISResult<IAvatar>>> load,bool softDelete)
    {
        var result = new OASISResult<bool>();
        try
        {
            var avatarResult = await load();
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,avatarResult.Message,avatarResult.Exception);
                result.ErrorCode=avatarResult.ErrorCode;
                return result;
            }
            if(avatarResult.Result?.ProviderUniqueStorageKey==null
                || !avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,out var signature)
                || string.IsNullOrWhiteSpace(signature))
                throw new InvalidOperationException("Authenticated avatar has no Solana storage signature for deletion.");
            return await DeleteAvatarAsync(signature,softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting selected Solana avatar: {ex.Message}", ex);
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
            // Immutable signature must authenticate as an avatar before generic tombstoning.
            var avatar=await LoadAvatarByProviderKeyAsync(providerKey);
            if(avatar.IsError)
            {
                OASISErrorHandling.HandleError(ref result,avatar.Message,avatar.Exception);
                result.ErrorCode=avatar.ErrorCode;
                return result;
            }
            if(avatar.Result==null) throw new InvalidOperationException("Solana avatar signature read returned no authenticated avatar.");
            await CreateVerifiedTombstoneAsync<SolanaAvatarDto,IAvatar>(providerKey,
                avatar.Result.Id,avatar.Result.Version,record=>record.GetAvatar());
            result.IsSaved = true;
            result.IsDeleted = true;
            result.DeletedCount = 1;
            result.Result = true;
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
