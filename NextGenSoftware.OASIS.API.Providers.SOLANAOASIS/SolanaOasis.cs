using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS
{
    public class SolanaOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private ISolanaRepository _solanaRepository;
        private ISolanaService _solanaService;
        private KeyManager _keyManager;
        private WalletManager _walletManager;
        private string _mnemonicWords = "";

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

                return _keyManager;
            }
        }
        
        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

                return _walletManager;
            }
        }
        
        public SolanaOASIS(string mnemonicWords)
        {
            _mnemonicWords = mnemonicWords;
            this.ProviderName = nameof(SolanaOASIS);
            this.ProviderDescription = "Solana Blockchain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _solanaRepository = new SolanaRepository(_mnemonicWords);
                _solanaService = new SolanaService();

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In SolanaOASIS Provider in ActivateProviderAsync. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _solanaRepository = new SolanaRepository(_mnemonicWords);
                _solanaService = new SolanaService();

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In SolanaOASIS Provider in ActivateProvider. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _solanaRepository = null;
            _solanaService = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _solanaRepository = null;
            _solanaService = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var solanaAvatarDto = await _solanaRepository.GetAsync<SolanaAvatarDto>(providerKey);

                result.IsLoaded = true;
                result.IsError = false;
                result.Result = solanaAvatarDto.GetAvatar();
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                string transactionHash;
                // Update if avatar if transaction hash exist
                if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                    avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var avatarSolanaHash))
                {
                    var solanaAvatarDto = await _solanaRepository.GetAsync<SolanaAvatarDto>(avatarSolanaHash);
                    transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDto);
                    
                }
                // Create avatar if transaction hash not exist
                else
                {
                    var solanaAvatarDto = avatar.GetSolanaAvatarDto();
                    transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDto);
                }

                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return SaveAvatarDetailAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                string transactionHash;
                // Update if avatar if transaction hash exist
                if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                    avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var avatarDetailSolanaHash))
                {
                    var solanaAvatarDetailDto = await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                    transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
                }
                // Create avatar if transaction hash not exist
                else
                {
                    var solanaAvatarDetailDto = avatar.GetSolanaAvatarDetailDto();
                    transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
                }
                
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
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
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var deleteResult = await _solanaRepository.DeleteAsync(providerKey);
                
                result.IsError = !deleteResult;
                result.IsSaved = deleteResult;
                result.Result = deleteResult;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var solanaHolonDto = await _solanaRepository.GetAsync<SolanaHolonDto>(providerKey);

                result.IsLoaded = true;
                result.IsError = false;
                result.Result = solanaHolonDto.GetHolon();
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
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
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }
        
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                string transactionHash;
                // Update if avatar if transaction hash exist
                if (holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                    holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var avatarDetailSolanaHash))
                {
                    var solanaAvatarDetailDto = await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                    transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
                }
                // Create avatar if transaction hash not exist
                else
                {
                    var solanaAvatarDetailDto = holon.GetSolanaHolonDto();
                    transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
                }
                
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                if (saveChildren)
                {
                    var holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        holon.Children = holonsResult.Result;
                    else
                        OASISErrorHandling.HandleWarning(ref result, $"{holonsResult?.Message} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult?.Message}");
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            var errorMessage = "Error occured in SaveHolonsAsync method in SolanaOASIS Provider";
            var result = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                foreach (var holon in holons)
                {
                    string transactionHash;
                    // Update if avatar if transaction hash exist
                    if (holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                        holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var avatarDetailSolanaHash))
                    {
                        var solanaAvatarDetailDto = await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                        transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
                    }
                    // Create avatar if transaction hash not exist
                    else
                    {
                        var solanaAvatarDetailDto = holon.GetSolanaHolonDto();
                        transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
                    }
                    
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                    if(string.IsNullOrEmpty(transactionHash))
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: transaction processing failed!");
                        if (!continueOnError)
                            break;
                    }

                    //TODO: Need to apply this to Mongo & IPFS, etc too...
                    if ((saveChildren && !recursive && currentChildDepth == 0) || saveChildren && recursive && currentChildDepth >= 0 && (maxChildDepth == 0 || (maxChildDepth > 0 && currentChildDepth <= maxChildDepth)))
                    {
                        currentChildDepth++;
                        var holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            holon.Children = holonsResult.Result;
                        else
                        {
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult?.Message}");
                            if (!continueOnError)
                                break;
                        }
                    }
                }

                result.Result = holons;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return DeleteHolonAsync(providerKey, softDelete).Result;
        }
        
        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var deleteResult = await _solanaRepository.DeleteAsync(providerKey);
                
                result.IsError = !deleteResult;
                result.IsSaved = deleteResult;
                result.Result = deleteResult;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, e.Message);
            }
            return result;
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transaction)
        {
            OASISResult<ITransactionRespone> result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error occured in SendTransactionAsync method in SolanaOASIS Provider. Reason: ";

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            try
            {
                var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
                {
                    Amount = (ulong) transaction.Amount,
                    FromAccount = new BaseAccountRequest()
                    {
                        PublicKey = transaction.FromWalletAddress
                    },
                    ToAccount = new BaseAccountRequest()
                    {
                        PublicKey = transaction.ToWalletAddress
                    }
                });

                if (solanaTransactionResult.IsError ||
                    string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, solanaTransactionResult.Message);
                    return result;
                }

                result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}, {e.Message}", e);
            }

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            var errorMessageTemplate = "Error was occured in SendTransactionByIdAsync method in SolanaOASIS while sending transaction. Reason: ";

            var senderAvatarPublicKeyResult = KeyManager.GetProviderPublicKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
            var receiverAvatarPublicKeyResult = KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

            if (senderAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                    senderAvatarPublicKeyResult.Exception);
                return result;
            }

            if (receiverAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                    receiverAvatarPublicKeyResult.Exception);
                return result;
            }

            var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
            var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];
            result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount); 
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message), result.Exception);
            
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            var errorMessageTemplate = "Error was occured in SendTransactionByUsernameAsync method in SolanaOASIS while sending transaction. Reason: ";

            var senderAvatarPublicKeyResult = KeyManager.GetProviderPublicKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.SolanaOASIS);
            var receiverAvatarPublicKeyResult = KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, Core.Enums.ProviderType.SolanaOASIS);

            if (senderAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                    senderAvatarPublicKeyResult.Exception);

                return result;
            }

            if (receiverAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                    receiverAvatarPublicKeyResult.Exception);
                return result;
            }

            var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
            var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

            result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount); 

            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            var errorMessageTemplate = "Error was occured in SendTransactionByEmailAsync method in SolanaOASIS while sending transaction. Reason: ";

            var senderAvatarPublicKeysResult = KeyManager.GetProviderPublicKeysForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);
            var receiverAvatarPublicKeyResult = KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);

            if (senderAvatarPublicKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                    senderAvatarPublicKeysResult.Exception);
                return result;
            }

            if (receiverAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                    receiverAvatarPublicKeyResult.Exception);
                return result;
            }

            var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result[0];
            var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

            result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount); 

            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message), result.Exception);
            
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
            var errorMessageTemplate = "Error was occured in SendTransactionByDefaultWallet method in SolanaOASIS while sending transaction. Reason: ";

            var senderAvatarPublicKeysResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
            var receiverAvatarPublicKeyResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

            if (senderAvatarPublicKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                    senderAvatarPublicKeysResult.Exception);
                return result;
            }

            if (receiverAvatarPublicKeyResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                    receiverAvatarPublicKeyResult.Exception);
                return result;
            }

            var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result.PublicKey;
            var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result.PublicKey;
            result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount); 

            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message), result.Exception);
            
            return result;
        }

        private async Task<OASISResult<ITransactionRespone>> SendSolanaTransaction(string fromAddress, string toAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            var errorMessageTemplate = "Error was occured in SendSolanaTransaction method in SolanaOASIS while sending transaction. Reason: ";
            
            try
            {
                var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
                {
                    Amount = (ulong)amount,
                    FromAccount = new BaseAccountRequest()
                    {
                        PublicKey = fromAddress
                    },
                    ToAccount = new BaseAccountRequest()
                    {
                        PublicKey = toAddress
                    }
                });

                if (solanaTransactionResult.IsError ||
                    string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, solanaTransactionResult.Message), solanaTransactionResult.Exception);
                    return result;
                }

                result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, e.Message), e);
            }

            return result;
        }

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            if (transation == null)
                throw new ArgumentNullException(nameof(transation));

            var result = new OASISResult<INFTTransactionRespone>();
            var errorMessageTemplate = "Error was occured in SendNFTAsync in SolanaOASIS sending nft. Reason: ";
            
            try
            {
                var solanaNftTransactionResult = await _solanaService.MintNftAsync(new MintNftRequest()
                {
                    Amount = (ulong) transation.Amount,
                    FromAccount = new BaseAccountRequest()
                    {
                        PublicKey = transation.FromWalletAddress
                    },
                    ToAccount = new BaseAccountRequest()
                    {
                        PublicKey = transation.ToWalletAddress
                    }
                });

                if (solanaNftTransactionResult.IsError ||
                    string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, solanaNftTransactionResult.Message), solanaNftTransactionResult.Exception);
                    return result;
                }

                result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, e.Message), e);
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            if (transation == null)
                throw new ArgumentNullException(nameof(transation));

            var result = new OASISResult<INFTTransactionRespone>();
            var errorMessageTemplate = "Error was occured in MintNFTAsync in SolanaOASIS minting NFT. Reason: ";

            try
            {
                MintNftRequest mintRequest = new MintNftRequest()
                {
                    Amount = (ulong)transation.Price,
                    MintAccount = new BaseAccountRequest()
                    {
                        PublicKey = transation.MintWalletAddress
                    },
                    ToAccount = new BaseAccountRequest()
                    {
                        PublicKey = transation.MintWalletAddress
                    },

                    MemoText = $"Solana NFT minted on The OASIS with title {transation.Title} by AvatarId {transation.MintedByAvatarId} for price {transation.Price}. {transation.MemoText}"
                };

                var solanaNftTransactionResult = await _solanaService.MintNftAsync(mintRequest);

                if (solanaNftTransactionResult.IsError ||
                    string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, solanaNftTransactionResult.Message), solanaNftTransactionResult.Exception);
                    return result;
                }
                else
                {
                    result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;
                    
                    if (!TransactionHelper.CheckForTransactionErrors(ref result))
                    {
                        //The meta data is normally stored on another provider and it is up to NFTManager which offchain provider is used,..
                        //Holon holonNFT = new Holon(HolonType.NFT);
                        //holonNFT.Name = $"Solana NFT Minted On The OASIS with title {transation.Title}";
                        //holonNFT.Description = mintRequest.MemoText;
                        //holonNFT.MetaData["NFT.Hash"] = result.Result.TransactionResult;

                        //OASISResult<IHolon> saveHolonResult =  await SaveHolonAsync(holonNFT);
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, e.Message), e);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNFT(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
        {
            var result = new OASISResult<IOASISNFT>();

            try
            {
                var solanaHolonDto = await _solanaRepository.GetAsync<SolanaHolonDto>(id.ToString()); //TODO: Need to think how this will work more...

                result.IsLoaded = true;
                result.IsError = false;
                IHolon holon = solanaHolonDto.GetHolon();

                if (holon != null) 
                {
                    result.Result = new OASISNFT()
                    {
                         //TODO: Come back to this! ;-)
                    };
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SolanaOASIS Provider. Reason: {e.Message}");
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNFT(string hash)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
        {
            var result = new OASISResult<IOASISNFT>();

            try
            {
                var solanaHolonDto = await _solanaRepository.GetAsync<SolanaHolonDto>(hash); //TODO: Need to think how this will work more...

                result.IsLoaded = true;
                result.IsError = false;
                IHolon holon = solanaHolonDto.GetHolon();

                if (holon != null)
                {
                    result.Result = new OASISNFT()
                    {
                        //TODO: Come back to this! ;-)
                    };
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SolanaOASIS Provider. Reason: {e.Message}");
            }

            return result;
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}
