using System;
using System.Collections.Generic;
using System.Linq;
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
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider
    {
        private readonly NextGenSoftwareOASISService _nextGenSoftwareOasisService;
        private readonly Account _oasisAccount;
        private readonly Web3 _web3Client;
        private KeyManager _keyManager;
        private WalletManager _walletManager;

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this);
                 //_keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _keyManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this);
                    //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _walletManager;
            }
        }
        
        public EthereumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);

            if (!string.IsNullOrEmpty(hostUri) && !string.IsNullOrEmpty(chainPrivateKey) && chainId > 0)
            {
                _oasisAccount = new Account(chainPrivateKey, chainId);
                _web3Client = new Web3(_oasisAccount, hostUri);

                _nextGenSoftwareOasisService = new NextGenSoftwareOASISService(_web3Client, contractAddress);
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in SaveAvatarAsync method in EthereumOASIS while saving avatar. Reason: ";

            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {   
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in SaveAvatarDetail method in EthereumOASIS while saving avatar. Reason: ";

            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {   
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in SaveAvatarDetailAsync method in EthereumOASIS while saving and avatar detail. Reason: ";
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in DeleteAvatar method in EthereumOASIS while deleting avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in DeleteAvatarAsync method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in SaveHolonsAsync method in EthereumOASIS while saving holons. Reason: ";

            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    var createHolonResult = await _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo);

                    if (createHolonResult.HasErrors() is true)
                    {
                        ErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
                        if(!continueOnError)
                            break;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in DeleteHolon method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in DeleteHolonAsync method in EthereumOASIS while deleting holon. Reason: ";
            
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in LoadHolon method in EthereumOASIS while loading holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId).Result;

                if (holonDto == null)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in EthereumOASIS while loading holons. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);

                if (holonDto == null)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in SaveHolon method in EthereumOASIS while saving holon. Reason: ";

            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));
            
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in SaveHolonAsync method in EthereumOASIS while saving holon. Reason: ";

            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in SaveHolons method in EthereumOASIS while saving holons. Reason: ";

            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    var createHolonResult = _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo).Result;

                    if (createHolonResult.HasErrors() is true)
                    {
                        ErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
                        if(!continueOnError)
                            break;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in LoadAvatarDetail method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId).Result;

                if (avatarDetailDto == null)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in LoadAvatarDetailAsync method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);

                if (avatarDetailDto == null)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in LoadAvatarAsync method in EthereumOASIS while loading an avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);
                if (avatarDto == null)
                {
                    ErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
            string errorMessage = "Error in LoadAvatar method in EthereumOASIS load avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId).Result;

                if (avatarDto == null)
                {
                    ErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendTransactionByIdAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

            if (senderAvatarPrivateKeysResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendTransactionByUsernameAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendTransactionByEmailAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderUniqueStorageKeyForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<string> SendTransaction(IWalletTransaction transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }
        
        public async Task<OASISResult<string>> SendTransactionAsync(IWalletTransaction transaction)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendTransactionAsync method in EthereumOASIS sending transaction. Reason: ";
            
            try
            {
                var transactionResult = await _web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(transaction.ToWalletAddress, transaction.Amount);

                if (transactionResult.HasErrors() is true)
                {
                    result.Message = string.Concat(errorMessage, "Ethereum transaction performing failed! " +
                                     $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {transaction.Amount}." +
                                     $"Reason: {transactionResult.Logs}");
                    ErrorHandling.HandleError(ref result, result.Message);
                    return result;
                }
                
                result.IsError = false;
                result.IsSaved = true;
                result.Result = transactionResult.TransactionHash;
            }
            catch (RpcResponseException ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }
        
        private async Task<OASISResult<string>> SendEthereumTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<string>();
            string errorMessage = "Error in SendEthereumTransaction method in EthereumOASIS sending transaction. Reason: ";
            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = new Web3(senderEthAccount);
                
                var transactionResult = await web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
                
                if (transactionResult.HasErrors() is true)
                {
                    ErrorHandling.HandleError(ref result, string.Concat(errorMessage, transactionResult.Logs));
                    return result;
                }

                result.IsError = false;
                result.IsSaved = true;
                result.Result = transactionResult.TransactionHash;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }
    }
}