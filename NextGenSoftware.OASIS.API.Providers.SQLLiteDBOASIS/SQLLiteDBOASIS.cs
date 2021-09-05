using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class SQLLiteDBOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private string connectionString;
        private DataContext appDataContext;

        private AvatarRepository avatarRepository;


        public SQLLiteDBOASIS(string connectionString)
        {
            appDataContext = new DataContext(connectionString);
            avatarRepository = new AvatarRepository(appDataContext);
            this.connectionString=connectionString;

            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDB Provider";
            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddKarmaToAvatarAsync(IAvatar Avatar, int karma)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            List<Avatar> avatars= await avatarRepository.GetAvatarsAsync();
            return(avatars);
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            return avatarRepository.GetAvatars();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid id)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(id);
            return(avatar);
        }

        public override IAvatar LoadAvatar(Guid id)
        {
            Avatar avatar = avatarRepository.GetAvatar(id);
            return(avatar);
        }

        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(username, password);
            return(avatar);
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            Avatar avatar = avatarRepository.GetAvatar(username, password);
            return(avatar);
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            Avatar converted=(Avatar)avatar;
            if(avatar.Id == Guid.Empty){
                return await avatarRepository.AddAsync(converted);
            }
            else{
                return await avatarRepository.UpdateAsync(converted);
            }
        }

        //TODO: Move this into Search Reposirary like Avatar is...
        public override async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            throw new NotImplementedException();
        }

        public override void ActivateProvider()
        {
            if (appDataContext == null){

                appDataContext=new DataContext(connectionString);
                avatarRepository=new AvatarRepository(appDataContext);

            }
            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            appDataContext.Database.CloseConnection();
            appDataContext.Dispose();

            appDataContext = null;
            avatarRepository = null;

            base.DeActivateProvider();
        }

        public override IAvatar SaveAvatar(IAvatar avatar)
        {
            Avatar converted=(Avatar)avatar;
            if(avatar.Id == Guid.Empty){
                return avatarRepository.Add(converted);
            }
            else{
                return avatarRepository.Update(converted);
            }
        }

        public override IAvatar LoadAvatar(string username)
        {
            Avatar avatar = avatarRepository.GetAvatar(username);
            return(avatar);
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            return avatarRepository.Delete(id, softDelete);
        }

        public override async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await avatarRepository.DeleteAsync(id, softDelete);
        }

    

        public override async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(providerKey);
            return(avatar);
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            Avatar avatar = avatarRepository.GetAvatar(providerKey);
            return(avatar);
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return avatarRepository.Delete(providerKey, softDelete);
        }

        public override async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await avatarRepository.DeleteAsync(providerKey, softDelete);
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IHolon SaveHolon(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }
    }
}
