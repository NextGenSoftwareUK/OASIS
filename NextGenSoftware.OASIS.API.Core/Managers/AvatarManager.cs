using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class AvatarManager : OASISManager
    {
        private AvatarManagerConfig _config;
        private static AvatarManager _instance;

        public static AvatarManager Instance
        {
            get
            {
                  if (_instance == null)
                   _instance = new AvatarManager();

                return _instance;
            }
        }

        
        public List<IOASISStorage> OASISStorageProviders { get; set; }
        
        public Task<IAvatar> LoadAvatarAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public AvatarManagerConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new AvatarManagerConfig();
                }

                return _config;
            }
        }

        //Events
        //public delegate void AvatarManagerError(object sender, AvatarManagerErrorEventArgs e);
        //public event AvatarManagerError OnAvatarManagerError;

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        /*
        public AvatarManager(List<IOASISStorage> OASISStorageProviders)
        {
            this.OASISStorageProviders = OASISStorageProviders;

            foreach (IOASISStorage provider in OASISStorageProviders)
            {
                provider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
                provider.ActivateProvider();
            }
        }*/

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public AvatarManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {
            //if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
            //    ProviderManager.RegisterProvider(OASISStorageProvider);

            //ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider.ProviderType);
        }

        public AvatarManager() : base(null)
        {
            //if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
            //    ProviderManager.RegisterProvider(OASISStorageProvider);

            //ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider.ProviderType);
        }

        //private void OASISStorageProvider_OnStorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        //{
        //    //TODO: Not sure if we need to have a OnAvatarManagerError as well as the StorageProvider Error Event?
        //    OnAvatarManagerError?.Invoke(this, e);
        //}

        public async Task<IAvatar> Authenticate(string username, string password)
        {
            IEnumerable<IAvatar> _avatars = await LoadAllAvatarsAsync();

            var avatar = await Task.Run(() => _avatars.SingleOrDefault(x => x.Username == username && x.Password == password));

            if (avatar == null)
                return null;

            avatar.Password = null;
            return avatar;
        }

        public async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync(ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderManager.CurrentStorageProviderType)
                return await ((IOASISStorage)ProviderManager.SwitchCurrentStorageProvider(provider)).LoadAllAvatarsAsync();

           // ProviderManager.SwitchCurrentStorageProvider(providerType);

            return await ProviderManager.CurrentStorageProvider.LoadAllAvatarsAsync();
        }

        public async Task<IAvatar> LoadAvatarAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderManager.CurrentStorageProviderType)
                return await ((IOASISStorage)ProviderManager.SwitchCurrentStorageProvider(provider)).LoadAvatarAsync(providerKey);

            return await ProviderManager.CurrentStorageProvider.LoadAvatarAsync(providerKey);
        }

        public async Task<IAvatar> LoadAvatarAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            return await GetOASISStorageProvider(providerType).LoadAvatarAsync(id);
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return await GetOASISStorageProvider(providerType).LoadAvatarAsync(username, password);
        }

        public IAvatar LoadAvatar(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return  GetOASISStorageProvider(providerType).LoadAvatar(username, password);
        }

        public async Task<IAvatar> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            return await GetOASISStorageProvider(providerType).SaveAvatarAsync(avatar);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            //TODO: Check this with above changes to do with checking provider is registered before trying to use, etc...
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.SwitchCurrentStorageProvider(provider)).AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);

            return await ProviderManager.CurrentStorageProvider.AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            //TODO: Check this with above changes to do with checking provider is registered before trying to use, etc...
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);

            return await ProviderManager.CurrentStorageProvider.SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
