using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS
{
    public class SolanaOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private readonly ISolanaRepository _solanaRepository;
        private readonly ISolanaService _solanaService;
        
        public SolanaOASIS(string mnemonicWords)
        {
            this.ProviderName = nameof(SolanaOASIS);
            this.ProviderDescription = "Solana Blockchain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _solanaRepository = new SolanaRepository(mnemonicWords);
            _solanaService = new SolanaService();
        }


        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<Avatar> holonResult = await _solanaRepository.GetAsync<Avatar>(providerKey);
            OASISResult<IAvatar> result = new OASISResult<IAvatar>(holonResult.Result);
            OASISResultHolonToHolonHelper<Avatar, IAvatar>.CopyResult(holonResult, result);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            OASISResult<Avatar> holonResult = _solanaRepository.Get<Avatar>(providerKey);
            OASISResult<IAvatar> result = new OASISResult<IAvatar>(holonResult.Result);
            OASISResultHolonToHolonHelper<Avatar, IAvatar>.CopyResult(holonResult, result);
            return result;
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

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
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
            OASISResult<Avatar> avatarResult = avatar.IsNewHolon ?
                _solanaRepository.Create((Avatar)avatar) : _solanaRepository.Update((Avatar)avatar);

            OASISResult<IAvatar> result = new OASISResult<IAvatar>(avatarResult.Result);
            OASISResultHolonToHolonHelper<Avatar, IAvatar>.CopyResult(avatarResult, result);
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<Avatar> avatarResult = avatar.IsNewHolon ?
                await _solanaRepository.CreateAsync((Avatar)avatar) : await _solanaRepository.UpdateAsync((Avatar)avatar);

            OASISResult<IAvatar> result = new OASISResult<IAvatar>(avatarResult.Result);
            OASISResultHolonToHolonHelper<Avatar, IAvatar>.CopyResult(avatarResult, result);
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            OASISResult<AvatarDetail> avatarDetailResult =  avatar.IsNewHolon ?
                _solanaRepository.Create((AvatarDetail) avatar) : _solanaRepository.Update((AvatarDetail)avatar);

            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>(avatarDetailResult.Result);
            OASISResultHolonToHolonHelper<AvatarDetail, IAvatarDetail>.CopyResult(avatarDetailResult, result);
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            OASISResult<AvatarDetail> avatarDetailResult = avatar.IsNewHolon ?
                 await _solanaRepository.CreateAsync((AvatarDetail)avatar) : await _solanaRepository.UpdateAsync((AvatarDetail)avatar);

            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>(avatarDetailResult.Result);
            OASISResultHolonToHolonHelper<AvatarDetail, IAvatarDetail>.CopyResult(avatarDetailResult, result);
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
            return _solanaRepository.Delete<Avatar>(providerKey);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Avatar>(providerKey);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<Holon> holonResult = _solanaRepository.Get<Holon>(providerKey);
            OASISResult<IHolon> result = new OASISResult<IHolon>(holonResult.Result);
            OASISResultHolonToHolonHelper<Holon, IHolon>.CopyResult(holonResult, result);
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<Holon> holonResult = await _solanaRepository.GetAsync<Holon>(providerKey);
            OASISResult<IHolon> result = new OASISResult<IHolon>(holonResult.Result);
            OASISResultHolonToHolonHelper<Holon, IHolon>.CopyResult(holonResult, result);
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
            string errorMessage = "Error occured in SaveHolon method in SolanaOASIS Provider";
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<Holon> holonResult = null;

            try
            {
                holonResult = holon.IsNewHolon ?
                    _solanaRepository.Create((Holon)holon) : _solanaRepository.Update((Holon)holon);

                if (!(holonResult != null && !holonResult.IsError && holonResult.Result != null))
                    ErrorHandling.HandleError(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: {holonResult.Message}");

                if ((holonResult != null && !holonResult.IsError && holonResult.Result != null) || continueOnError)
                {
                    holon = holonResult.Result;

                    //if ((saveChildren && !recursive && maxChildDepth == 0) || saveChildren && recursive && maxChildDepth >= 0)
                    if (saveChildren)
                    {
                        OASISResult<IEnumerable<IHolon>> holonsResult = SaveHolons(holon.Children, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            holon.Children = holonsResult.Result;
                        else
                            ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult.Message}");
                    }
                }

                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            string errorMessage = "Error occured in SaveHolonAsync method in SolanaOASIS Provider";
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<Holon> holonResult = null;

            try
            {
                holonResult = holon.IsNewHolon ?
                    await _solanaRepository.CreateAsync((Holon)holon) : await _solanaRepository.UpdateAsync((Holon)holon);

                if (!(holonResult != null && !holonResult.IsError && holonResult.Result != null))
                    ErrorHandling.HandleError(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: {holonResult.Message}");

                if ((holonResult != null && !holonResult.IsError && holonResult.Result != null) || continueOnError)
                {
                    holon = holonResult.Result;

                    //if ((saveChildren && !recursive && maxChildDepth == 0) || saveChildren && recursive && maxChildDepth >= 0)
                    if (saveChildren)
                    {
                        OASISResult<IEnumerable<IHolon>> holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            holon.Children = holonsResult.Result;
                        else
                            ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult.Message}");
                    }
                }

                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            string errorMessage = "Error occured in SaveHolonsAsync method in SolanaOASIS Provider";
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<Holon> holonResult = null;
            List<IHolon> savedHolons = new List<IHolon>();

            try
            {
                foreach (var holon in holons)
                {
                    holonResult = holon.IsNewHolon ?
                        _solanaRepository.Create((Holon)holon) : _solanaRepository.Update((Holon)holon);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                        savedHolons.Add(holonResult.Result);
                    else
                    {
                        ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: {holonResult.Message}");

                        if (!continueOnError)
                            break;
                    }

                    //TODO: Need to apply this to Mongo & IPFS, etc too...
                    if ((saveChildren && !recursive && currentChildDepth == 0) || saveChildren && recursive && currentChildDepth >= 0 && (maxChildDepth == 0 || (maxChildDepth > 0 && currentChildDepth <= maxChildDepth)))
                    {
                        currentChildDepth++;
                        OASISResult<IEnumerable<IHolon>> holonsResult = SaveHolons(holon.Children, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            holon.Children = holonsResult.Result;
                        else
                        {
                            ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult.Message}");

                            if (!continueOnError)
                                break;
                        }
                    }
                }

                result.Result = holons;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            string errorMessage = "Error occured in SaveHolonsAsync method in SolanaOASIS Provider";
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<Holon> holonResult = null;
            List<IHolon> savedHolons = new List<IHolon>();
            
            try
            {
                foreach (var holon in holons)
                {
                    holonResult =  holon.IsNewHolon ?
                        await _solanaRepository.CreateAsync((Holon)holon) : await _solanaRepository.UpdateAsync((Holon)holon);
                    
                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                        savedHolons.Add(holonResult.Result);
                    else
                    {
                        ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: {holonResult.Message}");

                        if (!continueOnError)
                            break;
                    }

                    //TODO: Need to apply this to Mongo & IPFS, etc too...
                    if ((saveChildren && !recursive && currentChildDepth == 0) || saveChildren && recursive && currentChildDepth >= 0 && (maxChildDepth == 0 || (maxChildDepth > 0 && currentChildDepth <= maxChildDepth)))
                    {
                        currentChildDepth++;
                        OASISResult<IEnumerable<IHolon>> holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            holon.Children = holonsResult.Result;
                        else
                        {
                            ErrorHandling.HandleWarning(ref result, $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult.Message}");

                            if (!continueOnError)
                                break;
                        }
                    }
                }

                result.Result = holons;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Holon>(providerKey);
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

        public OASISResult<bool> SendTransaction(IWalletTransaction transation)
        {
            return SendTransactionAsync(transation).Result;
        }

        public async Task<OASISResult<bool>> SendTransactionAsync(IWalletTransaction transation)
        {
            if (transation == null)
                throw new ArgumentNullException(nameof(transation));
            var result = new OASISResult<bool>();
            try
            {
                var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
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

                if (solanaTransactionResult.IsError ||
                    string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
                {
                    ErrorHandling.HandleError(ref result, solanaTransactionResult.Message);

                    result.Result = false;
                    result.Message = $"Transaction performing failed! Reason: {solanaTransactionResult.Message}";
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, e.Message);

                result.Result = false;
                result.Message = "Transaction performing failed! Please try again later!";
            }

            return result;
        }

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            if (transation == null)
                throw new ArgumentNullException(nameof(transation));
            var result = new OASISResult<bool>();
            try
            {
                var solanaNftTransactionResult = await _solanaService.MintNft(new MintNftRequest()
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
                    ErrorHandling.HandleError(ref result, solanaNftTransactionResult.Message);

                    result.Result = false;
                    result.Message = $"NFT transaction performing failed! Reason: {solanaNftTransactionResult.Message}";
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, e.Message);

                result.Result = false;
                result.Message = "NFT transaction performing failed! Please try again later!";
            }

            return result;
        }
    }
}
