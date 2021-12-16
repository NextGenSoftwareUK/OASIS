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
    public class SolanaOASIS : OASISStorageBase, IOASISStorage, IOASISNET
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
            var transactionResult = avatar.Id == Guid.Empty ?
                _solanaRepository.Create((Avatar)avatar) : _solanaRepository.Update((Avatar)avatar);

            avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionResult}};

            return avatar;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var transactionSignature = avatar.Id == Guid.Empty ? 
                await _solanaRepository.CreateAsync((Core.Holons.Avatar)avatar) : await _solanaRepository.UpdateAsync((Core.Holons.Avatar)avatar);
            avatar.ProviderKey = new Dictionary<ProviderType, string>() {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
            return avatar;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            var transactionSignature = avatar.Id == Guid.Empty ? 
                _solanaRepository.Create((AvatarDetail) avatar) : _solanaRepository.Update((AvatarDetail)avatar);
            avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
            return avatar;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var transactionResult = avatar.Id == Guid.Empty ? 
                await _solanaRepository.CreateAsync((AvatarDetail) avatar) : await _solanaRepository.UpdateAsync((AvatarDetail)avatar);
            avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionResult}};
            return avatar;
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

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
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
            return _solanaRepository.Delete<Avatar>(providerKey);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Avatar>(providerKey);
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Holon>(providerKey);
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
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

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Holon>(providerKey);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey)
        {
            return _solanaRepository.Get<Holon>(providerKey);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey)
        {
            return await _solanaRepository.GetAsync<Holon>(providerKey);
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var transactionSignature = holon.Id == Guid.Empty ? 
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            return await _solanaRepository.GetAsync<Avatar>(providerKey);
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey)
        {
            return _solanaRepository.Get<Avatar>(providerKey);
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails()
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }
    }
}
