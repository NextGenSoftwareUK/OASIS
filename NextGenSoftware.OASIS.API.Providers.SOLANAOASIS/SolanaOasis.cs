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
    public class SolanaOasis : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private readonly ISolanaRepository _solanaRepository;
        public SolanaOasis()
        {
            this.ProviderName = nameof(SolanaOasis);
            this.ProviderDescription = "Solana Blockchain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _solanaRepository = new SolanaRepository();
        }
        
        public override async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            var transactionResult = Avatar.Id == Guid.Empty ?
                _solanaRepository.Create((Core.Holons.Avatar)Avatar) : _solanaRepository.Update((Avatar)Avatar);
            Avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionResult}};
            return Avatar;
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            var transactionSignature = Avatar.Id == Guid.Empty ? 
                await _solanaRepository.CreateAsync((Core.Holons.Avatar)Avatar) : await _solanaRepository.UpdateAsync((Core.Holons.Avatar)Avatar);
            Avatar.ProviderKey = new Dictionary<ProviderType, string>() {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
            return Avatar;
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
        {
            var transactionSignature = Avatar.Id == Guid.Empty ? 
                _solanaRepository.Create((AvatarDetail) Avatar) : _solanaRepository.Update((AvatarDetail)Avatar);
            Avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionSignature}};
            return Avatar;
        }

        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var transactionResult = Avatar.Id == Guid.Empty ? 
                await _solanaRepository.CreateAsync((AvatarDetail) Avatar) : await _solanaRepository.UpdateAsync((AvatarDetail)Avatar);
            Avatar.ProviderKey = new Dictionary<ProviderType, string>()
                {{Core.Enums.ProviderType.SolanaOASIS, transactionResult}};
            return Avatar;
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Avatar>(providerKey) != string.Empty;
        }

        public override async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Avatar>(providerKey) != string.Empty;
        }

        public override async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return await _solanaRepository.DeleteAsync<Holon>(providerKey) != string.Empty;
        }

        public override async Task<ISearchResults> SearchAsync(ISearchParams searchParams)
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

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _solanaRepository.Delete<Holon>(providerKey) != string.Empty;
        }

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            return _solanaRepository.Get<Holon>(providerKey);
        }

        public override async Task<IHolon> LoadHolonAsync(string providerKey)
        {
            return await _solanaRepository.GetAsync<Holon>(providerKey);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
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

        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarAsync(string username)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            return await _solanaRepository.GetAsync<Avatar>(providerKey);
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            return _solanaRepository.Get<Avatar>(providerKey);
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }
    }
}
