
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class AvatarManager : OASISManager
    {
        public static IAvatar LoggedInAvatar { get; set; }
        private ProviderManagerConfig _config;
        
        public List<IOASISStorage> OASISStorageProviders { get; set; }
        
        public ProviderManagerConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new ProviderManagerConfig();

                return _config;
            }
        }

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public AvatarManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }

        public IEnumerable<IAvatar> LoadAllAvatarsWithPasswords(ProviderType provider = ProviderType.Default)
        {
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAllAvatars();
            return avatars;
        }

        public IEnumerable<IAvatar> LoadAllAvatars(ProviderType provider = ProviderType.Default)
        {
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAllAvatars();

            foreach (IAvatar avatar in avatars)
                avatar.Password = null;

            return avatars;
        }

        public async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync(ProviderType provider = ProviderType.Default)
        {
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAllAvatarsAsync().Result;

            foreach (IAvatar avatar in avatars)
                avatar.Password = null;

            return avatars;
        }

        public async Task<IAvatar> LoadAvatarAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatarAsync(providerKey).Result;
            // avatar.Password = null;
            return avatar;
        }

        public async Task<IAvatar> LoadAvatarAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatarAsync(id).Result;
            // avatar.Password = null;
            return avatar;
        }

        public IAvatar LoadAvatar(Guid id, ProviderType providerType = ProviderType.Default)
        {
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatar(id);
            //avatar.Password = null;
            return avatar;
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatarAsync(username, password).Result;
        }

        public IAvatar LoadAvatar(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatar(username, password);
        }

        public IAvatar LoadAvatar(string username, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatar(username);
        }

        public async Task<IAvatar> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            avatar = await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).SaveAvatarAsync(PrepareAvatarForSaving(avatar));

            bool needToChangeBack = false;
            foreach (ProviderType type in ProviderManager.ProvidersThatAreAutoReplicating)
            {
                if (type != providerType)
                {
                    await ProviderManager.SetAndActivateCurrentStorageProvider(type).SaveAvatarAsync(avatar);
                    needToChangeBack = true;
                }
            }

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

            return avatar;
        }

        public IAvatar SaveAvatar(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).SaveAvatar(PrepareAvatarForSaving(avatar));

            bool needToChangeBack = false;
            foreach (ProviderType type in ProviderManager.ProvidersThatAreAutoReplicating)
            {
                if (type != providerType)
                {
                    ProviderManager.SetAndActivateCurrentStorageProvider(type).SaveAvatar(avatar);
                    needToChangeBack = true;
                }
            }

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

            return avatar;
        }


        //TODO: Need to refactor methods below to match the new above ones.
        public bool DeleteAvatar(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).DeleteAvatar(id, softDelete);
        }

        public async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).DeleteAvatarAsync(id, softDelete);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatar(avatarId);
            return await ProviderManager.CurrentStorageProvider.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public ManagerResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return new ManagerResult<KarmaAkashicRecord>(ProviderManager.SetAndActivateCurrentStorageProvider(provider).AddKarmaToAvatar(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }
        public ManagerResult<KarmaAkashicRecord> AddKarmaToAvatar(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            ManagerResult<KarmaAkashicRecord> result = new ManagerResult<KarmaAkashicRecord>();
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatar(avatarId);
            
            if (avatar != null)
                result.Result = ProviderManager.CurrentStorageProvider.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
            else
            {
                result.IsError = true;
                result.ErrorMessage = "Avatar Not Found";
            }

            return result;
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).RemoveKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatar(avatarId);
            return await ProviderManager.CurrentStorageProvider.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).RemoveKarmaFromAvatar(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public ManagerResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            ManagerResult<KarmaAkashicRecord> result = new ManagerResult<KarmaAkashicRecord>();
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatar(avatarId);

            if (avatar != null)
                result.Result = ProviderManager.CurrentStorageProvider.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
            else
            {
                result.IsError = true;
                result.ErrorMessage = "Avatar Not Found";
            }

            return result;
        }

        private IAvatar PrepareAvatarForSaving(IAvatar avatar)
        {
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (avatar.Id != Guid.Empty)
            {
                avatar.ModifiedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = LoggedInAvatar.Id;
            }
            else
            {
                avatar.IsActive = true;
                avatar.CreatedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = LoggedInAvatar.Id;
            }

            return avatar;
        }
    }
}
