using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
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

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IDisposable
    {
        private static readonly Dictionary<Guid, GetAccountResponseDto> _avatarIdToEOSIOAccountLookup = new();
        private IEosClient _eosClient;
        private IEosProviderRepository<AvatarDetailDto> _avatarDetailRepository;
        private IEosProviderRepository<AvatarDto> _avatarRepository;
        private IEosProviderRepository<HolonDto> _holonRepository;
        private IEosTransferRepository _transferRepository;
        private AvatarManager _avatarManager;
        private KeyManager _keyManager;
        private WalletManager _walletManager;

        public ChainAPI ChainAPI => new ChainAPI();

        public string HostURI { get; set; }
        public string EOSAccountName { get; set; }
        public string EOSChainId { get; set; }
        public string EOSAccountPk { get; set; }

        public EOSIOOASIS(string hostUri, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            if (string.IsNullOrEmpty(hostUri))
                throw new ArgumentNullException(nameof(hostUri));

            ProviderName = "EOSIOOASIS";
            ProviderDescription = "EOSIO Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            HostURI = hostUri;
            EOSAccountName = eosAccountName;
            EOSChainId = eosChainId;
            EOSAccountPk = eosAccountPk;

            //_eosClient = new EosClient(new Uri(hostUri));
            //_holonRepository = new HolonEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, eosAccountName,hostUri, eosChainId, eosAccountPk);
            //_avatarRepository = new AvatarEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_transferRepository = new EosTransferRepository(eosAccountName, hostUri, eosChainId, eosAccountPk);
        }

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    //_avatarManager = new AvatarManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS), OASISDNA);
                    _avatarManager = new AvatarManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...
                
                return _avatarManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, AvatarManager.OASISDNA);
                //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS),AvatarManager.OASISDNA);

                return _walletManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    //_keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS));
                    _keyManager = new KeyManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...

                return _keyManager;
            }
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = await _eosClient.GetNodeInfo();

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProvider method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = _eosClient.GetNodeInfo().Result;

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return base.ActivateProvider();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return base.DeActivateProvider();
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
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarEntity = avatarDto.GetBaseAvatar();
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
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
                var avatarDetailEntity = avatarDetailDto.GetBaseAvatarDetail();
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarDetailEntity = avatarDetailDto.GetBaseAvatarDetail();
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail,
            int version = 0)
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
                        .Select(avatarDetailDto => avatarDetailDto.GetBaseAvatarDetail())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

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
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

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
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatarDetail = _avatarDetailRepository.Read(Avatar.Id).Result;
                if (existAvatarDetail != null)
                {
                    _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarDetailInfo
                    }, Avatar.Id).Wait();
                }
                else
                {
                    var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    _avatarDetailRepository.Create(new AvatarDetailDto
                    {
                        Info = avatarDetailInfo,
                        AvatarId = Avatar.Id.ToString(),
                        EntityId = avatarDetailEntityId
                    }).Wait();
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatarDetail = await _avatarDetailRepository.Read(Avatar.Id);
                if (existAvatarDetail != null)
                {
                    await _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarDetailInfo
                    }, Avatar.Id);
                }
                else
                {
                    var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    await _avatarDetailRepository.Create(new AvatarDetailDto
                    {
                        Info = avatarDetailInfo,
                        AvatarId = Avatar.Id.ToString(),
                        EntityId = avatarDetailEntityId
                    });
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername,
            bool softDelete = true)
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

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = _holonRepository.Read(id).Result;
                var holonEntity = holonDto.GetBaseHolon();
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = await _holonRepository.Read(id);
                var holonEntity = holonDto.GetBaseHolon();
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = _holonRepository.ReadAll().Result;
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = await _holonRepository.ReadAll();
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = _holonRepository.Read(holon.Id).Result;
                if (existAvatar != null)
                {
                    _holonRepository.Update(new HolonDto
                    {
                        Info = holonInfo
                    }, holon.Id).Wait();
                }
                else
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                    _holonRepository.Create(new HolonDto
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    }).Wait();
                }

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = await _holonRepository.Read(holon.Id);
                if (existAvatar != null)
                {
                    await _holonRepository.Update(new HolonDto
                    {
                        Info = holonInfo
                    }, holon.Id);
                }
                else
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                    await _holonRepository.Create(new HolonDto
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    });
                }

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id).Wait();
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        }).Wait();
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        await _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id);
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        await _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        });
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                 _holonRepository.DeleteHard(id).Wait();
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                //if (softDelete)
                //    await _holonRepository.DeleteSoft(id);
                //else
                //    await _holonRepository.DeleteHard(id);

                await _holonRepository.DeleteHard(id);
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }


        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISSuperStar

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }


        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transaction)
        {
            return await _transferRepository.TransferEosToken(
                transaction.FromWalletAddress, transaction.ToWalletAddress, transaction.Amount);
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByIdAsync method in EosioOasis sending transaction. Reason: ";

            var fromAvatarResult = await AvatarManager.LoadAvatarAsync(fromAvatarId);
            var toAvatarResult = await AvatarManager.LoadAvatarAsync(toAvatarId);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarResult.Message),
                    fromAvatarResult.Exception);
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarResult.Message),
                    toAvatarResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            var receiverAvatarAccountName = toAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByUsernameAsync method in EosioOasis sending transaction. Reason: ";

            var fromAvatarDetailResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(fromAvatarUsername);
            var toAvatarDetailResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(toAvatarUsername);

            if (fromAvatarDetailResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarDetailResult.Message),
                    fromAvatarDetailResult.Exception);
                return result;
            }

            if (toAvatarDetailResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarDetailResult.Message),
                    toAvatarDetailResult.Exception);
                return result;
            }

            var fromAvatarResult = await AvatarManager.LoadAvatarAsync(fromAvatarDetailResult.Result.Id);
            var toAvatarResult = await AvatarManager.LoadAvatarAsync(toAvatarDetailResult.Result.Id);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarResult.Message),
                    fromAvatarResult.Exception);
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarResult.Message),
                    toAvatarResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            var receiverAvatarAccountName = toAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByEmailAsync method in EosioOasis sending transaction. Reason: ";

            var fromAvatarResult = await AvatarManager.LoadAvatarByEmailAsync(fromAvatarEmail);
            var toAvatarResult = await AvatarManager.LoadAvatarByEmailAsync(toAvatarEmail);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarResult.Message),
                    fromAvatarResult.Exception);
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarResult.Message),
                    toAvatarResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            var receiverAvatarAccountName = toAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId,
            decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage =
                "Error in SendTransactionByDefaultWalletAsync method in EosioOasis sending transaction. Reason: ";

            var fromWalletResult =
                await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EOSIOOASIS);
            var toWalletResult =
                await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EOSIOOASIS);

            if (fromWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromWalletResult.Message),
                    fromWalletResult.Exception);
                return result;
            }

            if (toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message),
                    toWalletResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromWalletResult.Result.Name;
            var receiverAvatarAccountName = toWalletResult.Result.Name;
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName,
                amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();

            OASISResult<ITransactionRespone> transferResult = await _transferRepository.TransferEosNft(
                transation.FromWalletAddress,
                transation.ToWalletAddress,
                transation.Amount,
                "SYS");

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ITransactionRespone, INFTTransactionRespone>(transferResult, result);
            //OASISResultHelper.CopyResult<ITransactionRespone, INFTTransactionRespone>(transferResult, result);
            result.Result.TransactionResult = transferResult.Result.TransactionResult;
            result.Result.OASISNFT = null; //TODO: We may want to look up/pass the NFT MetaData in future...

            return result;

            //return await _transferRepository.TransferEosNft(
            //    transation.FromWalletAddress,
            //    transation.ToWalletAddress,
            //    transation.Amount,
            //    "SYS");
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        #endregion

        public async Task<GetAccountResponseDto> GetEOSIOAccountAsync(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                });
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;
                
                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }
        
        public GetAccountResponseDto GetEOSIOAccount(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                }).Result;
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;
                
                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }

        public async Task<string> GetBalanceAsync(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = await _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                });
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;
                
                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                }).Result;
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;
                
                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            //TODO: Add support for multiple accounts later.
            return GetBalanceForEOSIOAccount(GetEOSIOAccountNamesForAvatar(avatarId)[0], code, symbol);
        }

        public List<string> GetEOSIOAccountNamesForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS).Result;
        }

        public string GetEOSIOAccountPrivateKeyForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS).Result[0];
        }

        public GetAccountResponseDto GetEOSIOAccountForAvatar(Guid avatarId)
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
            return KeyManager.GetAvatarIdForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS)
                .Result;
        }

        public IAvatar GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS)
                .Result;
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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
    }
}