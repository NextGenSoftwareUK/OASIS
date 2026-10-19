using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result=new OASISResult<IHolon>();
            var saved=await SaveHolonsAsync(new[] {holon},saveChildren,recursive,maxChildDepth,0,continueOnError,saveChildrenOnProvider);
            if(saved.IsError)
            {
                OASISErrorHandling.HandleError(ref result,saved.Message,saved.Exception);
                result.ErrorCode=saved.ErrorCode;
                return result;
            }
            result.Result=saved.Result.Single(); result.IsSaved=true;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (version != 0)
                    throw new NotSupportedException("Ethereum collection history is not implemented; only current version 0 is supported.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError)
                        throw new InvalidOperationException(activation.Message,activation.Exception);
                }
                // Count and indexed reads share one block: swap-delete cannot skip/duplicate rows.
                var block = new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var count = await _nextGenSoftwareOasisService.GetAvatarsCountQueryAsync(block);
                var details = new List<IAvatar>();
                var identities = new HashSet<Guid>();
                for (BigInteger index = 0; index < count; index++)
                {
                    var row = (await _nextGenSoftwareOasisService.ContractHandler
                        .QueryDeserializingToObjectAsync<ContractDefinition.GetAvatarAtFunction,ContractDefinition.GetAvatarByIdOutputDTO>(
                            new ContractDefinition.GetAvatarAtFunction { Index=index },block)).ReturnValue1;
                    if (row == null || !Guid.TryParseExact(row.AvatarId,"D",out var id))
                        throw new InvalidDataException("Ethereum avatar has an invalid public GUID.");
                    var detail = DeserializePrivateStorage<Avatar>("avatar",row.EntityId,row.Info,id);
                    if (detail == null || !identities.Add(detail.Id))
                        throw new InvalidDataException("Ethereum avatar collection contains a missing or duplicate identity.");
                    details.Add(detail);
                }
                result.Result = details;
                result.IsLoaded = true;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result,$"Error loading native Ethereum avatars: {error.Message}",error);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in LoadAvatarDetail method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var avatarDetailDto = _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId).Result;

                if (avatarDetailDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = ReadPrivateStorageVersion<AvatarDetail>("avatar-detail",avatarDetailDto.ReturnValue1.EntityId,avatarDetailDto.ReturnValue1.Info,id,version);
                if(avatarDetailEntityResult==null){result.ErrorCode=OASISResultErrorCodes.RecordNotFound;throw new System.Collections.Generic.KeyNotFoundException("Ethereum detail version not found.");}
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
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
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => FindStoredIdentityAsync(avatarEmail,row=>row.Email,()=>LoadAllAvatarDetailsAsync(version));

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => FindStoredIdentityAsync(avatarUsername,row=>row.Username,()=>LoadAllAvatarDetailsAsync(version));

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in LoadAvatarDetailAsync method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var avatarDetailDto = await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);

                if (avatarDetailDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = ReadPrivateStorageVersion<AvatarDetail>("avatar-detail",avatarDetailDto.ReturnValue1.EntityId,avatarDetailDto.ReturnValue1.Info,id,version);
                if(avatarDetailEntityResult==null){result.ErrorCode=OASISResultErrorCodes.RecordNotFound;throw new System.Collections.Generic.KeyNotFoundException("Ethereum detail version not found.");}
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
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

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameVersionAsync(string avatarUsername, int version = 0)
            => LoadAvatarDetailByUsernameAsync(avatarUsername,version);

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailVersionAsync(string avatarEmail, int version = 0)
            => LoadAvatarDetailByEmailAsync(avatarEmail,version);

    }
}
