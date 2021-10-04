using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class SQLLiteDBOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private string connectionString;
        private DataContext appDataContext;

        private AvatarRepository avatarRepository;
        private HolonRepository holonRepository;


        public SQLLiteDBOASIS(string connectionString)
        {
            //appDataContext = new DataContext(connectionString);
            appDataContext=new DataContext();

            avatarRepository = new AvatarRepository(appDataContext);
            holonRepository = new HolonRepository(appDataContext);

            this.connectionString=connectionString;

            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDB Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType holonType)
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

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(providerKey);
            return(avatar);
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid id)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(id);
            return(avatar);
        }

        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
        {
            Avatar avatar = await avatarRepository.GetAvatarByEmailAsync(avatarEmail);
            return(avatar);
        }

        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            Avatar avatar = await avatarRepository.GetAvatarAsync(avatarUsername);
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

                //appDataContext=new DataContext(connectionString);
                appDataContext=new DataContext();
                avatarRepository=new AvatarRepository(appDataContext);
                holonRepository=new HolonRepository(appDataContext);

            }
            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            appDataContext.Database.CloseConnection();
            appDataContext.Dispose();

            appDataContext = null;
            avatarRepository = null;
            holonRepository = null;

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
            return avatarRepository.Delete(id,softDelete);
        }

        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return avatarRepository.DeleteByEmail(avatarEmail,softDelete);
        }

        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return avatarRepository.Delete(avatarUsername,softDelete);
        }

        public override async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await avatarRepository.DeleteAsync(id, softDelete);
        }

        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await avatarRepository.DeleteAsync(avatarEmail, softDelete);
        }

        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await avatarRepository.DeleteAsync(avatarUsername, softDelete);
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
            return holonRepository.Delete(id, softDelete);
        }

        public override async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            return await holonRepository.DeleteAsync(id, softDelete);
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            return holonRepository.Delete(providerKey, softDelete);
        }

        public override async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return await holonRepository.DeleteAsync(providerKey, softDelete);
        }

        public override IHolon LoadHolon(Guid id)
        {
            return holonRepository.GetHolon(id);
        }

        public override async Task<IHolon> LoadHolonAsync(Guid id)
        {
            Holon holon = await holonRepository.GetHolonAsync(id);
            return(holon);
        }

        public override IHolon LoadHolon(string providerKey)
        {
            return holonRepository.GetHolon(providerKey);
        }

        public override async Task<IHolon> LoadHolonAsync(string providerKey)
        {
            Holon holon = await holonRepository.GetHolonAsync(providerKey);
            return(holon);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            IEnumerable<Holon> holonsList=holonRepository.GetAllHolonsForParent(id, type);
            return(holonsList);
        }

        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            IEnumerable<Holon> holonsList= await holonRepository.GetAllHolonsForParentAsync(id, type);
            return(holonsList);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            IEnumerable<Holon> holonsList=holonRepository.GetAllHolonsForParent(providerKey, type);
            return(holonsList);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = await holonRepository.GetAllHolonsForParentAsync(providerKey, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = repoResult.Result;

            return result;
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
        {
            IEnumerable<Holon> holonsList=holonRepository.GetAllHolons(type);
            return(holonsList);
        }

        public override async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            IEnumerable<Holon> holonsList= await holonRepository.GetAllHolonsAsync(type);
            return(holonsList);
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
        {
            OASISResult<IHolon> result = holon.IsNewHolon ? holonRepository.Add(holon)
                : holonRepository.Update(holon);

            if (!result.IsError && result.Result != null && saveChildrenRecursive)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result;
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
        {
            OASISResult<IHolon> result = holon.IsNewHolon ? holonRepository.Add(holon)
                : holonRepository.Update(holon);

            if (!result.IsError && result.Result != null && saveChildrenRecursive)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = await SaveHolonsAsync(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result;
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = SaveHolon(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildrenRecursive)
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(holonResult.Result.Children);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result;
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = await SaveHolonAsync(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildrenRecursive)
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = await SaveHolonsAsync(holonResult.Result.Children);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result;
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            return avatarRepository.GetAvatarDetail(id);
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            return avatarRepository.GetAvatarDetail(avatarUsername);
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            AvatarDetail avatar = await avatarRepository.GetAvatarDetailAsync(id);
            return(avatar);
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            AvatarDetail avatar = await avatarRepository.GetAvatarDetailAsync(avatarUsername);
            return(avatar);
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            AvatarDetail avatar = await avatarRepository.GetAvatarDetailByEmailAsync(avatarEmail);
            return(avatar);
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            return avatarRepository.GetAvatarDetails();
        }

        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            IEnumerable<AvatarDetail> details = await avatarRepository.GetAvatarDetailsAsync();
            return details;
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail avatar)
        {
            if(avatar.Id == Guid.Empty){
                return avatarRepository.Add((AvatarDetail)avatar);
            }
            else{
                return avatarRepository.Update((AvatarDetail)avatar);
            }
        }

        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            if(avatar.Id == Guid.Empty){
                return await avatarRepository.AddAsync((AvatarDetail)avatar);
            }
            else{
                return await avatarRepository.UpdateAsync((AvatarDetail)avatar);
            }
        }
    }
}
