//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;

//namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
//{
//    public class ElrondOASISOASIS : OASISStorageBase, IOASISStorage, IOASISNET
//    {
//        public ElrondOASISOASIS()
//        {
//            this.ProviderName = "ElrondOASIS";
//            this.ProviderDescription = "Elrond Provider";
//            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.ElrondOASIS);
//            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
//        }

//        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteHolon(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteHolon(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<IPlayer> GetPlayersNearMe()
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatar> LoadAllAvatars()
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatarByEmail(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatarByUsername(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetail(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatar(Guid Id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatar(string username, string password)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatar(string username)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetail(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatarForProviderKey(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override IHolon LoadHolon(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IHolon LoadHolon(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IHolon> LoadHolonAsync(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IHolon> LoadHolonAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.Holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar SaveAvatar(IAvatar Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//<<<<<<< Updated upstream
//        public override Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//=======
//>>>>>>> Stashed changes
//        public override IHolon SaveHolon(IHolon holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IHolon> SaveHolonAsync(IHolon holon)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
