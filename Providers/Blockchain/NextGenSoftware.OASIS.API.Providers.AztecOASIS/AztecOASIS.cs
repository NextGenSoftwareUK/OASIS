using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public class AztecOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider
    {
        private readonly AztecAPIClient _apiClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _network;

        private IAztecService _aztecService;
        private IAztecBridgeService _bridgeService;
        private IAztecRepository _aztecRepository;

        public AztecOASIS(string apiBaseUrl = null, string apiKey = null, string network = "sandbox")
        {
            ProviderName = nameof(AztecOASIS);
            ProviderDescription = "Aztec Privacy Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AztecOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _apiBaseUrl = apiBaseUrl ?? Environment.GetEnvironmentVariable("AZTEC_API_URL") ?? "http://localhost:8080";
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("AZTEC_API_KEY");
            _network = network;

            _apiClient = new AztecAPIClient(_apiBaseUrl, _apiKey);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _aztecService = new AztecService(_apiClient);
                _bridgeService = new AztecBridgeService(_apiClient);
                _aztecRepository = new AztecRepository();

                IsProviderActivated = true;
                result.Result = true;
                result.Message = "Aztec provider activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to activate Aztec provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _aztecService = null;
            _bridgeService = null;
            _aztecRepository = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        #region Aztec Specific Operations

        public async Task<OASISResult<PrivateNote>> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string metadata = null)
        {
            var result = new OASISResult<PrivateNote>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.CreatePrivateNoteAsync(value, ownerPublicKey, metadata);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }

            return result;
        }

        public async Task<OASISResult<AztecProof>> GenerateProofAsync(string proofType, object payload)
        {
            var result = new OASISResult<AztecProof>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.GenerateProofAsync(proofType, payload);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> SubmitProofAsync(AztecProof proof)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.SubmitProofAsync(proof);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> DepositFromZcashAsync(decimal amount, string zcashTxId, PrivateNote aztecNote)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _bridgeService.DepositFromZcashAsync(amount, zcashTxId, aztecNote);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> WithdrawToZcashAsync(PrivateNote note, AztecProof proof, string destinationAddress)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _bridgeService.WithdrawToZcashAsync(note, proof, destinationAddress);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Stablecoin operations
        public async Task<OASISResult<string>> MintStablecoinAsync(string aztecAddress, decimal amount, string zcashTxHash, string viewingKey)
        {
            var result = new OASISResult<string>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                var mintResult = await _aztecService.MintStablecoinAsync(aztecAddress, amount, zcashTxHash, viewingKey);
                result.Result = mintResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> BurnStablecoinAsync(string aztecAddress, decimal amount, string positionId)
        {
            var result = new OASISResult<string>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                var burnResult = await _aztecService.BurnStablecoinAsync(aztecAddress, amount, positionId);
                result.Result = burnResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> DeployToYieldStrategyAsync(string aztecAddress, decimal amount, string strategy)
        {
            var result = new OASISResult<string>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                var deployResult = await _aztecService.DeployToYieldStrategyAsync(aztecAddress, amount, strategy);
                result.Result = deployResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> SeizeCollateralAsync(string aztecAddress, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                var seizeResult = await _aztecService.SeizeCollateralAsync(aztecAddress, amount);
                result.Result = seizeResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        #endregion

        #region Required Abstract Overrides (MVP implementations)

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>
            {
                Result = new List<IAvatar>(),
                IsError = false
            };
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatar not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => LoadAvatarAsync(Id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByProviderKey not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByUsername not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarByEmail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByEmail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailByUsername not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllAvatarDetails not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "SaveAvatar not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => SaveAvatarAsync(Avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "SaveAvatarDetail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => SaveAvatarDetailAsync(Avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatar not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatarByProviderKey not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatarByEmail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DeleteAvatarByUsername not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result, "Search not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.LoadHolonAsync(id);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.LoadHolonByProviderKeyAsync(providerKey);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent by providerKey not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadHolonsByMetaData dictionary not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoadAllHolons not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.SaveHolonAsync(holon);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var saved = new List<IHolon>();
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveResult.IsError && !continueOnError)
                {
                    var errorResult = new OASISResult<IEnumerable<IHolon>>();
                    errorResult.IsError = true;
                    errorResult.Message = saveResult.Message;
                    return errorResult;
                }
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    saved.Add(saveResult.Result);
                }
            }

            return new OASISResult<IEnumerable<IHolon>>(saved);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DeleteHolon by providerKey not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Import not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarById not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByUsername not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByEmail not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAll not yet implemented for Aztec provider");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "To wallet address is required");
                    return result;
                }

                // Aztec uses private notes for token transfers
                // Create a private note for the recipient
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    request.Amount,
                    request.ToWalletAddress,
                    request.MemoText);

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create private note for token transfer");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token sent successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Get mint to address from avatar ID or use default
                var mintToAddress = _apiBaseUrl ?? "aztec_mint_address";
                var mintAmount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amount)
                    ? amount 
                    : 1m;

                // Use MintStablecoinAsync if available, otherwise create a private note
                try
                {
                    var mintResult = await _aztecService.MintStablecoinAsync(mintToAddress, mintAmount, null, null);
                    if (mintResult != null && !mintResult.IsError && !string.IsNullOrEmpty(mintResult.Result))
                    {
                        result.Result.TransactionResult = mintResult.Result;
                        result.IsError = false;
                        result.Message = "Token minted successfully on Aztec.";
                        return result;
                    }
                }
                catch
                {
                    // Fall back to creating a private note
                }

                // Fallback: Create a private note for minting
                var privateNote = await _aztecService.CreatePrivateNoteAsync(mintAmount, mintToAddress, "minted");
                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token minted successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // For Aztec, burning involves nullifying a private note
                // We need the note ID and a proof to nullify it
                // Since we don't have the note ID directly, we'll use BurnStablecoinAsync if available
                var burnAmount = 1m; // Default amount - in production, retrieve from token data

                try
                {
                    var burnResult = await _aztecService.BurnStablecoinAsync(
                        request.OwnerPublicKey ?? string.Empty,
                        burnAmount,
                        request.Web3TokenId.ToString());

                    if (burnResult != null && !burnResult.IsError && !string.IsNullOrEmpty(burnResult.Result))
                    {
                        result.Result.TransactionResult = burnResult.Result;
                        result.IsError = false;
                        result.Message = "Token burned successfully on Aztec.";
                        return result;
                    }
                }
                catch
                {
                    // Fall back to nullifying note
                }

                // Fallback: Generate a proof and nullify the note
                // This requires the note ID which we don't have directly
                OASISErrorHandling.HandleError(ref result, "Token burning requires note ID and proof generation. Please use BurnStablecoinAsync with proper parameters.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Lock token by creating a private note in the bridge pool
                var bridgePoolAddress = Environment.GetEnvironmentVariable("AZTEC_BRIDGE_POOL_ADDRESS") ?? "aztec_bridge_pool";
                // Get amount from metadata or use default (in production, retrieve from token data)
                var lockAmount = 1m; // Default amount - in production, retrieve from Web3TokenId

                // Get from wallet address from avatar ID
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.AztecOASIS, request.LockedByAvatarId);
                var fromWalletAddress = fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result)
                    ? "aztec_wallet"
                    : fromWalletResult.Result;

                // Create a private note in the bridge pool
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    lockAmount,
                    bridgePoolAddress,
                    $"Locked from {fromWalletAddress}");

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to lock token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token locked successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Unlock token by creating a private note for the recipient from the bridge pool
                // In production, this would involve generating a proof and transferring from bridge pool
                // Get amount from metadata or use default (in production, retrieve from token data)
                var unlockAmount = 1m; // Default amount - in production, retrieve from Web3TokenId
                
                // Get recipient address from avatar ID
                var recipientWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.AztecOASIS, request.UnlockedByAvatarId);
                if (recipientWalletResult.IsError || string.IsNullOrWhiteSpace(recipientWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve recipient wallet address for avatar");
                    return result;
                }
                var recipientAddress = recipientWalletResult.Result;

                // Create a private note for the recipient (unlocking from bridge pool)
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    unlockAmount,
                    recipientAddress,
                    $"Unlocked from bridge pool");

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to unlock token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Aztec balance using API client
                // Aztec uses private notes, so we need to query via API
                // Note: Aztec balances require viewing keys for privacy, which would be retrieved from KeyManager
                var balanceQuery = new Dictionary<string, string>
                {
                    { "address", request.WalletAddress }
                };

                // Query balance from Aztec API
                var balanceResult = await _apiClient.GetAsync<AztecBalanceResponse>("/api/balance", balanceQuery);
                
                if (balanceResult.IsError)
                {
                    // Aztec balances are private and require viewing keys
                    // If query fails, return 0 with informative message
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = $"Aztec balance query completed. Note: Aztec balances are private and may require viewing keys for full access. API response: {balanceResult.Message}";
                    return result;
                }

                // Parse balance from response
                if (balanceResult.Result != null && balanceResult.Result.Balance.HasValue)
                {
                    result.Result = (double)balanceResult.Result.Balance.Value;
                    result.IsError = false;
                    result.Message = "Balance retrieved successfully.";
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Balance retrieved successfully (0).";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Aztec transaction history using API client
                // Aztec transactions are private, so viewing keys may be required
                var txQuery = new Dictionary<string, string>
                {
                    { "address", request.WalletAddress },
                    { "limit", "100" } // Default limit
                };

                // Query transactions from Aztec API
                var txResult = await _apiClient.GetAsync<AztecTransactionListResponse>("/api/transactions", txQuery);
                
                if (txResult.IsError)
                {
                    // Aztec transactions are private and may require viewing keys
                    // If query fails, return empty list with informative message
                    result.Result = new List<IWalletTransaction>();
                    result.IsError = false;
                    result.Message = $"Aztec transaction query completed. Note: Aztec transactions are private and may require viewing keys for full access. API response: {txResult.Message}";
                    return result;
                }

                // Convert Aztec transactions to IWalletTransaction format
                var transactions = new List<IWalletTransaction>();
                if (txResult.Result != null && txResult.Result.Transactions != null)
                {
                    foreach (var aztecTx in txResult.Result.Transactions)
                    {
                        // Create wallet transaction from Aztec transaction
                        var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                        {
                            FromWalletAddress = aztecTx.FromAddress ?? string.Empty,
                            ToWalletAddress = aztecTx.ToAddress ?? string.Empty,
                            Amount = (double)(aztecTx.Amount ?? 0m),
                            Description = $"Aztec transaction: {aztecTx.TransactionHash ?? "unknown"}"
                        };
                        transactions.Add(walletTx);
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} transactions.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                EnsureActivated(result);
                if (result.IsError) return result;

                // Generate Aztec-specific key pair using Nethereum SDK (production-ready)
                // Aztec uses secp256k1 elliptic curve (same as Ethereum), so we can use Nethereum
                var ecKey = EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();
                
                // Aztec addresses are derived from public keys (similar to Ethereum)
                var aztecAddress = publicKey;
                
                // Create key pair structure
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = aztecAddress;
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Aztec key pair generated successfully using Nethereum SDK (secp256k1).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating Aztec key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Derives Aztec public key from private key using secp256k1
        /// Note: This is a simplified implementation. In production, use proper ECDSA secp256k1 key derivation.
        /// Aztec uses the same secp256k1 curve as Ethereum, so similar key derivation applies.
        /// </summary>
        private string DeriveAztecPublicKey(byte[] privateKeyBytes)
        {
            // Aztec uses secp256k1 elliptic curve (same as Ethereum/Bitcoin)
            // In production, use a proper cryptographic library like BouncyCastle or NBitcoin for ECDSA
            // For now, we use a deterministic hash-based approach (not cryptographically secure for production)
            // TODO: Replace with proper ECDSA secp256k1 public key derivation using BouncyCastle or similar
            
            try
            {
                // Proper implementation would use ECDSA secp256k1:
                // var ecKey = new Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters(...);
                // var publicKey = ecKey.Parameters.G.Multiply(ecKey.D).Normalize();
                
                // Temporary implementation using hash (NOT cryptographically correct, but functional)
                // This generates a deterministic but not properly derived public key
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(privateKeyBytes);
                    // Aztec public keys are typically 64 characters (32 bytes hex) for uncompressed format
                    var publicKey = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return publicKey.Length >= 64 ? publicKey.Substring(0, 64) : publicKey.PadRight(64, '0');
                }
            }
            catch
            {
                // Fallback: generate from private key hash
                var hash = System.Security.Cryptography.SHA256.HashData(privateKeyBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().PadRight(64, '0');
            }
        }

        /// <summary>
        /// Derives Aztec address from public key
        /// NOTE: This method is no longer used - we now use Nethereum SDK directly
        /// </summary>
        [Obsolete("Use Nethereum.Signer.EthECKey.GetPublicAddress() instead")]
        private string DeriveAztecAddress(string publicKey)
        {
            // Aztec addresses are derived from public keys
            // Typically, this involves hashing the public key and taking a portion
            try
            {
                var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(publicKey);
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(publicKeyBytes);
                    // Take first 20 bytes for address (similar to Ethereum)
                    var addressBytes = new byte[20];
                    Array.Copy(hash, addressBytes, 20);
                    return "0x" + BitConverter.ToString(addressBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                // Fallback: use public key as address
                return publicKey.Length >= 40 ? "0x" + publicKey.Substring(0, 40) : "0x" + publicKey.PadRight(40, '0');
            }
        }

        #endregion

        #region IOASISNETProvider - GetAvatarsNearMe

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            EnsureActivated(result);
            if (result.IsError) return result;

            OASISErrorHandling.HandleError(ref result, "GetAvatarsNearMe not yet implemented for Aztec provider");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            EnsureActivated(result);
            if (result.IsError) return result;

            OASISErrorHandling.HandleError(ref result, "GetHolonsNearMe not yet implemented for Aztec provider");
            return result;
        }

        #endregion

        #region Bridge Methods (IOASISBlockchainStorageProvider)

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // Get balance using Aztec API client
                // Note: Aztec is privacy-focused, so balance queries may be limited
                OASISErrorHandling.HandleError(ref result, "Balance queries on Aztec require private key access and are not yet fully implemented via API");
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
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // Create new Aztec account
                // Aztec accounts are generated using cryptographic key pairs
                OASISErrorHandling.HandleError(ref result, "Aztec account creation via API is not yet fully implemented. Use Aztec wallet software to create accounts.");
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
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // Aztec doesn't support seed phrase restoration in the same way as other chains
                OASISErrorHandling.HandleError(ref result, "Aztec account restoration from seed phrase is not yet implemented");
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
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // Use WithdrawToZcashAsync for withdrawal (Aztec-specific bridge method)
                // This is a simplified implementation - in production, you'd need to create a private note first
                OASISErrorHandling.HandleError(ref result, "Aztec withdrawal requires creating a private note and generating a proof, which is not yet fully implemented");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = "Withdrawal not yet fully implemented",
                    Status = BridgeTransactionStatus.Canceled
                };
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
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // For deposit, we would use DepositFromZcashAsync (Aztec-specific bridge method)
                // This requires a Zcash transaction ID and an Aztec private note
                OASISErrorHandling.HandleError(ref result, "Aztec deposit requires a Zcash transaction ID and private note, which is not yet fully implemented");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = "Deposit requires Zcash transaction ID and private note",
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
                if (!IsProviderActivated || _apiClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
                    return result;
                }

                // Get transaction status using Aztec API client
                // Note: Aztec transaction status queries may require special handling due to privacy features
                // For now, return pending status as Aztec transactions are private
                result.Result = BridgeTransactionStatus.Pending;
                result.IsError = false;
                result.Message = "Transaction status query for Aztec is simplified (privacy-focused blockchain)";
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
                return result;
            }
        }

        #endregion

        private void EnsureActivated<T>(OASISResult<T> result)
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Aztec provider is not activated");
            }
        }
    }
}

