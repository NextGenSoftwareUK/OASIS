using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class AvatarManager : OASISManager
    {
        public static IAvatar LoggedInAvatar { get; set; }

        private ProviderManagerConfig _config;
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
        
        public ProviderManagerConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new ProviderManagerConfig();
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

        //public async Task<IAvatar> Authenticate(string username, string password)
        //{



        //    IEnumerable<IAvatar> _avatars = await LoadAllAvatarsAsync();

        //    var avatar = await Task.Run(() => _avatars.SingleOrDefault(x => x.Username == username && x.Password == password));

        //    if (avatar == null)
        //        return null;

        //    avatar.Password = null;
        //    return avatar;
        //}

        public IEnumerable<IAvatar> LoadAllAvatarsWithPasswords(ProviderType provider = ProviderType.Default)
        {
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadAllAvatars();

           // foreach (IAvatar avatar in avatars)
           //     avatar.Password = null;

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
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).LoadAvatarAsync(username, password).Result;
            //avatar.Password = null;
            return avatar;
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
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (avatar.Id == Guid.Empty)
            {
                avatar.ModifiedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = LoggedInAvatar.Id;
            }
            else
            {
                avatar.CreatedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = LoggedInAvatar.Id;
            }

            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).SaveAvatarAsync(avatar);
        }

        public IAvatar SaveAvatar(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (avatar.Id == Guid.Empty)
            {
                avatar.ModifiedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = LoggedInAvatar.Id;
            }
            else
            {
                avatar.CreatedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = LoggedInAvatar.Id;
            }

            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).SaveAvatar(avatar);
        }

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

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
