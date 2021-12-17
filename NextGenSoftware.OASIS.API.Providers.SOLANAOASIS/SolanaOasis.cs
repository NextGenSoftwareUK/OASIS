using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS
{
    public class SolanaOASIS : OASISStorageBase, IOASISBlockchainStorage, IOASISNET
    {
        private readonly ISolanaRepository _solanaRepository;
        public SolanaOASIS(string mnemonicWords)
        {
            this.ProviderName = nameof(SolanaOASIS);
            this.ProviderDescription = "Solana Blockchain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _solanaRepository = new SolanaRepository(mnemonicWords);
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

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Avatar>(providerKey);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Avatar>(providerKey);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var transactionSignature =  holon.Id == Guid.Empty ? 
                        await _solanaRepository.CreateAsync((Holon)holon) : await _solanaRepository.UpdateAsync((Holon)holon);
                    holon.ProviderKey = new Dictionary<ProviderType, string>() { { Core.Enums.ProviderType.SolanaOASIS, transactionSignature } };
                }
                response.Result = holons;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.IsError = true;
                response.Message = e.Message;
                response.IsSaved = false;
            }

            return response;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Holon>(providerKey);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            OASISResult<Holon> holonResult = _solanaRepository.Get<Holon>(providerKey);
            OASISResult<IHolon> result = new OASISResult<IHolon>(holonResult.Result);
            OASISResultHolonToHolonHelper<Holon, IHolon>.CopyResult(holonResult, result);
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            OASISResult<Holon> holonResult = await _solanaRepository.GetAsync<Holon>(providerKey);
            OASISResult<IHolon> result = new OASISResult<IHolon>(holonResult.Result);
            OASISResultHolonToHolonHelper<Holon, IHolon>.CopyResult(holonResult, result);
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                var transactionSignature = holon.Id == Guid.Empty ?
                    _solanaRepository.Create((Holon)holon) : _solanaRepository.Update((Holon)holon);
                holon.ProviderKey = new Dictionary<ProviderType, string>()
                    {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
                response.Result = holon;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                var transactionSignature = holon.Id == Guid.Empty ? 
                    await _solanaRepository.CreateAsync((Holon)holon) : await _solanaRepository.UpdateAsync((Holon)holon);
                holon.ProviderKey = new Dictionary<ProviderType, string>() {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
                response.Result = holon;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var transactionSignature = holon.IsNewHolon ? 
                        _solanaRepository.Create((Holon)holon) : _solanaRepository.Update((Holon)holon);

                    holon.ProviderKey = new Dictionary<ProviderType, string>() { { Core.Enums.ProviderType.SolanaOASIS, transactionSignature } };
                }
                response.Result = holons;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.IsError = true;
                response.Message = e.Message;
                response.IsSaved = false;
            }

            return response;
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

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id)
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

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username)
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

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true, int version = 0)
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
    }
}
