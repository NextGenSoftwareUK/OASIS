using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly NextGenSoftwareOASISService _nextGenSoftwareOasisService;

        public EthereumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);

            if (!string.IsNullOrEmpty(hostUri) && !string.IsNullOrEmpty(chainPrivateKey) && chainId > 0)
            {
                var account = new Account(chainPrivateKey, chainId);
                var web3 = new Web3(account, hostUri);

                _nextGenSoftwareOasisService = new NextGenSoftwareOASISService(web3, contractAddress);
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = avatar;
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Avatar Detail (Id): {avatarDetailId}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = avatar;
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Avatar Detail (Id): {avatarDetailId}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = avatar;
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Deleting of Avatar (Id): {id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = false;
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = $"Smart contract side thrown an exception, while executing the request. Maybe specified avatar (with Id: {id}) not found!";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Deleting of Avatar (Id): {id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = false;
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = $"Smart contract side thrown an exception, while executing the request. Maybe specified avatar (with Id: {id}) not found!";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));
            
            var result = new OASISResult<IEnumerable<IHolon>>();
        
            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    await _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo);
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Deleting of holon (Id): {id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = false;
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Deleting of holon (Id): {id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = false;
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = false;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId).Result;

                if (holonDto == null)
                {
                    result.Message = $"Holon (with id {id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = $"Smart contract side thrown an exception, while executing the request. Maybe Holon (with id {id}) not found!";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);

                if (holonDto == null)
                {
                    result.Message = $"Holon (with id {id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = $"Smart contract side thrown an exception, while executing the request. Maybe Holon (with id {id}) not found!";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = holon;
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));
            
            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = holon;
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));
            
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo).Wait();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId).Result;

                if (avatarDetailDto == null)
                {
                    result.Message = $"Avatar details (with id {id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);

                if (avatarDetailDto == null)
                {
                    result.Message = $"Avatar details (with id {id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);

                if (avatarDto == null)
                {
                    result.Message = $"Avatar (with id {Id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsLoaded = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsLoaded = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId).Result;

                if (avatarDto == null)
                {
                    result.Message = $"Avatar (with id {Id}) not found!";
                    result.IsError = true;
                    result.IsLoaded = false;
                    result.Result = null;
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    result.Message = $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!";
                    result.IsError = true;
                    result.IsSaved = false;
                    result.Result = avatar;
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                result.Exception = ex;
                result.Message = "Smart contract side thrown an exception, while executing the request.";
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public bool IsVersionControlEnabled { get; set; }
        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}