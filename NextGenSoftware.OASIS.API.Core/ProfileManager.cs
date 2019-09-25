using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core
{
    public class ProfileManager
    {
        private ProfileManagerConfig _config;

        public List<IOASISStorage> OASISStorageProviders { get; set; }
        public IOASISStorage DefaultProvider { get; set; }

        public ProfileManagerConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new ProfileManagerConfig();
                }

                return _config;
            }
        }

        //Events
        public delegate void ProfileManagerError(object sender, ProfileManagerErrorEventArgs e);
        public event ProfileManagerError OnProfileManagerError;

        public delegate void StorageProviderError(object sender, ProfileManagerErrorEventArgs e);

        public ProfileManager(List<IOASISStorage> OASISStorageProviders)
        {
            this.OASISStorageProviders = OASISStorageProviders;

            foreach (IOASISStorage provider in OASISStorageProviders)
            {
                provider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
                provider.ActivateProvider();
            }

            DefaultProvider = OASISStorageProviders.FirstOrDefault(x => x.Type == ProviderType.HoloOASIS);
        }

        private void OASISStorageProvider_OnStorageProviderError(object sender, ProfileManagerErrorEventArgs e)
        {
            //TODO: Not sure if we need to have a OnProfileManagerError as well as the StorageProvider Error Event?
            OnProfileManagerError?.Invoke(this, e);
        }

        public async Task<IProfile> LoadProfileAsync(string providerKey)
        {
            return await DefaultProvider.LoadProfileAsync(providerKey);
        }

        public async Task<IProfile> LoadProfileAsync(Guid id)
        {
            return await DefaultProvider.LoadProfileAsync(id);
        }

        public async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            return await DefaultProvider.LoadProfileAsync(username, password);
        }

        public async Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            return await DefaultProvider.SaveProfileAsync(profile);
        }

        public async Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await DefaultProvider.AddKarmaToProfileAsync(profile, karma);
        }

        public async Task<bool> RemoveKarmaFromProfileAsync(IProfile profile, int karma)
        {
            return await DefaultProvider.RemoveKarmaFromProfileAsync(profile, karma);
        }
    }
}
