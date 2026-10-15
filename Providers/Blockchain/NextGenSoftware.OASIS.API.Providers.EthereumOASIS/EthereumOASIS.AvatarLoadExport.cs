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
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
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
                var count = await _nextGenSoftwareOasisService.GetAvatarDetailsCountQueryAsync(block);
                var details = new List<IAvatarDetail>();
                var identities = new HashSet<Guid>();
                for (BigInteger index = 0; index < count; index++)
                {
                    var row = (await _nextGenSoftwareOasisService.ContractHandler
                        .QueryDeserializingToObjectAsync<ContractDefinition.GetAvatarDetailAtFunction,ContractDefinition.GetAvatarDetailByIdOutputDTO>(
                            new ContractDefinition.GetAvatarDetailAtFunction { Index=index },block)).ReturnValue1;
                    if (row == null || !Guid.TryParseExact(row.AvatarId,"D",out var id))
                        throw new InvalidDataException("Ethereum avatar detail has an invalid public GUID.");
                    var detail = DeserializePrivateStorage<AvatarDetail>("avatar-detail",row.EntityId,row.Info,id);
                    if (detail == null || !identities.Add(detail.Id))
                        throw new InvalidDataException("Ethereum avatar detail collection contains a missing or duplicate identity.");
                    details.Add(detail);
                }
                result.Result = details;
                result.IsLoaded = true;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result,$"Error loading native Ethereum avatar details: {error.Message}",error);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => LoadProviderIdentityAsync(providerKey,version,id=>LoadAvatarAsync(id,version));

        public Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyVersionAsync(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey,version);

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsByVersionAsync(int version = 0)
            => LoadAllAvatarsAsync(version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }


        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }


        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatarAsync method in EthereumOASIS while loading an avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);
                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = DeserializePrivateStorage<Avatar>("avatar",avatarDto.ReturnValue1.EntityId,avatarDto.ReturnValue1.Info,Id);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => FindStoredIdentityAsync(avatarEmail,row=>row.Email,()=>LoadAllAvatarsAsync(version));

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => FindStoredIdentityAsync(avatarUsername,row=>row.Username,()=>LoadAllAvatarsAsync(version));

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatar method in EthereumOASIS load avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId).Result;

                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = DeserializePrivateStorage<Avatar>("avatar",avatarDto.ReturnValue1.EntityId,avatarDto.ReturnValue1.Info,Id);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in SaveAvatar method in EthereumOASIS saving avatar. Reason: ";

            try
            {
                var avatarInfo = SerializePrivateStorage("avatar",avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (!StorageReceiptSucceeded(requestTransaction))
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!"));
                    return result;
                }
                
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

        public bool IsVersionControlEnabled { get; set; }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

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
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

    }
}
