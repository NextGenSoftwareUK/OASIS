using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Utilities;
using EOSNewYork.EOSCore.Response.API;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider, IOASISSuperStar
    {
        private static Dictionary<Guid, Account> _avatarIdToEOSIOAccountLookup = new Dictionary<Guid, Account>();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS), AvatarManager.OASISDNA);
                    //_avatarManager = new AvatarManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...

                return _avatarManager;
            }
        }
        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS), AvatarManager);
                    //_keyManager = new KeyManager(this, AvatarManager); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...

                return _keyManager;
            }
        }

        private readonly IEosClient _eosClient;
        private readonly IEosProviderRepository<HolonDto> _holonRepository;
        private readonly IEosProviderRepository<AvatarDetailDto> _avatarDetailRepository;
        private readonly IEosProviderRepository<AvatarDto> _avatarRepository;

        public EOSIOOASIS(string hostUri, string eosAccountCode)
        {
            if (string.IsNullOrEmpty(hostUri))
                throw new ArgumentNullException(nameof(hostUri));
            
            this.ProviderName = "EOSIOOASIS";
            this.ProviderDescription = "EOSIO Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _eosClient = new EosClient(new Uri(hostUri));
            _holonRepository = new HolonEosProviderRepository(_eosClient, eosAccountCode);
            _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, eosAccountCode);
            _avatarRepository = new AvatarEosProviderRepository(_eosClient, eosAccountCode);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            // Get server state. Just need to receive correct response, otherwise exception would be thrown.
            var nodeInfo = _eosClient.GetNodeInfo().Result;
            
            // Response was received, but payload was incorrect.
            if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
            {
                return new OASISResult<bool>(false);
            }
            
            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _eosClient.Dispose();
            return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = await _avatarRepository.ReadAll();
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result = 
                    allAvatarsDTOs
                        .Select(avatarDto => JsonConvert.DeserializeObject<Avatar>(avatarDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = _avatarRepository.ReadAll().Result;
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result = 
                    allAvatarsDTOs
                        .Select(avatarDto => JsonConvert.DeserializeObject<Avatar>(avatarDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            if (Id == null)
                throw new ArgumentNullException(nameof(Id));
            
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarDto = await _avatarRepository.Read(Id);
                var avatarEntity = JsonConvert.DeserializeObject<Avatar>(avatarDto.Info);
                if (avatarEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar with such ID, not found!";
                    return result;
                }
                
                result.Result = avatarEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarDto = _avatarRepository.Read(Id).Result;
                var avatarEntity = JsonConvert.DeserializeObject<Avatar>(avatarDto.Info);
                if (avatarEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar with such ID, not found!";
                    return result;
                }
                
                result.Result = avatarEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

        //    if (rows.rows.Count == 0)
        //        return null;

        //    var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
        //    var Avatar = AvatarRow.ToAvatar();

        //    Avatar.Password = StringCipher.Decrypt(Avatar.Password);
        //    return new OASISResult<IAvatar>(Avatar);
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailDto = _avatarDetailRepository.Read(id).Result;
                var avatarDetailEntity = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.Info);
                if (avatarDetailEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar Detail with such ID, not found!";
                    return result;
                }
                
                result.Result = avatarDetailEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailDto = await _avatarDetailRepository.Read(id);
                var avatarDetailEntity = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.Info);
                if (avatarDetailEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar Detail with such ID, not found!";
                    return result;
                }
                
                result.Result = avatarDetailEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

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
                        .Select(avatarDetailDto => JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
                        .Select(avatarDetailDto => JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));
            
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

                _avatarRepository.Create(new AvatarDto()
                {
                    Info = avatarInfo,
                    AvatarId = Avatar.Id.ToString(),
                    IsDeleted = false,
                    EntityId = avatarEntityId
                }).Wait();

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));
            
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

                await _avatarRepository.Create(new AvatarDto()
                {
                    Info = avatarInfo,
                    AvatarId = Avatar.Id.ToString(),
                    IsDeleted = false,
                    EntityId = avatarEntityId
                });

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                _avatarDetailRepository.Create(new AvatarDetailDto()
                {
                    Info = avatarDetailInfo,
                    AvatarId = Avatar.Id.ToString(),
                    EntityId = avatarDetailEntityId
                }).Wait();

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                await _avatarDetailRepository.Create(new AvatarDetailDto()
                {
                    Info = avatarDetailInfo,
                    AvatarId = Avatar.Id.ToString(),
                    EntityId = avatarDetailEntityId
                });

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    _avatarRepository.DeleteSoft(id).Wait();
                else
                    _avatarRepository.DeleteHard(id).Wait();

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    await _avatarRepository.DeleteSoft(id);
                else
                    await _avatarRepository.DeleteHard(id);

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = _holonRepository.Read(id).Result;
                var holonEntity = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                if (holonEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Holon with such ID, not found!";
                    return result;
                }
                
                result.Result = holonEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = await _holonRepository.Read(id);
                var holonEntity = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                if (holonEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Holon with such ID, not found!";
                    return result;
                }
                
                result.Result = holonEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = _holonRepository.ReadAll().Result;
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result = 
                    allHolonDTOs
                        .Select(holonDto => JsonConvert.DeserializeObject<Holon>(holonDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = await _holonRepository.ReadAll();
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result = 
                    allHolonDTOs
                        .Select(holonDto => JsonConvert.DeserializeObject<Holon>(holonDto.Info))
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                var holonInfo = JsonConvert.SerializeObject(holon);

                _holonRepository.Create(new HolonDto()
                {
                    Info = holonInfo,
                    HolonId = holon.Id.ToString(),
                    EntityId = holonEntityId,
                    IsDeleted = false
                }).Wait();

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));
            
            var result = new OASISResult<IHolon>();
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                var holonInfo = JsonConvert.SerializeObject(holon);

                await _holonRepository.Create(new HolonDto()
                {
                    Info = holonInfo,
                    HolonId = holon.Id.ToString(),
                    EntityId = holonEntityId,
                    IsDeleted = false
                });

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    _holonRepository.Create(new HolonDto()
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    }).Wait();   
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));
            
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    await _holonRepository.Create(new HolonDto()
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    });   
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    _holonRepository.DeleteSoft(id).Wait();
                else
                    _holonRepository.DeleteHard(id).Wait();

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    await _holonRepository.DeleteSoft(id);
                else
                    await _holonRepository.DeleteHard(id);

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch(Exception ex)
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

        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        // public async Task<Account> GetEOSIOAccountAsync(string eosioAccountName)
        // {
        //     var account = await ChainAPI.GetAccountAsync(eosioAccountName);
        //     return account;
        // }
        //
        // public Account GetEOSIOAccount(string eosioAccountName)
        // {
        //     var account = ChainAPI.GetAccount(eosioAccountName);
        //     return account;
        // }

        public async Task<string> GetBalanceAsync(string eosioAccountName, string code, string symbol)
        {
            // var currencyBalance = await ChainAPI.GetCurrencyBalanceAsync(eosioAccountName, code, symbol);
            // return currencyBalance.balances[0];
            throw new NotImplementedException();
        }

        public string GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            // var currencyBalance = ChainAPI.GetCurrencyBalance(eosioAccountName, code, symbol);
            // return currencyBalance.balances[0];
            throw new NotImplementedException();
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            //TODO: Add support for multiple accounts later.
            return GetBalanceForEOSIOAccount(GetEOSIOAccountNamesForAvatar(avatarId)[0], code, symbol);
        }

        public List<string> GetEOSIOAccountNamesForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPublicKeysForAvatar(avatarId, Core.Enums.ProviderType.EOSIOOASIS).Result;
        }

        public string GetEOSIOAccountPrivateKeyForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, Core.Enums.ProviderType.EOSIOOASIS).Result;
        }

        public Account GetEOSIOAccountForAvatar(Guid avatarId)
        {
            //TODO: Do we need to cache this?
            // if (!_avatarIdToEOSIOAccountLookup.ContainsKey(avatarId))
            //     _avatarIdToEOSIOAccountLookup[avatarId] = GetEOSIOAccount(GetEOSIOAccountNamesForAvatar(avatarId)[0]);

            //TODO: Add support for multiple accounts later.
            return _avatarIdToEOSIOAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarIdForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS).Result;
        }

        public IAvatar GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS).Result;
        }
    }
}
