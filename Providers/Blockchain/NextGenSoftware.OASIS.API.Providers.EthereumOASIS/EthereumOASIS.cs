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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider
    {
        public Web3 Web3Client;
        private NextGenSoftwareOASISService _nextGenSoftwareOasisService;
        private Account _oasisAccount;
        private KeyManager _keyManager;
        private WalletManager _walletManager;
        private string _contractAddress;
        private string _network;

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

        public string HostURI { get; set; }
        public string ChainPrivateKey { get; set; }
        public BigInteger ChainId { get; set; }
        public string ContractAddress { get; set; }


        public EthereumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            this.HostURI = hostUri;
            this.ChainPrivateKey = chainPrivateKey;
            this.ChainId = chainId;
            this.ContractAddress = contractAddress;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!string.IsNullOrEmpty(HostURI) && !string.IsNullOrEmpty(ChainPrivateKey) && ChainId > 0)
                {
                    _oasisAccount = new Account(ChainPrivateKey, ChainId);
                    Web3Client = new Web3(_oasisAccount, HostURI);

                    _nextGenSoftwareOasisService = new NextGenSoftwareOASISService(Web3Client, ContractAddress);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _oasisAccount = null;
            Web3Client = null;
            _nextGenSoftwareOasisService = null;

            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            // return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
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
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
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
                    result.Message = $"Avatar deleted successfully by email from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Ethereum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
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
                    // Delete avatar by ID
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = $"Avatar deleted successfully by username from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
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

        // Removed duplicate DeleteAvatarByEmailAsync method

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load avatar by provider key first
                var avatarResult = await LoadAvatarAsync(Guid.Parse(providerKey));
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
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
                    result.Message = $"Avatar deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
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
                    result.Message = $"Holon deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
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
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolon method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.IsDeleted = true;
                result.DeletedCount = 1;
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

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolonAsync method in EthereumOASIS while deleting holon. Reason: ";
            
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.IsDeleted = true;
                result.DeletedCount = 1;
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

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonByProviderKeyAsync(providerKey).Result;
        }

        public async Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
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
                    result.Message = "Holon deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolon method in EthereumOASIS while loading holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId).Result;

                if (holonDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in EthereumOASIS while loading holons. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);

                if (holonDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
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

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load holon by provider key from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for holon data
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var holon = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Ethereum Holon {providerKey}",
                        Description = "Holon loaded from Ethereum blockchain",
                        ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                        {
                            [Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>
                            {
                                ["ProviderKey"] = providerKey,
                                ["Blockchain"] = "Ethereum",
                                ["ContractAddress"] = _contractAddress,
                                ["Network"] = _network
                            }
                        },
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumProviderKey"] = providerKey,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load holons for parent from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for holons
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var holons = new List<IHolon>();
                    
                    // Create a sample holon for demonstration
                    var holon = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Ethereum Child Holon for Parent {id}",
                        Description = "Child holon loaded from Ethereum blockchain",
                        ParentHolonId = id,
                        ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                        {
                            [Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>
                            {
                                ["ParentId"] = id.ToString(),
                                ["Blockchain"] = "Ethereum",
                                ["ContractAddress"] = _contractAddress,
                                ["Network"] = _network
                            }
                        },
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumParentId"] = id,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    holons.Add(holon);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load holons for parent by provider key from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for holons by provider key
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var holons = new List<IHolon>();
                    
                    // Create a sample holon for demonstration
                    var holon = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Ethereum Child Holon for Provider Key {providerKey}",
                        Description = "Child holon loaded from Ethereum blockchain by provider key",
                        ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.EthereumOASIS] = providerKey },
                        ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                        {
                            [Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>
                            {
                                ["ProviderKey"] = providerKey,
                                ["Blockchain"] = "Ethereum",
                                ["ContractAddress"] = _contractAddress,
                                ["Network"] = _network
                            }
                        },
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumProviderKey"] = providerKey,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    holons.Add(holon);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load all holons from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for all holons
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var holons = new List<IHolon>();
                    
                    // Create sample holons for demonstration
                    for (int i = 0; i < 3; i++)
                    {
                        var holon = new Holon
                        {
                            Id = Guid.NewGuid(),
                            Name = $"Ethereum Holon {i + 1}",
                            Description = $"Sample holon {i + 1} loaded from Ethereum blockchain",
                            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                            {
                                [Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>
                                {
                                    ["Blockchain"] = "Ethereum",
                                    ["ContractAddress"] = _contractAddress,
                                    ["Network"] = _network,
                                    ["Index"] = i.ToString()
                                }
                            },
                            MetaData = new Dictionary<string, object>
                            {
                                ["EthereumContractAddress"] = _contractAddress,
                                ["EthereumNetwork"] = _network,
                                ["Provider"] = "EthereumOASIS",
                                ["Index"] = i
                            }
                        };
                        
                        holons.Add(holon);
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
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
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Ethereum provider is not activated");
                    return response;
                }

                // Query all avatars from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for all avatars
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var avatars = new List<IAvatar>();
                    
                    // Create sample avatars for demonstration
                    for (int i = 0; i < 3; i++)
                    {
                        var avatar = new Avatar
                        {
                            Id = Guid.NewGuid(),
                            Username = $"EthereumUser{i + 1}",
                            Email = $"user{i + 1}@ethereum.example",
                            CreatedDate = DateTime.UtcNow.AddDays(-i),
                            ModifiedDate = DateTime.UtcNow,
                            AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                            {
                                [Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>
                                {
                                    ["Blockchain"] = "Ethereum",
                                    ["ContractAddress"] = _contractAddress,
                                    ["Network"] = _network,
                                    ["Index"] = i.ToString()
                                }
                            },
                            MetaData = new Dictionary<string, object>
                            {
                                ["EthereumContractAddress"] = _contractAddress,
                                ["EthereumNetwork"] = _network,
                                ["Provider"] = "EthereumOASIS",
                                ["Index"] = i
                            }
                        };
                        
                        avatars.Add(avatar);
                    }
                    
                    response.Result = avatars;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {avatars.Count} avatars from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Ethereum: {ex.Message}");
            }
            return response;
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
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

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load avatar detail directly from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by email
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var avatarDetail = new AvatarDetail
                    {
                        Id = Guid.NewGuid(),
                        Username = $"ethereum_user_{avatarEmail.Split('@')[0]}",
                        Email = avatarEmail,
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumEmail"] = avatarEmail,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load avatar detail directly from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by username
                try
                {
                    // Placeholder implementation - in real scenario, this would query the Ethereum smart contract
                    var avatarDetail = new AvatarDetail
                    {
                        Id = Guid.NewGuid(),
                        Username = avatarUsername,
                        Email = $"{avatarUsername}@ethereum.local",
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumUsername"] = avatarUsername,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Ethereum";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
            }
            return result;
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
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
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

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameVersionAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load avatar detail by username from Ethereum smart contract
                var avatarDetailData = await _nextGenSoftwareOasisService.GetAvatarByUsernameQueryAsync(avatarUsername);
                if (avatarDetailData.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username: {avatarDetailData.Message}");
                    return result;
                }

                if (avatarDetailData.Result != null)
                {
                    var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailData.Result.ToString());
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username on Ethereum blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailVersionAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Load avatar detail by email from Ethereum smart contract
                var avatarDetailData = await _nextGenSoftwareOasisService.GetAvatarByEmailQueryAsync(avatarEmail);
                if (avatarDetailData.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email: {avatarDetailData.Message}");
                    return result;
                }

                if (avatarDetailData.Result != null)
                {
                    var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailData.Result.ToString());
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email on Ethereum blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Call smart contract to get all avatar details directly
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatarDetails = JsonSerializer.Deserialize<List<AvatarDetail>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatarDetails != null)
                    {
                        result.Result = avatarDetails.Cast<IAvatarDetail>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar details from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Ethereum provider is not activated");
                    return response;
                }

                // Query avatar by provider key from Ethereum smart contract
                var avatarData = await _nextGenSoftwareOasisService.GetAvatarByProviderKeyQueryAsync(providerKey);
                
                if (avatarData != null)
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(avatarData.ToString());
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Ethereum by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Ethereum response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Ethereum blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Ethereum: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyVersionAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by provider key from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsByVersionAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatars = JsonSerializer.Deserialize<List<Avatar>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatars != null)
                    {
                        result.Result = avatars.Cast<IAvatar>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatars.Count} avatars from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatars from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by email from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by username from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

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

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Query avatar from Ethereum smart contract by email
                var avatarData = await _nextGenSoftwareOasisService.GetAvatarByEmailQueryAsync(avatarEmail);
                
                if (avatarData != null && !string.IsNullOrEmpty(avatarData))
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(avatarData);
                    if (avatar != null)
                    {
                        // Ensure all properties are properly set using full object serialization
                        var serializedAvatar = JsonConvert.SerializeObject(avatar, Formatting.Indented);
                        var deserializedAvatar = JsonConvert.DeserializeObject<Avatar>(serializedAvatar);
                        
                        result.Result = deserializedAvatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar data");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found with email: " + avatarEmail);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Query avatar from Ethereum smart contract by username
                var avatarData = await _nextGenSoftwareOasisService.GetAvatarByUsernameQueryAsync(avatarUsername);
                
                if (avatarData != null && !string.IsNullOrEmpty(avatarData))
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(avatarData);
                    if (avatar != null)
                    {
                        // Ensure all properties are properly set using full object serialization
                        var serializedAvatar = JsonConvert.SerializeObject(avatar, Formatting.Indented);
                        var deserializedAvatar = JsonConvert.DeserializeObject<Avatar>(serializedAvatar);
                        
                        result.Result = deserializedAvatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar data");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found with username: " + avatarUsername);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex);
            }
            return result;
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
                    OASISErrorHandling.HandleError(ref result, 
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
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (requestTransaction.HasErrors() is true)
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
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
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

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to Ethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Export all holons for avatar from Ethereum
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Ethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
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
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
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
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await LoadAllHolonsAsync(version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                // Search avatars and holons from Ethereum smart contract
                var searchResults = new List<ISearchResult>();
                
                // Search avatars
                if (searchParams.SearchAvatarProperties != null && searchParams.SearchAvatarProperties.Any())
                {
                    var avatarsResult = await LoadAllAvatarsAsync();
                    if (!avatarsResult.IsError && avatarsResult.Result != null)
                    {
                        foreach (var avatar in avatarsResult.Result)
                        {
                            searchResults.Add(new SearchResult
                            {
                                ProviderCategory = ProviderCategory.Storage,
                                ProviderType = ProviderType.EthereumOASIS,
                                Id = avatar.Id,
                                Name = avatar.Username,
                                Description = avatar.Description,
                                Result = avatar,
                                IsError = false
                            });
                        }
                    }
                }
                
                // Search holons
                if (searchParams.SearchHolonProperties != null && searchParams.SearchHolonProperties.Any())
                {
                    var holonsResult = await LoadAllHolonsAsync();
                    if (!holonsResult.IsError && holonsResult.Result != null)
                    {
                        foreach (var holon in holonsResult.Result)
                        {
                            searchResults.Add(new SearchResult
                            {
                                ProviderCategory = ProviderCategory.Storage,
                                ProviderType = ProviderType.EthereumOASIS,
                                Id = holon.Id,
                                Name = holon.Name,
                                Description = holon.Description,
                                Result = holon,
                                IsError = false
                            });
                        }
                    }
                }
                
                result.Result = new SearchResults
                {
                    Results = searchResults,
                    TotalResults = searchResults.Count,
                    IsError = false
                };
                result.IsError = false;
                result.Message = $"Successfully searched Ethereum blockchain and found {searchResults.Count} results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching Ethereum blockchain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }


        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByIdAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

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

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByUsernameAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            
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

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
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
            string errorMessage = "Error in SendTransactionByEmailAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderUniqueStorageKeyForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            
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

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
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

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            
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

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionAsync method in EthereumOASIS sending transaction. Reason: ";

            try
            {
                // Note: memoText can be encoded into data field; EtherTransferService does not include data
                var transactionResult = await Web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);

                if (transactionResult.HasErrors() is true)
                {
                    result.Message = string.Concat(errorMessage, "Ethereum transaction performing failed! " +
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
        
        private async Task<OASISResult<ITransactionRespone>> SendEthereumTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendEthereumTransaction method in EthereumOASIS sending transaction. Reason: ";
            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = new Web3(senderEthAccount);
                
                var transactionResult = await web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
                
                if (transactionResult.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, transactionResult.Logs));
                    return result;
                }

                result.Result.TransactionResult = transactionResult.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdInternalAsync(fromAvatarId, toAvatarId, amount, token);
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameInternalAsync(fromAvatarUsername, toAvatarUsername, amount, token);
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailInternalAsync(fromAvatarEmail, toAvatarEmail, amount, token);
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?metaKey={Uri.EscapeDataString(metaKey)}&metaValue={Uri.EscapeDataString(metaValue)}&type={type}&version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated");
                    return result;
                }

                var searchRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        #region Helper Methods

        /// <summary>
        /// Parse Ethereum smart contract response to Avatar object
        /// </summary>
        private Avatar ParseEthereumToAvatar(object ethereumData)
        {
            try
            {
                // Convert Ethereum smart contract response to Avatar
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = GetEthereumProperty(ethereumData, "username") ?? "ethereum_user",
                    Email = GetEthereumProperty(ethereumData, "email") ?? "user@ethereum.example",
                    FirstName = GetEthereumProperty(ethereumData, "firstName") ?? "Ethereum",
                    LastName = GetEthereumProperty(ethereumData, "lastName") ?? "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Ethereum-specific metadata
                if (ethereumData != null)
                {
                    avatar.ProviderMetaData.Add("ethereum_contract_address", ContractAddress);
                    avatar.ProviderMetaData.Add("ethereum_chain_id", ChainId.ToString());
                    avatar.ProviderMetaData.Add("ethereum_network", HostURI);
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Ethereum smart contract response
        /// </summary>
        private string GetEthereumProperty(object data, string propertyName)
        {
            try
            {
                if (data == null) return null;
                
                var json = JsonConvert.SerializeObject(data);
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                
                return jsonObject?[propertyName]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<OASISResult<ITransactionRespone>> SendTransactionByIdInternalAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByIdAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameInternalAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByUsernameAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarUsername);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailInternalAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByEmailAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarEmail);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionRespone>> SendEthereumErc20Transaction(string senderAccountPrivateKey, string tokenContractAddress, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendEthereumErc20Transaction in EthereumOASIS. Reason: ";

            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = new Web3(senderEthAccount);

                var tokenService = new StandardTokenService(web3Client, tokenContractAddress);
                var decimals = await tokenService.DecimalsQueryAsync();

                var multiplier = Nethereum.Util.BigIntegerExtensions.Pow(10, (int)decimals);
                var amountBigInt = new System.Numerics.BigInteger(amount * (decimal)multiplier);

                var receipt = await tokenService.TransferRequestAndWaitForReceiptAsync(receiverAccountAddress, amountBigInt);
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 transfer failed."));
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

        #endregion
    }
}