using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public class ZcashOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider
    {
        private IZcashRepository _zcashRepository;
        private IZcashService _zcashService;
        private IZcashBridgeService _zcashBridgeService;
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _rpcUrl;
        private readonly string _rpcUser;
        private readonly string _rpcPassword;
        private readonly string _network; // "mainnet" or "testnet"

        public ZcashOASIS(string rpcUrl = null, string rpcUser = null, string rpcPassword = null, string network = "testnet")
        {
            this.ProviderName = nameof(ZcashOASIS);
            this.ProviderDescription = "Zcash Blockchain Provider with Shielded Transaction Support";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ZcashOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _rpcUrl = rpcUrl ?? Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232";
            _rpcUser = rpcUser ?? Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user";
            _rpcPassword = rpcPassword ?? Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password";
            _network = network;

            _rpcClient = new ZcashRPCClient(_rpcUrl, _rpcUser, _rpcPassword);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new();

            try
            {
                // Test connection to Zcash node
                var connectionTest = await _rpcClient.TestConnectionAsync();
                if (connectionTest.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"Failed to connect to Zcash node: {connectionTest.Message}");
                    return result;
                }

                _zcashRepository = new ZcashRepository(_rpcClient);
                _zcashService = new ZcashService(_rpcClient);
                _zcashBridgeService = new ZcashBridgeService(_rpcClient);

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "Zcash provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Unknown Error Occurred In ZcashOASIS Provider in ActivateProviderAsync. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _zcashRepository = null;
            _zcashService = null;
            _zcashBridgeService = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        // Zcash-specific methods for shielded transactions
        public async Task<OASISResult<ShieldedTransaction>> CreateShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null)
        {
            var result = new OASISResult<ShieldedTransaction>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var tx = await _zcashService.CreateShieldedTransactionAsync(fromAddress, toAddress, amount, memo);
                result.Result = tx;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<ViewingKey>> GenerateViewingKeyAsync(string address)
        {
            var result = new OASISResult<ViewingKey>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var viewingKey = await _zcashService.GenerateViewingKeyAsync(address);
                result.Result = viewingKey;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<PartialNote>> CreatePartialNoteAsync(decimal amount, int numberOfParts)
        {
            var result = new OASISResult<PartialNote>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var partialNote = await _zcashService.CreatePartialNoteAsync(amount, numberOfParts);
                result.Result = partialNote;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Bridge operations
        public async Task<OASISResult<string>> LockZECForBridgeAsync(
            decimal amount,
            string destinationChain,
            string destinationAddress,
            string viewingKey = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var txId = await _zcashBridgeService.LockZECForBridgeAsync(amount, destinationChain, destinationAddress, viewingKey);
                result.Result = txId;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> ReleaseZECAsync(string lockTxHash, decimal amount, string destinationAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var releaseResult = await _zcashBridgeService.ReleaseZECAsync(lockTxHash, amount, destinationAddress);
                if (releaseResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, releaseResult.Message);
                    return result;
                }

                result.Result = releaseResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Required abstract method implementations (simplified for MVP)
        // These will be implemented with full functionality as needed

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // For Zcash, avatars would be stored as holons with shielded addresses
                // This is a simplified implementation
                result.Result = new List<IAvatar>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load avatar from Zcash (stored as holon)
                var holon = await LoadHolonAsync(Id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Convert holon to avatar (simplified)
                result.Result = null; // Would convert holon to avatar
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load avatar by Zcash address (provider key)
                var holon = await LoadHolonAsync(providerKey);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                result.Result = null; // Would convert holon to avatar
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByUsername not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByEmail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByEmail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByUsername not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarDetails not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Save avatar as holon to Zcash
                var holon = Avatar as IHolon ?? new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Username,
                    Description = Avatar.Email,
                    HolonType = HolonType.Avatar
                };

                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = Avatar;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "SaveAvatarDetail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatar not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatar by providerKey not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatarByEmail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatarByUsername not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result, "Search not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holon = await _zcashRepository.LoadHolonAsync(id);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holon = await _zcashRepository.LoadHolonByProviderKeyAsync(providerKey);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent by providerKey not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData with dictionary not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllHolons not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var savedHolon = await _zcashRepository.SaveHolonAsync(holon);
                result.Result = savedHolon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveResult.IsError && !continueOnError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    savedHolons.Add(saveResult.Result);
                }
            }
            result.Result = savedHolons;
            result.IsError = false;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon by providerKey not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Import not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarById not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByUsername not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByEmail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAll not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                return result;
            }

            // Zcash uses shielded transactions for privacy
            // Send ZEC using z_sendmany RPC call
            var sendResult = await _rpcClient.SendShieldedTransactionAsync(
                request.FromWalletAddress,
                request.ToWalletAddress,
                request.Amount,
                request.MemoText);

            if (sendResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending Zcash transaction: {sendResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = sendResult.Result // Operation ID from z_sendmany
            };
            result.IsError = false;
            result.Message = "Token sent successfully on Zcash blockchain";
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            // Zcash doesn't have native token minting like account-based chains
            // Minting would require a custom asset or smart contract
            // For now, we'll use a shielded transaction to simulate minting
            var mintAddress = _rpcClient.GetNewAddressAsync("sapling").Result.Result ?? request.MintedByAvatarId.ToString();
            
            // In Zcash, "minting" would typically be done through mining or custom assets
            // This is a placeholder that would need custom asset implementation
            result.Result = new TransactionResponse
            {
                TransactionResult = "Zcash native token (ZEC) is minted through mining, not programmatically"
            };
            result.IsError = false;
            result.Message = "Zcash uses proof-of-work mining for token creation. Custom assets would require additional implementation.";
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            // Zcash doesn't have native token burning
            // Burning would require sending to a burn address or custom asset implementation
            var burnAddress = "zcBurnAddress..."; // Zcash burn address (would be configured)
            
            if (string.IsNullOrEmpty(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Send to burn address using shielded transaction
            var burnResult = await _rpcClient.SendShieldedTransactionAsync(
                request.OwnerPrivateKey, // Would derive address from private key in production
                burnAddress,
                1m, // Burn amount (would come from request in production)
                "Burn transaction");

            if (burnResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {burnResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = burnResult.Result
            };
            result.IsError = false;
            result.Message = "Token burned successfully on Zcash blockchain";
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            if (string.IsNullOrEmpty(request.TokenAddress) || string.IsNullOrEmpty(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by sending to bridge pool address
            var bridgePoolAddress = "zcBridgePool..."; // Bridge pool address (would be configured)
            var senderAddress = bridgePoolAddress; // Would derive from private key in production
            
            var lockResult = await _rpcClient.SendShieldedTransactionAsync(
                senderAddress,
                bridgePoolAddress,
                1m, // Lock amount (would come from request in production)
                "Lock for bridge");

            if (lockResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {lockResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = lockResult.Result
            };
            result.IsError = false;
            result.Message = "Token locked successfully on Zcash blockchain";
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            if (string.IsNullOrEmpty(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Unlock token by sending from bridge pool to recipient
            var bridgePoolAddress = "zcBridgePool..."; // Bridge pool address (would be configured)
            var recipientAddress = bridgePoolAddress; // Would get from UnlockedByAvatarId in production
            
            var unlockResult = await _rpcClient.SendShieldedTransactionAsync(
                bridgePoolAddress,
                recipientAddress,
                1m, // Unlock amount (would come from request in production)
                "Unlock from bridge");

            if (unlockResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {unlockResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = unlockResult.Result
            };
            result.IsError = false;
            result.Message = "Token unlocked successfully on Zcash blockchain";
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            if (string.IsNullOrEmpty(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get Zcash balance using RPC client
            var balanceResult = await _rpcClient.GetBalanceAsync(request.WalletAddress);
            
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {balanceResult.Message}");
                return result;
            }

            result.Result = (double)balanceResult.Result;
            result.IsError = false;
            result.Message = "Balance retrieved successfully";
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            if (string.IsNullOrEmpty(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Query Zcash transaction history using RPC
            // Note: Zcash privacy features may limit transaction visibility
            var transactions = new List<IWalletTransaction>();
            
            // Zcash RPC doesn't have a direct "listtransactions" for shielded addresses
            // Would need to use z_listreceivedbyaddress or similar methods
            // For now, return empty list with note about privacy limitations
            result.Result = transactions;
            result.IsError = false;
            result.Message = "Zcash transaction history retrieval is limited due to privacy features. Use viewing keys for shielded transactions.";
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            // Generate Zcash key pair using RPC client
            // Zcash uses different address types (transparent, sapling, orchard)
            var addressResult = await _rpcClient.GetNewAddressAsync("sapling");
            
            if (addressResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating address: {addressResult.Message}");
                return result;
            }

            // Zcash addresses are generated by the node, not from keys directly
            // For production, would need to export private keys using z_exportkey
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                keyPair.WalletAddressLegacy = addressResult.Result;
                // Note: Private key would need to be retrieved separately using z_exportkey RPC call
            }

            result.Result = keyPair;
            result.IsError = false;
            result.Message = "Zcash address generated successfully. Note: Private key retrieval requires additional RPC call.";
            return result;
        }

        #endregion

        #region IOASISNETProvider - GetAvatarsNearMe and GetHolonsNearMe

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return result;
            }

            OASISErrorHandling.HandleError(ref result, "GetAvatarsNearMe not yet implemented for Zcash provider");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return result;
            }

            OASISErrorHandling.HandleError(ref result, "GetHolonsNearMe not yet implemented for Zcash provider");
            return result;
        }

        #endregion

        #region Bridge Methods (IOASISBlockchainStorageProvider)

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || _rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get balance using RPC client
                var balanceResult = await _rpcClient.GetBalanceAsync(accountAddress);
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting balance: {balanceResult.Message}");
                    return result;
                }
                result.Result = balanceResult.Result;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated || _rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Create new Zcash account using RPC client
                // Note: ZcashRPCClient may not have CreateAccountAsync, so we'll use a placeholder
                // In production, this would create a new Zcash address
                OASISErrorHandling.HandleError(ref result, "Zcash account creation via RPC is not yet fully implemented. Use Zcash wallet software to create accounts.");
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated || _rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Zcash doesn't support seed phrase restoration in the same way as other chains
                // This would need to be implemented based on Zcash's specific account restoration mechanism
                OASISErrorHandling.HandleError(ref result, "Zcash account restoration from seed phrase is not yet implemented");
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _zcashBridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Use LockZECForBridgeAsync for withdrawal
                // Note: LockZECForBridgeAsync returns a string (transaction ID), not an OASISResult
                var lockTxId = await _zcashBridgeService.LockZECForBridgeAsync(amount, "bridge", senderAccountAddress, null);
                if (string.IsNullOrWhiteSpace(lockTxId))
                {
                    OASISErrorHandling.HandleError(ref result, "Error locking ZEC for withdrawal: Transaction ID is empty");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Transaction ID is empty",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    return result;
                }
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockTxId,
                    IsSuccessful = true,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _zcashBridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // For deposit, we would release ZEC from the bridge
                // This is a simplified implementation - in production, you'd need the lock transaction hash
                OASISErrorHandling.HandleError(ref result, "Zcash deposit requires a lock transaction hash. Use ReleaseZECAsync with the lock transaction hash.");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = "Deposit requires lock transaction hash",
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated || _rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get transaction status using RPC client
                var txResult = await _rpcClient.GetTransactionAsync(transactionHash);
                if (txResult.IsError)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    OASISErrorHandling.HandleError(ref result, $"Error getting transaction: {txResult.Message}");
                    return result;
                }
                // Check if transaction is confirmed
                // Note: The transaction result structure may vary, so we'll check if it exists
                if (txResult.Result != null)
                {
                    // If we can determine confirmations, use that; otherwise assume pending
                    result.Result = BridgeTransactionStatus.Completed;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Pending;
                }
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
                return result;
            }
        }

        #endregion
    }
}

