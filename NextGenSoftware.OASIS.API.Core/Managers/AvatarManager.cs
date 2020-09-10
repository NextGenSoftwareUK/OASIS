using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class AvatarManager : OASISManager
    {
        private AvatarManagerConfig _config;
        //private static AvatarManager _instance;

        //public static AvatarManager Instance
        //{
        //    get
        //    {
        //          if (_instance == null)
        //           _instance = new AvatarManager();

        //        return _instance;
        //    }
        //}

        
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

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);


        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public AvatarManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }

        // TODO: Not sure good idea? All because want to cache AvatarManager in WebAPI.Program, also is it a good idea?
        // Dont think its needed? Because not expensive to create a new AvatarManager, the expensive bit is already cached in the ProviderManager (in OASIS.API.Core) & OASISConteollerBase (in WebAPI)
        public AvatarManager() : base(null)
        {

        }

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
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAllAvatarsAsync();
        }

        public async Task<IAvatar> LoadAvatarAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAvatarAsync(providerKey);
        }

        public async Task<IAvatar> LoadAvatarAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatarAsync(id);
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatarAsync(username, password);
        }

        public IAvatar LoadAvatar(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatar(username, password);
        }

        public async Task<IAvatar> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).SaveAvatarAsync(avatar);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
