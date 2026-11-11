using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => FindStoredIdentityAsync<IAvatarDetail>(async () => (await _avatarDetailRepository.ReadAll().ConfigureAwait(false))
                .Select(row => row.GetBaseAvatarDetail()), detail => detail.Email, avatarEmail);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailDto = await _avatarDetailRepository.Read(id).ConfigureAwait(false);
                var avatarDetailEntity = avatarDetailDto?.GetBaseAvatarDetail();
                if (avatarDetailEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar Detail with such ID, not found!";
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.Result = avatarDetailEntity;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => FindStoredIdentityAsync<IAvatarDetail>(async () => (await _avatarDetailRepository.ReadAll().ConfigureAwait(false))
                .Select(row => row.GetBaseAvatarDetail()), detail => detail.Username, avatarUsername);

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var allAvatarDetailsDTOs = _avatarDetailRepository.ReadAll().Result;
                if (allAvatarDetailsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarDetailsDTOs
                        .Select(avatarDetailDto => avatarDetailDto.GetBaseAvatarDetail())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var allAvatarDetailsDTOs = await _avatarDetailRepository.ReadAll();
                if (allAvatarDetailsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarDetailsDTOs
                        .Select(avatarDetailDto => avatarDetailDto.GetBaseAvatarDetail())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = OasisJson.Serialize<object>(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = _avatarRepository.Read(Avatar.Id).Result;
                if (existAvatar != null)
                {
                    _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarInfo
                    }, Avatar.Id).Wait();
                }
                else
                {
                    var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);

                    _avatarRepository.Create(new AvatarDto
                    {
                        Info = avatarInfo,
                        AvatarId = Avatar.Id.ToString(),
                        IsDeleted = false,
                        EntityId = avatarEntityId
                    }).Wait();
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = OasisJson.Serialize<object>(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = await _avatarRepository.Read(Avatar.Id);
                if (existAvatar != null)
                {
                    await _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarInfo
                    }, Avatar.Id);
                }
                else
                {
                    var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    await _avatarRepository.Create(new AvatarDto
                    {
                        Info = avatarInfo,
                        AvatarId = Avatar.Id.ToString(),
                        IsDeleted = false,
                        EntityId = avatarEntityId
                    });
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
            => SaveAvatarDetailAsync(Avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var detail = new AvatarDetailDto
                {
                    Info = OasisJson.Serialize<object>(Avatar),
                    AvatarId = Avatar.Id.ToString(),
                    EntityId = HashUtility.GetNumericHash(Avatar.Id)
                };

                var existingDetail = await _avatarDetailRepository.Read(Avatar.Id).ConfigureAwait(false);
                if (existingDetail == null)
                    await _avatarDetailRepository.Create(detail).ConfigureAwait(false);
                else
                    await _avatarDetailRepository.Update(detail, Avatar.Id).ConfigureAwait(false);

                result.Result = Avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public Task<OASISResult<bool>> HardDeleteAvatarDetailAsync(Guid id)
            => DeleteAvatarDetailAsync(id, softDelete: false);

        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    await _avatarDetailRepository.DeleteSoft(id).ConfigureAwait(false);
                else
                    await _avatarDetailRepository.DeleteHard(id).ConfigureAwait(false);
                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex), ex);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<bool>();
            try
            {
                if (await _avatarRepository.Read(id).ConfigureAwait(false) == null)
                {
                    result.IsError = true;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    result.Message = "EOSIO avatar was not found.";
                    return result;
                }

                if (softDelete)
                    await _avatarRepository.DeleteSoft(id).ConfigureAwait(false);
                else
                {
                    var detail = await _avatarDetailRepository.ReadOwnedRowAsync(id, includeDeleted: true)
                        .ConfigureAwait(false);
                    await _avatarRepository.DeleteHardWithDetailAsync(id, detail != null).ConfigureAwait(false);
                }

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

    }
}
